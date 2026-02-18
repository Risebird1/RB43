using System;
using System.Collections.Generic;

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
        public DateTime Timestamp { get; set; }
        public VapiCall Call { get; set; }
        public VapiTranscript Transcript { get; set; }
        public VapiToolCalls ToolCalls { get; set; }
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
    /// Tool calls from webhook
    /// </summary>
    public class VapiToolCalls
    {
        public List<VapiToolCall> ToolCalls { get; set; }
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
