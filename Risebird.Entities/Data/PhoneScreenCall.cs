using System;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// Persistence row for a single AI Phone Screener call placed to a candidate
    /// in a skill channel (IRF). Provider-agnostic: the same shape backs Plivo
    /// today and any future voice platform via a backend adapter.
    /// Maps 1:1 to dbo.PhoneScreenCalls.
    /// </summary>
    public class PhoneScreenCall
    {
        public int Id { get; set; }

        public string Provider { get; set; }
        public int IRFId { get; set; }
        public int? CandidateId { get; set; }
        public int? CandidateMasterId { get; set; }

        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public string Phone { get; set; }

        public string ExternalCallId { get; set; }
        public string ConversationId { get; set; }

        public string Status { get; set; }
        public string EndedReason { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int? DurationSeconds { get; set; }
        public decimal? Cost { get; set; }

        public string RecordingUrl { get; set; }
        public string Transcript { get; set; }
        public string Summary { get; set; }

        public string SuccessEvaluation { get; set; }
        public string Disposition { get; set; }
        public string Qualified { get; set; }
        public string FailedCriteria { get; set; }
        public string Availability { get; set; }

        public string StructuredData { get; set; }
        public string RawPayload { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}
