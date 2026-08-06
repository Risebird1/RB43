using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Normalized, provider-agnostic call-result contract that the Phone Screener
    /// UI binds to. A backend adapter maps each voice provider's payload onto
    /// these fields so the Razor views never reference provider-specific data.
    /// </summary>
    public class PhoneScreenCallResultVM
    {
        public int CallId { get; set; }
        public string Provider { get; set; }

        public int IRFId { get; set; }
        public string IRFCode { get; set; }
        public string IRFName { get; set; }
        public string AgentLabel { get; set; }

        public int? CandidateId { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public string Phone { get; set; }

        /// <summary>Canonical value: queued | ringing | in-progress | ended.</summary>
        public string Status { get; set; }
        /// <summary>Original provider/internal status before normalization.</summary>
        public string RawStatus { get; set; }
        public string EndedReason { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int? DurationSeconds { get; set; }
        public decimal? Cost { get; set; }

        public string RecordingUrl { get; set; }
        public string Transcript { get; set; }
        public string Summary { get; set; }

        /// <summary>true/pass -> PASS, false/fail -> FAIL, number -> score, else neutral.</summary>
        public string SuccessEvaluation { get; set; }
        public string Disposition { get; set; }
        public string Qualified { get; set; }
        public string FailedCriteria { get; set; }
        public string Availability { get; set; }

        /// <summary>Arbitrary structured output as a JSON string (rendered dynamically).</summary>
        public string StructuredDataJson { get; set; }
        /// <summary>Original provider payload as a JSON string.</summary>
        public string RawJson { get; set; }

        public string DurationDisplay
        {
            get
            {
                if (!DurationSeconds.HasValue || DurationSeconds.Value <= 0) return "-";
                var t = TimeSpan.FromSeconds(DurationSeconds.Value);
                return string.Format("{0}:{1:00}", (int)t.TotalMinutes, t.Seconds);
            }
        }
    }

    /// <summary>Lightweight IRF header used to build the Outcomes hero band.</summary>
    public class PhoneScreenIRFHeader
    {
        public int Id { get; set; }
        public string IRFCode { get; set; }
        public string IRFName { get; set; }
        public string ClientName { get; set; }
        public string PlivoAssistantUrl { get; set; }
        public string PlivoWhatsAppAssistantUrl { get; set; }
        public string ElevenLabsAgentId { get; set; }
        public DateTime? StartDate { get; set; }
    }

    /// <summary>Model for the Campaign Outcomes screen (one skill channel).</summary>
    public class PhoneScreenOutcomesVM
    {
        public int IRFId { get; set; }
        public string IRFCode { get; set; }
        public string IRFName { get; set; }
        public string ClientName { get; set; }
        public string PlivoAssistantUrl { get; set; }
        public bool HasAssistantConfigured { get; set; }
        public string PlivoWhatsAppAssistantUrl { get; set; }
        public bool HasWhatsAppAssistantConfigured { get; set; }
        public string ElevenLabsAgentId { get; set; }
        public bool HasElevenLabsWhatsAppConfigured { get; set; }
        public DateTime? LaunchDate { get; set; }

        public List<PhoneScreenOutcomeRowVM> Rows { get; set; } = new List<PhoneScreenOutcomeRowVM>();

        public int CallsPlaced { get; set; }
        public int QualifiedPass { get; set; }
        public int InProgress { get; set; }
        public int FailedOrNoAnswer { get; set; }
        public int ConnectedNotQualified { get; set; }
        public int Unanswered { get; set; }
    }

    /// <summary>One candidate row on the Campaign Outcomes table.</summary>
    public class PhoneScreenOutcomeRowVM
    {
        public int? CallId { get; set; }
        public int CandidateId { get; set; }
        public int IRFId { get; set; }

        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public string Phone { get; set; }

        public string Provider { get; set; }
        public string Channel { get; set; }          // voice | whatsapp
        public string Status { get; set; }          // canonical
        public string RawStatus { get; set; }
        public string EndedReason { get; set; }
        public string Summary { get; set; }
        public string RecordingUrl { get; set; }
        public string SuccessEvaluation { get; set; }

        public DateTime? StartedAt { get; set; }
        public int? DurationSeconds { get; set; }

        public bool HasCall { get; set; }

        public string DurationDisplay
        {
            get
            {
                if (!DurationSeconds.HasValue || DurationSeconds.Value <= 0) return "-";
                var t = TimeSpan.FromSeconds(DurationSeconds.Value);
                return string.Format("{0}:{1:00}", (int)t.TotalMinutes, t.Seconds);
            }
        }
    }
}
