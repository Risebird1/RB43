using System.Collections.Generic;
using Newtonsoft.Json;

namespace Risebird.Entities.Data.ElevenLabsModels
{
    /// <summary>
    /// Request body for POST /v1/convai/whatsapp/outbound-message.
    /// Starts an outbound WhatsApp screening conversation driven by an ElevenLabs agent.
    /// </summary>
    public class ElevenLabsWhatsAppOutboundRequest
    {
        [JsonProperty("whatsapp_phone_number_id")]
        public string WhatsAppPhoneNumberId { get; set; }

        /// <summary>Candidate's WhatsApp number in international format, digits only (e.g. 919667032088).</summary>
        [JsonProperty("whatsapp_user_id")]
        public string WhatsAppUserId { get; set; }

        [JsonProperty("template_name")]
        public string TemplateName { get; set; }

        [JsonProperty("template_language_code")]
        public string TemplateLanguageCode { get; set; }

        [JsonProperty("template_params")]
        public List<ElevenLabsTemplateComponent> TemplateParams { get; set; }

        [JsonProperty("agent_id")]
        public string AgentId { get; set; }

        [JsonProperty("conversation_initiation_client_data", NullValueHandling = NullValueHandling.Ignore)]
        public ElevenLabsConversationInitiationClientData ConversationInitiationClientData { get; set; }
    }

    public class ElevenLabsTemplateComponent
    {
        /// <summary>"body" | "header" | "button".</summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "body";

        [JsonProperty("parameters")]
        public List<ElevenLabsTemplateTextParam> Parameters { get; set; } = new List<ElevenLabsTemplateTextParam>();
    }

    public class ElevenLabsTemplateTextParam
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "text";

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class ElevenLabsConversationInitiationClientData
    {
        /// <summary>Dynamic variables echoed back on the post-call webhook (we pass log_id + candidate context).</summary>
        [JsonProperty("dynamic_variables")]
        public Dictionary<string, object> DynamicVariables { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>Response from the outbound-message endpoint.</summary>
    public class ElevenLabsWhatsAppOutboundResponse
    {
        [JsonProperty("conversation_id")]
        public string ConversationId { get; set; }

        // Populated by the service (not part of the ElevenLabs body).
        [JsonIgnore] public string Error { get; set; }
        [JsonIgnore] public int HttpStatusCode { get; set; }
        [JsonIgnore] public string RawResponseBody { get; set; }
    }
}
