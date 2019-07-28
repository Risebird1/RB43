using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class QCInterviewerVM
    {
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
        [RegularExpression(@"^(\d{10})$", ErrorMessage = "Please enter a 10 digit mobile number")]
        public long MobileNumber { get; set; }

        public int userId { get; set; }
        public string PrimarySkill { get; set; }
        public string ResumeLink { get; set; }

        [Required(ErrorMessage = "Please mention available slots")]
        [Display(Name = "Availeble Slots")]
        public string Slots { get; set; }

        [Display(Name ="Number Of Interview")]
        public int NoOfInterview { get; set; }
        [Display(Name ="Success Rate")]
        public double SuccessRate { get; set; }
        public bool IsApproved { get; set; }

        [Display(Name = "Upload Resume")]
        public IFormFile File { get; set; }

        //[Required(ErrorMessage = "Please mention skill")]
        public int[] SelectedSkillIds {get;set;}
        //public int SelectedSkillId { get; set; }

        [Display(Name = "Skills")]
        public SelectList PrimarySkills { get; set; }
    }
}
