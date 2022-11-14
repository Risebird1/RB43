using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class Filters
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] SkillChannel { get; set; }
    }
}
