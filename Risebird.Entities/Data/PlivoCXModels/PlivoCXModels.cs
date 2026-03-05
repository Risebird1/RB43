using Newtonsoft.Json;
using System.Collections.Generic;

namespace Risebird.Entities.Data.PlivoCXModels
{
    // ─── Trigger request (our server → Plivo CX) ────────────────────────────────

    /// <summary>
    /// Variables posted to the Plivo CX "API Request" trigger endpoint.
    /// These become flow variables inside the agent and are injected into
    /// the AI Conversation node's instructions at runtime.
    /// </summary>
    public class PlivoCXTriggerRequest
    {
        [JsonProperty("phone_number")]
        public string PhoneNumber     { get; set; }

        [JsonProperty("candidate_name")]
        public string CandidateName   { get; set; }

        [JsonProperty("candidate_email")]
        public string CandidateEmail  { get; set; }

        [JsonProperty("irf_id")]
        public string IrfId           { get; set; }

        [JsonProperty("job_title")]
        public string JobTitle        { get; set; }

        [JsonProperty("client_name")]
        public string ClientName      { get; set; }

        /// <summary>Our internal DB log id — returned in the hangup webhook so we can match the row</summary>
        [JsonProperty("log_id")]
        public string LogId           { get; set; }
    }

    /// <summary>
    /// Response from the Plivo CX trigger API
    /// </summary>
    public class PlivoCXTriggerResponse
    {
        [JsonProperty("api_id")]
        public string ApiId           { get; set; }

        [JsonProperty("conversation_id")]
        public string ConversationId  { get; set; }

        [JsonProperty("message")]
        public string Message         { get; set; }

        [JsonProperty("error")]
        public string Error           { get; set; }
    }

    // ─── Hangup webhook (Plivo CX → our server) ─────────────────────────────────

    /// <summary>
    /// Payload posted to our Hangup URL when a Plivo CX call ends.
    /// Plivo CX sends all system variables plus any variables extracted by the
    /// AI Conversation node's "Extract Variables" tab.
    /// </summary>
    public class PlivoCXHangupPayload
    {
        // ── System variables ──────────────────────────────────────────────────
        [JsonProperty("conversation_id")]
        public string ConversationId      { get; set; }

        [JsonProperty("call_uuid")]
        public string CallUUID            { get; set; }

        [JsonProperty("call_status")]
        public string CallStatus          { get; set; }

        [JsonProperty("duration")]
        public string Duration            { get; set; }

        [JsonProperty("hangup_cause")]
        public string HangupCause         { get; set; }

        [JsonProperty("transcript")]
        public string Transcript          { get; set; }

        [JsonProperty("summary")]
        public string Summary             { get; set; }

        // ── Variables we passed to the trigger (echoed back) ─────────────────
        [JsonProperty("phone_number")]
        public string PhoneNumber         { get; set; }

        [JsonProperty("candidate_name")]
        public string CandidateName       { get; set; }

        [JsonProperty("candidate_email")]
        public string CandidateEmail      { get; set; }

        [JsonProperty("irf_id")]
        public string IrfId               { get; set; }

        [JsonProperty("job_title")]
        public string JobTitle            { get; set; }

        [JsonProperty("client_name")]
        public string ClientName          { get; set; }

        [JsonProperty("log_id")]
        public string LogId               { get; set; }

        // ── Variables extracted by AI Conversation node ───────────────────────
        [JsonProperty("interview_date")]
        public string InterviewDate       { get; set; }

        [JsonProperty("interview_time")]
        public string InterviewTime       { get; set; }

        [JsonProperty("candidate_confirmed")]
        public string CandidateConfirmed  { get; set; }   // "true" | "false"

        [JsonProperty("confirmed_candidate_name")]
        public string ConfirmedCandidateName { get; set; }
    }

    /// <summary>
    /// Context object passed internally (timer function → service)
    /// </summary>
    public class PlivoCXCallContext
    {
        public int    CandidateId    { get; set; }
        public string RecipientName  { get; set; }
        public string RecipientPhone { get; set; }
        public string RecipientEmail { get; set; }
        public int?   IRFId          { get; set; }
        public string JobTitle       { get; set; }
        public string ClientName     { get; set; }
        public string TriggeredBy    { get; set; }
    }
}
