using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerSkill
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public int SkillId { get; set; }
        public string Level { get; set; }
        public string SkillName { get; set; }
    }
}
