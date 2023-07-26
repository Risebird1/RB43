using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class ClientUserVM
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
        public string MobileNumber { get; set; }
        public int idProfile { get; set; }
        public string ClientName { get; set; }
        public string Role { get; set; }

        [Required(ErrorMessage = "Please Select Client")]
        public int? SelectedClient { get; set; }
        [Display(Name = "Client")]
        public SelectList Clients { get; set; }

        [Required(ErrorMessage = "Please Select Client Role")]
        public int? SelectedClientRole { get; set; }
        [Display(Name = "Client Role")]
        public SelectList ClientRoles { get; set; }

        public int ClientId { get; set; }
        public int UserId { get; set; }

        [Display(Name = "Password")]
        public string UserPassword { get; set; }
        [Display(Name = "Whatsapp Number")]
        public string WhatsappNumber { get; set; }
        //[Display(Name = "Password Reset after first login/Password aging")]
        //public bool IsPasswordReset { get; set; }
        public bool IsRecruiterActive { get; set; }
        public bool IsQCRequired { get; set; }
        public bool IsRedoInterview { get; set; }
        [Display(Name = "MCQ Interiew Invite Allowed Limit")]
        public int MCQValidation { get; set; }
        public string uniqueid { get; set; }
        public bool IsClientCancelInterview { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
        public string logolink { get; set; }
    }
}
