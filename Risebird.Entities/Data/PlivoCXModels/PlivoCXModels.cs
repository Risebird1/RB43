using Newtonsoft.Json;
using System.Collections.Generic;

namespace Risebird.Entities.Data.PlivoCXModels
{
    // ─── Nested hangup event (actual Plivo CX webhook shape) ────────────────────
    //
    // Plivo CX POSTs application/json with this structure:
    //
    //  {
    //    "created_at": "2026-03-06T08:31:20Z",
    //    "data": {
    //      "object": {
    //        "call_uuid": "...",
    //        "conversation_id": "1651-2261780595",
    //        "conversation_url": "https://cx.plivo.com/...",
    //        "event_data": {
    //          "Initiate Interview Call.call_status": "completed",
    //          "AI Conversation.interview_date":      "tomorrow",
    //          "AI Conversation.interview_time":      "10:00 AM",
    //          "API Request.log_id":                  "6",
    //          ...
    //        }
    //      }
    //    }
    //  }
    //
    // The event_data keys are prefixed with the node name ("NodeName.variable").
    // Use GetEventDataValue() in the controller to look up by suffix.

    public class PlivoCXHangupEvent
    {
        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("data")]
        public PlivoCXHangupEventData Data { get; set; }
    }

    public class PlivoCXHangupEventData
    {
        [JsonProperty("object")]
        public PlivoCXHangupEventObject Object { get; set; }
    }

    public class PlivoCXHangupEventObject
    {
        [JsonProperty("call_uuid")]
        public string CallUuid { get; set; }

        [JsonProperty("conversation_id")]
        public string ConversationId { get; set; }

        [JsonProperty("conversation_url")]
        public string ConversationUrl { get; set; }

        /// <summary>
        /// All node-scoped variables, keyed as "NodeName.variable_name".
        /// Use GetEventDataValue() to retrieve by variable name ignoring the node prefix.
        /// </summary>
        [JsonProperty("event_data")]
        public Dictionary<string, string> EventData { get; set; }
    }


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

        // ── Date context variables ─────────────────────────────────────────────
        // Injected so the AI agent uses real calendar dates instead of relative
        // words. Reference these in the AI Conversation node instructions as
        // {{today_date}}, {{tomorrow_date}}, {{day_after_tomorrow_date}}.

        /// <summary>e.g. "Friday, 06 March 2026"</summary>
        [JsonProperty("today_date")]
        public string TodayDate           { get; set; }

        /// <summary>e.g. "Saturday, 07 March 2026"</summary>
        [JsonProperty("tomorrow_date")]
        public string TomorrowDate        { get; set; }

        /// <summary>e.g. "Sunday, 08 March 2026"</summary>
        [JsonProperty("day_after_tomorrow_date")]
        public string DayAfterTomorrowDate { get; set; }
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

        /// <summary>Full raw JSON body returned by Plivo CX — for debug logging</summary>
        [JsonIgnore]
        public string RawResponseBody { get; set; }

        /// <summary>HTTP status code from the trigger POST</summary>
        [JsonIgnore]
        public int HttpStatusCode     { get; set; }
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

        /// <summary>
        /// AI-assessed candidate disposition based on the conversation.
        /// Values: "interested" | "uninterested" | "cant_say"
        /// </summary>
        [JsonProperty("candidate_disposition")]
        public string CandidateDisposition { get; set; }
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
