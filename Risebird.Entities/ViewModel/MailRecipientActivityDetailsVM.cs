using Risebird.Entities.Data;
using System;

namespace Risebird.Entities.ViewModel
{
    public class MailRecipientActivityDetailsVM : SendgridEventData
    {
        public int? InterviewId { get; set; }
        public int? IRFId { get; set; }
        public string SkillChannelName { get; set; }
        public DateTime? interviewDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string MailSubject { get; set; }
        public string MailBody { get; set; }
        public string SentBy { get; set; }
        public int partitionNumber { get; set; }
        public string eventType { get; set; }
        public string latestEvent { get; set; }
        public DateTime ActivityDate { get; set; }
        public DateTime SentOn { get; set; }
        public string MobileNumber { get; set; }
        public string RecipientName { get; set; }
        public string CompanyName { get; set; }
        public string ResumeLink { get; set; }
        public int? VideoConfirmation { get; set; }
        public int RedMarker { get; set; }
        public string interviewer { get; set; }
        public int TotalInterviews { get; set; }
        public string ChannelOwner { get; set; }
        public string SkillChannel { get; set; }
        public string candidateEmail { get; set; }
        public string candidateName { get; set; }
        public string CalendlyLink { get; set; }
        public string CandidateClientHR { get; set; }
        public DateTime EndDate { get; set; }
    }
}
