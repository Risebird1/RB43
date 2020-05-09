using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerSkillsVM
    {
        public int id { get; set; }

        [DisplayName("InterviewerSkillId")]
        [Required]
        public string InterviewerSkillId { get; set; }


        [DisplayName("JD Title")]
        [Required]
        public string JDTiltle { get; set; }

        [DisplayName("Start Date")]
        //[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        [Required]
        public DateTime StartDate { get; set; }

        [DisplayName("End Date")]
        [DataType(DataType.DateTime)]
        //[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        [Required]
        public DateTime EndDate { get; set; }

        [DisplayName("Risebird HR")]
        [Required]
        public int RisebirdHr { get; set; }

        [DisplayName("Per Interview Rate")]
        [Required]
        public double PerMintuteRate { get; set; }

        public SelectList JobLevelList { get; set; }
        public IList<TransInterviewerSkillsofSkills> SkillCollection { get; set; }
        public IList<TransInterviewerSkillsofGeneralSkill> SkillGeneral { get; set; }
        [Required]
        public IList<TransInterviewerSkillsSelectability> Selectability { get; set; }
        public IList<TransInterviewerSkillsLeadInterviewer> InterviewerCollection { get; set; }
        public IList<InterviewerVM> InterviewerDetailsCollection { get; set; }

        public SelectList SelectabilityTypeList { get; set; }
        public SelectList SkillGeneralTypeList { get; set; }

        [DisplayName("Calendly Link")]
        public string CalendlyLink { get; set; }
        public string PrimarySkill { get; set; }
        public string LoggedEmail { get; set; }
        public string EmailId { get; set; }
        public string Name { get; set; }
        public bool IsPaused { get; set; }
        public int UserId { get; set; }
        //=================================================================

        [Required(ErrorMessage = "Please enter First Name")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Please Enter Last Name")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; }

        public int userId { get; set; }
        [Display(Name = "Resume Link")]
        [Required(ErrorMessage = "Please enter a link.")]
        public string ResumeLink { get; set; }

        [Required(ErrorMessage = "Please mention available slots")]
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

    }

    public class InterviewerSkillsModel
    {
        public int id { get; set; }
        public string InterviewerSkillId { get; set; }
        public string JDTiltle { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RisebirdHr { get; set; }
        public double PerMintuteRate { get; set; }
        public string CalendlyLink { get; set; }
        public string PrimarySkill { get; set; }
        public string EmailId { get; set; }
        public string Name { get; set; }
        public bool IsPaused { get; set; }
        public string ImageUrl { get; set; }
        public string InterviewerSkillStatus { get; set; }
        public bool IsApproved { get; set; }
        public int UserId { get; set; }

    }
}
