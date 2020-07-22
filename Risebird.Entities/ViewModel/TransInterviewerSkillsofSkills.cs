using System;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerSkillsofSkills
    {
        public int id { get; set; }
        public int InterviewerSkillID { get; set; }
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public int LevelId { get; set; }
        public string LevelName { get; set; }
        public int Weightage { get; set; }
    }
    public class TransInterviewerSkillsofGeneralSkill
    {
        public int id { get; set; }
        public int InterviewerSkillID { get; set; }
        public int LevelId { get; set; }
        public string LevelName { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public int Weightage { get; set; }
        public String Name { get; set; }

    }
}
