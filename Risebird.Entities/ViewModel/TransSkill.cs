using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class TransSkill
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int SkillId { get; set; }
        public int LevelId { get; set; }
        public int Weightage { get; set; }
    }
    public class TransSkillGeneral
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int SkillId { get; set; }
        public int LevelId { get; set; }
        public string Type { get; set; }
        public int Weightage { get; set; }
    }
}
