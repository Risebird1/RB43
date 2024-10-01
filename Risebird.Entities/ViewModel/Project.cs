using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
        [DisplayName("Project Owner Email")]
        [Required]
        public string ProjectOwnerId { get; set; }
        public string ArchiveStatus { get; set; }

        public int TotalIRF { get; set; }
        public int TotalInterviews { get; set; }
        public bool IsDeleted { get; set; }
        public IList<ProjectIRFMapping> ProjectIRFMappings { get; set; }
        public bool IsClientUser { get; set; }

        [Required]
        [DisplayName("Client")]
        public int ClientId { get; set; }
        public DateTime CreatedOn { get; set; }
        public SelectList ClientList { get; set; }
        public string ProjectStatus { get; set; }
        public string ProjectUniqueid { get; set; }
        public int SkillSetsevaluate { get; set; }
        [DisplayName("Project Title")]
        public string ProjectTitle { get; set; }
    }
}
