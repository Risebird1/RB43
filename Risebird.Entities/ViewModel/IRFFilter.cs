using System;

namespace Risebird.Entities.ViewModel
{
    public class IRFFilter
    {
        public string[] RFID { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] ClientName { get; set; }
        public string[] EndClientName { get; set; }
        public string[] ArchiveStatus { get; set; }
        public string[] SubscriptionStatus { get; set; }
        public string[] ClientRole { get; set; }
        public string[] PrimarySkill { get; set; }
        //public string searchValue { get; set; }

    }
    public class InvoiceFilter
    {
        public string Month { get; set; }
        public string Year { get; set; }
        public string[] PaymentStatus { get; set; }
        public string[] Client { get; set; }
    }
}
