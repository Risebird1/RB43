using System.ComponentModel.DataAnnotations;
namespace Risebird.Entities.Data
{
    public class GeneralSkillMatrix
    {
        public int id { get; set; }

        [Display(Name = "Skill Name")]
        [Required(ErrorMessage = "Please Enter GeneralSkill Matrix Name")]
        public string Name { get; set; }

        [Display(Name = "Skill Description")]
        [Required(ErrorMessage = "Please Enter GeneralSkill Matrix Description")]
        public string Description { get; set; }
    }
}
