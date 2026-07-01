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
        private readonly ILogger<DmIvAutoQcBackfillService> _log;

        public DmIvAutoQcBackfillService(
            IAutoQcRemovalMonitorRepository monitorRepository,
            IDmIvAutoQcFlagService autoQcFlagService,
            ILogger<DmIvAutoQcBackfillService> log)
        {
            _monitorRepository = monitorRepository;
            _autoQcFlagService = autoQcFlagService;
            _log = log;
        }

        public Task<AutoQcPerIrfBackfillPreviewVm> GetPerIrfBackfillPreviewAsync(int irfId, DateTime fromUtc, DateTime toUtc) =>
            _monitorRepository.GetPerIrfBackfillPreviewAsync(irfId, fromUtc, toUtc);

        public Task<IReadOnlyList<IrfAuditDirectoryRowVm>> GetIrfAuditDirectoryAsync(DateTime fromIst, DateTime toIst) =>
            _monitorRepository.GetIrfAuditDirectoryAsync(fromIst, toIst);

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
