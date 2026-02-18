using System;
using Newtonsoft.Json;

namespace Risebird.Entities.Data.VapiModels
{
    /// <summary>
    /// Response from Vapi when call is initiated
    /// </summary>
    public class VapiCallResponse
    {
        public string Id { get; set; }
        public string OrgId { get; set; }
        public string AssistantId { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public VapiCustomer Customer { get; set; }
        public VapiPhoneNumber PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string EndedReason { get; set; }
        public VapiCost Cost { get; set; }
        public VapiArtifact Artifact { get; set; }
        public VapiAnalysis Analysis { get; set; }
    }

    /// <summary>
    /// Customer information in Vapi call
    /// </summary>
    public class VapiCustomer
    {
        [JsonProperty("number")]
        public string Number { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("email")]
        public string Email { get; set; }
    }

    /// <summary>
    /// Phone number details for Vapi calls
    /// </summary>
    public class VapiPhoneNumber
    {
        /// <summary>
        /// Twilio phone number in E.164 format (e.g., +14155551234)
        /// This must be a number you've imported into Vapi
        /// </summary>
        [JsonProperty("twilioPhoneNumber")]
        public string TwilioPhoneNumber { get; set; }
        
        /// <summary>
        /// Twilio Account SID
        /// </summary>
        [JsonProperty("twilioAccountSid")]
        public string TwilioAccountSid { get; set; }
        
        /// <summary>
        /// Response-only: The number (populated in responses)
        /// </summary>
        [JsonProperty("number")]
        public string Number { get; set; }
        
        /// <summary>
        /// Response-only: The provider (populated in responses)
        /// </summary>
        [JsonProperty("provider")]
        public string Provider { get; set; }
    }

    /// <summary>
    /// Cost breakdown of the call
    /// </summary>
    public class VapiCost
    {
        public double Total { get; set; }
        public double Transcriber { get; set; }
        public double Model { get; set; }
        public double Voice { get; set; }
        public double Transport { get; set; }
    }

    /// <summary>
    /// Call artifacts (recordings, transcripts, etc.)
    /// </summary>
    public class VapiArtifact
    {
        public string Transcript { get; set; }
        public string RecordingUrl { get; set; }
        public string StereoRecordingUrl { get; set; }
        public string VideoRecordingUrl { get; set; }
    }

    /// <summary>
    /// Analysis data from the call
    /// </summary>
    public class VapiAnalysis
    {
        public string Summary { get; set; }
        public object StructuredData { get; set; }
        public VapiSuccessEvaluation SuccessEvaluation { get; set; }
    }

    /// <summary>
    /// Success evaluation of the call
    /// </summary>
    public class VapiSuccessEvaluation
    {
        public bool Passed { get; set; }
        public string Reason { get; set; }
    }
}
