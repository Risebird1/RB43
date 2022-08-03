
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Data
{
    public class UserList
    {
        public int Id { get; set; }

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

        public bool IsActive { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime LastLogout { get; set; }
        public string ImageURL { get; set; }

        [Required(ErrorMessage = "Please Select User Profile")]
        public int? IdProfile { get; set; }
        [Display(Name = "User Profile")]
        public SelectList UserProfiles { get; set; }
        public string Name { get; set; }
        public string profileName { get; set; }
        public string UserPassword { get; set; }
        [Display(Name = "Whatsapp Number")]
        public string WhatsappNumber { get; set; }
        public string ClientIds { get; set; }
        public List<SelectListItem> ClientList { get; set; }
        public string[] ClientsArray { get; set; }
    }
}
