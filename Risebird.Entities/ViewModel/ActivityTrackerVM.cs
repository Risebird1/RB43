using System;

namespace Risebird.Entities.ViewModel
{
    public class ActivityTrackerVM
    {
        public int id { get; set; }
        public int ActivityType { get; set; }
        public string Summary { get; set; }
        public string Name { get; set; }
        public string PerformedBy { get; set; }
        public string FileUrl { get; set; }
        public DateTime PerformedAt { get; set; }
        public string ActivityTypeDesc { get; set; }
    }
}
