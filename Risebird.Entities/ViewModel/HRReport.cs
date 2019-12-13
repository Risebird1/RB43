using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class HRReport
    {
        public int id { get; set; }
        public int CandidateId { get; set; }
        public int HRId { get; set; }
        public string HREmailId { get; set; }
        public int IRFId { get; set; }
        public string IRFName { get; set; }

        [DisplayName("Candidate's Name")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        [Required]
        public string CandidateName { get; set; }
        public string CandidateEmailId { get; set; }
        public string JDLink { get; set; }
        
        [Display(Name = "Current Employer")]
        public string currentEmployerName { get; set; }
        
        public IList<JoinabilityHRReport> JoinabilityHRReportCollection { get; set; }

        [DisplayName("Joinability Comments")]
        public string JoinabilityComments { get; set; }
        public bool IsEdited { get; set; }

        public int JobLevel { get; set; }

        [DisplayName("Job Level")]
        public string JobLevelName { get; set; }

        [DisplayName("Preferred Location")]
        public string PreferredLocation { get; set; }

        [Display(Name = "Uploaded Candidate Resume")]
        public string ResumeLink { get; set; }

        public bool IsClientUser { get; set; }

        public SelectList Level { get; set; }
        //public SelectList LevelTech { get; set; }

    }
}
