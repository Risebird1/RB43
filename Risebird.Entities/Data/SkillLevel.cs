using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Data
{
    public class SkillLevel
    {
        public int id { get; set; }
        [Display(Name = "Skill Level Name")]
        [Required(ErrorMessage = "Please Enter Skill Level Name")]
        public string Name { get; set; }
        [Display(Name = "Skill Level Description")]
        [Required(ErrorMessage = "Please Enter Skill Level Description")]
        public string Description { get; set; }
    }
}
