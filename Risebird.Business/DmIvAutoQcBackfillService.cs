using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.Entities.Data;
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
        private readonly ILogger<DmIvAutoQcBackfillService> _log;

        public DmIvAutoQcBackfillService(
            IAutoQcRemovalMonitorRepository monitorRepository,
            IDmIvAutoQcFlagService autoQcFlagService,
            IRiskCategoryService riskCategoryService,
            ILogger<DmIvAutoQcBackfillService> log)
        {
            _monitorRepository = monitorRepository;
            _autoQcFlagService = autoQcFlagService;
            _riskCategoryService = riskCategoryService;
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
            var processed = 0;
            var remaining = 0;

            for (var batchIndex = 0; batchIndex < maxBatches; batchIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var candidates = (await _monitorRepository.GetRiskCategoryBackfillCandidatesByIrfAsync(
                    irfId, fromIst, toIst, batchSize + 1)).ToList();

                if (candidates.Count == 0)
                {
                    remaining = 0;
                    break;
                }

                var batch = candidates.Take(batchSize).ToList();
                remaining = Math.Max(0, candidates.Count - batch.Count);

                foreach (var interviewId in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var ok = await _riskCategoryService.AddUpdateRiskCategory(interviewId);
                    if (ok)
                        processed++;
                }

                // Prefer-missing ordering: once only existing (severity-filled) rows remain,
                // one pass is enough; further loops would re-process forever.
                if (remaining == 0)
                    break;

                // If everything remaining already has Severity (ORDER BY puts them last),
                // candidate list still returns them — stop after first full regrade pass of missing ones.
                // Heuristic: if this batch did not shrink the "prefer missing" set, continue until maxBatches.
            }

            result.Ok = true;
            result.Processed = processed;
            result.Remaining = remaining;
            result.Message = remaining > 0
                ? $"Refreshed risk for {processed} interview(s). Click Refresh risk again for ~{remaining}+ more."
                : $"Refreshed risk for {processed} interview(s). Expand the IRF again to see updated P0–P5 counts.";
            _log.LogInformation("RiskCategory backfill irf={IrfId}: processed={Processed} remainingHint={Remaining}", irfId, processed, remaining);
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
