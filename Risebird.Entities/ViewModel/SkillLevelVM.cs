using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class SkillLevelVM
    {
        [Display(Name="Skill Level Name")]
        [Required(ErrorMessage = "Please Enter Skill Level Name")]
        public string Name { get; set; }
        [Display(Name = "Skill Level Description")]
        [Required(ErrorMessage = "Please Enter Skill Level Description")]
        public string Description { get; set; }
    }
}
