using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Risebird.Entities.Data.PlivoModels
{
    // ─── DB entity ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Persisted assistant configuration — analogous to a Vapi "assistant" object.
    /// One row per named assistant (e.g. "InterviewBookingAssistant").
    /// Loaded at call-time and sent to OpenAI Realtime API as a session.update event.
    /// </summary>
    public class PlivoAssistantConfig
    {
        public int Id { get; set; }

        /// <summary>Human-readable identifier, e.g. "InterviewBookingAssistant"</summary>
        public string AssistantName { get; set; }

        /// <summary>
        /// The full system prompt / instructions.
        /// Dynamic tokens supported: {{candidateName}}, {{clientName}}, {{jobTitle}},
        /// {{irfId}}, {{candidateEmail}}, {{todayDate}}.
        /// These are interpolated at call-time before being sent to OpenAI.
        /// </summary>
        public string Instructions { get; set; }

        /// <summary>
        /// OpenAI Realtime voice name.
        /// Supported: alloy, echo, fable, onyx, nova, shimmer.
        /// </summary>
        public string Voice { get; set; } = "alloy";

        /// <summary>OpenAI Realtime model, e.g. gpt-4o-realtime-preview</summary>
        public string Model { get; set; } = "gpt-4o-realtime-preview";

        /// <summary>LLM temperature (0.0 – 1.0)</summary>
        public double Temperature { get; set; } = 0.8;

        /// <summary>Max tokens per AI response turn</summary>
        public int MaxResponseTokens { get; set; } = 500;

        /// <summary>VAD silence threshold (0.0 – 1.0); lower = more sensitive</summary>
        public double VadThreshold { get; set; } = 0.5;

        /// <summary>Milliseconds of silence before the AI starts responding</summary>
        public int SilenceDurationMs { get; set; } = 500;

        /// <summary>JSON array of extra tool definitions to merge alongside submitInterviewSlot</summary>
        public string ExtraToolsJson { get; set; }

        /// <summary>Whether this config is the active one used by the timer function and manual calls</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }

    // ─── View model used by the SetupAssistant form ──────────────────────────────

    public class PlivoAssistantSetupVM
    {
        public int? Id { get; set; }
        public string AssistantName { get; set; }
        public string Instructions { get; set; }
        public string Voice { get; set; } = "alloy";
        public string Model { get; set; } = "gpt-4o-realtime-preview";
        public double Temperature { get; set; } = 0.8;
        public int MaxResponseTokens { get; set; } = 500;
        public double VadThreshold { get; set; } = 0.5;
        public int SilenceDurationMs { get; set; } = 500;

        /// <summary>Available voices with display labels</summary>
        [JsonIgnore]
        public static IReadOnlyList<(string Value, string Label)> AvailableVoices =>
            new List<(string, string)>
            {
                ("alloy",   "Alloy (neutral, clear)"),
                ("echo",    "Echo (warm, male)"),
                ("fable",   "Fable (expressive, UK accent)"),
                ("onyx",    "Onyx (deep, male)"),
                ("nova",    "Nova (friendly, female)"),
                ("shimmer", "Shimmer (soft, female)")
            };
    }
}
