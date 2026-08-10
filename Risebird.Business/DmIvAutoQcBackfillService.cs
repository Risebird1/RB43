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
        // Injected per Part A spec ("if ITrialJudgmentService is injectable, call it after risk writes").
        // BackfillMissingTrialFlagsAsync(int? interviewId = null) is NOT IRF-scoped — calling it here would
        // process trial-flag candidates across every IRF as a side effect of a single per-IRF risk refresh,
        // so we deliberately skip that call (TrialFlagsInserted stays 0) and rely on
        // SyncTrialFlagsVerdictFromRiskCategoryAsync (already IRF-window scoped) for verdict sync.
        private readonly ITrialJudgmentService _trialJudgmentService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger<DmIvAutoQcBackfillService> _log;

        public DmIvAutoQcBackfillService(
            IAutoQcRemovalMonitorRepository monitorRepository,
            IDmIvAutoQcFlagService autoQcFlagService,
            IRiskCategoryService riskCategoryService,
            ITrialJudgmentService trialJudgmentService,
            IApplicationErrorLogger applicationErrorLogger,
            ILogger<DmIvAutoQcBackfillService> log)
        {
            _monitorRepository = monitorRepository;
            _autoQcFlagService = autoQcFlagService;
            _riskCategoryService = riskCategoryService;
            _trialJudgmentService = trialJudgmentService;
            _applicationErrorLogger = applicationErrorLogger;
            _log = log;
        }

        public Task<AutoQcPerIrfBackfillPreviewVm> GetPerIrfBackfillPreviewAsync(int irfId, DateTime fromUtc, DateTime toUtc) =>
            _monitorRepository.GetPerIrfBackfillPreviewAsync(irfId, fromUtc, toUtc);

        public Task<IReadOnlyList<IrfAuditDirectoryRowVm>> GetIrfAuditDirectoryAsync(DateTime fromIst, DateTime toIst, string ownerEmail = null, string profileName = null) =>
            _monitorRepository.GetIrfAuditDirectoryAsync(fromIst, toIst, ownerEmail, profileName);

        public Task<(IReadOnlyList<IrfAuditInterviewerRiskRowVm> Active, IReadOnlyList<IrfAuditInterviewerRiskRowVm> Removed)> GetIrfAuditInterviewerRiskBreakdownAsync(int irfId, DateTime fromIst, DateTime toIst) =>
            _monitorRepository.GetIrfAuditInterviewerRiskBreakdownAsync(irfId, fromIst, toIst);

        public Task<IReadOnlyList<IrfAuditInterviewerRiskDetailVm>> GetIrfAuditInterviewerRiskInterviewDetailsAsync(int irfId, DateTime fromIst, DateTime toIst, int? transInterviewerId = null) =>
            _monitorRepository.GetIrfAuditInterviewerRiskInterviewDetailsAsync(irfId, fromIst, toIst, transInterviewerId);

        public Task<IrfAuditWindowQualityVm> GetIrfAuditWindowQualityAsync(int irfId, DateTime fromIst, DateTime toIst) =>
            _monitorRepository.GetIrfAuditWindowQualityAsync(irfId, fromIst, toIst);

        public Task<string> GetIrfChannelOwnerEmailAsync(int irfId) =>
            _monitorRepository.GetIrfChannelOwnerEmailAsync(irfId);

        public Task<InterviewerRiskOverviewPageVm> GetInterviewerRiskOverviewAsync(
            DateTime fromIst, DateTime toIst, string ownerEmail, decimal? minRiskRatio,
            bool pendingReviewOnly, int offset, int pageSize) =>
            _monitorRepository.GetInterviewerRiskOverviewAsync(
                fromIst, toIst, ownerEmail, minRiskRatio, pendingReviewOnly, offset, pageSize);

        public Task<IReadOnlyList<InterviewerRiskOverviewChannelRowVm>> GetInterviewerRiskOverviewChannelsAsync(
            int interviewerId, DateTime fromIst, DateTime toIst, string ownerEmail) =>
            _monitorRepository.GetInterviewerRiskOverviewChannelsAsync(interviewerId, fromIst, toIst, ownerEmail);

        public Task<IReadOnlyList<InterviewerRiskOverviewRowVm>> GetInterviewerRiskOverviewExportRowsAsync(
            DateTime fromIst, DateTime toIst, string ownerEmail, decimal? minRiskRatio, bool pendingReviewOnly) =>
            _monitorRepository.GetInterviewerRiskOverviewExportRowsAsync(
                fromIst, toIst, ownerEmail, minRiskRatio, pendingReviewOnly);

        public async Task<IrfAuditRiskBackfillResultVm> BackfillRiskCategoryForIrfAsync(int irfId, DateTime fromIst, DateTime toIst,
            bool ignoreMassFireGuard = false,
            string executedBy = null,
            CancellationToken cancellationToken = default)
        {
            var result = new IrfAuditRiskBackfillResultVm { FromIst = fromIst.Date, ToIst = toIst.Date };
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

            result.Scanned = candidates.Count;

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

            // Pre-write snapshot of which candidates already have a RiskCategory row, so we can
            // classify insert vs. update vs. delete after the write pass (dry-run vs. post-state).
            var beforeIds = new HashSet<int>(await _monitorRepository.GetExistingRiskCategoryInterviewIdsAsync(interviewIds));

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
            var skipped = 0;
            var skipReasons = new List<string>();
            foreach (var interviewId in interviewIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ok = await _riskCategoryService.AddUpdateRiskCategory(interviewId);
                if (ok)
                {
                    processed++;
                }
                else
                {
                    skipped++;
                    if (skipReasons.Count < 10)
                        skipReasons.Add($"interview {interviewId}: write failed");
                }
            }

            var afterIds = new HashSet<int>(await _monitorRepository.GetExistingRiskCategoryInterviewIdsAsync(interviewIds));
            var inserted = afterIds.Count(id => !beforeIds.Contains(id));
            var deleted = beforeIds.Count(id => !afterIds.Contains(id));
            var updated = Math.Max(0, processed - inserted - deleted);

            result.Ok = true;
            result.Processed = processed;
            result.Remaining = remainingHint;
            result.Inserted = inserted;
            result.Updated = updated;
            result.Deleted = deleted;
            result.Skipped = skipped;
            result.SkipReasons = skipReasons.Count > 0 ? string.Join("; ", skipReasons) : null;

            var synced = await _monitorRepository.SyncTrialFlagsVerdictFromRiskCategoryAsync();
            result.SyncUpdates = synced;
            // TrialFlagsInserted stays 0 — see the ITrialJudgmentService comment on the constructor field.
            result.TrialFlagsInserted = 0;

            var nowIst = DateTime.UtcNow.AddMinutes(330);
            result.LastBackfillLabel = $"{nowIst:yyyy-MM-dd HH:mm} IST · {fromIst:dd MMM}–{toIst:dd MMM}";

            result.Message = remainingHint > 0
                ? $"Scanned {result.Scanned}: {inserted} inserted, {updated} updated, {deleted} deleted, {skipped} skipped. Synced {synced} trial verdict(s). Click Refresh risk again for ~{remainingHint}+ more."
                : $"Scanned {result.Scanned}: {inserted} inserted, {updated} updated, {deleted} deleted, {skipped} skipped. Synced {synced} trial verdict(s). Expand the IRF again to see updated P0–P5 counts.";

            _log.LogInformation(
                "RiskCategory backfill irf={IrfId}: scanned={Scanned} processed={Processed} inserted={Inserted} updated={Updated} deleted={Deleted} skipped={Skipped} remainingHint={Remaining} trialSynced={Synced} ignoreMassFire={Ignore}",
                irfId, result.Scanned, processed, inserted, updated, deleted, skipped, remainingHint, synced, ignoreMassFireGuard);

            try
            {
                await _monitorRepository.RecordRiskBackfillRunAsync(irfId, fromIst, toIst, result.Scanned, processed, synced, executedBy, result.Message);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

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
