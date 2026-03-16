using System;

namespace Risebird.Entities.Data.PlivoWhatsAppModels
{
    /// <summary>
    /// Database entity for PlivoWhatsAppLogs.
    /// Tracks each outbound Plivo CX WhatsApp AI conversation and its booking outcome.
    /// Mirrors PlivoCXCallLog in structure — same flow, different channel (WhatsApp vs. voice).
    /// </summary>
    public class PlivoWhatsAppLog
    {
        public int      Id                      { get; set; }

        // ── Candidate context ──────────────────────────────────────────────────
        public string   RecipientName           { get; set; }
        public string   RecipientPhone          { get; set; }
        public string   RecipientEmail          { get; set; }
        public int?     IRFId                   { get; set; }
        public int?     CandidateId             { get; set; }
        public string   JobTitle                { get; set; }
        public string   ClientName              { get; set; }
        /// <summary>'schedule' | 'reschedule'</summary>
        public string   MessagePurpose          { get; set; } = "schedule";
        public string   OriginalInterviewDate   { get; set; }

        // ── Plivo CX conversation ──────────────────────────────────────────────
        public string   PlivoCXConversationId   { get; set; }
        public string   PlivoCXRequestId        { get; set; }
        /// <summary>initiated | triggered | trigger-failed | completed</summary>
        public string   MessageStatus           { get; set; } = "initiated";

        // ── AI-extracted conversation output ───────────────────────────────────
        public string   ConversationTranscript  { get; set; }
        public string   ConversationSummary     { get; set; }
        /// <summary>AI-reported interview date (raw string, may be relative)</summary>
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

        // ── Overall status and disposition ────────────────────────────────────
        /// <summary>initiated | triggered | trigger-failed | completed</summary>
        public string   Status                  { get; set; } = "initiated";
        /// <summary>interested | uninterested | cant_say</summary>
        public string   CandidateDisposition    { get; set; }
        public DateTime? ConversationEndedAt    { get; set; }

        // ── Audit ─────────────────────────────────────────────────────────────
        public string   TriggeredBy             { get; set; }
        public DateTime CreatedAt               { get; set; }
        public DateTime? UpdatedAt              { get; set; }
    }
}
