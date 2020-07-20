using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerProfessional
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }

        [Required(ErrorMessage = "Please enter Company Name")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }
        [Display(Name = "Company Type")]
        public string Type { get; set; }
        [Display(Name = "Company Stage")]
        public string CompanyStage { get; set; }
        [Display(Name = "Company Size")]
        public string Size { get; set; }
        [Display(Name = "Job Function")]
        public int JobFunctionId { get; set; }
        [Display(Name = "Industry")]
        public int IndustryId { get; set; }
        [Display(Name = "Headquarters")]
        public string Headquarters { get; set; }
        [Display(Name = "Role")]
        public string Role { get; set; }

        [Required(ErrorMessage = "Please enter your Domain")]
        [Display(Name = "Domain")]
        public string Domain { get; set; }
        [Display(Name = "Seniority Level")]
        public string SeniorityLevel { get; set; }

        [Required(ErrorMessage = "Please enter Years of Experience")]
        [Display(Name = "Years of Experience")]
        public int ExperienceId { get; set; }
        [Display(Name = "Work Nature")]
        public string WorkNature { get; set; }
        [Display(Name = "Primary Responsibility")]
        public string PrimaryResponsibility { get; set; }
        [Display(Name = "Company Linkedin Page")]
        public string CompanyLinkedinPage { get; set; }
        [Display(Name = "Company Website")]
        public string CompanyWebsite { get; set; }

    }
}
