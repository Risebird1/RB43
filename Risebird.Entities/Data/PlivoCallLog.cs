using System;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// Tracks Plivo outbound AI call logs in the database
    /// </summary>
    public class PlivoCallLog
    {
        public int Id { get; set; }

        /// <summary>Plivo CallUUID — assigned by Plivo when the call is answered</summary>
        public string PlivoCallUUID { get; set; }

        /// <summary>Plivo request_uuid — returned immediately when the call is fired via REST API</summary>
        public string PlivoRequestUUID { get; set; }

        public int? InterviewId { get; set; }
        public int? IRFId { get; set; }
        public int? RecipientId { get; set; }
        public string RecipientPhone { get; set; }
        public string RecipientName { get; set; }
        public string RecipientEmail { get; set; }

        /// <summary>Plivo number used as the caller ID</summary>
        public string FromNumber { get; set; }

        /// <summary>queued / ringing / in-progress / completed / no-answer / busy / failed</summary>
        public string CallStatus { get; set; }

        public DateTime? ScheduledDateTime { get; set; }
        public DateTime? CallStartedAt { get; set; }
        public DateTime? CallEndedAt { get; set; }

        /// <summary>Plivo hangup cause (e.g. NORMAL_CLEARING, NO_ANSWER)</summary>
        public string HangupCause { get; set; }

        public int? DurationSeconds { get; set; }
        public string Transcript { get; set; }
        public string RecordingUrl { get; set; }
        public bool? InterviewBooked { get; set; }
        public DateTime? BookedInterviewDateTime { get; set; }
        public string BookingConfirmationDetails { get; set; }

        /// <summary>Billable amount in USD from Plivo CDR</summary>
        public decimal? CallCost { get; set; }

        /// <summary>OpenAI Realtime session/model used for the AI conversation</summary>
        public string AIModel { get; set; }

        public string Summary { get; set; }
        public string StructuredData { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
