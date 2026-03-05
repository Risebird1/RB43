using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Risebird.Entities.Data.PlivoModels
{
    // ─── Events sent TO OpenAI Realtime API ──────────────────────────────────────

    public class OAISessionUpdateEvent
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "session.update";

        [JsonProperty("session")]
        public OAISessionConfig Session { get; set; }
    }

    public class OAISessionConfig
    {
        [JsonProperty("modalities")]
        public List<string> Modalities { get; set; } = new List<string> { "text", "audio" };

        [JsonProperty("instructions")]
        public string Instructions { get; set; }

        [JsonProperty("voice")]
        public string Voice { get; set; } = "alloy";

        [JsonProperty("input_audio_format")]
        public string InputAudioFormat { get; set; } = "pcm16";

        [JsonProperty("output_audio_format")]
        public string OutputAudioFormat { get; set; } = "pcm16";

        [JsonProperty("input_audio_transcription")]
        public OAITranscriptionConfig InputAudioTranscription { get; set; }

        [JsonProperty("turn_detection")]
        public OAITurnDetectionConfig TurnDetection { get; set; }

        [JsonProperty("tools")]
        public List<OAITool> Tools { get; set; }

        [JsonProperty("tool_choice")]
        public string ToolChoice { get; set; } = "auto";

        [JsonProperty("temperature")]
        public double Temperature { get; set; } = 0.8;

        [JsonProperty("max_response_output_tokens")]
        public object MaxResponseOutputTokens { get; set; } = 500;
    }

    public class OAITranscriptionConfig
    {
        [JsonProperty("model")]
        public string Model { get; set; } = "whisper-1";
    }

    public class OAITurnDetectionConfig
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "server_vad";

        [JsonProperty("threshold")]
        public double Threshold { get; set; } = 0.5;

        [JsonProperty("prefix_padding_ms")]
        public int PrefixPaddingMs { get; set; } = 300;

        [JsonProperty("silence_duration_ms")]
        public int SilenceDurationMs { get; set; } = 500;
    }

    public class OAITool
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "function";

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("parameters")]
        public OAIToolParameters Parameters { get; set; }
    }

    public class OAIToolParameters
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "object";

        [JsonProperty("properties")]
        public Dictionary<string, OAIToolProperty> Properties { get; set; }

        [JsonProperty("required")]
        public List<string> Required { get; set; }
    }

    public class OAIToolProperty
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("enum", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Enum { get; set; }
    }

    public class OAIInputAudioBufferAppendEvent
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "input_audio_buffer.append";

        [JsonProperty("audio")]
        public string Audio { get; set; }
    }

    public class OAIConversationItemCreateEvent
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "conversation.item.create";

        [JsonProperty("item")]
        public OAIConversationItem Item { get; set; }
    }

    public class OAIConversationItem
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("call_id")]
        public string CallId { get; set; }

        [JsonProperty("output")]
        public string Output { get; set; }
    }

    public class OAIResponseCreateEvent
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "response.create";
    }

    // ─── Events received FROM OpenAI Realtime API ────────────────────────────────

    public class OAIRealtimeEvent
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("event_id")]
        public string EventId { get; set; }

        [JsonProperty("response_id")]
        public string ResponseId { get; set; }

        [JsonProperty("item_id")]
        public string ItemId { get; set; }

        [JsonProperty("output_index")]
        public int? OutputIndex { get; set; }

        [JsonProperty("content_index")]
        public int? ContentIndex { get; set; }

        /// <summary>Base64 PCM16 24kHz audio chunk (response.audio.delta)</summary>
        [JsonProperty("delta")]
        public string Delta { get; set; }

        /// <summary>Full transcript of AI speech (response.audio_transcript.delta)</summary>
        [JsonProperty("transcript")]
        public string Transcript { get; set; }

        /// <summary>Function call name (response.function_call_arguments.done)</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Function call ID (response.function_call_arguments.done)</summary>
        [JsonProperty("call_id")]
        public string CallId { get; set; }

        /// <summary>JSON string of function arguments (response.function_call_arguments.done)</summary>
        [JsonProperty("arguments")]
        public string Arguments { get; set; }

        [JsonProperty("error")]
        public JObject Error { get; set; }
    }

    // ─── Plivo WebSocket stream events ───────────────────────────────────────────

    public class PlivoStreamEvent
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("sequenceNumber")]
        public string SequenceNumber { get; set; }

        [JsonProperty("start")]
        public PlivoStreamStart Start { get; set; }

        [JsonProperty("media")]
        public PlivoStreamMedia Media { get; set; }

        [JsonProperty("stop")]
        public PlivoStreamStop Stop { get; set; }
    }

    public class PlivoStreamStart
    {
        [JsonProperty("streamId")]
        public string StreamId { get; set; }

        [JsonProperty("callId")]
        public string CallId { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("customParameters")]
        public Dictionary<string, string> CustomParameters { get; set; }
    }

    public class PlivoStreamMedia
    {
        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        [JsonProperty("sampleRate")]
        public int SampleRate { get; set; }

        [JsonProperty("payload")]
        public string Payload { get; set; }
    }

    public class PlivoStreamStop
    {
        [JsonProperty("callId")]
        public string CallId { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class PlivoPlayAudioEvent
    {
        [JsonProperty("event")]
        public string Event { get; set; } = "playAudio";

        [JsonProperty("media")]
        public PlivoPlayAudioMedia Media { get; set; }
    }

    public class PlivoPlayAudioMedia
    {
        [JsonProperty("contentType")]
        public string ContentType { get; set; } = "audio/x-mulaw";

        [JsonProperty("sampleRate")]
        public int SampleRate { get; set; } = 8000;

        [JsonProperty("payload")]
        public string Payload { get; set; }
    }

    public class PlivoClearAudioEvent
    {
        [JsonProperty("event")]
        public string Event { get; set; } = "clearAudio";
    }
}
