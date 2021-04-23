using System;

namespace Risebird.Entities.Data
{
    public class ActivityTrackerFilter
    {
        public string[] ActivityType { get; set; }
        public string[] PerformedBy { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
