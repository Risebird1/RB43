using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Risebird.Entities.Data.VapiModels
{
    /// <summary>
    /// Base webhook message from Vapi
    /// </summary>
    public class VapiWebhookMessage
    {
        public VapiMessageContent Message { get; set; }
    }

    /// <summary>
    /// Content of webhook message
    /// </summary>
    public class VapiMessageContent
    {
        public string Type { get; set; }
        public string CallId { get; set; }
        public string OrgId { get; set; }
        
        /// <summary>
        /// Unix timestamp in milliseconds
        /// </summary>
        public long? Timestamp { get; set; }
        
        /// <summary>
        /// Converts Unix timestamp to DateTime (UTC)
        /// </summary>
        public DateTime? TimestampAsDateTime
        {
            get
            {
                if (!Timestamp.HasValue) return null;
                return DateTimeOffset.FromUnixTimeMilliseconds(Timestamp.Value).UtcDateTime;
            }
        }
        
        public VapiCall Call { get; set; }
        public VapiTranscript Transcript { get; set; }
        
        /// <summary>
        /// Tool calls array (direct array, not wrapped in object)
        /// </summary>
        public List<VapiToolCall> ToolCalls { get; set; }
    }

    /// <summary>
    /// Call object in webhook
    /// </summary>
    public class VapiCall
    {
        public string Id { get; set; }
        public string AssistantId { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public VapiCustomer Customer { get; set; }
        
        /// <summary>
        /// ISO 8601 formatted date strings - will deserialize correctly to DateTime
        /// </summary>
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        
        public string EndedReason { get; set; }
        public VapiArtifact Artifact { get; set; }
        public VapiAnalysis Analysis { get; set; }
    }

    /// <summary>
    /// Transcript update from webhook
    /// </summary>
    public class VapiTranscript
    {
        public string Role { get; set; }
        public string TranscriptType { get; set; }
        public string Transcript { get; set; }
    }

    /// <summary>
    /// Individual tool call
    /// </summary>
    public class VapiToolCall
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public VapiToolFunction Function { get; set; }
    }

    /// <summary>
    /// Function details in tool call
    /// </summary>
    public class VapiToolFunction
    {
        public string Name { get; set; }
        
        /// <summary>
        /// Arguments can be either a JSON object or a JSON string
        /// Use JsonExtensionData to capture any format
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> Arguments { get; set; }
    }

    /// <summary>
    /// Tool call result to send back to Vapi
    /// </summary>
    public class VapiToolCallResult
    {
        public List<VapiToolCallResultItem> Results { get; set; }
    }

    /// <summary>
    /// Individual tool call result item
    /// </summary>
    public class VapiToolCallResultItem
    {
        public string ToolCallId { get; set; }
        public string Result { get; set; }
    }
}
