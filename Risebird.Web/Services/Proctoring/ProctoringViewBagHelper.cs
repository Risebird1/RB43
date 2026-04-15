using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Services.Proctoring
{
    /// <summary>
    /// ViewBag for the proctoring bootstrap partial on the AI Interview page (<c>/AIInterview/Index</c>).
    /// </summary>
    public static class ProctoringViewBagHelper
    {
        public static void ApplyForMeeting(
            Controller controller,
            MeetingVM meetingDetails,
            IProctoringTokenService proctoringTokenService,
            IConfiguration configuration,
            string sessionKeyForToken)
        {
            if (meetingDetails == null)
                return;

            if (IsTerminalProctoringMeetingStatus(meetingDetails.MeetingStatus))
            {
                controller.ViewBag.ProctoringToken = null;
                return;
            }

            var email = meetingDetails.candidateEmail ?? string.Empty;
            controller.ViewBag.ProctoringInterviewId = meetingDetails.InterviewId;
            controller.ViewBag.ProctoringToken = proctoringTokenService.CreateToken(
                email,
                meetingDetails.InterviewId,
                sessionKeyForToken ?? string.Empty);
            controller.ViewBag.ProctoringCandidateName = meetingDetails.candidateName ?? string.Empty;
            controller.ViewBag.ProctoringCandidateEmail = email;
            var durationMin = meetingDetails.interviewDuration > 0 ? meetingDetails.interviewDuration : 60;
            var meetingEndLocal = meetingDetails.interviewDate.AddMinutes(durationMin);
            controller.ViewBag.ProctoringMeetingStartIso = meetingDetails.interviewDate.ToString(
                "yyyy-MM-ddTHH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture);
            controller.ViewBag.ProctoringMeetingEndIso = meetingEndLocal.ToString(
                "yyyy-MM-ddTHH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture);
            controller.ViewBag.ProctoringClipboardBlocking = configuration.GetValue("Proctoring:ClipboardBlocking", true);
            controller.ViewBag.ProctoringRestrictedDomains = configuration.GetSection("Proctoring:RestrictedDomains").Get<string[]>()
                ?? new[] { "google.com", "stackoverflow.com", "chat.openai.com" };
            var allowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                controller.Request.Host.Host,
                "portal.risebird.io"
            };
            controller.ViewBag.ProctoringAllowedExamHosts = allowedHosts.ToArray();
        }

        private static bool IsTerminalProctoringMeetingStatus(int? ms)
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
