using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
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
                return;
            }

            await _meetingService.RemoveOrShortlistInterviewerFromIRF(ctx.IrfId, ctx.InterviewerEmail, "Rejected");
            var (isActive, isAvailable) = DmIvRecordHelper.ComputeFlagsFromVet(false, "Rejected");
            await _interviewerService.UpdateIRFInterviewerActiveStatus(
                ctx.UserId, isActive, ctx.IrfId, changedBy, isAvailable: isAvailable);

            await _auditRepository.InsertAsync(
                ctx.TransInterviewerId,
                DmIvInactiveAuditEventTypes.AutoQcRemoval,
                fromActive,
                verdict,
                reasonCategory: null,
                triggeringInterviewId: interviewId,
                changedBy: changedBy);

            await _auditRepository.UpdateAutoQcFieldsAsync(ctx.TransInterviewerId, verdict, interviewId);
        }

        internal static string ResolveVerdict(string interviewerSuspicious, bool? redoInterview)
        {
            if (string.Equals(interviewerSuspicious, "Yes", StringComparison.OrdinalIgnoreCase))
                return "suspicious";
            if (redoInterview == true)
                return "redo";
            return null;
        }
    }
}
