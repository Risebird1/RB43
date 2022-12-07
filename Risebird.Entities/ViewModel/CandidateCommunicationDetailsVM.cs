using System;

namespace Risebird.Entities.ViewModel
{
    public class CandidateCommunicationDetailsVM
    {
        #region VoiceCallProperties
        public int InterviewId { get; set; }
        public string Caller { get; set; }
        public string CallRecipient { get; set; }
        public string CallRecordingUri { get; set; }

        #endregion


        #region WhatsappProperties
        public string WhatsappRecipient { get; set; }
        public string WhatsappMessage { get; set; }
        public string WhatsappButtonLink { get; set; }
        public string WhatsappEvent { get; set; }
        public DateTime? InterviewDate { get; set; }

        #endregion


        #region MailProperties

        public string CandidateEmail { get; set; }
        public string sg_message_id { get; set; }
        public string SkillChannelName { get; set; }
        public DateTime? interviewDateMail { get; set; }
        public DateTime CreatedAt { get; set; }
        public string MailSubject { get; set; }
        public string MailBody { get; set; }
        public string SentBy { get; set; }
        public int partitionNumber { get; set; }
        public string eventType { get; set; }
        public string latestEvent { get; set; }
        public DateTime ActivityDate { get; set; }
        public DateTime SentOn { get; set; }

        #endregion
    }
}
