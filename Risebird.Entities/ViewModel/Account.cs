using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

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
}
