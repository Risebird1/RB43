using System;

namespace Risebird.Entities.Data
{
    public class Meeting
    {
        public string CancelationStatus { get; set; }
        public int id { get; set; }
        public string ClientName { get; set; }
        public string ClientCompName { get; set; }
        public int ClientId { get; set; }
        public string JobRole { get; set; }
        public DateTime interviewDate { get; set; }
        public DateTime EndDate { get; set; }
        public string interviewSlot { get; set; }
        public string interviewDuration { get; set; }
        public string PrimarySkill { get; set; }
        public string JDTiltle { get; set; }
        public string candidateEmail { get; set; }
        public string candidateName { get; set; }
        public string CalendlyLink { get; set; }
        public string ClientHrContact { get; set; }
        public string resumeLink { get; set; }
        public string confirmationStatus { get; set; }
        public string zoomLink { get; set; }
        public string zoomDetails { get; set; }
        public string risebirdHR { get; set; }
        public string interviewer { get; set; }
        public string InterviewerPhone { get; set; }
        public string interviewerComment { get; set; }
        public string intervieweStatus { get; set; }
        public string selectionStatus { get; set; }
        public string videoLink { get; set; }
        public string reportLink { get; set; }
        public string fraudSyncComment { get; set; }
        public string hrCoordinatorComment { get; set; }
        public string trainableComment { get; set; }
        public string interviewOnDevice { get; set; }
        public string sCreatedBy { get; set; }
        public DateTime dtCreatedOn { get; set; }
        public string IRFID { get; set; }
        public string PreferredLocation { get; set; }
        public string ExpectedPerHourRate { get; set; }
        public string staffingAgency { get; set; }
        public string joiningTime { get; set; }
        public int irfIntId { get; set; }
        public string JDLink { get; set; }
        public string MobileNumber { get; set; }
        public string ApprovalComments { get; set; }
        public int JobLevel { get; set; }
        public string JobLevelDesc { get; set; }
        public int? IsApproved { get; set; }
        public string NextLevelApprovalStat { get; set; }
        public int? MeetingStatus { get; set; }
        public int? InterviewStatus { get; set; }
        public int? IsSelected { get; set; }
        public int? QCStatus { get; set; }
        public string QCComment { get; set; }
        public string QCStatusDesc { get; set; }
        public string MeetingStatusDesc { get; set; }
        public string InterviewStatusDesc { get; set; }
        public string QCInterviewer { get; set; }
        public bool FraudLipSync { get; set; }
        public bool QuestionsRelevance { get; set; }
        public bool HandsOnCoding { get; set; }
        public bool BiasAndCandidateHandling { get; set; }
        public bool TechnicalCommunicationSkills { get; set; }
        public bool ChapteringCompleted { get; set; }
        public bool QuestionsFilled { get; set; }
        public bool TechSkillsMetric { get; set; }
        public bool SelectabilityMetric { get; set; }
        public bool JoinabilityMetric { get; set; }
        public bool GeneralSkillMetric { get; set; }
        public bool CommentsFilled { get; set; }
        public bool MeaningfulAssessments { get; set; }
        public bool CorrectEnglishGrammar { get; set; }
        public bool ProctoringReportFilled { get; set; }
        public bool IsEmergency { get; set; }

        public int CandidateMasterId { get; set; }

        public string CompanyDetails { get; set; }
        public string Status { get; set; }
        public string DateRequired { get; set; }
        public int IsOnilne { get; set; }
        public string HRCoordinatorEmailId { get; set; }
        public string HRMobileNumber { get; set; }
        public string HRComment { get; set; }

        public int iscIntId { get; set; }
        public string IRFnSkill { get; set; }
        public string BussinessType { get; set; }
        public int CandidateAvailable { get; set; }
        public string ClientHrMobile { get; set; }
        public string StaffingAgencyMobile { get; set; }
        public string HRImageFileURL { get; set; }
        public string ISCId { get; set; }
        public string ZoomId { get; set; }
        public string NickName { get; set; }
        public int? HeuristicProfileType { get; set; }

        public string HeuristicProfileTypeDesc { get; set; }
        public string CandidateClientHR { get; set; }
        public int VideoDuration { get; set; }
        public int Timer { get; set; }
        public bool IsProctor { get; set; }
        public string AudioComment { get; set; }
        public string JobCountry { get; set; }
        public int QCPresent { get; set; }
        public string MeetingPlatform { get; set; }
        public string BackUpLink { get; set; }
        public string BackUpLinkId { get; set; }
        public string InterviewerFirstName { get; set; }
        public string ClientHREmailId { get; set; }
        public string StaffingAgencyEmailId { get; set; }
        public bool IsInterviewerActive { get; set; }
        public bool IsFixedInterviewduration { get; set; }
        public int MaxInterviewDuration { get; set; }
    }
}
