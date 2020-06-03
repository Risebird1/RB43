using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class Candidate
    {
        public int id { get; set; }
        public int CandidateMasterId { get; set; }
        public int IRFId { get; set; }

        [Required(ErrorMessage = "Please enter Candidate's Name.")]
        public string Name { get; set; }

        [Display(Name = "Candidate Email")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        [Required(ErrorMessage = "Please enter Candidate Email Id.")]
        public string EmailId { get; set; }

        [Display(Name = "Resume Link")]
        
        public string ResumeLink { get; set; }
        public string IRFName { get; set; }
        public int TotalInvites { get; set; }
        public int InviteConfirmation { get; set; }

        
        public string Type { get; set; }

        [DisplayName("Location Submitted For")]
       
        public string Location { get; set; }
        [DisplayName("Current Location")]
        
        //[RegularExpression("^[a-zA-Z]{1,20}$", ErrorMessage = "Kindly Enter only letters.")]
        public string CurrentLocation { get; set; }
       
        public string Skill { get; set; }
        [DisplayName("Work Authorization")]
      
        public string WorkAuth { get; set; }
        [DisplayName("Current Employer")]
        //[Required]
        public string CurrentEmployer { get; set; }
        [Required]
        public string Phone { get; set; }

        [DisplayName("Total Experience(years)")]
       
        public decimal TotalExperience { get; set; }
        [DisplayName("Current Salary(Per Hour in $)")]
       
        public decimal CurrentSalary { get; set; }
        [DisplayName("Expected Salary(lacs)")]
       
        public decimal ExpectedSalary { get; set; }
       
        public decimal Notice { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime StartDate { get; set; }
        public string CalendlyLink { get; set; }
        public string ClientName { get; set; }
        public string ClientCompName { get; set; }
        public string JDTiltle { get; set; }
        public string JDLink { get; set; }
        public string JoinabilityComments { get; set; }
        public string SelectabilityComments { get; set; }
        public double SelectabilityScore { get; set; }
        public double JoinabilityScore { get; set; }
        public int AddedByUserId { get; set; }
        public int? Status { get; set; }
        public string StatusValue { get; set; }
    }
}
