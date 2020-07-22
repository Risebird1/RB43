using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class TechSkillComments
    {
        public IList<SkillComment> SkillCollection { get; set; }
        public int IRFId { get; set; }
        public string Message { get; set; }
        public SelectList Skills { get; set; }
    }
}
