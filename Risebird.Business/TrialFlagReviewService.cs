using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class TrialFlagReviewService : ITrialFlagReviewService
    {
        private const int LiveMetricsConcurrency = 4;

        private readonly ITrialFlagReviewRepository _repository;
        private readonly IMeetingService _meetingService;
        private readonly IRiskCategoryService _riskCategoryService;
        private readonly ITrialJudgmentRepository _trialJudgmentRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public TrialFlagReviewService(
            ITrialFlagReviewRepository repository,
            IMeetingService meetingService,
            IRiskCategoryService riskCategoryService,
            ITrialJudgmentRepository trialJudgmentRepository,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _repository = repository;
            _meetingService = meetingService;
            _riskCategoryService = riskCategoryService;
            _trialJudgmentRepository = trialJudgmentRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<TrialFlagReviewPageVm> GetPageAsync(string tab, bool refreshMetrics = false)
        {
            var normalizedTab = NormalizeTab(tab);
            var (tableExists, totalRows) = await _repository.GetDiagnosticsAsync();

            var pendingCount = 0;
            var keptCount = 0;
            var removedCount = 0;
            if (tableExists && totalRows > 0)
            {
                var counts = await _repository.GetStatusCountsAsync();
                pendingCount = counts.Pending;
                keptCount = counts.Kept;
                removedCount = counts.Removed;
            }

            var rows = (await _repository.GetTrialFlagsAsync(normalizedTab)).ToList();

            if (refreshMetrics && rows.Count > 0)
            {
                await RefreshRiskCategoriesForRowsAsync(rows);
                await _repository.SyncTrialFlagsVerdictFromRiskCategoryAsync();
                rows = (await _repository.GetTrialFlagsAsync(normalizedTab)).ToList();
                await EnrichRowsWithLiveFactorsParallelAsync(rows);
            }

            var loadedRows = pendingCount + keptCount + removedCount;

            return new TrialFlagReviewPageVm
            {
                ActiveTab = normalizedTab,
                Rows = rows,
                PendingCount = pendingCount,
                KeptCount = keptCount,
                RemovedCount = removedCount,
                TrialFlagsTableExists = tableExists,
                TotalRowsInTable = totalRows,
                LiveMetrics = refreshMetrics,
                EmptyStateHint = BuildEmptyStateHint(tableExists, totalRows, loadedRows, rows.Count, normalizedTab, pendingCount, keptCount, removedCount)
            };
        }

        private static bool ReviewStatusMatches(string actual, string expected) =>
            string.Equals(actual?.Trim(), expected, StringComparison.OrdinalIgnoreCase);

        private static string BuildEmptyStateHint(
            bool tableExists,
            int totalRows,
            int loadedRows,
            int filteredRows,
            string activeTab,
            int pendingCount,
            int keptCount,
            int removedCount)
        {
            if (!tableExists)
                return "dbo.TrialFlags does not exist on this database. Run Risebird.DB/Tables/TrialFlags.sql, then redeploy usp_GetTrialFlagsForReview.";

            if (totalRows == 0)
                return "dbo.TrialFlags exists but has 0 rows. Rows are inserted only when trial judgment runs after QC AI merge on a first completed interview. If merges already finished before the producer was deployed, run POST /Dashboard/BackfillTrialFlags (or redeploy Azure Functions so future merges insert automatically).";

            if (loadedRows == 0 && totalRows > 0)
                return "TrialFlags has rows but usp_GetTrialFlagsForReview returned none. Redeploy the latest stored procedure on this database.";

            if (filteredRows == 0)
            {
                if (activeTab == TrialFlagConstants.ReviewPending && pendingCount == 0 && (keptCount > 0 || removedCount > 0))
                    return $"No pending flags. {keptCount} kept and {removedCount} removed — switch tabs above.";
                if (activeTab == TrialFlagConstants.ReviewKept && keptCount == 0)
                    return $"No kept flags. Pending: {pendingCount}, Removed: {removedCount}.";
                if (activeTab == TrialFlagConstants.ReviewRemoved && removedCount == 0)
                    return $"No removed flags. Pending: {pendingCount}, Kept: {keptCount}.";
                return "Rows exist but none match this tab. Check ReviewStatus values in TrialFlags (expected: pending, kept, removed).";
            }

            return null;
        }

        private async Task RefreshRiskCategoriesForRowsAsync(IReadOnlyList<TrialFlagReviewRowVm> rows)
        {
            if (rows == null || rows.Count == 0)
                return;

            var interviewIds = rows
                .Select(r => r.TriggeringInterviewId)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (interviewIds.Count == 0)
                return;

            // Global refresh path: dry-run then mass-fire guard before any write.
            var dryRuns = new RiskCategory[interviewIds.Count];
            using (var gate = new SemaphoreSlim(LiveMetricsConcurrency))
            {
                var evalTasks = interviewIds.Select(async (interviewId, index) =>
                {
                    await gate.WaitAsync();
                    try
                    {
                        dryRuns[index] = await _riskCategoryService.EvaluateRiskCategoryDryRun(interviewId)
                            ?? new RiskCategory { InterviewId = interviewId };
                    }
                    finally
                    {
                        gate.Release();
                    }
                });
                await Task.WhenAll(evalTasks);
            }

            var guard = RiskCategoryMassFireGuard.Evaluate(dryRuns);
            if (guard.ShouldAbort)
            {
                var abortMsg = guard.Message
                    ?? $"aborted: {guard.CheckName} fired on {guard.FireRatePct:0}% of {guard.SampleSize} interviews — investigate";
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                    "TrialFlagReview risk refresh " + abortMsg + " — skipped writes");
                // Do not throw: keep the Trial Flag Review page usable; ops re-runs after the check fix.
                return;
            }

            using (var gate = new SemaphoreSlim(LiveMetricsConcurrency))
            {
                var writeTasks = interviewIds.Select(async interviewId =>
                {
                    await gate.WaitAsync();
                    try
                    {
                        await _riskCategoryService.AddUpdateRiskCategory(interviewId);
                    }
                    finally
                    {
                        gate.Release();
                    }
                });
                await Task.WhenAll(writeTasks);
            }
        }

        /// <summary>
        /// Re-evaluate trial factors from current interview + QC merge data (optional refresh path).
        /// </summary>
        private async Task EnrichRowsWithLiveFactorsParallelAsync(IReadOnlyList<TrialFlagReviewRowVm> rows)
        {
            if (rows == null || rows.Count == 0)
                return;

            using var gate = new SemaphoreSlim(LiveMetricsConcurrency);
            var tasks = rows.Select(async row =>
            {
                if (row.TriggeringInterviewId <= 0)
                    return;

                await gate.WaitAsync();
                try
                {
                    await EnrichRowWithLiveFactorsAsync(row);
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task EnrichRowWithLiveFactorsAsync(TrialFlagReviewRowVm row)
        {
            try
            {
                var ctx = await _trialJudgmentRepository.GetContextAsync(row.TriggeringInterviewId);
                if (ctx == null)
                    return;

                var stored = await _trialJudgmentRepository.GetStoredMergeResultAsync(row.TriggeringInterviewId);
                var judgment = TrialJudgmentService.EvaluateFactors(ctx, stored?.MergeResult);

                row.TranscriptQualityPass = judgment.TranscriptQualityPass;
                row.ReportQualityPass = judgment.ReportQualityPass;
                row.DecisionQualityPass = judgment.DecisionQualityPass;
                row.CandidateRatingPass = judgment.CandidateRatingPass;
                row.JoinTimePass = judgment.JoinTimePass;
                row.InterviewDurationPass = judgment.InterviewDurationPass;
                row.TranscriptQualityScore = ctx.TranscriptQualityScore;
                row.ReportQualityScore = ctx.ReportQualityScore;
                row.CandidateRating = ctx.CandidateRating;
                row.JoinTimeMinutes = ctx.JoinTimeMinutes;
                row.InterviewDurationMinutes = ctx.InterviewDurationMinutes;
            }
            catch
            {
                // Keep stored snapshot columns when live evaluation fails.
            }
        }

        public async Task<TrialFlagReviewActionResultVm> KeepOnIrfAsync(int trialFlagId, string reviewerEmail, string comment)
        {
            var row = await _repository.GetTrialFlagByIdAsync(trialFlagId);
            if (row == null)
                return Fail("Trial flag not found.");

            if (!string.Equals(row.ReviewStatus, TrialFlagConstants.ReviewPending, StringComparison.OrdinalIgnoreCase))
                return Fail("This trial flag was already reviewed.");

            var ok = await _repository.UpdateReviewAsync(trialFlagId, TrialFlagConstants.ReviewKept, reviewerEmail, comment);
            return ok
                ? Success("Interviewer kept on IRF.")
                : Fail("Could not save review decision. Refresh and try again.");
        }

        public async Task<TrialFlagReviewActionResultVm> RemoveFromIrfAsync(int trialFlagId, string reviewerEmail, string comment)
        {
            var row = await _repository.GetTrialFlagByIdAsync(trialFlagId);
            if (row == null)
                return Fail("Trial flag not found.");

            if (!string.Equals(row.ReviewStatus, TrialFlagConstants.ReviewPending, StringComparison.OrdinalIgnoreCase))
                return Fail("This trial flag was already reviewed.");

            var interviewerKey = !string.IsNullOrWhiteSpace(row.InterviewerEmail)
                ? row.InterviewerEmail.Trim()
                : row.InterviewerName?.Trim();
            if (string.IsNullOrWhiteSpace(interviewerKey))
                return Fail("Interviewer email is missing for this trial flag.");

            var removed = await _meetingService.RemoveOrShortlistInterviewerFromIRF(
                row.IrfId,
                interviewerKey,
                "Rejected");
            if (!removed)
                return Fail("Could not remove interviewer from IRF. Try again or use DM Approval.");

            var ok = await _repository.UpdateReviewAsync(trialFlagId, TrialFlagConstants.ReviewRemoved, reviewerEmail, comment);
            if (!ok)
                return Fail("Interviewer was removed from IRF, but the trial flag could not be marked reviewed.");

            return Success("Interviewer removed from IRF.");
        }

        public Task<IReadOnlyList<InterviewAiRiskSummaryRowVm>> GetInterviewAiRiskSummaryAsync(int interviewId) =>
            _repository.GetInterviewAiRiskSummaryAsync(interviewId);

        private static string NormalizeTab(string tab)
        {
            if (string.Equals(tab, TrialFlagConstants.ReviewKept, StringComparison.OrdinalIgnoreCase))
                return TrialFlagConstants.ReviewKept;
            if (string.Equals(tab, TrialFlagConstants.ReviewRemoved, StringComparison.OrdinalIgnoreCase))
                return TrialFlagConstants.ReviewRemoved;
            return TrialFlagConstants.ReviewPending;
        }

        private static TrialFlagReviewActionResultVm Success(string message) =>
            new TrialFlagReviewActionResultVm { Ok = true, Message = message };

        private static TrialFlagReviewActionResultVm Fail(string message) =>
            new TrialFlagReviewActionResultVm { Ok = false, Message = message };
    }
}

