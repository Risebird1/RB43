using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Risebird.Entities.Data.VapiModels
{
    /// <summary>
    /// Request model for initiating a Vapi phone call
    /// </summary>
    public class VapiCallRequest
    {
        /// <summary>
        /// Phone number ID from Vapi dashboard (the ID of the phone number you imported or created in Vapi)
        /// Example: "pn_abc123..." or a UUID
        /// </summary>
        [JsonProperty("phoneNumberId")]
        public string PhoneNumberId { get; set; }

        /// <summary>
        /// Customer to call (who you're calling)
        /// </summary>
        [JsonProperty("customer")]
        public VapiCustomer Customer { get; set; }

        /// <summary>
        /// Assistant ID to use for the call (optional if assistant is provided)
        /// </summary>
        [JsonProperty("assistantId")]
        public string AssistantId { get; set; }

        /// <summary>
        /// Transient assistant configuration (optional if assistantId is provided)
        /// </summary>
        [JsonProperty("assistant")]
        public VapiAssistant Assistant { get; set; }

        /// <summary>
        /// Assistant overrides for this specific call
        /// </summary>
        [JsonProperty("assistantOverrides")]
        public VapiAssistantOverrides AssistantOverrides { get; set; }
    }

    /// <summary>
    /// Vapi Assistant configuration
    /// </summary>
    public class VapiAssistant
    {
        public string Name { get; set; }
        public VapiModel Model { get; set; }
        public VapiVoice Voice { get; set; }
        public string FirstMessage { get; set; }
        public List<VapiTool> Tools { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Model configuration for Vapi assistant
    /// </summary>
    public class VapiModel
    {
        public string Provider { get; set; } = "openai";
        public string Model { get; set; } = "gpt-4o-mini";
        public List<VapiMessage> Messages { get; set; }
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 250;
    }

    /// <summary>
    /// Voice configuration for Vapi assistant
    /// </summary>
    public class VapiVoice
    {
        public string Provider { get; set; } = "11labs";
        public string VoiceId { get; set; }
        public double? Speed { get; set; }
        public double? Stability { get; set; }
        public double? SimilarityBoost { get; set; }
    }

    /// <summary>
    /// Message in conversation history
    /// </summary>
    public class VapiMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    /// <summary>
    /// Tool definition for Vapi assistant
    /// </summary>
    public class VapiTool
    {
        public string Type { get; set; }
        public VapiFunction Function { get; set; }
        public VapiServer Server { get; set; }
        public bool Async { get; set; }
    }

    /// <summary>
    /// Function definition for tool
    /// </summary>
    public class VapiFunction
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public VapiParameters Parameters { get; set; }
    }

    /// <summary>
    /// Parameters schema for function
    /// </summary>
    public class VapiParameters
    {
        public string Type { get; set; } = "object";
        public Dictionary<string, VapiProperty> Properties { get; set; }
        public List<string> Required { get; set; }
    }

    /// <summary>
    /// Property definition in parameters
    /// </summary>
    public class VapiProperty
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public List<string> Enum { get; set; }
    }

    /// <summary>
    /// Server configuration for webhook
    /// </summary>
    public class VapiServer
    {
        public string Url { get; set; }
        public int TimeoutSeconds { get; set; } = 20;
    }

    /// <summary>
    /// Assistant overrides for specific call
    /// Allows you to override assistant configuration for a specific call
    /// </summary>
    public class VapiAssistantOverrides
    {
        /// <summary>
        /// Override the model configuration (including system messages)
        /// </summary>
        [JsonProperty("model")]
        public VapiModelOverride Model { get; set; }
        
        /// <summary>
        /// Override the first message
        /// </summary>
        [JsonProperty("firstMessage")]
        public string FirstMessage { get; set; }
    }
    
    /// <summary>
    /// Model override for assistant
    /// </summary>
    public class VapiModelOverride
    {
        /// <summary>
        /// Override the system messages
        /// </summary>
        [JsonProperty("messages")]
        public List<VapiMessage> Messages { get; set; }
    }
}
