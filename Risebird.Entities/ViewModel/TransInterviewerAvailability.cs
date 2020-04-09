using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerAvailability
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public string DayTimeSlot { get; set; }
        public string DaySlot { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

    }
}
