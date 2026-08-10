using Microsoft.Extensions.Logging;

using Risebird.Business.Interface;

using Risebird.CommonHelper.Interface;

using Risebird.Entities.Data;

using Risebird.Entities.Enum;

using Risebird.Entities.ViewModel;

using Risebird.Repository.Interface;

using System;

using System.Collections.Generic;

using System.Threading;

using System.Threading.Tasks;



namespace Risebird.Business

{

    public class DmIvAutoQcRemovalService : IDmIvAutoQcRemovalService

    {

        private readonly ITransInterviewerAuditRepository _auditRepository;

        private readonly IInterviewerService _interviewerService;

        private readonly IMeetingService _meetingService;

        private readonly IApplicationErrorLogger _applicationErrorLogger;

        private readonly ILogger<DmIvAutoQcRemovalService> _log;



        public DmIvAutoQcRemovalService(

            ITransInterviewerAuditRepository auditRepository,

            IInterviewerService interviewerService,

            IMeetingService meetingService,

            IApplicationErrorLogger applicationErrorLogger,

            ILogger<DmIvAutoQcRemovalService> log)

        {

            _auditRepository = auditRepository;

            _interviewerService = interviewerService;

            _meetingService = meetingService;

            _applicationErrorLogger = applicationErrorLogger;

            _log = log;

        }



        public async Task TryApplyAutoQcRemovalAsync(int interviewId, int irfId, string interviewerSuspicious,

            bool? redoInterview, CancellationToken cancellationToken = default)

        {

            cancellationToken.ThrowIfCancellationRequested();



            var verdict = DmIvAutoQcAuditConstants.ResolveVerdict(interviewerSuspicious, redoInterview);

            if (verdict == null)

                return;



            var ctx = await _auditRepository.GetContextForInterviewAsync(interviewId);

            if (ctx == null)

            {

                _log.LogWarning("DmIvAutoQcRemoval: no TransInterviewer for interviewId={InterviewId}", interviewId);

                return;

            }



            if (irfId > 0 && ctx.IrfId != irfId)

            {

                _log.LogWarning(

                    "DmIvAutoQcRemoval: irfId mismatch interviewId={InterviewId} expected={Expected} actual={Actual}",

                    interviewId, irfId, ctx.IrfId);

            }



            var options = new AutoQcRemovalOptions

            {

                Source = DmIvAutoQcAuditConstants.SourceForwardTrigger,

                Tag = DmIvAutoQcAuditConstants.Tag,

                BadgeVerdict = verdict,

                BadgeTriggeringInterviewId = interviewId,

                SkipIfAlreadyInactive = false

            };



            await ApplyAutoQcRemovalForContextAsync(ctx, interviewId, verdict, options, cancellationToken);

        }



        public async Task<bool> ApplyAutoQcRemovalForContextAsync(TransInterviewerInterviewContext ctx, int triggeringInterviewId,

            string verdict, AutoQcRemovalOptions options, CancellationToken cancellationToken = default)

