using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Data
{
    public class Recruiter
    {
        public int id { get; set; }
        public int UserId { get; set; }

        [Required(ErrorMessage = "Please enter Domain")]
        public string Domain { get; set; }
        [Display(Name = "Email ID")]
        [Required(ErrorMessage = "Please Enter Email ID")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        public string EmailId { get; set; }

        [Display(Name = "Mobile Number")]

        public string MobileNumber { get; set; }

        [Required(ErrorMessage = "Please enter First Name")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Please enter Last Name")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
    }
}
