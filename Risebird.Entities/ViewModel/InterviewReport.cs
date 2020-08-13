using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class InterviewReport
    {
        public int id { get; set; }
        public int CandidateId { get; set; }
        public int IRFId { get; set; }
        public string IRFName { get; set; }

        public int InterviewId { get; set; }

        [DisplayName("Candidate's Name")]
        //[RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        //ErrorMessage = "Please Enter Correct Email Address")]
        //[Required]
        public string CandidateName { get; set; }

        [DisplayName("Confirmation Status")]
        //[Required]
        public string confirmationStatus { get; set; }

        [DisplayName("Overall Score")]
        [Required]
        public int OverallScore { get; set; }

        [DisplayName("Overall Rating")]
        [Required]
        public int Rating { get; set; }
        [Display(Name = "Current Employer")]
        public string currentEmployerName { get; set; }

        [Display(Name = "Expected Salary")]
        public string expectedPerHourRate { get; set; }
        [Display(Name = "Notice Period")]
        public string joiningTime { get; set; }

        public ProctoringReport Proctoring { get; set; }

        public IList<QuestionsDiscussed> QuestionsCollection { get; set; }

        public IList<TechSkillReport> TechSkillReportCollection { get; set; }

        public IList<SkillGeneralReport> SkillGeneralReportCollection { get; set; }

        //public IList<JoinabilityReport> JoinabilityReportCollection { get; set; }
        //JoinabilityHRReportCollection
        public IList<JoinabilityHRReport> JoinabilityHRReportCollection { get; set; }
        //public IList<SelectabilityReport> SelectabilityReportCollection { get; set; }
        //SelectabilityEvaluatorReportCollection
        public IList<SelectabilityEvaluatorReport> SelectabilityEvaluatorReportCollection { get; set; }

        [DisplayName("Technical Skill Comments")]
        public string TechSkillComments { get; set; }
        [DisplayName("General Skills Comments")]
        public string SkillGeneralComments { get; set; }
        [DisplayName("Joinability Comments")]
        public string JoinabilityComments { get; set; }
        [DisplayName("Selectability Comments")]
        public string SelectabilityComments { get; set; }
        public bool IsEdited { get; set; }

        public int JobLevel { get; set; }

        [DisplayName("Job Level")]
        public string JobLevelName { get; set; }

        public int? InterviewStatus { get; set; }
        public int? MeetingStatus { get; set; }

        public string PageVideoLink { get; set; }
        public string AudioLink { get; set; }
        public string VideoLink { get; set; }
        [DisplayName("Preferred Location")]
        public string PreferredLocation { get; set; }


        [Display(Name = "Interview Status")]
        public SelectList InterviewStatusList { get; set; }

        [Display(Name = "Meeting Status")]
        public SelectList MeetingStatusList { get; set; }

        [Display(Name = "Upload Image")]
        public IFormFile AudioFile { get; set; }

        [Display(Name = "Upload Video")]
        public IFormFile VideoFile { get; set; }
        [Display(Name = "Interviewer Comment")]
        public string InterviewerComment { get; set; }

        [Display(Name = "Uploaded Candidate Resume")]
        public string resumeLink { get; set; }
        [Display(Name = "Interview Date")]
        public string interviewDate { get; set; }

        public bool IsClientUser { get; set; }

        public SelectList Level { get; set; }
        public SelectList LevelTech { get; set; }
        public SelectList QuestionsLevelList { get; set; }
        public SelectList ConfirmationTypeList { get; set; }
        public SelectList QuestionsList { get; set; }
        [Display(Name = "Proctoring Count")]
        public int ProctoringCount { get; set; }

        public bool IsInterviewer { get; set; }
        public int? IsApproved { get; set; }
        public string ApprovalComments { get; set; }

        public int? IsSelected { get; set; }
        public string FinalComments { get; set; }
        public bool IsQC { get; set; }
        public bool IsBiometricEnabled { get; set; }
        public double InterviewBiometryScore { get; set; }
        public double JoiningBiometryScore { get; set; }

        [Display(Name = "Candidate Email")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        //  [Required(ErrorMessage = "Please enter Candidate Email Id.")]
        public string CandidateEmail { get; set; }

        [Display(Name = "Mobile Number")]
        public string CandidateMobileNumber { get; set; }
        [Display(Name = "Candidate Visa")]
        public string CandidateVisa { get; set; }
        public string ThumbnailImage { get; set; }
        public string PrimarySkill { get; set; }

        [Display(Name = "Feedback Audio File")]
        public IFormFile FeedbackAudioFile { get; set; }

        public string FeedbackAudioLink { get; set; }


        [Display(Name = "Skill Quality")]
        public string SkillQuality { get; set; }

        public SelectList SkillQualityList { get; set; }
        [Display(Name = "Certification Quality")]
        public string CertificationQuality { get; set; }

        public SelectList CertificationQualityList { get; set; }
        [Display(Name = "Education Quality")]
        public string EducationQuality { get; set; }

        public SelectList EducationQualityList { get; set; }
        [Display(Name = "Employers Quality")]
        public string EmployersQuality { get; set; }
        public SelectList EmployersQualityList { get; set; }

        [Display(Name = "Career Progression")]
        public string CareerProgression { get; set; }

        public SelectList CareerProgressionList { get; set; }
        [Display(Name = "Projects Quality")]
        public string ProjectsQuality { get; set; }
        public SelectList ProjectsQualityList { get; set; }

        [Display(Name = "Online Presence")]
        public string OnlinePresence { get; set; }

        public SelectList OnlinePresenceList { get; set; }
        [Display(Name = "Behavioral Score")]
        public string BehavioralScore { get; set; }
        public SelectList BehavioralScoreList { get; set; }

        [Display(Name = "Interviwer Communication")]
        public string InterviwerCommunication { get; set; }

        public SelectList InterviwerCommunicationList { get; set; }
        [Display(Name = "Interview Experience")]
        public string Interviewexperience { get; set; }
        public SelectList InterviewexperienceList { get; set; }

        [Display(Name = "Number of interviews completed")]
        public string Interviewscompleted { get; set; }
        [Display(Name = "Readymade Assessment")]
        public string ReadymadeAssessment { get; set; }
        public string Creativity { get; set; }
        [Display(Name = "Company Size")]
        public string CompanySize { get; set; }
        [Display(Name = "Job Function")]
        public string JobFunction { get; set; }
        public string Industry { get; set; }
        [Display(Name = "Company Headquarters")]
        public string CompanyHeadquarters { get; set; }
        [Display(Name = "Company Type")]
        public string CompanyType { get; set; }
        public SelectList CompanyTypelist { get; set; }
        [Display(Name = "Company Stage")]
        public string CompanyStage { get; set; }
        public SelectList CompanyStageList { get; set; }

        [Display(Name = "Skill Category")]
        public string SkillCategory { get; set; }
        public SelectList SkillCategoryList { get; set; }

        [Display(Name = "Years of experience")]
        public string Yearsofexperience { get; set; }
        public SelectList YearsofexperienceList { get; set; }

        [Display(Name = "Seniority level")]
        public string Senioritylevel { get; set; }
        public SelectList SenioritylevelList { get; set; }

        [Display(Name = "Skill Nature")]
        public string SkillNature { get; set; }
        public SelectList SkillNatureList { get; set; }

        public string Domain { get; set; }
        public string SkillIP { get; set; }
        [Display(Name = "PerHour Rate -India")]
        public string PerHourRateIndia { get; set; }
        public string DecisionMaking { get; set; }
        public string Articulation { get; set; }
        public string ReadinessOnlineInterviews { get; set; }
        public string Teamwork { get; set; }
        public string RelationshipManagement { get; set; }
        public string ConflictResolution { get; set; }
        public string Adaptability { get; set; }
        public string ModeInterviews { get; set; }
        public string WorkUnderPressure { get; set; }
        public string TimeManagement { get; set; }

        public string SharedEmailAddress { get; set; }

    }
}
