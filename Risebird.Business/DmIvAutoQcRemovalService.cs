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



            var verdict = ResolveVerdict(interviewerSuspicious, redoInterview);

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



        internal static string ResolveVerdict(string interviewerSuspicious, bool? redoInterview)

        {

            if (string.Equals(interviewerSuspicious, "Yes", StringComparison.OrdinalIgnoreCase))

                return DmIvAutoQcAuditConstants.VerdictSuspicious;

            if (redoInterview == true)

                return DmIvAutoQcAuditConstants.VerdictRedo;

            return null;

        }

    }

}


