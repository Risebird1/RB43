using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ClonableTemplateQuestion
    {
        public int id { get; set; }
        public int ClonableIRFId { get; set; }
        public int UserId { get; set; }
        public int SkillId { get; set; }
        public string Question { get; set; }
        public string SkillName { get; set; }
        public bool IsSelected { get; set; }
        
    }
}
