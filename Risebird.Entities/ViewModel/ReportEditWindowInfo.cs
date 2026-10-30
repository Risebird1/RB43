using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Interviewer report edit window: what the server needs to decide whether the Interviewer profile may
    /// open or save the feedback report of one interview. Read live on every page load and save.
    /// </summary>
    public class ReportEditWindowInfo
    {
        public int InterviewId { get; set; }
        public int IRFId { get; set; }
        public DateTime InterviewDate { get; set; }
        /// <summary>Scheduled duration in minutes (IRF.InterviewDuration, or PlanTypeChargesMaster.DurationSelectedMin for new skill channels).</summary>
        public int? InterviewDuration { get; set; }
        /// <summary>InterviewReport.CreatedOn of the first submission; null while no report has been submitted.</summary>
        public DateTime? ReportSubmittedOn { get; set; }
        /// <summary>IRF.EnableReportEditWindow. NULL or false: legacy behaviour.</summary>
        public bool? EnableReportEditWindow { get; set; }
        public string Interviewer { get; set; }
        /// <summary>'Edit' rows in InterviewReportEditLog saved by the interview's own interviewer.</summary>
        public int InterviewerEditCount { get; set; }
        /// <summary>Verdict currently stored in InterviewComments, read at save time.</summary>
        public int? CurrentInterviewStatus { get; set; }
        /// <summary>OverallScore currently stored in InterviewReport, read at save time.</summary>
        public double? CurrentOverallScore { get; set; }
    }

    /// <summary>One row of the /Interview/ReportEditAlerts ("911") page: a change to a submitted report.</summary>
    public class ReportEditAlert
    {
        public int id { get; set; }
        public int InterviewId { get; set; }
        public int? IRFId { get; set; }
        public string Action { get; set; }
        public string PerformedBy { get; set; }
        public DateTime PerformedOn { get; set; }
        public int? OldInterviewStatus { get; set; }
        public int? NewInterviewStatus { get; set; }
        public int? OldMeetingStatus { get; set; }
        public int? NewMeetingStatus { get; set; }
        public string OldInterviewerComment { get; set; }
        public string NewInterviewerComment { get; set; }
        public double? OldOverallScore { get; set; }
        public double? NewOverallScore { get; set; }
        public string EditReason { get; set; }
        public string Source { get; set; }
        public string Interviewer { get; set; }
        public string CandidateName { get; set; }
        public string JDTiltle { get; set; }
        public DateTime? ReportSubmittedOn { get; set; }

        public bool IsVerdictChange => (OldInterviewStatus ?? -1) != (NewInterviewStatus ?? -1);
        public bool IsScoreChange => Math.Abs((OldOverallScore ?? 0) - (NewOverallScore ?? 0)) > 0.0001 || OldOverallScore.HasValue != NewOverallScore.HasValue;
        public bool IsMeetingStatusChange => (OldMeetingStatus ?? -1) != (NewMeetingStatus ?? -1);
        public bool IsCommentChange => (OldInterviewerComment ?? string.Empty) != (NewInterviewerComment ?? string.Empty);
        /// <summary>True when the edit was made by the interview's own interviewer, false for internal staff (Admin / QC / support).</summary>
        public bool IsByInterviewer => !string.IsNullOrEmpty(Interviewer) && !string.IsNullOrEmpty(PerformedBy)
            && string.Equals(Interviewer.Trim(), PerformedBy.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public enum InterviewReportEditStatus
    {
        /// <summary>IRF flag off: legacy rules, nothing enforced on the server.</summary>
        NotEnforced = 0,
        SubmitAllowed = 1,
        SubmitWindowExpired = 2,
        EditAllowed = 3,
        EditWindowExpired = 4,
        EditLimitReached = 5
    }

    /// <summary>
    /// Interviewer report edit window rules, shared by the controller (server-side enforcement) and the
    /// My Interviews views (Feedback Report button), so both always agree.
    /// </summary>
    public static class InterviewReportEditState
    {
        public const int DefaultSubmitWindowMinutes = 30;
        public const int DefaultEditWindowMinutes = 30;
        public const int DefaultExpireTimeHours = 168;
        public const int DefaultInterviewDurationMinutes = 60;
        public const int MaxInterviewerEdits = 1;
        public const int MinEditReasonLength = 20;
        public const int MinStrongEditReasonLength = 100;
        public const double StrongReasonScoreChange = 1;

        public static int ParseOrDefault(string value, int fallback)
        {
            return int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
        }

        public static int GetDurationMinutes(int? durationMinutes)
        {
            return durationMinutes.HasValue && durationMinutes.Value > 0 ? durationMinutes.Value : DefaultInterviewDurationMinutes;
        }

        public static int GetDurationMinutes(string durationMinutes)
        {
            return GetDurationMinutes(int.TryParse(durationMinutes, out var parsed) ? parsed : (int?)null);
        }

        public static InterviewReportEditStatus Evaluate(bool? enableReportEditWindow, DateTime interviewDate, int durationMinutes,
            DateTime? reportSubmittedOn, int interviewerEditCount, int submitWindowMinutes, int editWindowMinutes, DateTime now)
        {
            if (enableReportEditWindow != true)
            {
                return InterviewReportEditStatus.NotEnforced;
            }
            if (!reportSubmittedOn.HasValue)
            {
                var submitDeadline = interviewDate.AddMinutes(durationMinutes).AddMinutes(submitWindowMinutes);
                return now <= submitDeadline ? InterviewReportEditStatus.SubmitAllowed : InterviewReportEditStatus.SubmitWindowExpired;
            }
            if (interviewerEditCount >= MaxInterviewerEdits)
            {
                return InterviewReportEditStatus.EditLimitReached;
            }
            return now <= reportSubmittedOn.Value.AddMinutes(editWindowMinutes)
                ? InterviewReportEditStatus.EditAllowed
                : InterviewReportEditStatus.EditWindowExpired;
        }

        public static InterviewReportEditStatus Evaluate(ReportEditWindowInfo info, int submitWindowMinutes, int editWindowMinutes, DateTime now)
        {
            return Evaluate(info.EnableReportEditWindow, info.InterviewDate, GetDurationMinutes(info.InterviewDuration),
                info.ReportSubmittedOn, info.InterviewerEditCount, submitWindowMinutes, editWindowMinutes, now);
        }

        /// <summary>
        /// Feedback Report button on My Interviews. Flag on: from interview start while the submit window or the
        /// single edit is still open. Flag off: legacy interview start + ExpireTime hours.
        /// </summary>
        public static bool IsFeedbackReportButtonVisible(bool? enableReportEditWindow, DateTime interviewDate, string durationMinutes,
            DateTime? reportSubmittedOn, int interviewerEditCount, int submitWindowMinutes, int editWindowMinutes, int expireTimeHours, DateTime now)
        {
            if (interviewDate > now)
            {
                return false;
            }
            var status = Evaluate(enableReportEditWindow, interviewDate, GetDurationMinutes(durationMinutes), reportSubmittedOn,
                interviewerEditCount, submitWindowMinutes, editWindowMinutes, now);
            if (status == InterviewReportEditStatus.NotEnforced)
            {
                return now <= interviewDate.AddHours(expireTimeHours);
            }
            return status == InterviewReportEditStatus.SubmitAllowed || status == InterviewReportEditStatus.EditAllowed;
        }

        public static bool IsBlocked(InterviewReportEditStatus status)
        {
            return status == InterviewReportEditStatus.SubmitWindowExpired
                || status == InterviewReportEditStatus.EditWindowExpired
                || status == InterviewReportEditStatus.EditLimitReached;
        }

        public static string GetBlockedMessage(InterviewReportEditStatus status, int submitWindowMinutes, int editWindowMinutes)
        {
            switch (status)
            {
                case InterviewReportEditStatus.SubmitWindowExpired:
                    return "The feedback report window has expired. Reports must be submitted within " + submitWindowMinutes + " minutes of the scheduled interview end. Please contact Risebird support.";
                case InterviewReportEditStatus.EditWindowExpired:
                    return "This report is locked. Reports can be edited only within " + editWindowMinutes + " minutes of first submission. Please contact Risebird support for any correction.";
                case InterviewReportEditStatus.EditLimitReached:
                    return "This report is locked: only one edit is allowed after submission. Please contact Risebird support for any further correction.";
                default:
                    return string.Empty;
            }
        }
    }
}
