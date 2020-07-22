using System;

namespace Risebird.Entities.ViewModel
{
    public class IRFFilter
    {
        public string[] RFID { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] ClientName { get; set; }
        public string[] ArchiveStatus { get; set; }
        public string[] SubscriptionStatus { get; set; }
        public string[] ClientRole { get; set; }
        public string[] PrimarySkill { get; set; }

    }
}
