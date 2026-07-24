using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    public class DmIvAutoQcBackfillService : IDmIvAutoQcBackfillService
    {
        private readonly IAutoQcRemovalMonitorRepository _monitorRepository;
        private readonly IDmIvAutoQcFlagService _autoQcFlagService;
        private readonly IRiskCategoryService _riskCategoryService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger<DmIvAutoQcBackfillService> _log;

        public DmIvAutoQcBackfillService(
            IAutoQcRemovalMonitorRepository monitorRepository,
            IDmIvAutoQcFlagService autoQcFlagService,
            IRiskCategoryService riskCategoryService,
            IApplicationErrorLogger applicationErrorLogger,
            ILogger<DmIvAutoQcBackfillService> log)
        {
            _monitorRepository = monitorRepository;
            _autoQcFlagService = autoQcFlagService;
            _riskCategoryService = riskCategoryService;
            _applicationErrorLogger = applicationErrorLogger;
            _log = log;
        }

        public Task<AutoQcPerIrfBackfillPreviewVm> GetPerIrfBackfillPreviewAsync(int irfId, DateTime fromUtc, DateTime toUtc) =>
            _monitorRepository.GetPerIrfBackfillPreviewAsync(irfId, fromUtc, toUtc);

        public Task<IReadOnlyList<IrfAuditDirectoryRowVm>> GetIrfAuditDirectoryAsync(DateTime fromIst, DateTime toIst) =>
            _monitorRepository.GetIrfAuditDirectoryAsync(fromIst, toIst);

        public Task<IReadOnlyList<IrfAuditInterviewerRiskRowVm>> GetIrfAuditInterviewerRiskBreakdownAsync(int irfId, DateTime fromIst, DateTime toIst) =>
            _monitorRepository.GetIrfAuditInterviewerRiskBreakdownAsync(irfId, fromIst, toIst);

        public Task<IReadOnlyList<IrfAuditInterviewerRiskDetailVm>> GetIrfAuditInterviewerRiskInterviewDetailsAsync(int irfId, DateTime fromIst, DateTime toIst, int? transInterviewerId = null) =>
            _monitorRepository.GetIrfAuditInterviewerRiskInterviewDetailsAsync(irfId, fromIst, toIst, transInterviewerId);

        public async Task<IrfAuditRiskBackfillResultVm> BackfillRiskCategoryForIrfAsync(int irfId, DateTime fromIst, DateTime toIst,
            bool ignoreMassFireGuard = false,
            CancellationToken cancellationToken = default)
        {
            var result = new IrfAuditRiskBackfillResultVm();
            if (irfId <= 0)
            {
                result.Ok = false;
                result.Message = "Invalid IRF.";
                return result;
            }

            var batchSize = DmIvAutoQcAuditConstants.RiskCategoryBackfillBatchSize;
            var maxBatches = DmIvAutoQcAuditConstants.RiskCategoryBackfillMaxBatches;
            var maxInterviews = batchSize * maxBatches;

            // Reader/writer isolation (trial-read-architecture Part 2):
            // Never wrap the full IRF/90-day window in one SqlTransaction — each interview
            // evaluate/write commits independently so Trial Flag Review List readers are not blocked.
            // Single candidate fetch for the whole run so the mass-fire guard can evaluate
            // before any write (prefer-missing ordering from SP).
            cancellationToken.ThrowIfCancellationRequested();
            var candidates = (await _monitorRepository.GetRiskCategoryBackfillCandidatesByIrfAsync(
                irfId, fromIst, toIst, maxInterviews + 1)).ToList();

            if (candidates.Count == 0)
            {
                result.Ok = true;
                result.Processed = 0;
                result.Remaining = 0;
                result.Message = "No interviews in window to refresh.";
                return result;
            }

            var interviewIds = candidates.Take(maxInterviews).ToList();
            var remainingHint = Math.Max(0, candidates.Count - interviewIds.Count);

            // Dry-run evaluate all — mass-fire guard runs before any write.
            var dryRuns = new List<RiskCategory>(interviewIds.Count);
            var dryRunOkCount = 0;
            foreach (var interviewId in interviewIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var eval = await _riskCategoryService.EvaluateRiskCategoryDryRun(interviewId);
                if (eval != null)
                    dryRunOkCount++;
                dryRuns.Add(eval ?? new RiskCategory { InterviewId = interviewId });
            }

            if (!ignoreMassFireGuard)
            {
                // If dry-run SP (@DryRun) is not deployed yet, skip the guard rather than blocking all writes.
                if (dryRunOkCount == 0 && interviewIds.Count > 0)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                        "RiskCategory backfill: dry-run returned no results (deploy usp_AddUpdateRiskCategory with @DryRun). Proceeding without mass-fire guard.");
                }
                else
                {
                    var guard = RiskCategoryMassFireGuard.Evaluate(dryRuns);
                    if (guard.ShouldAbort)
                    {
                        var abortMsg = guard.Message
                            ?? $"aborted: {guard.CheckName} fired on {guard.FireRatePct:0}% of {guard.SampleSize} interviews — investigate";
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning, abortMsg);
                        _log.LogWarning(
                            "RiskCategory backfill ABORTED irf={IrfId}: check={Check} rate={Rate:0}% sample={Sample} (ignoreMassFireGuard=false)",
                            irfId, guard.CheckName, guard.FireRatePct, guard.SampleSize);

                        result.Ok = false;
                        result.Aborted = true;
                        result.AbortedCheckName = guard.CheckName;
                        result.AbortedFireRatePct = guard.FireRatePct;
                        result.Processed = 0;
                        result.Remaining = interviewIds.Count + remainingHint;
                        result.Message = abortMsg + " (pass ignoreMassFireGuard=1 to override after investigating)";
                        return result;
                    }
                }
            }

            var processed = 0;
            foreach (var interviewId in interviewIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ok = await _riskCategoryService.AddUpdateRiskCategory(interviewId);
                if (ok)
                    processed++;
            }

            result.Ok = true;
            result.Processed = processed;
            result.Remaining = remainingHint;

            var synced = await _monitorRepository.SyncTrialFlagsVerdictFromRiskCategoryAsync();
            result.Message = remainingHint > 0
                ? $"Refreshed risk for {processed} interview(s). Synced {synced} trial verdict(s). Click Refresh risk again for ~{remainingHint}+ more."
                : $"Refreshed risk for {processed} interview(s). Synced {synced} trial verdict(s). Expand the IRF again to see updated P0–P5 counts.";
            _log.LogInformation("RiskCategory backfill irf={IrfId}: processed={Processed} remainingHint={Remaining} trialSynced={Synced} ignoreMassFire={Ignore}",
                irfId, processed, remainingHint, synced, ignoreMassFireGuard);
            return result;
        }

        public async Task<AutoQcPerIrfBackfillResultVm> RunPerIrfBackfillAsync(int irfId, DateTime fromUtc, DateTime toUtc, string executedBy,
            CancellationToken cancellationToken = default)
        {
            var result = new AutoQcPerIrfBackfillResultVm();
            if (irfId <= 0)
            {
                result.Ok = false;
                result.Message = "Invalid IRF.";
                return result;
            }

            var preview = await _monitorRepository.GetPerIrfBackfillPreviewAsync(irfId, fromUtc, toUtc);
            var candidates = (await _monitorRepository.GetPerIrfBackfillCandidatesAsync(irfId, fromUtc, toUtc)).ToList();
            var runId = Guid.NewGuid();
            var newFlags = 0;
            var skipped = preview.AlreadyFlaggedCount;

            var grouped = candidates.GroupBy(x => x.TransInterviewerId).ToList();
            foreach (var group in grouped)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rows = group.OrderByDescending(x => x.VerdictFinalizedAt).ThenByDescending(x => x.InterviewId).ToList();
                var first = rows[0];
                var badge = ResolveBadgeVerdict(rows);
                var badgeInterview = ResolveBadgeInterviewId(rows, badge);
                var metadata = JsonConvert.SerializeObject(new
                {
                    triggeringInterviews = rows.Select(r => new { interviewId = r.InterviewId, verdict = r.Verdict }).ToList(),
                    qcDetailsBaseUrl = "/Interview/QCMergedAIDetails?interviewId="
                });

                var ctx = new TransInterviewerInterviewContext
                {
                    TransInterviewerId = first.TransInterviewerId,
                    UserId = first.UserId,
                    IrfId = first.IrfId,
                    IsActive = true,
                    InterviewerEmail = first.InterviewerEmail,
                    InterviewerCodedId = first.InterviewerCodedId,
                    AutoQcVerdict = first.CurrentAutoQcVerdict
                };

                var options = new AutoQcFlagOptions
                {
                    Source = DmIvAutoQcAuditConstants.SourcePerIrfBackfill,
                    Tag = DmIvAutoQcAuditConstants.FlagTag,
                    BackfillBatchId = runId,
                    ExtraMetadata = metadata,
                    BadgeVerdict = badge,
                    BadgeTriggeringInterviewId = badgeInterview
                };

                var anyNewAudit = false;
                foreach (var row in rows)
                {
                    var flagged = await _autoQcFlagService.ApplyAutoQcFlagForContextAsync(
                        ctx, row.InterviewId, row.Verdict, options, cancellationToken);
                    if (flagged)
                        anyNewAudit = true;
                }

                if (anyNewAudit)
                    newFlags += 1;
            }

            await _monitorRepository.RecordPerIrfBackfillRunAsync(
                runId, irfId, fromUtc, toUtc, preview.VerdictCount, newFlags, skipped, executedBy ?? "System");

            result.Ok = true;
            result.RunId = runId;
            result.VerdictsScanned = preview.VerdictCount;
            result.NewFlagsSet = newFlags;
            result.AlreadyFlaggedSkipped = skipped;
            result.Message =
                $"Backfill complete. {preview.VerdictCount} verdicts scanned. {newFlags} new flags set. {skipped} already flagged and skipped.";
            _log.LogInformation("Per-IRF AutoQc backfill run {RunId} irf={IrfId}: new={New} skipped={Skipped}", runId, irfId, newFlags, skipped);
            return result;
        }

        internal static string ResolveBadgeVerdict(IReadOnlyList<AutoQcBackfillCandidateRow> rows)
        {
            if (rows.Any(x => x.Verdict == DmIvAutoQcAuditConstants.VerdictSuspicious))
                return DmIvAutoQcAuditConstants.VerdictSuspicious;
            return DmIvAutoQcAuditConstants.VerdictRedo;
        }

        internal static int ResolveBadgeInterviewId(IReadOnlyList<AutoQcBackfillCandidateRow> rows, string badgeVerdict)
        {
            var match = rows
                .Where(x => x.Verdict == badgeVerdict)
                .OrderByDescending(x => x.VerdictFinalizedAt)
                .ThenByDescending(x => x.InterviewId)
                .FirstOrDefault();
            if (match != null)
                return match.InterviewId;
            return rows.OrderByDescending(x => x.VerdictFinalizedAt).ThenByDescending(x => x.InterviewId).First().InterviewId;
        }
    }
}
