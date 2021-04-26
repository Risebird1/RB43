using System;

namespace Risebird.Entities.ViewModel
{
    public class UnassignedInterviewsFilter
    {
        public string[] Status { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] Client { get; set; }
        public string[] ClientHREmail { get; set; }
    }
}
