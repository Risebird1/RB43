using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class InterviewReport
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int InterviewId { get; set; }

        [DisplayName("Candidate's Mail")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        [Required]
        public string CandidateName { get; set; }

        [DisplayName("Confirmation Status")]
        [Required]
        public string confirmationStatus { get; set; }

        [DisplayName("Overall Score")]
        [Required]
        public int OverallScore { get; set; }

        public IList<QuestionsDiscussed> QuestionsCollection { get; set; }

        public IList<TechSkillReport> TechSkillReportCollection { get; set; }

        public IList<SkillGeneralReport> SkillGeneralReportCollection { get; set; }

        public IList<JoinabilityReport> JoinabilityReportCollection { get; set; }

    }
}
