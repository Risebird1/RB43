using System;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// Debug log for tracking all Vapi webhook requests
    /// </summary>
    public class VapiWebhookDebugLog
    {
        public int Id { get; set; }
        public DateTime RequestTimestamp { get; set; }
        public string WebhookType { get; set; }
        public string CallId { get; set; }
        public string AssistantId { get; set; }
        public string ToolCallName { get; set; }
        public string RawPayload { get; set; }
        public string ProcessingStatus { get; set; }
        public string ErrorMessage { get; set; }
        public bool? CallLogFound { get; set; }
        public int? CallLogId { get; set; }
        public bool? DataSaved { get; set; }
    }
}
