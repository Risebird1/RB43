namespace Risebird.Entities.ViewModel
{
    public class TransTemplateTechSkill
    {
        public int id { get; set; }
        public int TemplateId { get; set; }

        //[Required(ErrorMessage = "The Skill field is required")]
        public int? SkillId { get; set; }

        //[Required(ErrorMessage = "The Level field is required")]
        public int? LevelId { get; set; }

        //[Required(ErrorMessage = "The Weightage field is required")]
        public int? Weightage { get; set; }
        public bool IsSelected { get; set; }

    }
}
