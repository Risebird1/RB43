using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Flat projection of PlivoWhatsAppLogs + IRF + InterviewMeetings used by
    /// the PlivoWhatsAppInterview/Index view.
    /// Mirrors PlivoCXCallLogDetailVM in structure (same channel, different transport).
    /// </summary>
    public class PlivoWhatsAppLogDetailVM
    {
        // ── Pagination / aggregate metadata (window functions) ─────────────────
        public int TotalCount            { get; set; }
        public int OverallReplied        { get; set; }
        public int OverallMeetingCreated { get; set; }
        public int OverallFailed         { get; set; }

        // ── Message identity ───────────────────────────────────────────────────
        public int      LogId                   { get; set; }
        public string   CandidateEmail          { get; set; }
        public string   CandidateName           { get; set; }
        public string   RecipientPhone          { get; set; }
        public string   IRFId                   { get; set; }
        public string   JDTiltle                { get; set; }
        public string   ClientName              { get; set; }
        public string   TriggeredBy             { get; set; }
        public string   MessagePurpose          { get; set; }
        public string   CandidateDisposition    { get; set; }
        public string   OriginalInterviewDate   { get; set; }
        public string   PlivoCXConversationId   { get; set; }

        // ── Conversation status ────────────────────────────────────────────────
        public string   MessageStatus           { get; set; }
        /// <summary>initiated | triggered | trigger-failed | completed</summary>
        public string   Status                  { get; set; }
        public DateTime MessageSentAt           { get; set; }
        public DateTime? ConversationEndedAt    { get; set; }
        public DateTime? UpdatedAt              { get; set; }

        // ── AI-extracted outputs ───────────────────────────────────────────────
        public string   ConversationTranscript  { get; set; }
        public string   ConversationSummary     { get; set; }
        public string   AIReportedInterviewDate { get; set; }
        public string   AIReportedInterviewTime { get; set; }
        public bool     AIReportedBooked        { get; set; }

        // ── Booking outcome ────────────────────────────────────────────────────
        public bool     InterviewBooked         { get; set; }
        public bool     BookingApiSuccess       { get; set; }
        public string   BookingApiResponse      { get; set; }
        public string   BookedInterviewDate     { get; set; }
        public string   BookedInterviewTime     { get; set; }
        public DateTime? BookedInterviewDateTime { get; set; }

        // ── InterviewMeeting ───────────────────────────────────────────────────
        public string   MeetingCreated              { get; set; }
        public int?     MeetingId                   { get; set; }
        public DateTime? MeetingScheduledAt         { get; set; }
        public string   InterviewSlot               { get; set; }
        public int?     InterviewDuration            { get; set; }
        public string   MeetingConfirmationStatus   { get; set; }
        public string   Interviewer                 { get; set; }
        public string   RisebirdHR                  { get; set; }
        public string   ClientHrContact             { get; set; }
        public bool?    IsAutoCancelled             { get; set; }
        public string   CancellationReason          { get; set; }
        public bool?    IsCandidateResponsed        { get; set; }
        public string   CandidateResponse           { get; set; }
        public DateTime? MeetingCreatedOn           { get; set; }
    }
}
