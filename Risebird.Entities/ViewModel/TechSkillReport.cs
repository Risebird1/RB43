using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class TechSkillReport
    {
        public int id { get; set; }
        public int InterviewId { get; set; }
        public string RequiredLevel { get; set; }
        public int RequiredLevelId { get; set; }
        public string SkillName { get; set; }
        public int SkillId { get; set; }
        public string ActualLevel { get; set; }
        public int ActualLevelId { get; set; }
        public float WeightageRequired { get; set; }
        public float WeightageActual { get; set; }
        public string TechComments { get; set; }
        public string Concepts { get; set; }
        [Display(Name = "Nature of Question")]
        public string Natureofquestion { get; set; }
        [Display(Name = "Time Limit")]
        public int Timelimit { get; set; }
    }
}
