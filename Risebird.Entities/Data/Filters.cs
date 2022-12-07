using System;

namespace Risebird.Entities.Data
{
    public class Filters
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] SkillChannel { get; set; }
    }
}
