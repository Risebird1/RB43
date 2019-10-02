using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class Project
    {
        public int id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Client { get; set; }
        [DisplayName("End Client")]
        [Required]
        public string EndClient { get; set; }
        [Required]
        public string Location { get; set; }
        [DisplayName("Project OwnerEmail")]
        [Required]
        public string ProjectOwnerId { get; set; }

        public int TotalIRF { get; set; }
        public int TotalInterviews { get; set; }
        public bool IsDeleted { get; set; }
        public IList<ProjectIRFMapping> ProjectIRFMappings { get; set; }
        public bool IsClientUser { get; set; }

    }
}
