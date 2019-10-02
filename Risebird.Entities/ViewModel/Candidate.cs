using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class Candidate
    {
        public int id { get; set; }
        public int IRFId { get; set; }

        [Required(ErrorMessage = "Please enter Candidate's Name.")]
        public string Name { get; set; }

        [Display(Name = "Candidate Email")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        [Required(ErrorMessage = "Please enter Candidate Email Id.")]
        public string EmailId { get; set; }

        [Display(Name = "Resume Link")]
        [Required]
        public string ResumeLink { get; set; }
        public string IRFName { get; set; }
    }
}