        {

            cancellationToken.ThrowIfCancellationRequested();

            options ??= new AutoQcRemovalOptions();



            if (string.IsNullOrWhiteSpace(verdict))

                return false;



            var source = string.IsNullOrWhiteSpace(options.Source)

                ? DmIvAutoQcAuditConstants.SourceForwardTrigger

                : options.Source;



            if (source == DmIvAutoQcAuditConstants.SourceForwardTrigger)

            {

                var alreadyRecorded = await _auditRepository.HasAutoQcRemovalAuditForInterviewAsync(

                    ctx.TransInterviewerId, triggeringInterviewId, source);

                if (alreadyRecorded)

                {

                    _log.LogInformation(

                        "DmIvAutoQcRemoval: skip duplicate forward removal transId={TransId} interviewId={InterviewId}",

                        ctx.TransInterviewerId, triggeringInterviewId);

                    return false;

                }

            }



            if (options.SkipIfAlreadyInactive && ctx.RbVetAlreadyNo)

            {

                _log.LogInformation(

                    "DmIvAutoQcRemoval: skip already-inactive transId={TransId} interviewId={InterviewId}",

                    ctx.TransInterviewerId, triggeringInterviewId);

                return false;

            }



            const string changedBy = "AISystem";

            var fromActive = ctx.IsActive ? "Active" : "Inactive";



            var interviewers = new List<TransInterviewer>

            {

                new TransInterviewer { id = ctx.TransInterviewerId, IsSMEVerified = false }

            };

            var vetOk = await _interviewerService.UpdateVerifiedInterviewers(interviewers, changedBy);

            if (!vetOk)

            {

                _log.LogWarning("DmIvAutoQcRemoval: UpdateVerifiedInterviewers failed transId={TransId}", ctx.TransInterviewerId);

                return false;

            }



            await _meetingService.RemoveOrShortlistInterviewerFromIRF(ctx.IrfId, ctx.InterviewerEmail, "Rejected");

            var (isActive, isAvailable) = DmIvRecordHelper.ComputeFlagsFromVet(false, "Rejected");

            await _interviewerService.UpdateIRFInterviewerActiveStatus(

                ctx.UserId, isActive, ctx.IrfId, changedBy, isAvailable: isAvailable);



            var badgeVerdict = string.IsNullOrWhiteSpace(options.BadgeVerdict) ? verdict : options.BadgeVerdict;

            var badgeInterviewId = options.BadgeTriggeringInterviewId ?? triggeringInterviewId;



            await _auditRepository.InsertAsync(

                ctx.TransInterviewerId,

                DmIvInactiveAuditEventTypes.AutoQcRemoval,

                fromActive,

                badgeVerdict,

                reasonCategory: null,

                triggeringInterviewId: badgeInterviewId,

                changedBy: changedBy,

                auditTag: options.Tag ?? DmIvAutoQcAuditConstants.Tag,

                auditSource: source,

                backfillBatchId: options.BackfillBatchId,

                extraMetadata: options.ExtraMetadata);



            await _auditRepository.UpdateAutoQcFieldsAsync(ctx.TransInterviewerId, badgeVerdict, badgeInterviewId);

            return true;

        }

