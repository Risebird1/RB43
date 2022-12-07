using System;

namespace Risebird.Functions
{
    public class SentMailDetails
    {
        public int id { get; set; }
        public string To { get; set; }
        public string Cc { get; set; }
        public string Bcc { get; set; }
        public string MailBody { get; set; }
        public string MailSubject { get; set; }
        public string sg_message_id { get; set; }
        public int? IRFId { get; set; }
        public int? InterviewId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SentBy { get; set; }
    }
}
