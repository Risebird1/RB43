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
        public string Weekday { get; set; }
        public string Weekend { get; set; }
        public bool IsAvailableOnPublicHolidays { get; set; }
    }
}
