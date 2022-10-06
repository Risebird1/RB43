using System;

namespace Risebird.Entities.ViewModel
{
    public class TransSkill
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int SkillId { get; set; }
        public string SkillName { get; set; }
        public int LevelId { get; set; }
        public string LevelName { get; set; }
        public int Weightage { get; set; }
        public string Concepts { get; set; }
        public string Natureofquestion { get; set; }
        public int Timelimit { get; set; }
    }
    public class TransSkillGeneral
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int LevelId { get; set; }
        public string LevelName { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public int Weightage { get; set; }
        public string Name { get; set; }

    }
}
