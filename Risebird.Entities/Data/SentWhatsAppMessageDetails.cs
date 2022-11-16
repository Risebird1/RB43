using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class SentWhatsAppMessageDetails
    {
        public int id { get; set; }
        public string Recipient { get; set; }
        public string CountryCode { get; set; }
        public string WhatsappNumber { get; set; }
        public string Message { get; set; }
        public string ButtonLink { get; set; }
        public string Event { get; set; }
        public int? IRFId { get; set; }
        public int? InterviewId { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string Header { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SentBy { get; set; }
    }
}
