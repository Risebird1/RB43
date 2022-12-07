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
    }
}
