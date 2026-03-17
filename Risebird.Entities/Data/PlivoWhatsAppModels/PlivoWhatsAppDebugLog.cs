using System;

namespace Risebird.Entities.Data.PlivoWhatsAppModels
{
    /// <summary>
    /// Raw payload log for every Plivo CX WhatsApp trigger request/response
    /// and ConversationEndCallback. Maps to PlivoWhatsAppDebugLogs table.
    /// Mirrors PlivoCXWebhookDebugLog but uses LogId (PlivoWhatsAppLogs.Id)
    /// instead of CallLogId.
    /// </summary>
    public class PlivoWhatsAppDebugLog
    {
        public int      Id               { get; set; }
        public DateTime CreatedAt        { get; set; }

        /// <summary>trigger-request | trigger-response | wa-callback</summary>
        public string   LogType          { get; set; }

        /// <summary>HTTP status code (trigger responses) or null (inbound callbacks)</summary>
        public int?     HttpStatusCode   { get; set; }

        /// <summary>Plivo CX api_id / conversation_id if available at log time</summary>
        public string   PlivoCXId        { get; set; }

        /// <summary>FK to PlivoWhatsAppLogs.Id — set when known</summary>
        public int?     LogId            { get; set; }

        /// <summary>Full JSON body sent or received</summary>
        public string   RawPayload       { get; set; }

        /// <summary>success | failed | received | error</summary>
        public string   ProcessingStatus { get; set; }

        public string   ErrorMessage     { get; set; }
    }
}
