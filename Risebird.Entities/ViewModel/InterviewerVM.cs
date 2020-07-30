using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerVM
    {
        public InterviewerVM()
        {
            SubVideo_List = new List<InterviewerVM>();
        }
        public List<InterviewerVM> SubVideo_List { get; set; }

        public int id { get; set; }

        [Required(ErrorMessage = "Please enter First Name")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Please Enter Last Name")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        [Display(Name = "Email ID")]
        [Required(ErrorMessage = "Please Enter Email ID")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        public string EmailId { get; set; }
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; }

        public int userId { get; set; }
        public string PrimarySkill { get; set; }
        [Display(Name = "Resume Link")]
        [Required(ErrorMessage = "Please enter a link.")]
        public string ResumeLink { get; set; }

        //[Required(ErrorMessage = "Please mention available slots")]
        [Display(Name = "Available Slots")]
        public string Slots { get; set; }

        [Display(Name = "Number Of Interview")]
        public int NoOfInterview { get; set; }
        [Display(Name = "Success Rate")]
        public double SuccessRate { get; set; }
        [Display(Name = "Approved")]
        public bool IsApproved { get; set; }

        [Display(Name = "Upload Resume")]
        public IFormFile File { get; set; }

        //[Required(ErrorMessage = "Please mention skill")]
        public int[] SelectedSkillIds { get; set; }
        //public int SelectedSkillId { get; set; }

        [Display(Name = "Skills")]
        public SelectList PrimarySkills { get; set; }

        public string InterviewerSkillId { get; set; }

        public string VideoLink { get; set; }
        public int imid { get; set; }
        public string GitHubLink { get; set; }
        public string LinkedinLink { get; set; }
        public string StackOverflowLink { get; set; }
        public string CurrentRole { get; set; }
        public string Certificate { get; set; }
        public string Education { get; set; }
        public string PassingYear { get; set; }
        public string UniversityName { get; set; }
    }
}
