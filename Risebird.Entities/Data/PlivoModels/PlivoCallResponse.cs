using Newtonsoft.Json;

namespace Risebird.Entities.Data.PlivoModels
{
    /// <summary>
    /// Response from POST /Account/{auth_id}/Call/ — call is "fired" but not yet answered
    /// </summary>
    public class PlivoOutboundCallResponse
    {
        [JsonProperty("api_id")]
        public string ApiId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Unique ID for the call request — NOT the same as CallUUID.
        /// CallUUID is only available once the call is answered (via answer_url callback).
        /// </summary>
        [JsonProperty("request_uuid")]
        public string RequestUUID { get; set; }
    }

    /// <summary>
    /// Plivo CDR object returned by GET /Account/{auth_id}/Call/{call_uuid}/
    /// </summary>
    public class PlivoCallDetails
    {
        [JsonProperty("api_id")]
        public string ApiId { get; set; }

        [JsonProperty("call_uuid")]
        public string CallUUID { get; set; }

        [JsonProperty("from_number")]
        public string FromNumber { get; set; }

        [JsonProperty("to_number")]
        public string ToNumber { get; set; }

        [JsonProperty("call_direction")]
        public string CallDirection { get; set; }

        [JsonProperty("call_status")]
        public string CallStatus { get; set; }

        [JsonProperty("answer_time")]
        public string AnswerTime { get; set; }

        [JsonProperty("end_time")]
        public string EndTime { get; set; }

        [JsonProperty("initiation_time")]
        public string InitiationTime { get; set; }

        [JsonProperty("call_duration")]
        public int? CallDuration { get; set; }

        [JsonProperty("bill_duration")]
        public int? BillDuration { get; set; }

        [JsonProperty("total_amount")]
        public string TotalAmount { get; set; }

        [JsonProperty("total_rate")]
        public string TotalRate { get; set; }

        [JsonProperty("hangup_cause_name")]
        public string HangupCauseName { get; set; }
    }

    /// <summary>
    /// Hangup callback parameters POSTed by Plivo to hangup_url
    /// </summary>
    public class PlivoHangupPayload
    {
        public string CallUUID { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string CallStatus { get; set; }
        public string Duration { get; set; }
        public string HangupCause { get; set; }
        public string HangupSource { get; set; }
        public string BillDuration { get; set; }
        public string TotalAmount { get; set; }
        public string Direction { get; set; }
        public string RequestUUID { get; set; }
    }

    /// <summary>
    /// Answer URL callback parameters sent by Plivo when the call is answered
    /// </summary>
    public class PlivoAnswerPayload
    {
        public string CallUUID { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string CallStatus { get; set; }
        public string Direction { get; set; }
        public string RequestUUID { get; set; }
        public string ALegUUID { get; set; }
        public string ALegRequestUUID { get; set; }
    }

    /// <summary>
    /// Ring URL callback parameters
    /// </summary>
    public class PlivoRingPayload
    {
        public string CallUUID { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string CallStatus { get; set; }
        public string RequestUUID { get; set; }
    }
}
