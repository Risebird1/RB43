using System.Collections.Generic;
using Newtonsoft.Json;

namespace Risebird.Entities.Data.ElevenLabsModels
{
    // ElevenLabs Agents Platform: outbound voice calls and agent create / update, for PlivoCX
    // campaigns run on ElevenLabs. Shapes follow https://api.elevenlabs.io/openapi.json and the
    // MCQ project's working client (RisebirdMCQ/Models/ElevenLabsModels.cs).

    /// <summary>
    /// Body of POST /v1/convai/{twilio|exotel|sip-trunk}/outbound-call - the same for every line;
    /// only the path and the caller number (agent_phone_number_id) differ.
    /// </summary>
    public class ElevenLabsOutboundCallRequest
    {
        [JsonProperty("agent_id")]
        public string AgentId { get; set; }

        /// <summary>The caller number's id in ElevenLabs (Agents > Phone Numbers), e.g. phnum_....</summary>
        [JsonProperty("agent_phone_number_id")]
        public string AgentPhoneNumberId { get; set; }

        [JsonProperty("to_number")]
        public string ToNumber { get; set; }

        [JsonProperty("conversation_initiation_client_data")]
        public ElevenLabsConversationInitiationClientData ConversationInitiationClientData { get; set; }
    }

    public class ElevenLabsOutboundCallResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("conversation_id")]
        public string ConversationId { get; set; }

        [JsonProperty("callSid")]
        public string CallSid { get; set; }

        /// <summary>The SIP trunk endpoint returns this instead of callSid.</summary>
        [JsonProperty("sip_call_id")]
        public string SipCallId { get; set; }

        [JsonIgnore] public string Error { get; set; }
        [JsonIgnore] public string RawResponseBody { get; set; }
        [JsonIgnore] public int HttpStatusCode { get; set; }
    }

    /// <summary>
    /// Body of POST /v1/convai/agents/create and PATCH /v1/convai/agents/{id}. Sent in full on
    /// update too: a field left out keeps whatever the account default or the dashboard set.
    /// </summary>
    public class ElevenLabsCreateAgentRequest
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("conversation_config")]
        public ElevenLabsAgentConversationConfig ConversationConfig { get; set; }

        [JsonProperty("platform_settings")]
        public ElevenLabsAgentPlatformSettings PlatformSettings { get; set; }
    }

    public class ElevenLabsAgentConversationConfig
    {
        [JsonProperty("agent")]
        public ElevenLabsAgentConfig Agent { get; set; }

        [JsonProperty("tts")]
        public ElevenLabsAgentTts Tts { get; set; }

        [JsonProperty("asr")]
        public ElevenLabsAgentAsr Asr { get; set; }

        [JsonProperty("turn")]
        public ElevenLabsAgentTurn Turn { get; set; }

        [JsonProperty("conversation")]
        public ElevenLabsAgentConversation Conversation { get; set; }
    }

    public class ElevenLabsAgentConfig
    {
        /// <summary>ISO 639-1, e.g. "en".</summary>
        [JsonProperty("language")]
        public string Language { get; set; }

        /// <summary>Spoken as soon as the call connects; may use {{dynamic_variables}}.</summary>
        [JsonProperty("first_message")]
        public string FirstMessage { get; set; }

        [JsonProperty("prompt")]
        public ElevenLabsAgentPrompt Prompt { get; set; }

        /// <summary>
        /// Placeholder values for the {{dynamic_variables}} the prompt uses, so the agent can be
        /// tested from the dashboard. Real calls send their own values.
        /// </summary>
        [JsonProperty("dynamic_variables")]
        public ElevenLabsDynamicVariablesConfig DynamicVariables { get; set; }
    }

    public class ElevenLabsDynamicVariablesConfig
    {
        [JsonProperty("dynamic_variable_placeholders")]
        public Dictionary<string, string> Placeholders { get; set; }
    }

    public class ElevenLabsAgentPrompt
    {
        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [JsonProperty("llm")]
        public string Llm { get; set; }

        [JsonProperty("temperature")]
        public double? Temperature { get; set; }

        /// <summary>IANA zone, so "tomorrow" means tomorrow in IST.</summary>
        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        [JsonProperty("built_in_tools")]
        public ElevenLabsBuiltInTools BuiltInTools { get; set; }
    }

    /// <summary>
    /// System tools. end_call lets the agent hang up itself (the reason the Plivo agents kept the
    /// line open); voicemail_detection ends calls that reach voicemail. Tools not used are sent as
    /// explicit nulls so a toggle switched on in the dashboard is switched back off on update.
    /// </summary>
    public class ElevenLabsBuiltInTools
    {
        [JsonProperty("end_call", NullValueHandling = NullValueHandling.Include)]
        public ElevenLabsSystemTool EndCall { get; set; }

        [JsonProperty("voicemail_detection", NullValueHandling = NullValueHandling.Include)]
        public ElevenLabsSystemTool VoicemailDetection { get; set; }

        [JsonProperty("language_detection", NullValueHandling = NullValueHandling.Include)]
        public ElevenLabsSystemTool LanguageDetection { get; set; }

        [JsonProperty("transfer_to_number", NullValueHandling = NullValueHandling.Include)]
        public ElevenLabsSystemTool TransferToNumber { get; set; }

        [JsonProperty("transfer_to_agent", NullValueHandling = NullValueHandling.Include)]
        public ElevenLabsSystemTool TransferToAgent { get; set; }
    }

    public class ElevenLabsSystemTool
    {
        public ElevenLabsSystemTool() { }

        public ElevenLabsSystemTool(string name, string description = "")
        {
            Name = name;
            Description = description ?? "";
            Params = new ElevenLabsSystemToolParams { SystemToolType = name };
        }

        [JsonProperty("type")]
        public string Type { get; set; } = "system";

        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Empty uses the provider's built-in description.</summary>
        [JsonProperty("description")]
        public string Description { get; set; } = "";

        [JsonProperty("params")]
        public ElevenLabsSystemToolParams Params { get; set; }
    }

    public class ElevenLabsSystemToolParams
    {
        [JsonProperty("system_tool_type")]
        public string SystemToolType { get; set; }
    }

    public class ElevenLabsAgentTts
    {
        [JsonProperty("voice_id")]
        public string VoiceId { get; set; }

        [JsonProperty("model_id")]
        public string ModelId { get; set; }

        [JsonProperty("stability")]
        public double? Stability { get; set; }

        [JsonProperty("speed")]
        public double? Speed { get; set; }

        [JsonProperty("similarity_boost")]
        public double? SimilarityBoost { get; set; }
    }

    public class ElevenLabsAgentAsr
    {
        [JsonProperty("keywords")]
        public List<string> Keywords { get; set; }

        [JsonProperty("quality")]
        public string Quality { get; set; }
    }

    public class ElevenLabsAgentTurn
    {
        /// <summary>Seconds of silence before the agent prompts the caller.</summary>
        [JsonProperty("turn_timeout")]
        public double? TurnTimeout { get; set; }
    }

    public class ElevenLabsAgentConversation
    {
        /// <summary>Hard cap on call length, in seconds.</summary>
        [JsonProperty("max_duration_seconds")]
        public int? MaxDurationSeconds { get; set; }
    }

    public class ElevenLabsAgentPlatformSettings
    {
        /// <summary>
        /// What the analysis extracts from each call, keyed by the name the post-call webhook
        /// returns it under (analysis.data_collection_results.{key}.value).
        /// </summary>
        [JsonProperty("data_collection")]
        public Dictionary<string, ElevenLabsDataCollectionItem> DataCollection { get; set; }
    }

    public class ElevenLabsDataCollectionItem
    {
        /// <summary>boolean | string | integer | number</summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "string";

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    /// <summary>Response from agent create / update.</summary>
    public class ElevenLabsCreateAgentResponse
    {
        [JsonProperty("agent_id")]
        public string AgentId { get; set; }

        [JsonIgnore] public bool Success { get; set; }
        [JsonIgnore] public string Error { get; set; }
        [JsonIgnore] public string RawResponseBody { get; set; }
        [JsonIgnore] public int HttpStatusCode { get; set; }
    }

    /// <summary>Voice / model settings for the campaign agents (ElevenLabs:Campaigns:Agent in config).</summary>
    public class ElevenLabsCampaignAgentOptions
    {
        /// <summary>"Ananya" - the Indian-English voice the Plivo agents use (they speak through ElevenLabs TTS too).</summary>
        public string VoiceId { get; set; } = "ack0QsRaQyDLnVyMQTSd";
        /// <summary>English agents use a v2 English model.</summary>
        public string TtsModelId { get; set; } = "eleven_flash_v2";
        public double Stability { get; set; } = 0.5;
        public double Speed { get; set; } = 1.0;
        public double SimilarityBoost { get; set; } = 0.8;
        public string Llm { get; set; } = "gemini-3.5-flash";
        public double Temperature { get; set; } = 0;
        public string Timezone { get; set; } = "Asia/Kolkata";
        public double TurnTimeout { get; set; } = 7;
        public string Language { get; set; } = "en";
    }

    /// <summary>One campaign agent to create on ElevenLabs.</summary>
    public class ElevenLabsCampaignAgentDefinition
    {
        /// <summary>Matches PlivoCXAgent.Purpose and the Plivo agent it mirrors.</summary>
        public string Purpose { get; set; }
        /// <summary>PlivoCXAgent.Name / Description shown on Create campaign.</summary>
        public string Name { get; set; }
        public string Description { get; set; }
        /// <summary>False for confirm-only: the webhook then never books a slot.</summary>
        public bool CollectsSlot { get; set; }
        /// <summary>Name of the agent in the ElevenLabs dashboard.</summary>
        public string ElevenLabsName { get; set; }
        public string FirstMessage { get; set; }
        public string Prompt { get; set; }
        public int MaxDurationSeconds { get; set; }
        public Dictionary<string, ElevenLabsDataCollectionItem> DataCollection { get; set; }
    }

}
