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
        public string Type { get; set; }
        public string CompanyStage { get; set; }
        public string Size { get; set; }
        public int JobFunctionId { get; set; }
        public int IndustryId { get; set; }
        public string Headquarters { get; set; }
        public string Role { get; set; }

        [Required(ErrorMessage = "Please enter your Domain")]
        [Display(Name = "Domain")]
        public string Domain { get; set; }
        public string SeniorityLevel { get; set; }

        [Required(ErrorMessage = "Please enter Years of Experience")]
        [Display(Name = "Years of Experience")]
        public int ExperienceId { get; set; }
        public string WorkNature { get; set; }
        public string PrimaryResponsibility { get; set; }
        public string CompanyLinkedinPage { get; set; }
        public string CompanyWebsite { get; set; }

    }
}
