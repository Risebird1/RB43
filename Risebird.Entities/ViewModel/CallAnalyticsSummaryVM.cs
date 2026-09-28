using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Per-caller aggregate of CallRecordingData rows, used by the Call Analytics dashboard.
    /// </summary>
    public class CallAnalyticsSummaryVM
    {
        public string Caller { get; set; }
        public string CallerProfileType { get; set; } // caller's Profile.Name (HR, SeniorHR, InterviewerManagement, ...)
        public int TotalCalls { get; set; }
        public int CandidateCalls { get; set; }
        public int InterviewerCalls { get; set; }
        public int OtherCalls { get; set; }
        public int CallsWithRecording { get; set; }
        public int TotalDurationSeconds { get; set; }
        public DateTime? LastCallOn { get; set; }

        // Productivity metrics (ActivityTracker, keyed on PerformedBy)
        public int Reschedules { get; set; }           // UpdateMeeting(3) + "Interview Date changed"
        public int MeetingsConfirmed { get; set; }     // UpdateMeeting(3) + "Meeting Confirmed"
        public int NewSchedules { get; set; }          // ScheduleMeeting(14) - meetings booked via Add Meeting
        public int InterviewersOnboarded { get; set; } // UpdateInterviewer(13) + "Add Interviewer"
        public int QCDone { get; set; }                // QCPassed(7)/QCFailed(8)/QCDone(12), excluding "Payment SLA"

        public string TotalDurationDisplay => string.Format("{0}:{1:00}:{2:00}",
            (int)TimeSpan.FromSeconds(TotalDurationSeconds).TotalHours,
            TimeSpan.FromSeconds(TotalDurationSeconds).Minutes,
            TimeSpan.FromSeconds(TotalDurationSeconds).Seconds);
    }

    /// <summary>
    /// Per-caller-role aggregate of CallRecordingData rows, used by Call Analytics "By role".
    /// </summary>
    public class CallRoleRecipientSummaryVM
    {
        public string CallerProfileType { get; set; }
        public int People { get; set; }
        public int TotalCalls { get; set; }
        public int CandidateCalls { get; set; }
        public int InterviewerCalls { get; set; }
        public int OtherCalls { get; set; }
        public int CallsWithRecording { get; set; }
        public int TotalDurationSeconds { get; set; }
        public DateTime? LastCallOn { get; set; }
    }

    /// <summary>
    /// One row of usp_GetCandidateOutreachConversion: HR / Senior HR candidate calls for one caller and
    /// outreach type, with how many were connected and how many were followed by a booking.
    /// </summary>
    public class CandidateOutreachConversionVM
    {
        public string Caller { get; set; }
        public string CallerProfileType { get; set; }
        public string OutreachType { get; set; } // "Reschedule" | "NewSchedule"
        public int CallsPlaced { get; set; }
        public int CallsConnected { get; set; }
        public int Converted { get; set; }
    }
}
