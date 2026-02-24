using System;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// Tracks Vapi call logs in the database
    /// </summary>
    public class VapiCallLog
    {
        public int Id { get; set; }
        public string VapiCallId { get; set; }
        public int? InterviewId { get; set; }
        public int? IRFId { get; set; }
        public int? RecipientId { get; set; }
        public string RecipientPhone { get; set; }
        public string RecipientName { get; set; }
        public string RecipientEmail { get; set; }
        public string CallStatus { get; set; }
        public DateTime? ScheduledDateTime { get; set; }
        public DateTime? CallStartedAt { get; set; }
        public DateTime? CallEndedAt { get; set; }
        public string EndedReason { get; set; }
        public int? DurationSeconds { get; set; }
        public string Transcript { get; set; }
        public string RecordingUrl { get; set; }
        public bool? InterviewBooked { get; set; }
        public DateTime? BookedInterviewDateTime { get; set; }
        public string BookingConfirmationDetails { get; set; }
        public decimal? CallCost { get; set; }
        public string AssistantId { get; set; }
        public string Summary { get; set; }
        public string StructuredData { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
