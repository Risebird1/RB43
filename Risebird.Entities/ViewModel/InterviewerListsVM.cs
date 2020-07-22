using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerListsVM
    {
        public int id { get; set; }

        [Required(ErrorMessage = "Please enter First Name")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Email ID")]
        [Required(ErrorMessage = "Please Enter Email ID")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        public string EmailId { get; set; }

        public int UserId { get; set; }
        public string PrimarySkill { get; set; }

        [Display(Name = "Resume Link")]
        [Required(ErrorMessage = "Please enter a link.")]
        public string ResumeLink { get; set; }

        [Display(Name = "Approved")]
        public bool IsAdded { get; set; }

        [Display(Name = "Skills")]
        public SelectList PrimarySkills { get; set; }

        public string InterviewerSkillId { get; set; }
    }
}
