using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Risebird.Entities.Data.VapiModels
{
    /// <summary>
    /// Handles Vapi's "cost" field which can be either a plain number (0, before cost accrues)
    /// or a full breakdown object once the call completes.
    /// </summary>
    internal class VapiCostConverter : JsonConverter<VapiCost>
    {
        public override VapiCost ReadJson(JsonReader reader, Type objectType, VapiCost existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            // Plain number (e.g. 0) — return a zeroed cost object
            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                return new VapiCost { Total = (double)token };

            // Null — return null
            if (token.Type == JTokenType.Null)
                return null;

            // Full object — deserialize normally
            var cost = new VapiCost();
            serializer.Populate(token.CreateReader(), cost);
            return cost;
        }

        public override void WriteJson(JsonWriter writer, VapiCost value, JsonSerializer serializer)
            => serializer.Serialize(writer, value);
    }


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
        [JsonConverter(typeof(VapiCostConverter))]
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
