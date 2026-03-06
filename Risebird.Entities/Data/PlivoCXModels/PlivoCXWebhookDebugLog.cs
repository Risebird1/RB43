using System;

namespace Risebird.Entities.Data.PlivoCXModels
{
    /// <summary>
    /// Debug log for every Plivo CX trigger request and hangup/event callback.
    /// One row per interaction — used to diagnose call failures.
    /// </summary>
    public class PlivoCXWebhookDebugLog
    {
        public int      Id               { get; set; }
        public DateTime CreatedAt        { get; set; }

        /// <summary>trigger-request | trigger-response | hangup-callback | recording-callback</summary>
        public string   LogType          { get; set; }

        /// <summary>HTTP status code (trigger responses) or 0 (incoming webhooks)</summary>
        public int?     HttpStatusCode   { get; set; }

        /// <summary>Plivo CX api_id / conversation_id if available at the time of logging</summary>
        public string   PlivoCXId        { get; set; }

        /// <summary>Our internal PlivoCXCallLogs.Id — set when known</summary>
        public int?     CallLogId        { get; set; }

        /// <summary>Full JSON body — the payload we sent or received</summary>
        public string   RawPayload       { get; set; }

        /// <summary>success | failed | error</summary>
        public string   ProcessingStatus { get; set; }

        public string   ErrorMessage     { get; set; }
    }
}
