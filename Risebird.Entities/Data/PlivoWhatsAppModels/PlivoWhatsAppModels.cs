using Newtonsoft.Json;
using System.Collections.Generic;

namespace Risebird.Entities.Data.PlivoWhatsAppModels
{
    // ── Outbound send request ──────────────────────────────────────────────────

    /// <summary>
    /// Payload sent to Plivo Messaging API to send a WhatsApp template message.
    /// POST https://api.plivo.com/v1/Account/{AUTH_ID}/Message/
    /// </summary>
    public class PlivoWhatsAppSendRequest
    {
        [JsonProperty("src")]
        public string Src  { get; set; }   // your Plivo WhatsApp-enabled number

        [JsonProperty("dst")]
        public string Dst  { get; set; }   // candidate's phone number

        [JsonProperty("type")]
        public string Type { get; set; } = "whatsapp";

        /// <summary>For template (business-initiated) messages.</summary>
        [JsonProperty("template", NullValueHandling = NullValueHandling.Ignore)]
        public PlivoWhatsAppTemplate Template { get; set; }

        /// <summary>For session (reply-within-24h) messages. Used for fallback clarifications.</summary>
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }
    }

    public class PlivoWhatsAppTemplate
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; } = "en";

        [JsonProperty("components")]
        public List<PlivoWhatsAppTemplateComponent> Components { get; set; }
            = new List<PlivoWhatsAppTemplateComponent>();
    }

    public class PlivoWhatsAppTemplateComponent
    {
        [JsonProperty("type")]
        public string Type { get; set; }   // "body" | "header" | "button"

        [JsonProperty("parameters")]
        public List<PlivoWhatsAppTemplateParameter> Parameters { get; set; }
            = new List<PlivoWhatsAppTemplateParameter>();
    }

    public class PlivoWhatsAppTemplateParameter
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "text";

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    // ── Plivo CX trigger response (same shape as PlivoCXTriggerResponse) ─────────

    public class PlivoWhatsAppSendResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("api_id")]
        public string ApiId { get; set; }

        /// <summary>Plivo CX conversation ID — used to correlate the ConversationEndCallback.</summary>
        [JsonProperty("conversation_id")]
        public string ConversationId { get; set; }

        // Populated by PlivoWhatsAppService (not from Plivo JSON)
        public string Error           { get; set; }
        public int    HttpStatusCode  { get; set; }
        public string RawResponseBody { get; set; }
    }

    // ── Inbound message webhook ────────────────────────────────────────────────

    /// <summary>
    /// Plivo posts this to our InboundMessage endpoint when a candidate replies on WhatsApp.
    /// Configure as "Message URL" in Plivo console → Messaging → Your WhatsApp number.
    /// </summary>
    public class PlivoWhatsAppInboundPayload
    {
        [JsonProperty("From")]
        public string From            { get; set; }   // candidate's number

        [JsonProperty("To")]
        public string To              { get; set; }   // your Plivo number

        [JsonProperty("Text")]
        public string Text            { get; set; }   // candidate's reply text

        [JsonProperty("Type")]
        public string Type            { get; set; }   // "whatsapp"

        [JsonProperty("MessageUUID")]
        public string MessageUUID     { get; set; }

        [JsonProperty("Timestamp")]
        public string Timestamp       { get; set; }
    }

    // ── Delivery status webhook ────────────────────────────────────────────────

    /// <summary>
    /// Plivo posts this to our StatusCallback endpoint on each status change.
    /// Configure as "Delivery URL" in Plivo console → Messaging → Your WhatsApp number.
    /// </summary>
    public class PlivoWhatsAppStatusPayload
    {
        [JsonProperty("MessageUUID")]
        public string MessageUUID  { get; set; }

        [JsonProperty("Status")]
        public string Status       { get; set; }   // queued | sent | delivered | read | undelivered | failed

        [JsonProperty("From")]
        public string From         { get; set; }

        [JsonProperty("To")]
        public string To           { get; set; }

        [JsonProperty("ErrorCode")]
        public string ErrorCode    { get; set; }
    }

    // ── Internal trigger context ───────────────────────────────────────────────

    /// <summary>
    /// Internal DTO carrying the context needed to trigger one Plivo CX WhatsApp conversation.
    /// Variable names match the AI agent flow's expected inputs (same pattern as PlivoCXTriggerRequest).
    /// </summary>
    public class PlivoWhatsAppTriggerContext
    {
        public string   CandidateName         { get; set; }
        public string   CandidatePhone        { get; set; }
        public string   CandidateEmail        { get; set; }
        public int      IrfId                 { get; set; }
        public int?     CandidateId           { get; set; }
        public string   JobTitle              { get; set; }
        public string   ClientName            { get; set; }
        public string   MessagePurpose        { get; set; } = "schedule";
        public string   OriginalInterviewDate { get; set; }
        public string   TriggeredBy           { get; set; }
        public string   LogId                 { get; set; }   // DB PK passed to AI as variable
        public string   TodayDate             { get; set; }
        public string   TomorrowDateLabel     { get; set; }   // e.g. "Tuesday, 25 March 2026"
        public string   DayAfterDateLabel     { get; set; }
        // Stored on the log so the controller knows what dates were presented
        public System.DateTime Option1DateTime { get; set; }
        public System.DateTime Option2DateTime { get; set; }
    }
}
