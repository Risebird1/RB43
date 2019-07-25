using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class TechSkillComments
    {
        public IList<SkillComment> SkillCollection { get; set; }
        public int IRFId { get; set; }
    }
}
