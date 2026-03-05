using System;

namespace Risebird.Entities.Data.PlivoCXModels
{
    /// <summary>
    /// Tracks every outbound interview call made via Plivo CX AI Agent platform.
    /// One row per call attempt.
    /// </summary>
    public class PlivoCXCallLog
    {
        public int    Id                         { get; set; }

        // ── Plivo CX identifiers ────────────────────────────────────────────────
        /// <summary>Conversation / flow-run ID returned by the Plivo CX trigger API</summary>
        public string PlivoCXConversationId      { get; set; }

        /// <summary>API request ID from the trigger response</summary>
        public string PlivoCXRequestId           { get; set; }

        // ── Candidate context (passed as variables to the CX trigger) ───────────
        public string RecipientName              { get; set; }
        public string RecipientPhone             { get; set; }
        public string RecipientEmail             { get; set; }
        public int?   IRFId                      { get; set; }
        public int?   CandidateId                { get; set; }
        public string JobTitle                   { get; set; }
        public string ClientName                 { get; set; }

        // ── Call outcome ─────────────────────────────────────────────────────────
        public string CallStatus                 { get; set; }   // e.g. answered, no-answer, failed
        public int?   DurationSeconds            { get; set; }
        public string HangupCause                { get; set; }
        public string CallTranscript             { get; set; }
        public string ConversationSummary        { get; set; }

        // ── Interview booking (extracted by AI Conversation node) ────────────────
        public bool   InterviewBooked            { get; set; }
        public string BookedInterviewDate        { get; set; }   // raw value extracted by AI
        public string BookedInterviewTime        { get; set; }   // raw value extracted by AI
        public DateTime? BookedInterviewDateTime { get; set; }   // parsed & normalised
        public string BookingApiResponse         { get; set; }   // raw response from Schedule API
        public bool   BookingApiSuccess          { get; set; }

        // ── Timestamps ───────────────────────────────────────────────────────────
        public DateTime  CreatedAt               { get; set; }
        public DateTime? UpdatedAt               { get; set; }
        public DateTime? CallStartedAt           { get; set; }
        public DateTime? CallEndedAt             { get; set; }
        public string    TriggeredBy             { get; set; }   // "timer" | user email
    }
}
