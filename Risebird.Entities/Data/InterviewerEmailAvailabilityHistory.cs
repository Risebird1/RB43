using System;

namespace Risebird.Entities.Data
{
    public class InterviewerEmailAvailabilityHistory
    {
        public int      Id        { get; set; }
        public int      RecordId  { get; set; }
        public string   EventType { get; set; }
        public string   FromValue { get; set; }
        public string   ToValue   { get; set; }
        public string   ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
