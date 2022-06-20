using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class IRFVm
    {
        public IRFVm()
        {
            InterviewerCollection = new List<TransInterviewer>();
            EmergencyInterviewerCollection = new List<TransEmergencyInterviewer>();
            QualityController = new List<TransQualityController>();
            SkillCollection = new List<TransSkill>();
        }
        public int id { get; set; }

        [DisplayName("Skill Channel ID")]
        [Required]
        //[RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = "Only Alphabets and Numbers allowed.")]
        public string IRFId { get; set; }

        [DisplayName("Project Name")]
        [Required]
        public string ClientName { get; set; }

        [DisplayName("Job Title")]
        [Required(ErrorMessage = "Job Title is required.")]
        public string JDTiltle { get; set; }

        [DisplayName("Company Type")]
        [Required]
        public string CompanyType { get; set; }

        [DisplayName("Total Budget")]
        public int TotalBudget { get; set; }

        [DisplayName("Budget Consumed")]
        public string BudgetConsumed { get; set; }

        [DisplayName("Job Level")]
        [Required]
        public int? JobLevel { get; set; }
        public int ClientId { get; set; }
        [DisplayName("Job Country")]
        [Required]
        //[RegularExpression("^[a-zA-Z]{1,20}$", ErrorMessage = "Kindly Enter only letters.")]
        public string JobCountry { get; set; }

        [DisplayName("Start Date")]
        //[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        [Required]
        public DateTime StartDate { get; set; }

        [DisplayName("End Date")]
        [DataType(DataType.DateTime)]
        //[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        [Required]
        public DateTime EndDate { get; set; }

        [DisplayName("JD Link")]
        public string JDLink { get; set; }

        [DisplayName("Company Details")]
        public string CompanyDetails { get; set; }

        [DisplayName("No of Positions")]
        [Required]
        public int NoOfPosition { get; set; }
        [DisplayName("Client Profile Type")]
        public int Heuristicsscore { get; set; }

        [DisplayName("No of Interviews")]
        [Required]
        public int NoOfIntterview { get; set; }

        [DisplayName("Risebird HR")]
        [Required]
        public int RisebirdHr { get; set; }

        [DisplayName("Per Interview Rate")]
        [Required]
        public double PerMintuteRate { get; set; }

        public DateTime CreatedOn { get; set; }

        public SelectList JobLevelList { get; set; }
        public IList<TransSkill> SkillCollection { get; set; }
        public IList<TransSkillGeneral> SkillGeneral { get; set; }
        public IList<TransJoinability> Joinability { get; set; }
        public IList<TransSelectability> Selectability { get; set; }
        public IList<TransClientUser> ClientCollection { get; set; }
        public IList<TransQualityController> QualityController { get; set; }
        public IList<TransInterviewer> InterviewerCollection { get; set; }
        public IList<TransEmergencyInterviewer> EmergencyInterviewerCollection { get; set; }
        public IList<TransResumeEvaluator> ResumeEvaluatorCollection { get; set; }
        public IList<TransHR> HRCollection { get; set; }
        public IList<TransRecruiter> RecruiterCollection { get; set; }

        public IList<ClonableTemplateQuestion> ClonableTemplateQuestionsCollection { get; set; }
        public string ClientCompName { get; set; }

        public SelectList JoinabilityTypeList { get; set; }
        public SelectList SelectabilityTypeList { get; set; }
        public SelectList SkillGeneralTypeList { get; set; }

        public SelectList CompanyTypeList { get; set; }
        public IFormFile JDFile { get; set; }
        public IFormFile CompanyDetailFile { get; set; }

        [DisplayName("Calendly Link")]
        public string CalendlyLink { get; set; }
        public int TotalInvites { get; set; }
        public string ArchiveStatus { get; set; }
        public string[] ClientRole { get; set; }
        public string LoggedEmail { get; set; }

        [DisplayName("Enable Voice Biometric")]
        public bool IsBiometricEnabled { get; set; }
        public bool IsPaused { get; set; }
        public int? PriorityLevel { get; set; }
        public SelectList PriorityLevelList { get; set; }
        public List<InterviewerIds> InterviewerIds { get; set; }
        public string Type { get; set; }
        [DisplayName("Primary Skill")]
        public string PrimarySkillInvite { get; set; }
        public bool IsClone { get; set; }
        public IEnumerable<InterviewerProfileVM> InterviewerList { get; set; }
        [DisplayName("Billing Owner")]
        [Required]
        public int BillingOwner { get; set; }
        [Display(Name = "Assessment Group")]
        [Required]
        public string AssessmentGroupId { get; set; }
        public SelectList AssessmentGroupList { get; set; }
        public IEnumerable<IRFInterviewerMembers> InterviewerMembers { get; set; }
        public string OldCalendlyLink { get; set; }
        public string OldAssessmentGroupId { get; set; }
        public string OldJDLink { get; set; }
        public string ResourceUrl { get; set; }
        public IList<TransInterviewer> OldInterviewerCollection { get; set; }
        public IList<TransEmergencyInterviewer> OldEmergencyInterviewerCollection { get; set; }
        public IList<TransSkill> OldSkillCollection { get; set; }
        public string InterviewerId { get; set; }
        public bool isDuplicateEntries { get; set; }
        public string OldCompanyDetails { get; set; }
        public int TemplateId { get; set; }
        public int clonevalue { get; set; }

        [DisplayName("Scheduling Link")]
        public string SchedulingLink { get; set; }

        [DisplayName("QC Required")]
        public bool IsQCRequired { get; set; }

        [DisplayName("Joinability and Selectability Required")]
        public bool IsJoinabilitySelectabilityRequired { get; set; }

        [DisplayName("General Skill Required")]
        public bool IsGeneralSkillRequired { get; set; }

        public IRFRecommendations IRFRecommendations { get; set; }

        [DisplayName("Template Name")]
        //[Remote(action: "IsClonableTemplateNameInUse", controller: "IRF")]
        public string TemplateName { get; set; }

        public int UserId { get; set; }

        public IList<SkillComment> QuesionList { get; set; }
        //public int? ClonableSkillChannelId { get; set; }
        public int TemplateCreatedFromIRFId { get; set; }
        public bool UseAsCloneTemplate { get; set; }
        public int CreatedFromTemplateId { get; set; }
        public string AssessmentGroupName { get; set; }
        public SelectList Clients { get; set; }
        public SelectList ClientUser { get; set; }
        public SelectList LevelTech { get; set; }
        public SelectList SkillsList { get; set; }
        public string AudioComment { get; set; }
        public bool IsEdit { get; set; }
        public string Profile { get; set; }
        public string OldJDTitle { get; set; }
        public string MeetingPlatform { get; set; }
    }
    public class InterviewerIds
    {
        public int index { get; set; }
        public int Id { get; set; }
    }
    public class IRF
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string ClientName { get; set; }
        public string JDTiltle { get; set; }
        public string CompanyType { get; set; }
        public int ClientId { get; set; }
        public string ClientCompName { get; set; }
        public string JobCountry { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string JDLink { get; set; }
        public int NoOfPosition { get; set; }
        public int NoOfIntterview { get; set; }
        public int RisebirdHr { get; set; }
        public double PerMintuteRate { get; set; }
        public int Heuristicsscore { get; set; }

        public int JobLevel { get; set; }
        public bool IsClientUser { get; set; }
        public bool IsDeleted { get; set; }

        public int? TotalMeetings { get; set; }
        public int TotalBudget { get; set; }
        public string BudgetConsumed { get; set; }
        public int? SelectedMeetings { get; set; }
        public int UnAssignedMeeting { get; set; }
        public string UnAssignedMeetingDsr { get; set; }
        public int TodayMeeting { get; set; }
        public string TodayMeetingDsr { get; set; }
        public int TomorrowMeeting { get; set; }
        public string TomorrowMeetingDsr { get; set; }
        public int? ScheduledMeetings { get; set; }
        public int? ConfirmedMeetings { get; set; }
        public string CalendlyLink { get; set; }
        public int TotalInvites { get; set; }

        public string ArchiveStatus { get; set; }
        public string SubscriptionStatus { get; set; }
        public string ClientGST { get; set; }
        public string ClientAddress { get; set; }
        public string[] ClientRole { get; set; }
        public double Discount { get; set; }
        public bool IsBiometricEnabled { get; set; }
        public bool IsPaused { get; set; }

        public int? PriorityLevel { get; set; }
        public int? MeetingId { get; set; }

        public string ClientHrContact { get; set; }
        public string staffingAgency { get; set; }
        public int interviewSlot { get; set; }
        public string confirmationStatus { get; set; }
        public int? InterviewStatus { get; set; }
        public int? MeetingStatus { get; set; }
        public int ClientRoleId { get; set; }
        public DateTime interviewDate { get; set; }
        public string interviewer { get; set; }
        public float TotalBudgetConsumed { get; set; }
        public string ImageUrl { get; set; }
        public string IRFStatus { get; set; }
        public bool CuratedLink { get; set; }
        public string CompanyDetails { get; set; }
        public bool IsSubscribed { get; set; }
        public int? SubId { get; set; }
        public string IRFnSkill { get; set; }
        public string ProgressStatus { get; set; }
        public int idProgress { get; set; }
        public string Comment { get; set; }
        public string BussinessType { get; set; }
        public string PrimarySkillInvite { get; set; }
        public string AssignTo { get; set; }
        public string AssignStatus { get; set; }
        public string AssessmentGroupId { get; set; }
        public int BillingOwner { get; set; }
        public string BillingOwnerEmailId { get; set; }
        public string RisebirdHREmail { get; set; }
        public int CandidateCount { get; set; }
        public int CompletedInterviews { get; set; }
        public int RejectedMeetings { get; set; }
        public int NotScheduledCount { get; set; }
        public int ReScheduledCount { get; set; }
        public int FutureScheduledMeetings { get; set; }
        public bool IsQCRequired { get; set; }
        public bool IsJoinabilitySelectabilityRequired { get; set; }
        public float ScheduledPercentage { get; set; }
        public float CompletionRate { get; set; }
        public float SelectionRate { get; set; }
        public int Flags { get; set; }
        public int PendingCount { get; set; }
        public int UnassignedInterviewsCount { get; set; }
        public int UnfilledMeetingStatusCount { get; set; }
        public int UnfilledInterviewStatusCount { get; set; }
        public int UnfilledCommentsCount { get; set; }
        public int MissingZoomLink { get; set; }
        public int MissingVideoLink { get; set; }
        public bool IsGeneralSkillRequired { get; set; }
        public int flagcount { get; set; }
        public bool IsRecruiterActive { get; set; }
        public int UserId { get; set; }
        public int? CreatedFromTemplateId { get; set; }

    }
    public class SkillCahnnelDetailsVM
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string AssessmentGroup { get; set; }
        public int AssessmentGroupId { get; set; }
        public int CompetencyLevelId { get; set; }
        public int CountryType { get; set; }
        public int SeniorityLevelId { get; set; }

    }
}
