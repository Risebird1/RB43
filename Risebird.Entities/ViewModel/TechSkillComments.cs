using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class TechSkillComments
    {
        public TechSkillComments()
        {
            SkillCollection = (IList<SkillComment>)(new List<SkillComment>());
            IrfName = string.Empty;
        }
        public IList<SkillComment> SkillCollection { get; set; }
        public int IRFId { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
        public string Profile { get; set; }
        public SelectList Skills { get; set; }
        public string SkillsOptionsData { get; set; }
        public string IrfName { get; set; }
    }
}
