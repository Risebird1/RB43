using System;

namespace Risebird.Entities.Data
{
    public class SupportTicket
    {
        public int      Id           { get; set; }
        public string   SenderEmail  { get; set; }
        public string   Subject      { get; set; }
        public string   MailContent  { get; set; }
        public DateTime? ReceivedAt  { get; set; }
        public string   Intent       { get; set; }
        public int?     UserId       { get; set; }
        public int?     ClientId     { get; set; }
        public string   Status       { get; set; }
        public DateTime CreatedAt    { get; set; }
    }
}
