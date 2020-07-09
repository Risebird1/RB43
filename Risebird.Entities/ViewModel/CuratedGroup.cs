using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class CuratedGroup
    {
        public int id { get; set; }
        public string Name { get; set; }
        public int TotalSkillsChannels { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
