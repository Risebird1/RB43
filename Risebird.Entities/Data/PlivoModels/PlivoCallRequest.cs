using System.Collections.Generic;
using Newtonsoft.Json;

namespace Risebird.Entities.Data.PlivoModels
{
    /// <summary>
    /// Request payload for Plivo outbound call REST API
    /// POST https://api.plivo.com/v1/Account/{auth_id}/Call/
    /// </summary>
    public class PlivoOutboundCallRequest
    {
        /// <summary>E.164 destination number (candidate phone)</summary>
        [JsonProperty("to")]
        public string To { get; set; }

        /// <summary>E.164 Plivo caller ID (your purchased Indian number)</summary>
        [JsonProperty("from")]
        public string From { get; set; }

        /// <summary>URL Plivo fetches XML from when the call is answered</summary>
        [JsonProperty("answer_url")]
        public string AnswerUrl { get; set; }

        [JsonProperty("answer_method")]
        public string AnswerMethod { get; set; } = "GET";

        /// <summary>URL Plivo POSTs to when the call ends</summary>
        [JsonProperty("hangup_url")]
        public string HangupUrl { get; set; }

        [JsonProperty("hangup_method")]
        public string HangupMethod { get; set; } = "POST";

        /// <summary>URL Plivo POSTs to when the call starts ringing</summary>
        [JsonProperty("ring_url")]
        public string RingUrl { get; set; }

        [JsonProperty("ring_method")]
        public string RingMethod { get; set; } = "POST";

        /// <summary>Machine detection: true to detect answering machines</summary>
        [JsonProperty("machine_detection")]
        public string MachineDetection { get; set; }

        [JsonProperty("machine_detection_url")]
        public string MachineDetectionUrl { get; set; }

        /// <summary>Max call duration in seconds</summary>
        [JsonProperty("time_limit")]
        public int TimeLimit { get; set; } = 300;

        /// <summary>How long to wait (seconds) for the call to be answered before hanging up</summary>
        [JsonProperty("hangup_on_ring")]
        public int? HangupOnRing { get; set; }

        /// <summary>Custom SIP headers forwarded to the leg (key=value pairs)</summary>
        [JsonProperty("sip_headers")]
        public string SipHeaders { get; set; }
    }

    /// <summary>Internal model carrying all candidate context needed to place a Plivo call</summary>
    public class PlivoCallContext
    {
        public int LogId { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidatePhone { get; set; }
        public string JobTitle { get; set; }
        public int? IRFId { get; set; }
        public int? CandidateId { get; set; }
        public int? InterviewId { get; set; }
        public string ClientName { get; set; }
        public string SystemPrompt { get; set; }
    }
}
