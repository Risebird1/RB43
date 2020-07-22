using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Data
{
    public class Skill
    {
        [Display(Name = "Skill Id")]
        [Required(ErrorMessage = "Please Enter Skill Name")]
        public int id { get; set; }

        //[Display(Name = "Skill Name")]
        //[Required(ErrorMessage = "Please Enter Skill Name")]
        public string Name { get; set; }

        //[Display(Name = "Skill Description")]
        //[Required(ErrorMessage = "Please Enter Skill Description")]
        public string Description { get; set; }
    }
}
