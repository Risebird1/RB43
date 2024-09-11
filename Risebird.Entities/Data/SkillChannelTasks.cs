using System;

namespace Risebird.Entities.Data
{
    public class SkillChannelTasks
    {
        public int id { get; set; }
        public int? Status { get; set; }
        public string StatusValue { get; set; }
        public int IRFId { get; set; }
        public string TaskName { get; set; }
        public string TaskValue { get; set; }
        public DateTime CreatedOn { get; set; }
        public string PerformedBy { get; set; }
        public string Comments { get; set; }
    }
}
