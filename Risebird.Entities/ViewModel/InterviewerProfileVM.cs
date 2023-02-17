using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerProfileVM
    {
        public InterviewerProfileVM()
        {
            DOB = DateTime.Now;
        }
        public int id { get; set; }
        public int UserId { get; set; }
        [Required(ErrorMessage = "Please enter First Name")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Please Enter Last Name")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        //[Required(ErrorMessage = "Please Enter Coded Name")]
        [Display(Name = "Coded Name")]
        //[Remote("IsNickNameExists", "Interviewer", AdditionalFields = "UserId", ErrorMessage = "Coded Name already in use")]
        public string NickName { get; set; }

        public bool IsEdit { get; set; }


        [Display(Name = "Email ID")]
        [Required(ErrorMessage = "Please Enter Email ID")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        //[Remote("IsEmailIdExists", "Interviewer", AdditionalFields = "UserId,IsEdit", ErrorMessage = "EmailId already in use", HttpMethod = "POST")]
        public string EmailId { get; set; }

        [Display(Name = "Alternate Email ID")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        public string AltEmailId { get; set; }
        [Display(Name = "Date of Birth")]
        //[Required(ErrorMessage = "Please Enter Date Of Birth")]
        [DataType(DataType.Date)]
        //[DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime DOB { get; set; }
        [Display(Name = "Gender")]
        [Required]
        public string Gender { get; set; }

        [Display(Name = "Annual Salary")]
        public int AnnualSalary { get; set; }
        [Display(Name = "Country")]
        public string Country { get; set; }
        [Display(Name = "Current City")]
        public string City { get; set; }
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; }

        [Display(Name = "Alternate Mobile Number")]
        public string AltMobileNumber { get; set; }

        [Display(Name = "Current Role")]

        public string CurrentRole { get; set; }

        [Display(Name = "Years of Experience")]
        public decimal Experience { get; set; }

        [Display(Name = "Preferred Domain")]
        //[Required(ErrorMessage = "Please enter Preferred Domain")]
        public string PreferredDomain { get; set; }

        [Display(Name = "Resume Link")]
        public string ResumeLink { get; set; }

        [Display(Name = "Upload Resume")]
        public IFormFile ResumeFile { get; set; }
        [Display(Name = "Upload Coded Resume")]
        public IFormFile CodedResumeFile { get; set; }

        public string CodedResumeLink { get; set; }
        [Display(Name = "GitHub Link")]
        public string GitHubLink { get; set; }
        [Display(Name = "StackOverflow Link")]
        public string StackOverflowLink { get; set; }
        [Display(Name = "Linkedin Link")]
        public string LinkedinLink { get; set; }
        [Display(Name = "Blog Link")]
        public string Blog { get; set; }

        [Display(Name = "Lead Interviewer")]
        public bool IsLeadInterviewer { get; set; }
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }
        [Display(Name = "Is Approved")]
        public bool IsApproved { get; set; }


        // creating List items 
        public IList<TransInterviewerSkill> InterviewerSkill { get; set; }
        public IList<TransInterviewerEducation> InterviewerEducationDetails { get; set; }
        public IList<InterviewerProfessional> InterviewerProfessionalDetails { get; set; }
        public IList<TransInterviewerProjects> InterviewerProjectsDetails { get; set; }
        public IList<TransInterviewerAvailability> InterviewerAvailabilityDetails { get; set; }
        //public IList<TransInterviewerAccDetails> InterviewerAccDetails { get; set; }
        public TransInterviewerAccDetails InterviewerAccDetails { get; set; }
        public IList<TransInterviewerCertificate> InterviewerCertificateDetails { get; set; }

        //To add in the dropdown List
        [Display(Name = "Skills List")]
        public SelectList SkillTypeList { get; set; }
        public SelectList GenderList { get; set; }
        public SelectList CountryList { get; set; }

        //To Skill the dropdown List
        public SelectList SkillCategoryList { get; set; }
        public SelectList SkillIPList { get; set; }
        public SelectList SkillExpertiseLevelList { get; set; }
        public SelectList SkillYearsOfExperienceList { get; set; }
        public SelectList SkillNatureList { get; set; }
        //=========Availability============
        public SelectList WeekDayList { get; set; }
        public SelectList WeekEndList { get; set; }

        // To add in selected from the user
        //public int[] SelectedSkillIds { get; set; }
        public SelectList CompanyTypeList { get; set; }
        public SelectList LevelList { get; set; }
        public SelectList GraduationTypeList { get; set; }
        [Display(Name = "Education Mode")]
        public SelectList EducationModeList { get; set; }
        public SelectList DegreeList { get; set; }
        public SelectList NatureOfProjectList { get; set; }


        public SelectList CollegeTypeList { get; set; }
        public SelectList DayTimeSlotList { get; set; }
        public SelectList DaySlotList { get; set; }

        // To add the below fields used in View to display label and Validation

        [Display(Name = "Company Type")]
        public string CompanyType { get; set; }

        [Display(Name = "Level")]
        public string Level { get; set; }

        [Display(Name = "Graduation Type")]
        public string GraduationType { get; set; }

        [Display(Name = "College Type")]
        public string CollegeType { get; set; }

        [Display(Name = "Day Time Slot")]
        public string DayTimeSlot { get; set; }

        [Display(Name = "Day Slot")]
        public string DaySlot { get; set; }

        [Display(Name = "Skills")]
        public string PrimarySkill { get; set; }
        [Display(Name = "Assessment Group")]
        public string AssessmentGroupId { get; set; }

        //[Required]
        public string[] PreviousAssessmentGroupArray { get; set; }

        public string AssessmentGroupDifference { get; set; }
        public string[] AssessmentGroupArrayOld { get; set; }
        public string[] AssessmentGroupArray { get; set; }
        public List<SelectListItem> AssessmentGroupList { get; set; }
        public string Senioritylevel { get; set; }
        public string PerHourRateIndia { get; set; }
        public string PerHourRateOutside { get; set; }
        public string CompentencyLevel { get; set; }
        public string ProfileType { get; set; }
        public string Domain { get; set; }
        public string DomainName { get; set; }
        public string UserPassword { get; set; }
        [Display(Name = "Whatsapp Number")]
        public string WhatsappNumber { get; set; }
        public string ImageURL { get; set; }
        [Display(Name = "Image Upload")]
        public IFormFile ImageUpload { get; set; }
        [Display(Name = "Available for Insta interview (You have to be available within 30 mins)")]
        public bool Isinstaavailable { get; set; }

        //[Display(Name = "Product Domain")]
        //public bool IsProductDomain { get; set; }

        //[Display(Name = "Service Domain")]
        //public bool IsServiceDomain { get; set; }

        [Display(Name = "Service Rate (India)")]
        public int IndiaServiceRate { get; set; }

        [Display(Name = "Service Rate (Outside India)")]
        public int OutsideIndiaServiceRate { get; set; }

        [Display(Name = "Product Rate(India)")]
        public int IndiaProductRate { get; set; }

        [Display(Name = "Product Rate(Outside India)")]
        public int OutsideIndiaProductRate { get; set; }

        public SelectList IndiaServiceRateList { get; set; }
        public SelectList OutsideIndiaServiceRateList { get; set; }
        public SelectList AvailabilityStatus { get; set; }
        [Display(Name = "Available for interview")]
        public int Isavailable { get; set; }
        public string AvailableDetails { get; set; }
        public string uniqueid { get; set; }
        public bool IsInterested { get; set; }
        public string type { get; set; }
        public string ResourceUrl { get; set; }
        public bool isVerified { get; set; }
        public DateTime? LastLogin { get; set; }
        public string CreatedBy { get; set; }
    }
}
