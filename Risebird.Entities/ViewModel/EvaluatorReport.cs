using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class EvaluatorReport
    {
        public int id { get; set; }
        public int CandidateId { get; set; }
        public int ResumeEvaluatorId { get; set; }
        public string ResumeEvaluatorEmailId { get; set; }
        public int IRFId { get; set; }
        public string IRFName { get; set; }

        [DisplayName("Candidate's Name")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        [Required]
        public string CandidateName { get; set; }

        [DisplayName("Candidate EmailId")]

        public string CandidateEmailId { get; set; }
        public string JDLink { get; set; }

        //[DisplayName("Confirmation Status")]
        //[Required]
        //public string confirmationStatus { get; set; }

        //[DisplayName("Overall Score")]
        //[Required]
        //public int OverallScore { get; set; }

        //[DisplayName("Overall Rating")]
        //[Required]
        //public int Rating { get; set; }
        [Display(Name = "Current Employer")]
        public string currentEmployerName { get; set; }

        //[Display(Name = "Expected Salary")]
        //public string expectedPerHourRate { get; set; }

        public IList<SelectabilityEvaluatorReport> SelectabilityEvaluatorReportCollection { get; set; }
        public IList<TechSkillEvaluatorReport> TechSkillEvaluatorReportCollection { get; set; }
        public IList<SkillGeneralEvaluatorReport> SkillGeneralEvaluatorReportCollection { get; set; }
        public IList<CandidateAudio> CandidateAudioCollection { get; set; }

        [DisplayName("Selectability Comments")]
        public string SelectabilityComments { get; set; }

        [DisplayName("Tech Skill Comments")]
        public string TechSkillComments { get; set; }
        [DisplayName("General Skills Comments")]
        public string SkillGeneralComments { get; set; }
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
        public HRReport HRReport { get; set; }

        [DisplayName("Selectability Score")]

        public double SelectabilityScore { get; set; }
        [DisplayName("Tech Skills Score")]
        public double TechSkillScore { get; set; }
        [DisplayName("General Skills Score")]
        public double SkillGeneralScore { get; set; }
        public SelectList LevelTech { get; set; }
        public bool IsJoinabilitySelectabilityRequired { get; set; }


    }
}
