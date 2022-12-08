using System;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerAvailability
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public string Weekday { get; set; }
        public string Weekend { get; set; }
        public bool IsAvailableOnPublicHolidays { get; set; }
        public string SlotTime { get; set; }
        public string SlotDay { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

    }
}
