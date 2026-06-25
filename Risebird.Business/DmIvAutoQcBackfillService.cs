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
        private readonly IDmIvAutoQcRemovalService _autoQcRemovalService;
        private readonly ILogger<DmIvAutoQcBackfillService> _log;

        public DmIvAutoQcBackfillService(
            IAutoQcRemovalMonitorRepository monitorRepository,
            IDmIvAutoQcRemovalService autoQcRemovalService,
            ILogger<DmIvAutoQcBackfillService> log)
        {
            _monitorRepository = monitorRepository;
            _autoQcRemovalService = autoQcRemovalService;
            _log = log;
        }

        public Task<int> GetPreDeploymentCandidateCountAsync(DateTime? asOfUtc = null, int windowDays = 60) =>
            _monitorRepository.GetBackfillCandidateCountAsync(asOfUtc, windowDays);

        public Task<(bool HasRun, Guid? LatestBatchId, DateTime? LatestExecutedAtUtc)> GetRunStatusAsync() =>
            _monitorRepository.GetBackfillRunStatusAsync();

        public async Task<AutoQcBackfillResultVm> RunBackfillAsync(string executedBy, bool forceRerun = false, CancellationToken cancellationToken = default)
        {
            var result = new AutoQcBackfillResultVm();
            var status = await _monitorRepository.GetBackfillRunStatusAsync();
            result.PreDeploymentCandidateCount = await _monitorRepository.GetBackfillCandidateCountAsync();

            if (status.HasRun && !forceRerun)
            {
                result.Ok = false;
                result.Message = $"Backfill already executed on {status.LatestExecutedAtUtc:u} (batch {status.LatestBatchId}). Use forceRerun only for ops recovery.";
                result.BatchId = status.LatestBatchId;
                return result;
            }

            var candidates = (await _monitorRepository.GetBackfillCandidatesAsync()).ToList();
            if (candidates.Count == 0)
            {
                var batchId = Guid.NewGuid();
                await _monitorRepository.RecordBackfillRunAsync(batchId, 0, 0, 0, executedBy ?? "System");
                result.Ok = true;
                result.BatchId = batchId;
                result.Message = "Backfill completed: no qualifying interviews in the 60-day window.";
                return result;
            }

            var batch = Guid.NewGuid();
            var grouped = candidates
                .GroupBy(x => x.TransInterviewerId)
                .ToList();

            var removed = 0;
            var skipped = 0;
            var triggeringInterviewCount = candidates.Select(x => x.InterviewId).Distinct().Count();

            foreach (var group in grouped)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rows = group.OrderByDescending(x => x.VerdictFinalizedAt).ThenByDescending(x => x.InterviewId).ToList();
                var first = rows[0];

                if (first.RbVetAlreadyNo)
                {
                    skipped += 1;
                    continue;
                }

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
                    RbVetAlreadyNo = first.RbVetAlreadyNo
                };

                var options = new AutoQcRemovalOptions
                {
                    Source = DmIvAutoQcAuditConstants.SourceRetroactiveBackfill60Day,
                    Tag = DmIvAutoQcAuditConstants.Tag,
                    BackfillBatchId = batch,
                    ExtraMetadata = metadata,
                    SkipIfAlreadyInactive = true,
                    BadgeVerdict = badge,
                    BadgeTriggeringInterviewId = badgeInterview
                };

                var applied = await _autoQcRemovalService.ApplyAutoQcRemovalForContextAsync(
                    ctx,
                    badgeInterview,
                    badge,
                    options,
                    cancellationToken);

                if (applied)
                    removed += 1;
                else
                    skipped += 1;
            }

            await _monitorRepository.RecordBackfillRunAsync(batch, removed, skipped, triggeringInterviewCount, executedBy ?? "System");

            result.Ok = true;
            result.BatchId = batch;
            result.RecordsRemoved = removed;
            result.RecordsSkipped = skipped;
            result.TriggeringInterviewCount = triggeringInterviewCount;
            result.Message = $"Backfill completed: {removed} interviewer-channel record(s) removed, {skipped} skipped, {triggeringInterviewCount} triggering interview(s).";
            _log.LogInformation("AutoQc backfill batch {BatchId}: removed={Removed} skipped={Skipped}", batch, removed, skipped);
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
