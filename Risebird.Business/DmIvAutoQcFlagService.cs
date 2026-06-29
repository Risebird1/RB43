using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class DmIvAutoQcFlagService : IDmIvAutoQcFlagService
    {
        private readonly ITransInterviewerAuditRepository _auditRepository;
        private readonly ILogger<DmIvAutoQcFlagService> _log;

        public DmIvAutoQcFlagService(
            ITransInterviewerAuditRepository auditRepository,
            ILogger<DmIvAutoQcFlagService> log)
        {
            _auditRepository = auditRepository;
            _log = log;
        }

        public async Task TryApplyAutoQcFlagAsync(int interviewId, int irfId, string interviewerSuspicious,
            bool? redoInterview, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var verdict = DmIvAutoQcAuditConstants.ResolveVerdict(interviewerSuspicious, redoInterview);
            if (verdict == null)
                return;

            var ctx = await _auditRepository.GetContextForInterviewAsync(interviewId, irfId);
            if (ctx == null)
            {
                _log.LogWarning("DmIvAutoQcFlag: no TransInterviewer for interviewId={InterviewId}", interviewId);
                return;
            }

            if (irfId > 0 && ctx.IrfId != irfId)
            {
                _log.LogWarning(
                    "DmIvAutoQcFlag: irfId mismatch interviewId={InterviewId} expected={Expected} actual={Actual}",
                    interviewId, irfId, ctx.IrfId);
            }

            var options = new AutoQcFlagOptions
            {
                Source = DmIvAutoQcAuditConstants.SourceForwardTrigger,
                Tag = DmIvAutoQcAuditConstants.FlagTag,
                BadgeVerdict = verdict,
                BadgeTriggeringInterviewId = interviewId
            };

            await ApplyAutoQcFlagForContextAsync(ctx, interviewId, verdict, options, cancellationToken);
        }

        public async Task<bool> ApplyAutoQcFlagForContextAsync(TransInterviewerInterviewContext ctx, int triggeringInterviewId,
            string verdict, AutoQcFlagOptions options, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            options ??= new AutoQcFlagOptions();

            if (string.IsNullOrWhiteSpace(verdict) || ctx == null)
                return false;

            var source = string.IsNullOrWhiteSpace(options.Source)
                ? DmIvAutoQcAuditConstants.SourceForwardTrigger
                : options.Source;

            var alreadyRecorded = await _auditRepository.HasAutoQcFlagAuditForInterviewAsync(
                ctx.TransInterviewerId, triggeringInterviewId, auditSource: null);
            if (alreadyRecorded)
            {
                _log.LogInformation(
                    "DmIvAutoQcFlag: skip duplicate flag transId={TransId} interviewId={InterviewId}",
                    ctx.TransInterviewerId, triggeringInterviewId);
                return false;
            }

            const string changedBy = "AISystem";
            var badgeVerdict = string.IsNullOrWhiteSpace(options.BadgeVerdict) ? verdict : options.BadgeVerdict;
            var badgeInterviewId = options.BadgeTriggeringInterviewId ?? triggeringInterviewId;
            var fromValue = string.IsNullOrWhiteSpace(ctx.AutoQcVerdict) ? "unset" : ctx.AutoQcVerdict;

            await _auditRepository.InsertAsync(
                ctx.TransInterviewerId,
                DmIvInactiveAuditEventTypes.AutoQcFlag,
                fromValue,
                verdict,
                reasonCategory: null,
                triggeringInterviewId: triggeringInterviewId,
                changedBy: changedBy,
                auditTag: options.Tag ?? DmIvAutoQcAuditConstants.FlagTag,
                auditSource: source,
                backfillBatchId: options.BackfillBatchId,
                extraMetadata: options.ExtraMetadata);

            var shouldUpdateFields = string.IsNullOrWhiteSpace(ctx.AutoQcVerdict)
                || (badgeVerdict == DmIvAutoQcAuditConstants.VerdictSuspicious
                    && !string.Equals(ctx.AutoQcVerdict, DmIvAutoQcAuditConstants.VerdictSuspicious, StringComparison.OrdinalIgnoreCase));

            if (shouldUpdateFields && (!options.SkipIfAlreadyFlagged || string.IsNullOrWhiteSpace(ctx.AutoQcVerdict)))
            {
                await _auditRepository.UpdateAutoQcFieldsAsync(
                    ctx.TransInterviewerId, badgeVerdict, badgeInterviewId, source);
                ctx.AutoQcVerdict = badgeVerdict;
            }

            _log.LogInformation(
                "DmIvAutoQcFlag: flagged transId={TransId} interviewId={InterviewId} verdict={Verdict} source={Source}",
                ctx.TransInterviewerId, triggeringInterviewId, verdict, source);

            return true;
        }
    }
}
