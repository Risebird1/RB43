using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class Account
    {
        public int id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Client { get; set; }
        [DisplayName("End Client")]
        [Required]
        public string EndClient { get; set; }
        [Required]
        public string Location { get; set; }
        [DisplayName("Account Owner Email")]
        [Required]
        public string AccountOwnerId { get; set; }
        public string ArchiveStatus { get; set; }

        public int TotalProjects { get; set; }
        //public int TotalInterviews { get; set; }
        public bool IsDeleted { get; set; }
        public IList<AccountProjectMapping> AccountProjectMappings { get; set; }
        public bool IsClientUser { get; set; }

        public DateTime CreatedOn { get; set; }

        [Required]
        [DisplayName("Client")]
        public int ClientId { get; set; }
        public SelectList ClientList { get; set; }
        public string AccountStatus { get; set; }

    }
    public class UserProfile
    {
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        public string Role { get; set; }
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; }
        [Display(Name = "Whatsapp Number")]
        public string WhatsappNumber { get; set; }
        [Display(Name = "Image Link")]
        public string ImageLink { get; set; }
        [Display(Name = "Image Upload")]
        public IFormFile ImageUpload { get; set; }
        public string EmailId { get; set; }
    }
}
