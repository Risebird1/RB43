using System;

namespace Risebird.Entities.Data
{
    public class SupportTicket
    {
        public int      Id                          { get; set; }
        public string   SenderEmail                 { get; set; }
        public string   Subject                     { get; set; }
        public string   MailContent                 { get; set; }
        public DateTime? ReceivedAt                 { get; set; }
        public string   Intent                      { get; set; }
        public string   PrimarySpecialization       { get; set; }
        public string   MailBodyIntent              { get; set; }
        public bool     AttachmentJobDescription    { get; set; }
        public bool     AttachmentPdf               { get; set; }
        public bool     AttachmentResumes           { get; set; }
        public string   DriveFolderLink             { get; set; }
        public string   SummaryMail                 { get; set; }
        public string   SummaryCandidates           { get; set; }
        public string   SummaryInterviewScores      { get; set; }
        public int?     UserId                      { get; set; }
        public int?     ClientId                    { get; set; }
        public string   Status                      { get; set; }
        public DateTime CreatedAt                   { get; set; }
    }
}
