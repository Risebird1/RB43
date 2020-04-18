using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerProfileVM
    {
        public int id { get; set; }
        public int UserId { get; set; }
        [Required(ErrorMessage = "Please enter First Name")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Please Enter Last Name")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Please Enter Nick Name")]
        [Display(Name = "Nick Name")]
        [Remote("IsNickNameExists", "User",AdditionalFields = "UserId", ErrorMessage = "Nick Name already in use")]
        public string NickName { get; set; }

        [Display(Name = "Email ID")]
        [Required(ErrorMessage = "Please Enter Email ID")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        [Remote("IsEmailIdExists", "User", AdditionalFields = "UserId", ErrorMessage = "EmailId already in use")]
        public string EmailId { get; set; }

        [Display(Name = "Alternate Email ID")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        public string AltEmailId { get; set; }

        [Display(Name = "Current Location")]
        public string Location { get; set; }

        [Display(Name = "Mobile Number")]
        [Required(ErrorMessage ="Please enter Mobile Number")]
        public string MobileNumber { get; set; }

        [Display(Name = "Altername Mobile Number")]
        [Required(ErrorMessage = "Please enter Alternate Mobile Number")]
        public string AltMobileNumber { get; set; }

        [Display(Name = "Current Role")]
        [Required(ErrorMessage = "Please enter Current Role")]
        public string CurrentRole { get; set; }

        [Display(Name = "Years of Experience")]
        [Required(ErrorMessage = "Please enter Years of Experience")]
        public decimal Experience { get; set; }

        [Display(Name = "Preferred Domain")]
        [Required(ErrorMessage = "Please enter Preferred Domain")]
        public string PreferredDomain { get; set; }

        [Display(Name = "Resume Link")]
        //[Required(ErrorMessage = "Please enter a link.")]
        public string ResumeLink { get; set; }

        [Display(Name = "Upload Resume")]
        public IFormFile ResumeFile { get; set; }
        [Display(Name = "Upload Coded Resume")]
        public IFormFile CodedResumeFile { get; set; }

        public string CodedResumeLink { get; set; }

        [Display(Name = "Lead Interviewer")]
        public bool IsLeadInterviewer { get; set; }

        public bool IsActive { get; set; }
        [Display(Name = "IsApproved")]
        public bool IsApproved { get; set; }


        // creating List items 
        public IList<TransInterviewerSkill> InterviewerSkill { get; set; }
        public IList<TransInterviewerEducation> InterviewerEducationDetails { get; set; }
        public IList<TransInterviewerProfessional> InterviewerProfessionalDetails { get; set; }
        public IList<TransInterviewerProjects> InterviewerProjectsDetails { get; set; }
        public IList<TransInterviewerAvailability> InterviewerAvailabilityDetails { get; set; }
        public IList<TransInterviewerAccDetails> InterviewerAccDetails { get; set; }
        public IList<TransInterviewerCertificate> InterviewerCertificateDetails { get; set; }

        //To add in the dropdown List
        [Display(Name = "Skills List")]
        public SelectList SkillTypeList { get; set; }
        // To add in selected from the user
        //public int[] SelectedSkillIds { get; set; }
        public SelectList CompanyTypeList { get; set; }
        public SelectList LevelList { get; set; }
        public SelectList GraduationTypeList { get; set; }
        public SelectList CollegeTypeList { get; set; }
        public SelectList DayTimeSlotList { get; set; }
        public SelectList DaySlotList { get; set; }

        // To add the below fields used in View to display label and Validation

        [Display(Name = "Company Type")]
        [Required]
        public string CompanyType { get; set; }

        [Display(Name = "Level")]
        [Required]
        public string Level { get; set; }

        [Display(Name = "Graduation Type")]
        [Required]
        public string GraduationType { get; set; }

        [Display(Name = "College Type")]
        [Required]
        public string CollegeType { get; set; }

        [Display(Name = "Day Time Slot")]
        [Required]
        public string DayTimeSlot { get; set; }

        [Display(Name = "Day Slot")]
        [Required]
        public string DaySlot { get; set; }
    }
}
