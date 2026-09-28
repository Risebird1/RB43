using System;

namespace Risebird.Entities.ViewModel
{
    public class CallRecordingDataVM
    {
        public int id { get; set; }
        public int InterviewId { get; set; }
        public string IRFId { get; set; }
        public string RecordingSid { get; set; }
        public string CallSid { get; set; }
        public string CallRecipient { get; set; }
        public string Caller { get; set; }
        public string RecipientProfileType { get; set; }
        public string RecipientKind { get; set; } // recipientType sent by the call UI (Candidate / Interviewer); null for older rows
        public string CallerProfileType { get; set; } // caller's Profile.Name, resolved in usp_FetchCallRecordingDataDB
        public DateTime DateCreated { get; set; }
        public DateTime? StartTime { get; set; }
        public string Price { get; set; }
        public string PriceUnit { get; set; }
        public string Duration { get; set; }
        public string Status { get; set; }
        public string RecipientPhoneNumber { get; set; }
        public string RecipientCountryCode { get; set; }
        public string RecordingUri { get; set; }
    }
}