        public async Task<IrfAuditRiskRemoveResultVm> RemoveForRiskAuditAsync(
            int transInterviewerId,
            int irfId,
            string changedBy,
            string riskSummary,
            int? worstSeverityInterviewId,
            string comment = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (transInterviewerId <= 0 || irfId <= 0)
                return new IrfAuditRiskRemoveResultVm { Ok = false, Message = "Invalid interviewer or IRF." };

            var actor = string.IsNullOrWhiteSpace(changedBy) ? "System" : changedBy.Trim();
            var ctx = await _auditRepository.GetContextByTransInterviewerIdAsync(transInterviewerId);
            if (ctx == null)
                return new IrfAuditRiskRemoveResultVm { Ok = false, Message = "TransInterviewer channel not found." };

            if (ctx.IrfId != irfId)
                return new IrfAuditRiskRemoveResultVm { Ok = false, Message = "Interviewer is not on the selected IRF." };

            if (!ctx.IsActive)
                return new IrfAuditRiskRemoveResultVm { Ok = false, Message = "Interviewer is already inactive on this IRF." };

            if (string.IsNullOrWhiteSpace(ctx.InterviewerEmail))
                return new IrfAuditRiskRemoveResultVm { Ok = false, Message = "Interviewer email is missing; cannot run removal rail." };

            var fromActive = ctx.IsActive ? "Active" : "Inactive";
            var reasonDetail = string.IsNullOrWhiteSpace(riskSummary) ? "Risk audit removal" : riskSummary.Trim();
            if (!string.IsNullOrWhiteSpace(comment))
                reasonDetail = reasonDetail + "; comment=" + comment.Trim();
            if (reasonDetail.Length > 500)
                reasonDetail = reasonDetail.Substring(0, 500);

            var interviewers = new List<TransInterviewer>
            {
                new TransInterviewer { id = ctx.TransInterviewerId, IsSMEVerified = false }
            };
            var vetOk = await _interviewerService.UpdateVerifiedInterviewers(interviewers, actor);
            if (!vetOk)
            {
                _log.LogWarning("RiskAuditRemoval: UpdateVerifiedInterviewers failed transId={TransId}", ctx.TransInterviewerId);
                return new IrfAuditRiskRemoveResultVm { Ok = false, Message = "Could not update RB vet status." };
            }

            await _meetingService.RemoveOrShortlistInterviewerFromIRF(ctx.IrfId, ctx.InterviewerEmail, "Rejected");
            var (isActive, isAvailable) = DmIvRecordHelper.ComputeFlagsFromVet(false, "Rejected");
            await _interviewerService.UpdateIRFInterviewerActiveStatus(
                ctx.UserId, isActive, ctx.IrfId, actor, isAvailable: isAvailable);

            var triggerId = worstSeverityInterviewId.GetValueOrDefault();
            await _auditRepository.InsertAsync(
                ctx.TransInterviewerId,
                DmIvInactiveAuditEventTypes.RiskAuditRemoval,
                fromActive,
                "risk_audit",
                reasonCategory: reasonDetail,
                triggeringInterviewId: triggerId > 0 ? triggerId : (int?)null,
                changedBy: actor,
                auditTag: DmIvAutoQcAuditConstants.RiskAuditTag,
                auditSource: DmIvAutoQcAuditConstants.SourceRiskAuditManual,
                extraMetadata: reasonDetail);

            return new IrfAuditRiskRemoveResultVm { Ok = true, Message = "Interviewer removed from IRF." };
        }

        public async Task<IrfAuditRiskKeepResultVm> KeepForRiskAuditAsync(
            int transInterviewerId,
            int irfId,
            string changedBy,
            string riskSummary,
            int? worstSeverityInterviewId,
            string comment = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (transInterviewerId <= 0 || irfId <= 0)
                return new IrfAuditRiskKeepResultVm { Ok = false, Message = "Invalid interviewer or IRF." };

            var actor = string.IsNullOrWhiteSpace(changedBy) ? "System" : changedBy.Trim();
            var ctx = await _auditRepository.GetContextByTransInterviewerIdAsync(transInterviewerId);
            if (ctx == null)
                return new IrfAuditRiskKeepResultVm { Ok = false, Message = "TransInterviewer channel not found." };

            if (ctx.IrfId != irfId)
                return new IrfAuditRiskKeepResultVm { Ok = false, Message = "Interviewer is not on the selected IRF." };

            var reasonDetail = string.IsNullOrWhiteSpace(riskSummary) ? "Risk audit keep" : riskSummary.Trim();
            if (!string.IsNullOrWhiteSpace(comment))
                reasonDetail = reasonDetail + "; comment=" + comment.Trim();
            if (reasonDetail.Length > 500)
                reasonDetail = reasonDetail.Substring(0, 500);

            var triggerId = worstSeverityInterviewId.GetValueOrDefault();
            var fromActive = ctx.IsActive ? "Active" : "Inactive";

            // No IsActive/vet change — Keep is a review decision only.
            await _auditRepository.InsertAsync(
                ctx.TransInterviewerId,
                DmIvInactiveAuditEventTypes.RiskAuditKept,
                fromActive,
                "risk_audit_kept",
                reasonCategory: reasonDetail,
                triggeringInterviewId: triggerId > 0 ? triggerId : (int?)null,
                changedBy: actor,
                auditTag: DmIvAutoQcAuditConstants.RiskAuditTag,
                auditSource: DmIvAutoQcAuditConstants.SourceRiskAuditManual,
                extraMetadata: reasonDetail);

            return new IrfAuditRiskKeepResultVm { Ok = true, Message = "Interviewer kept on IRF." };
        }
    }
}


