using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class DmIvChannelDeactivationService : IDmIvChannelDeactivationService
    {
        private const string AuditSourceClientReport = "ClientReportRemove";

        private readonly ITransInterviewerAuditRepository _auditRepository;
        private readonly IMeetingService _meetingService;
        private readonly IInterviewerService _interviewerService;

        public DmIvChannelDeactivationService(
            ITransInterviewerAuditRepository auditRepository,
            IMeetingService meetingService,
            IInterviewerService interviewerService)
        {
            _auditRepository = auditRepository;
            _meetingService = meetingService;
            _interviewerService = interviewerService;
        }

        public async Task<DmIvChannelDeactivationPreview> GetClientReportPreviewAsync(int interviewId, string profileName,
            CancellationToken cancellationToken = default)
        {
            var futureCount = await _auditRepository.GetFutureAssignedCountForInterviewAsync(interviewId);
            return new DmIvChannelDeactivationPreview
            {
                IsClientRole = DmIvClientReportRoleHelper.IsClientRole(profileName),
                FutureAssignedCount = futureCount
            };
        }

        public async Task<DmIvChannelDeactivationResult> DeactivateInterviewerOnChannelFromClientReportAsync(
            int interviewId, string profileName, string changedByEmail,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(changedByEmail))
                return DmIvChannelDeactivationResult.Fail("Unauthorized");

            var role = profileName?.Trim();
            if (!DmIvClientReportRoleHelper.IsClientRole(role) && !DmIvClientReportRoleHelper.IsInternalRole(role))
                return DmIvChannelDeactivationResult.Fail("You do not have permission to remove this interviewer.");

            var ctx = await _auditRepository.GetContextForInterviewAsync(interviewId);
            if (ctx == null)
                return DmIvChannelDeactivationResult.Fail("Interviewer channel record not found for this interview.");

            var futureCount = await _auditRepository.GetFutureAssignedCountForInterviewAsync(interviewId);

            if (DmIvClientReportRoleHelper.IsClientRole(role))
            {
                var ok = await DeactivateClientVetAsync(ctx, interviewId, role, changedByEmail);
                if (!ok)
                    return DmIvChannelDeactivationResult.Fail("Could not remove interviewer. Please try again.");

                return new DmIvChannelDeactivationResult
                {
                    Success = true,
                    FlagFlipped = "Client Vet",
                    Role = role,
                    TransInterviewerId = ctx.TransInterviewerId,
                    IrfId = ctx.IrfId,
                    InterviewerUserId = ctx.UserId,
                    FutureAssignedCount = futureCount
                };
            }

            var rbOk = await DeactivateRbVetAsync(ctx, interviewId, role, changedByEmail);
            if (!rbOk)
                return DmIvChannelDeactivationResult.Fail("Could not remove interviewer. Please try again.");

            return new DmIvChannelDeactivationResult
            {
                Success = true,
                FlagFlipped = "RB Vet",
                Role = role,
                TransInterviewerId = ctx.TransInterviewerId,
                IrfId = ctx.IrfId,
                InterviewerUserId = ctx.UserId,
                FutureAssignedCount = futureCount
            };
        }

        private async Task<bool> DeactivateClientVetAsync(
            TransInterviewerInterviewContext ctx, int interviewId, string role, string changedByEmail)
        {
            var rbVetYes = ctx.IsRbVetYes;
            var fromClientAction = ctx.ClientAction ?? string.Empty;

            var cascadeOk = await _meetingService.RemoveOrShortlistInterviewerFromIRF(
                ctx.IrfId, ctx.InterviewerEmail, "Rejected");
            if (!cascadeOk)
                return false;

            var (isActive, isAvailable) = DmIvRecordHelper.ComputeFlagsFromVet(rbVetYes, "Rejected");
            var activeOk = await _interviewerService.UpdateIRFInterviewerActiveStatus(
                ctx.UserId, isActive, ctx.IrfId, changedByEmail, isAvailable: isAvailable);
            if (!activeOk)
                return false;

            await _auditRepository.InsertAsync(
                ctx.TransInterviewerId,
                DmIvInactiveAuditEventTypes.ClientVetFlippedToNo,
                fromClientAction,
                "Rejected",
                reasonCategory: role,
                triggeringInterviewId: interviewId,
                changedBy: changedByEmail,
                auditSource: AuditSourceClientReport,
                extraMetadata: BuildAuditMetadata(role, ctx.IrfId, interviewId, ctx.UserId, "Client Vet", changedByEmail));

            return true;
        }

        private async Task<bool> DeactivateRbVetAsync(
            TransInterviewerInterviewContext ctx, int interviewId, string role, string changedByEmail)
        {
            var rbBefore = ctx.IsRbVetYes ? "1" : "0";

            var interviewers = new List<TransInterviewer>
            {
                new TransInterviewer { id = ctx.TransInterviewerId, IsSMEVerified = false }
            };
            var vetOk = await _interviewerService.UpdateVerifiedInterviewers(interviewers, changedByEmail);
            if (!vetOk)
                return false;

            var cascadeOk = await _meetingService.RemoveOrShortlistInterviewerFromIRF(
                ctx.IrfId, ctx.InterviewerEmail, "Rejected");
            if (!cascadeOk)
                return false;

            var (isActive, isAvailable) = DmIvRecordHelper.ComputeFlagsFromVet(false, "Rejected");
            var activeOk = await _interviewerService.UpdateIRFInterviewerActiveStatus(
                ctx.UserId, isActive, ctx.IrfId, changedByEmail, isAvailable: isAvailable);
            if (!activeOk)
                return false;

            await _auditRepository.InsertAsync(
                ctx.TransInterviewerId,
                DmIvInactiveAuditEventTypes.RbVetFlippedToNo,
                rbBefore,
                "0",
                reasonCategory: role,
                triggeringInterviewId: interviewId,
                changedBy: changedByEmail,
                auditSource: AuditSourceClientReport,
                extraMetadata: BuildAuditMetadata(role, ctx.IrfId, interviewId, ctx.UserId, "RB Vet", changedByEmail));

            return true;
        }

        private static string BuildAuditMetadata(
            string role, int irfId, int interviewId, int interviewerUserId, string flagFlipped, string actingUserEmail)
        {
            return JsonConvert.SerializeObject(new
            {
                role,
                irfId,
                interviewId,
                interviewerUserId,
                flagFlipped,
                actingUserEmail
            });
        }
    }
}
