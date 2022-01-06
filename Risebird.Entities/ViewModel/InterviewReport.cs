using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
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
        [DisplayName("Total Experience")]
        public string TotalExperience { get; set; }

        [DisplayName("Candidate's Name")]
        //[RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        //ErrorMessage = "Please Enter Correct Email Address")]
        ////[Required]
        public string CandidateName { get; set; }

        [DisplayName("Confirmation Status")]
        ////[Required]
        public string confirmationStatus { get; set; }

        [DisplayName("Overall Score")]
        //[Required]
        public float OverallScore { get; set; }

        [DisplayName("Overall Rating")]
        //[Required]
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
        public int? PreviousMeetingStatus { get; set; }
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

        [Display(Name = "Uploaded Resume")]
        public string resumeLink { get; set; }
        [Display(Name = "Uploaded Resume(Encoded)")]
        public string encodedresumeLink { get; set; }
        [Display(Name = "Verified Aadhar Information")]
        public string AadharFileLink { get; set; }
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

        [Display(Name = "Interview Status")]
        public SelectList InterviewerApproved { get; set; }

        public string ApprovalComments { get; set; }

        public int? IsSelected { get; set; }
        public string FinalComments { get; set; }
        public bool IsQC { get; set; }
        public bool IsBiometricEnabled { get; set; }
        [Display(Name = "Voice Biometric Score")]
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

        [Display(Name = "Interviewer Communication")]
        public string InterviwerCommunication { get; set; }

        public SelectList InterviwerCommunicationList { get; set; }
        [Display(Name = "Interview Experience")]
        public string Interviewexperience { get; set; }
        public SelectList InterviewexperienceList { get; set; }

        [Display(Name = "Interview Conducted")]
        public string Interviewscompleted { get; set; }
        [Display(Name = "Ready-made Assessment")]
        public string ReadymadeAssessment { get; set; }
        public string Creativity { get; set; }
        [Display(Name = "Company Size")]
        public string CompanySize { get; set; }
        public string[] CompanySizeArray { get; set; }
        [Display(Name = "Job Function")]
        public string JobFunction { get; set; }
        public string Industry { get; set; }
        [Display(Name = "Country")]
        public string Country { get; set; }
        [Display(Name = "Company Type")]
        [Required(ErrorMessage = "This field is required.")]
        public string CompanyType { get; set; }
        public SelectList CompanyTypelist { get; set; }
        [Display(Name = "Company Stage")]
        public string CompanyStage { get; set; }
        public string[] CompanyStageArray { get; set; }
        public List<SelectListItem> CompanyStageList { get; set; }

        [Display(Name = "Skill Category")]
        public string SkillCategory { get; set; }
        public string[] SkillCategoryArray { get; set; }
        public List<SelectListItem> SkillCategoryList { get; set; }

        [Display(Name = "Years of Experience")]
        [Required(ErrorMessage = "This field is required.")]
        public string Yearsofexperience { get; set; }

        public string[] YearsofexperienceArray { get; set; }
        public List<SelectListItem> YearsofexperienceList { get; set; }

        [Display(Name = "Seniority Level")]
        [Required(ErrorMessage = "This field is required.")]
        public string Senioritylevel { get; set; }
        public string[] SenioritylevelArray { get; set; }
        public List<SelectListItem> SenioritylevelList { get; set; }

        [Display(Name = "Skill Nature")]
        public string SkillNature { get; set; }
        public SelectList SkillNatureList { get; set; }

        public string Domain { get; set; }
        public string DomainName { get; set; }

        [Display(Name = "Skill IP")]

        public string SkillIP { get; set; }
        [Display(Name = "Per-hour Rate - (India)")]
        public string PerHourRateIndia { get; set; }

        [Display(Name = "Per-hour Rate - (Elsewhere)")]
        public string PerHourRateOutside { get; set; }
        [Display(Name = "Decision Making")]
        public string DecisionMaking { get; set; }
        public string Articulation { get; set; }
        [Display(Name = "Readiness Online Interviews")]
        public string ReadinessOnlineInterviews { get; set; }
        [Display(Name = "Team Work")]
        public string Teamwork { get; set; }
        [Display(Name = "Relationship Management")]
        public string RelationshipManagement { get; set; }
        [Display(Name = "Conflict Resolution")]
        public string ConflictResolution { get; set; }
        public string Adaptability { get; set; }
        [Display(Name = "Mode of Interviews")]
        public string ModeInterviews { get; set; }
        [Display(Name = "Work Under Pressure")]
        public string WorkUnderPressure { get; set; }
        [Display(Name = "Time Management")]
        public string TimeManagement { get; set; }

        public string SharedEmailAddress { get; set; }
        public string ViewName { get; set; }
        public string LogoUrl { get; set; }
        public int? OldInterviewStatus { get; set; }
        public int? OldMeetingStatus { get; set; }
        public string OldconfirmationStatus { get; set; }
        public string UserDetails { get; set; }
        public IList<InterviewerQuestionsDiscussed> InterviewerQuestionsCollection { get; set; }

        [Display(Name = "Overall Personality Score")]
        //[Required]

        public float? OverallPersonalityScore { get; set; }
        public int? Openness { get; set; }
        public int? Conscientiousness { get; set; }
        public int? Extroversion { get; set; }
        public int? Agreeableness { get; set; }
        [Display(Name = "Sensitivity")]
        public int? Neuroticism { get; set; }

        [Display(Name = "Overall Profile Score")]
        //[Required]
        public float? ProfileScore { get; set; }

        [Display(Name = "Overall Interviewing Ability Score")]
        //[Required]
        public float? OverallInterviewingAbilityScore { get; set; }
        public string ProfileName { get; set; }
        public string ISCId { get; set; }
        public string BackUrl { get; set; }
        //public string PerHourRateOutside { get; set; }
        public List<SelectListItem> CompanySizeList { get; set; }
        public SelectList CountryList { get; set; }
        public SelectList IndianPrice { get; set; }
        public SelectList OutSideIndiaPrice { get; set; }
        public SelectList ProfileTypeList { get; set; }

        [Display(Name = "Profile Type")]

        public string ProfileType { get; set; }
        public SelectList CompentencyLevelList { get; set; }
        [Display(Name = "Competency Level")]

        public string CompentencyLevel { get; set; }
        public int InterviewerProfileId { get; set; }
        public int Userid { get; set; }
        public SelectList DomainList { get; set; }

        [Display(Name = "Thumbnail Time(Seconds)")]
        public int ThumbnailTimeSeconds { get; set; }

        [Display(Name = "Video Duration(Minutes)")]
        public int VideoDuration { get; set; }
        public string ResourceUrl { get; set; }
        public string OldInterviewerComment { get; set; }
        [Display(Name = "Assessment Group Id")]
        [Required(ErrorMessage = "This field is required.")]
        public string AssessmentGroupId { get; set; }
        public string[] AssessmentGroupIdArray { get; set; }
        [Display(Name = "Coded Resume Link")]
        public string CodedResumeLink { get; set; }
        [Display(Name = "Coded Resume File")]

        public IFormFile CodedResumeFile { get; set; }
        public List<SelectListItem> AssessmentGroupList { get; set; }
        public string QuestionsListData { get; set; }
        [Display(Name = "Trained")]
        [Required(ErrorMessage = "This field is required.")]
        public int Trained { get; set; }
        public int IndiaProductRate { get; set; }
        public int OutsideIndiaProductRate { get; set; }
        public bool IsQCRequired { get; set; }
    }
    public class RBInterviewDetails
    {
        public int Points { get; set; }
        public string Type { get; set; }
        public int id { get; set; }
        public int InterviewId { get; set; }
        public string Comment { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public string PreferredLocation { get; set; }
        public int MeetingStatus { get; set; }
        public string MeetingStatusDetails { get; set; }
        public int InterviewStatus { get; set; }
        public string InterviewStatusDetails { get; set; }
        public int Rate { get; set; }
        public string candidateEmail { get; set; }
        public string candidateName { get; set; }
        public int Userid { get; set; }
        public string FirstName { get; set; }
        public string VideoLink { get; set; }
        public string ClientName { get; set; }
        public string InterviewerEmailid { get; set; }
        public string AssessmentGroupId { get; set; }
        public int? VideoDuration { get; set; }
        public string ChannelOwner { get; set; }
        public string PrimarySkillInvite { get; set; }
        public string IRFId { get; set; }
        public bool IsEmergency { get; set; }
        public int TotalAmount { get; set; }
        public string ExpectedDate { get; set; }
    }
    public class SchedulingLinkVM
    {
        public string IRFId { get; set; }
        public string JDTiltle { get; set; }
        public string CalendlyLink { get; set; }
    }
    public class RBInterviewerPayoutDetails
    {
        public int Points { get; set; }
        public int id { get; set; }
        public int Userid { get; set; }
        public string FirstName { get; set; }
        public string InterviewerEmailid { get; set; }
        public string AccountNumber { get; set; }
        public string PAN { get; set; }
        public string IFSCCode { get; set; }
        public string AccountName { get; set; }
    }
}
