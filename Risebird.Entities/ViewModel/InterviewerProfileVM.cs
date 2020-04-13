using Microsoft.AspNetCore.Http;
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
        public string NickName { get; set; }
        [Display(Name = "Email ID")]
        [Required(ErrorMessage = "Please Enter Email ID")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
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
        [Required(ErrorMessage = "Please enter a link.")]
        public string ResumeLink { get; set; }
        [Display(Name = "Upload Resume")]
        public IFormFile File { get; set; }
        public string CodedResumeLink { get; set; }
        [Display(Name = "Lead Interviewer")]
        public bool IsLeadInterviewer { get; set; }
        public bool IsActive { get; set; }
        [Display(Name = "Approved")]
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
        public int[] SelectedSkillIds { get; set; }
        public SelectList CompanyTypeList { get; set; }
        public SelectList LevelList { get; set; }
        public SelectList GraduationTypeList { get; set; }
        public SelectList CollegeTypeList { get; set; }
        public SelectList DayTimeSlotList { get; set; }
        public SelectList DaySlotList { get; set; }

    }
}
