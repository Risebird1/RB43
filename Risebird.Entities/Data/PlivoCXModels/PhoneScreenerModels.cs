using Newtonsoft.Json;

namespace Risebird.Entities.Data.PlivoCXModels
{
    /// <summary>
    /// Payload POSTed to a skill channel's Plivo CX Phone Screener flow trigger URL
    /// to start an outbound pre-screening call. Field names match the flow's
    /// Start node payload_format (snake_case).
    /// </summary>
    public class PhoneScreenTriggerRequest
    {
        [JsonProperty("caller_id")]
        public string CallerId { get; set; }

        [JsonProperty("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonProperty("candidate_name")]
        public string CandidateName { get; set; }

        [JsonProperty("candidate_email")]
        public string CandidateEmail { get; set; }

        [JsonProperty("job_title")]
        public string JobTitle { get; set; }

        [JsonProperty("log_id")]
        public string LogId { get; set; }

        [JsonProperty("today_date")]
        public string TodayDate { get; set; }
    }

    /// <summary>
    /// Recording + transcript resolved out-of-band from Plivo's REST APIs
    /// (Voice Recording API + Transcription API) keyed by the call UUID.
    /// Used as a fallback when the Recording URL event callback does not deliver
    /// the media to our webhook (e.g. recording processed after hangup, or the
    /// event payload shape differs from what the webhook parsed).
    /// </summary>
    public class PhoneScreenMediaResult
    {
        public string RecordingUrl { get; set; }
        public string RecordingId { get; set; }
        public string Transcript { get; set; }
        public int? DurationSeconds { get; set; }
        public bool Found => !string.IsNullOrWhiteSpace(RecordingUrl) || !string.IsNullOrWhiteSpace(Transcript);
        public string Error { get; set; }
    }

    /// <summary>
    /// Flat JSON body the Plivo CX Phone Screener agent (and its failure-branch
    /// HTTP nodes) POST back to /PhoneScreener/Webhook when a call concludes.
    /// Every field is optional; the webhook degrades gracefully on partial data.
    /// </summary>
    public class PhoneScreenHangupPayload
    {
        [JsonProperty("log_id")]
        public string LogId { get; set; }

        [JsonProperty("conversation_id")]
        public string ConversationId { get; set; }

        [JsonProperty("call_uuid")]
        public string CallUUID { get; set; }

        [JsonProperty("call_status")]
        public string CallStatus { get; set; }

        [JsonProperty("ended_reason")]
        public string EndedReason { get; set; }

        [JsonProperty("duration")]
        public string Duration { get; set; }

        [JsonProperty("recording_url")]
        public string RecordingUrl { get; set; }

        [JsonProperty("transcript")]
        public string Transcript { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        // Screener extracted variables
        [JsonProperty("qualified")]
        public string Qualified { get; set; }

        [JsonProperty("failed_criteria")]
        public string FailedCriteria { get; set; }

        [JsonProperty("disposition")]
        public string Disposition { get; set; }

        [JsonProperty("degree")]
        public string Degree { get; set; }

        [JsonProperty("grad_year")]
        public string GradYear { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("joining")]
        public string Joining { get; set; }

        [JsonProperty("laptop")]
        public string Laptop { get; set; }

        [JsonProperty("comp_comfort")]
        public string CompComfort { get; set; }

        [JsonProperty("availability")]
        public string Availability { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }

        // Candidate echo-back (from trigger context)
        [JsonProperty("candidate_name")]
        public string CandidateName { get; set; }

        [JsonProperty("candidate_email")]
        public string CandidateEmail { get; set; }

        [JsonProperty("phone_number")]
        public string PhoneNumber { get; set; }
    }
}
