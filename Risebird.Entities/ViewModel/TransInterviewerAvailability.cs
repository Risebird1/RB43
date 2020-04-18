using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerAvailability
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public string DayTimeSlot { get; set; }
        public string DaySlot { get; set; }
        [DataType(DataType.Time)]
        public DateTime? StartTime { get; set; }
        [DataType(DataType.Time)]
        public DateTime? EndTime { get; set; }

    }
}
