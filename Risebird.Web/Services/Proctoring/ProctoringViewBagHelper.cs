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
        /// <param name="issueProctoringBootstrapEvenIfMeetingStatusTerminal">
        /// When true (AI interview page), always emit bootstrap if we can resolve an interview id.
        /// InterviewComments.MeetingStatus is often stale or set to Dropped/NoShow while the candidate still has a valid session link.
        /// </param>
        public static void ApplyForMeeting(
            Controller controller,
            MeetingVM meetingDetails,
            IProctoringTokenService proctoringTokenService,
            IConfiguration configuration,
            string sessionKeyForToken,
            bool issueProctoringBootstrapEvenIfMeetingStatusTerminal = false)
        {
            if (meetingDetails == null)
                return;

            // Prefer InterviewId from usp_GetAIInterviewDetailsBySessionId; fall back to other MeetingVM keys if mapping ever misses.
            var interviewId = meetingDetails.InterviewId > 0
                ? meetingDetails.InterviewId
                : (meetingDetails.id > 0 ? meetingDetails.id : meetingDetails.imid);
            if (interviewId <= 0)
                return;

            // Do not suppress proctoring bootstrap for MeetingStatus.Completed. AI interview pages
            // can still be served while InterviewComments lags or status is set early; skipping here
            // removes all RB_PROCTORING_* postMessage wiring so the Chrome extension never joins or fetches.
            if (!issueProctoringBootstrapEvenIfMeetingStatusTerminal
                && IsTerminalProctoringMeetingStatusExcludingCompleted(meetingDetails.MeetingStatus))
            {
                controller.ViewBag.ProctoringToken = null;
                return;
            }

            var email = meetingDetails.candidateEmail ?? string.Empty;
            var reqHost = controller.Request.Host.Host;
            var isLocalHost = IsLocalDevHost(reqHost);
            // Chrome extensions on localhost: strict targetOrigin can block delivery; * is acceptable for same-tab dev only.
            controller.ViewBag.ProctoringPostMessageTargetOrigin = isLocalHost
                ? "*"
                : $"{controller.Request.Scheme}://{controller.Request.Host.Value}";

            controller.ViewBag.ProctoringInterviewId = interviewId;
            controller.ViewBag.ProctoringToken = proctoringTokenService.CreateToken(
                email,
                interviewId,
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

        private static bool IsLocalDevHost(string host)
        {
            if (string.IsNullOrEmpty(host))
                return false;
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(host, "[::1]", StringComparison.OrdinalIgnoreCase))
                return true;
            return host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Terminal for proctoring bootstrap only — excludes <see cref="MeetingStatus.Completed"/> so live
        /// AI interview pages still emit extension handshakes when DB status is ahead of UX reality.
        /// </summary>
        private static bool IsTerminalProctoringMeetingStatusExcludingCompleted(int? ms)
        {
            if (!ms.HasValue)
                return false;
            return ms.Value == (int)MeetingStatus.NoShow
                || ms.Value == (int)MeetingStatus.Dropped
                || ms.Value == (int)MeetingStatus.NoShowInterviewer
                || ms.Value == (int)MeetingStatus.DroppedSelected
                || ms.Value == (int)MeetingStatus.DroppedRejected
                || ms.Value == (int)MeetingStatus.DroppedBorderLine
                || ms.Value == (int)MeetingStatus.NoShowStatus;
        }
    }
}
