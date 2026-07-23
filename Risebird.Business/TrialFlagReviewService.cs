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
            _applicationErrorLogger = applicationErrorLogger;
            // Intentionally unused: DI signature kept; live factor enrich is Detail-tier.
#pragma warning disable IDE0060
            _ = trialJudgmentRepository;
#pragma warning restore IDE0060
        }

        public async Task<TrialFlagReviewPageVm> GetPageShellAsync(string tab)
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

            var loadedRows = pendingCount + keptCount + removedCount;

            return new TrialFlagReviewPageVm
            {
                ActiveTab = normalizedTab,
                Rows = Array.Empty<TrialFlagReviewRowVm>(),
                PendingCount = pendingCount,
                KeptCount = keptCount,
                RemovedCount = removedCount,
                TrialFlagsTableExists = tableExists,
                TotalRowsInTable = totalRows,
                LiveMetrics = false,
                EmptyStateHint = BuildEmptyStateHint(tableExists, totalRows, loadedRows, 0, normalizedTab, pendingCount, keptCount, removedCount)
            };
        }

        /// <summary>Legacy alias — shell only (list is AJAX).</summary>
        public Task<TrialFlagReviewPageVm> GetPageAsync(string tab, bool refreshMetrics = false) =>
            GetPageShellAsync(tab);

        public async Task<TrialFlagReviewListResultVm> GetListAsync(TrialFlagReviewListRequest request)
        {
            request ??= new TrialFlagReviewListRequest();
            var tab = NormalizeTab(request.Tab);
            var offset = Math.Max(0, request.Offset);
            var pageSize = request.PageSize <= 0 ? 25 : Math.Min(request.PageSize, 200);

            var counts = await _repository.GetStatusCountsAsync();
            var (rows, total) = await _repository.GetTrialFlagsListAsync(
                tab, offset, pageSize, request.Search, request.Severity);

            return new TrialFlagReviewListResultVm
            {
                Draw = request.Draw,
                RecordsTotal = total,
                RecordsFiltered = total,
                Data = rows,
                PendingCount = counts.Pending,
                KeptCount = counts.Kept,
                RemovedCount = counts.Removed
            };
        }

        public Task<TrialFlagReviewRowVm> GetDetailAsync(int trialFlagId) =>
            _repository.GetTrialFlagDetailAsync(trialFlagId);

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
                return "dbo.TrialFlags does not exist on this database. Run Risebird.DB/Tables/TrialFlags.sql, then deploy List/Detail procs.";

            if (totalRows == 0)
                return "dbo.TrialFlags exists but has 0 rows. Rows are inserted only when trial judgment runs after QC AI merge on a first completed interview. If merges already finished before the producer was deployed, run POST /Dashboard/BackfillTrialFlags.";

            if (loadedRows == 0 && totalRows > 0)
                return "TrialFlags has rows but tab counts are empty. Check ReviewStatus values (pending/kept/removed).";

            if (filteredRows == 0)
            {
                if (activeTab == TrialFlagConstants.ReviewPending && pendingCount == 0 && (keptCount > 0 || removedCount > 0))
                    return $"No pending flags. {keptCount} kept and {removedCount} removed — switch tabs above.";
                if (activeTab == TrialFlagConstants.ReviewKept && keptCount == 0)
                    return $"No kept flags. Pending: {pendingCount}, Removed: {removedCount}.";
                if (activeTab == TrialFlagConstants.ReviewRemoved && removedCount == 0)
                    return $"No removed flags. Pending: {pendingCount}, Kept: {keptCount}.";
            }

            return null;
        }

        public async Task RefreshLiveMetricsForInterviewIdsAsync(IReadOnlyList<int> interviewIds)
        {
            if (interviewIds == null || interviewIds.Count == 0)
                return;

            var ids = interviewIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0)
                return;

            // Batched: dry-run all → mass-fire guard → sequential writes (no long ambient transaction).
            var dryRuns = new RiskCategory[ids.Count];
            using (var gate = new SemaphoreSlim(LiveMetricsConcurrency))
            {
                var evalTasks = ids.Select(async (interviewId, index) =>
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
                return;
            }

            using (var gate = new SemaphoreSlim(LiveMetricsConcurrency))
            {
                var writeTasks = ids.Select(async interviewId =>
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

            await _repository.SyncTrialFlagsVerdictFromRiskCategoryAsync();
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
