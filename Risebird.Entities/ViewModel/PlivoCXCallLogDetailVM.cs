using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Flat projection of PlivoCXCallLogs + IRF + InterviewMeetings used by
    /// the PlivoCXInterviewCall/Index view.
    /// </summary>
    public class PlivoCXCallLogDetailVM
    {
        // ── Pagination / aggregate metadata (populated by window functions) ───
        /// <summary>Total rows across all pages — read from the first row to drive pagination.</summary>
        public int TotalCount            { get; set; }
        /// <summary>Overall count of "completed" calls across all pages.</summary>
        public int OverallCompleted      { get; set; }
        /// <summary>Overall count of calls that produced an InterviewMeeting across all pages.</summary>
        public int OverallMeetingCreated { get; set; }
        /// <summary>Overall count of failed / no-answer calls across all pages.</summary>
        public int OverallNoAnswer       { get; set; }

        // ── Call identity ─────────────────────────────────────────────────────
        public int     CallLogId         { get; set; }
        public string  CandidateEmail    { get; set; }
        public string  CandidateName     { get; set; }
        public string  RecipientPhone    { get; set; }
        public string  IRFId             { get; set; }   // VARCHAR business key from IRF
        public string  JDTiltle          { get; set; }
        public string  ClientName        { get; set; }
        public string  TriggeredBy       { get; set; }
        public string  PlivoCXConversationId { get; set; }
        public string  CallPurpose       { get; set; }   // 'schedule' | 'reschedule'
        public string  CandidateDisposition { get; set; }

        // ── Call outcome ──────────────────────────────────────────────────────
        public string   CallStatus       { get; set; }
        public int?     DurationSeconds  { get; set; }
        public string   HangupCause      { get; set; }
        public DateTime CallInitiatedAt  { get; set; }
        public DateTime? CallEndedAt     { get; set; }

        // ── What the AI extracted ─────────────────────────────────────────────
        public bool     AIReportedBooked      { get; set; }
        public string   BookedInterviewDate   { get; set; }
        public string   BookedInterviewTime   { get; set; }
        public DateTime? BookedInterviewDateTime { get; set; }
        public bool     BookingApiSuccess     { get; set; }
        public string   BookingApiResponse    { get; set; }

        // ── Transcript / summary (for detail panel) ───────────────────────────
        public string   CallTranscript        { get; set; }
        public string   ConversationSummary   { get; set; }
        public string   RecordingUrl          { get; set; }   // Plivo recording (NULL until captured)

        // ── Did the call land as an InterviewMeeting? ─────────────────────────
        public string   MeetingCreated        { get; set; }   // 'YES' | 'NO'
        public int?     MeetingId             { get; set; }
        public DateTime? MeetingScheduledAt   { get; set; }
        public string   InterviewSlot         { get; set; }
        public int?     InterviewDuration     { get; set; }
        public string   MeetingConfirmationStatus { get; set; }
        public string   Interviewer           { get; set; }
        public string   RisebirdHR            { get; set; }
        public string   ClientHrContact       { get; set; }
        public bool?    IsAutoCancelled       { get; set; }
        public string   CancellationReason    { get; set; }
        public bool?    IsCandidateResponsed  { get; set; }
        public string   CandidateResponse     { get; set; }
        public DateTime? MeetingCreatedOn     { get; set; }
        /// <summary>
        /// Matches InterviewMeetings.RequestType — 'schedule_voiceAgent' | 'reschedule_voiceAgent' | null.
        /// Only populated when the meeting was created via the Plivo CX voice agent.
        /// </summary>
        public string   MeetingRequestType    { get; set; }
    }
}
