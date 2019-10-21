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
        public int IRFId { get; set; }

        [Required(ErrorMessage = "Please enter Candidate's Name.")]
        public string Name { get; set; }

        [Display(Name = "Candidate Email")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        [Required(ErrorMessage = "Please enter Candidate Email Id.")]
        public string EmailId { get; set; }

        [Display(Name = "Resume Link")]
        [Required]
        public string ResumeLink { get; set; }
        public string IRFName { get; set; }
        public int TotalInvites { get; set; }
        public int InviteConfirmation { get; set; }

        [Required]
        public string Type { get; set; }

        [DisplayName("Location Submitted For")]
        [Required]
        public string Location { get; set; }
        [DisplayName("Current Location")]
        [Required]
        //[RegularExpression("^[a-zA-Z]{1,20}$", ErrorMessage = "Kindly Enter only letters.")]
        public string CurrentLocation { get; set; }
        [Required]
        public string Skill { get; set; }
        [DisplayName("Work Authorization")]
        [Required]
        public string WorkAuth { get; set; }
        [DisplayName("Current Employer")]
        //[Required]
        public string CurrentEmployer { get; set; }
        [Required]
        public string Phone { get; set; }

        [DisplayName("Total Experience(years)")]
        [Required]
        public decimal TotalExperience { get; set; }
        [DisplayName("Current Salary(lacs)")]
        [Required]
        public decimal CurrentSalary { get; set; }
        [DisplayName("Expected Salary(lacs)")]
        [Required]
        public decimal ExpectedSalary { get; set; }

        [Required]
        public decimal Notice { get; set; }
        

    }
}
