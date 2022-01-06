using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class FitmentReportModel
    {
        public int CandidateId { get; set; }
        public int IRFId { get; set; }
        public string IRFName { get; set; }

        [DisplayName("Candidate's Name")]        
        public string CandidateName { get; set; }

        [DisplayName("Candidate EmailId")]
        public string CandidateEmailId { get; set; }

        [Display(Name = "Uploaded Candidate Resume")]
        public string ResumeLink { get; set; }

        [DisplayName("Fitment Status")]
        public int? FitmentStatus { get; set; }

        [DisplayName("Priority Status")]
        public int? PriorityStatus { get; set; }

        [DisplayName("Completion Status")]
        public int? CompletionStatus { get; set; }
        public IList<SelectabilityEvaluatorReport> SelectabilityReportCollection { get; set; }
        public IList<JoinabilityHRReport> JoinabilityReportCollection { get; set; }
        public bool IsEdited { get; set; }

        [DisplayName("Joinability Comments")]
        public string JoinabilityComments { get; set; }

        [DisplayName("Selectibility Comments")]
        public string SelectabilityComments { get; set; }
        public SelectList Level { get; set; }
        public string Profile { get; set; }

        [DisplayName("Selectability Score")]
        public double SelectabilityScore { get; set; }

        [DisplayName("Joinability Score")]
        public double JoinabilityScore { get; set; }

        [DisplayName("Fitment Score")]
        public double FitmentScore { get; set; }

    }
}
