using System;
using System.Threading.Tasks;
using Risebird.Business.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Services.Proctoring
{
    /// <summary>Persists proctoring telemetry only for AI Interview sessions (AIInterview/Index), not human interview pages.</summary>
    public sealed class ProctoringSessionEligibilityService : IProctoringSessionEligibilityService
    {
        private readonly IAIInterviewerService _aiInterviewerService;

        public ProctoringSessionEligibilityService(IAIInterviewerService aiInterviewerService)
        {
            _aiInterviewerService = aiInterviewerService;
        }

        public async Task<bool> ShouldPersistProctoringEventsAsync(ProctoringTokenPayload payload)
        {
            if (payload == null)
                return false;

            if (string.IsNullOrWhiteSpace(payload.AiSessionId) || !Guid.TryParse(payload.AiSessionId, out var sessionGuid))
                return false;

            MeetingVM details;
            try
            {
                details = await _aiInterviewerService.GetAIInterviewDetailsBySessionId(sessionGuid);
            }
            catch
            {
                return false;
            }

            if (details == null || details.InterviewId != payload.InterviewId)
                return false;
            if (!string.IsNullOrEmpty(details.candidateEmail) &&
                !string.Equals(details.candidateEmail.Trim(), payload.Email?.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
            if (IsTerminalMeetingStatus(details.MeetingStatus))
                return false;
            if (!details.IsProctoring)
                return false;

            return true;
        }

        public async Task<bool> ShouldPersistProctoringMediaAsync(ProctoringTokenPayload payload)
        {
            if (payload == null)
                return false;

            if (string.IsNullOrWhiteSpace(payload.AiSessionId) || !Guid.TryParse(payload.AiSessionId, out var sessionGuid))
                return false;

            MeetingVM details;
            try
            {
                details = await _aiInterviewerService.GetAIInterviewDetailsBySessionId(sessionGuid);
            }
            catch
            {
                return false;
            }

            if (details == null || details.InterviewId != payload.InterviewId)
                return false;
            if (!string.IsNullOrEmpty(details.candidateEmail) &&
                !string.Equals(details.candidateEmail.Trim(), payload.Email?.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
            if (!details.IsProctoring)
                return false;

            return true;
        }

        private static bool IsTerminalMeetingStatus(int? ms)
        {
            if (!ms.HasValue)
                return false;
            return ms.Value == (int)MeetingStatus.Completed
                || ms.Value == (int)MeetingStatus.NoShow
                || ms.Value == (int)MeetingStatus.Dropped
                || ms.Value == (int)MeetingStatus.NoShowInterviewer
                || ms.Value == (int)MeetingStatus.DroppedSelected
                || ms.Value == (int)MeetingStatus.DroppedRejected
                || ms.Value == (int)MeetingStatus.DroppedBorderLine
                || ms.Value == (int)MeetingStatus.NoShowStatus;
        }
    }
}
