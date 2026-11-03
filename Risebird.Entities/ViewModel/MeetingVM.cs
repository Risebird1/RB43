using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    [Serializable]
    public class MeetingVM
    {

        public InterviewReport IreportModel { get; set; }

        public MeetingVM()
        {
            SubVideo_List = new List<MeetingVM>();
            MeetingPatch = new List<QuestionDetails>();
        }
        public List<MeetingVM> SubVideo_List { get; set; }
        public int imid { get; set; }
        public int id { get; set; }


        [Display(Name = "Skill Channel ID")]
        [Required(ErrorMessage = "Please enter existing IRF Name.")]
        public string StatusValue { get; set; }
        public string IRFId { get; set; }
        public int InterviewId { get; set; }
        public int irfIntId { get; set; }
        [Display(Name = "Interview Date")]
        [Required(ErrorMessage = "Please enter Interview Date.")]
        public DateTime interviewDate { get; set; }
        public string ProjectUniqueid { get; set; }
        public string uniqueId { get; set; }

        [Display(Name = "Interview Slot")]
        [Required(ErrorMessage = "Please enter Interview Slot.")]
        public string interviewSlot { get; set; }
        [Display(Name = "Interview Duration")]
        [Required(ErrorMessage = "Please enter Interview Duration.")]
        public int interviewDuration { get; set; }
        public int deficit { get; set; }

        [Display(Name = "Candidate Email")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        [Required(ErrorMessage = "Please enter Candidate Email Id.")]
        public string candidateEmail { get; set; }
        [Required(ErrorMessage = "Please enter Candidate Name.")]
        [Display(Name = "Candidate Name")]
        public string candidateName { get; set; }
        [Display(Name = "Client HR")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        //[Required(ErrorMessage = "Please enter Client HR Email Id.")]
        public string ClientHrContact { get; set; }
        public string ClientHRAttemptContact { get; set; }
        public string ClientEmail { get; set; }
        public string Comment { get; set; }

        [Display(Name = "Current Employer")]
        public string currentEmployerName { get; set; }

        [Display(Name = "Current Renumeration")]
        public double CurrentRenumeration { get; set; }
        [Display(Name = "Resume Link")]
        //[Required]
        public string resumeLink { get; set; }

        [Display(Name = "Encoded Resume Link")]
        //[Required]
        public string encodedresumeLink { get; set; }
        [Display(Name = "Interviewer Resume Link")]
        //[Required]
        public string interviewerresumeLink { get; set; }
        [Display(Name = "Confirmation Status")]
        [Required(ErrorMessage = "Please enter Confirmation Status.")]
        public string confirmationStatus { get; set; }
        [Display(Name = "Zoom Meeting Link")]
        [Required(ErrorMessage = "Please enter Zoom Meeting Link.")]
        public string zoomLink { get; set; }
        [Display(Name = "Zoom Login Details")]
        //[Required(ErrorMessage = "Please enter Zoom Login Details.")]
        public string zoomDetails { get; set; }
        [Display(Name = "Risebird HR")]
        [Required(ErrorMessage = "Please enter Risebird HR Email Id.")]
        public string risebirdHR { get; set; }
        [Display(Name = "Interviewer")]
        public string interviewer { get; set; }

        [Display(Name = "Preferred Location")]

        public string preferredLocation { get; set; }
        public DateTime? Joinedon { get; set; }
        public int InterviewerLateJoin { get; set; }
        [Display(Name = "Staffing Agency")]

        public string staffingAgency { get; set; }
        [Display(Name = "Expected Salary")]
        public string expectedPerHourRate { get; set; }
        [Display(Name = "Notice Period")]

        public string joiningTime { get; set; }
        public DateTime? FirstJoinedon { get; set; }
        public DateTime? FirstPerformedOn { get; set; }
        public bool EIAssigned { get; set; }
        public string InterviewerComment { get; set; }
        public int? InterviewStatus { get; set; }
        public string InterviewStatusDescription { get; set; }
        public int interviewVediosequence { get; set; }
        public string PageNumber { get; set; }

        public string intervieweStatusStatDesc { get; set; }
        public string selectionStatus { get; set; }
        public string VideoLink { get; set; }
        public string ThumbnailImage { get; set; }
        public string ThumbnailImagestring { get; set; }

        public string reportLink { get; set; }
        public string fraudSyncComment { get; set; }
        public string trainableComment { get; set; }
        [Display(Name = "Mobile Number")]
        // [RegularExpression("^[0-9]{1,12}$", ErrorMessage ="Please enter a valid Mobile Number.")]
        //[Required(ErrorMessage = "Please enter a valid Mobile Number.")]
        public string MobileNumber { get; set; }
        public bool IsVideoRequired { get; set; }
        public string InterviewerMobile { get; set; }

        public DateTime dtCreatedOn { get; set; }
        [Display(Name = "Upload Resume")]
        //[Required]
        public IFormFile File { get; set; }
        public int? JobLevel { get; set; }
        public int? L2InterviewStatus { get; set; }
        public int? ClientInterviewStatus { get; set; }
        public int? FraudCheckStatus { get; set; }
        public int? TrainingStatus { get; set; }
        public string L2InterviewStatDesc { get; set; }
        public string ClientInterviewStatDesc { get; set; }
        public int? JoiningStatus { get; set; }
        public string JoiningComments { get; set; }

        public int? PreparationStatus { get; set; }
        public int? OfferStatus { get; set; }
        public string OfferComments { get; set; }

        public int? MeetingStatus { get; set; }
        public string MeetingStatusDescription { get; set; }
        public int? QCStatus { get; set; }
        public string JoiningStatusStatDesc { get; set; }
        public string PreparationStatusDesc { get; set; }
        public string OfferStatusStatDesc { get; set; }
        public string MeetingStatusStatusDesc { get; set; }
        public string FutureSchduleDate { get; set; }


        public string QCStatusStatusDesc { get; set; }
        [Display(Name = "Confirmation Status")]
        public SelectList ConfirmationStatusList { get; set; }
        [Display(Name = "Interview Slot")]
        public SelectList InterviewSlotList { get; set; }
        public SelectList IRFList { get; set; }
        public SelectList ClientHrContactList { get; set; }
        public SelectList StaffingAgencyList { get; set; }
        public string JDLink { get; set; }

        public int ClientId { get; set; }
        public string JDTiltle { get; set; }
        public string MAGName { get; set; }
        public string ClientName { get; set; }
        [Display(Name = "Approved")]
        public int? IsApproved { get; set; }
        public string NextLevelApprovalStat { get; set; }
        public string ApprovalComments { get; set; }
        public int? IsSelected { get; set; }
        public string FinalSelectionStat { get; set; }
        public string FinalComments { get; set; }
        public int InviteCount { get; set; }
        public int PerMintuteRate { get; set; }
        public int TotalBudget { get; set; }

        [Display(Name = "Emergency")]

        public bool IsEmergency { get; set; }
        public bool IsBiometricEnabled { get; set; }
        public int CandidateId { get; set; }
        public double InterviewBiometryScore { get; set; }
        public double JoiningBiometryScore { get; set; }

        [Display(Name = "Candidate Visa")]
        public string CandidateVisa { get; set; }

        public int QCInterview { get; set; }
        public string IsLike { get; set; }
        public int TotalVedioLike { get; set; }
        public int TotalVedioDisLike { get; set; }

        public int TotalView { get; set; }
        public string IsShared { get; set; }
        public List<VedioComments> Comments { get; set; }
        public int ShortListCount { get; set; }
        public int IsShortlisted { get; set; }
        public bool CuratedLink { get; set; }
        public string CandidateCodedname { get; set; }
        public string IsIntrested { get; set; }
        public string ShortListstatus { get; set; }
        public int shared { get; set; }
        public string newprofielName { get; set; }
        public DateTime? SharedOn { get; set; }
        public int IsOnline { get; set; }
        public int IsHrOnline { get; set; }
        public int IsInterviewerOnline { get; set; }
        public string QCInterviewer { get; set; }
        public List<VideoCountdetail> VideoCountDetail { get; set; }
        public string FeedbackAudioLink { get; set; }
        public string AadharFileLink { get; set; }
        public bool IsAdmin { get; set; }
        public int Score { get; set; }

        [Display(Name = "Co-ordinating HR")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        public string HRCoordinatorEmailId { get; set; }
        public string HRMobileNumber { get; set; }
        public string HRComment { get; set; }
        public bool MoveToCP { get; set; }
        public bool IsClientCurated { get; set; }
        public int CuratedGroupId { get; set; }
        public string ActionStatus { get; set; }
        public string CandidateAvailable { get; set; }

        public int TechSkillScore { get; set; }
        public string LogoUrl { get; set; }
        public int? VideoSentimentImid { get; set; }
        public string Client { get; set; }
        public List<QuestionDetails> MeetingPatch { get; set; }
        public int MeetingPatchCount { get; set; }
        public string CostEstimate { get; set; }
        public string TotalExperience { get; set; }
        public int ClientJdid { get; set; }
        public int Priority { get; set; }
        public int MeetingsCount { get; set; }
        public int PagesCount { get; set; }
        public bool Pending { get; set; }
        public int? Heuristicsscore { get; set; }
        public string HeuristicDesc { get; set; }
        public string JobCountry { get; set; }
        public string BillingOwner { get; set; }
        public string AssessmentGroup { get; set; }
        public string Senioritylevel { get; set; }
        public string PerHourRateIndia { get; set; }
        public string PerHourRateOutside { get; set; }
        public string ISCId { get; set; }
        public int iscIntId { get; set; }
        public string ZoomId { get; set; }
        public string PrimarySkill { get; set; }
        public string ProfileType { get; set; }
        public string CompentencyLevel { get; set; }
        public int InterviewerProfileId { get; set; }
        /// <summary>UserList.id of the interviewer (Suspicious list: Undo suspicious sends this).</summary>
        public int InterviewerUserId { get; set; }
        public string NickName { get; set; }
        public string Name { get; set; }
        public string CompanyType { get; set; }
        public string Domain { get; set; }
        public bool IsLock { get; set; }
        public string EmailId { get; set; }
        public bool UnlockVideo { get; set; }
        public string CandidateClientHR { get; set; }
        public int ThumbnailTimeSeconds { get; set; }
        public string WhoJoined { get; set; }
        //public long ZoomMeetingId { get; set; }
        public string ZoomMeetingId { get; set; }
        public int VideoDuration { get; set; }

        public int DurationSelectedMin { get; set; }
        public string OldInterviewer { get; set; }
        public string OldQCInterviewer { get; set; }
        public string OldzoomLink { get; set; }
        public string OldrisebirdHR { get; set; }
        public string OldConfirmationStatus { get; set; }
        public string OldHRCoordinatorEmailId { get; set; }
        public string OldIRFId { get; set; }
        public DateTime OldinterviewDate { get; set; }
        public string LoggedInUser { get; set; }
        public string ResourceUrl { get; set; }

        public string InterviewerMobileNumber { get; set; }
        public string ClientMobileNumber { get; set; }
        public SelectList InterviewingMembersList { get; set; }
        public SelectList QualityControllersList { get; set; }

        [Display(Name = "Channel Owner")]
        public string ChannelOwner { get; set; }
        public string RatingAddedOn { get; set; }
        public string CalendlyLink { get; set; }
        public bool IsQCRequired { get; set; }
        public bool IsCandidateResponsed { get; set; }
        public int CandidateResponse { get; set; }
        public int Rating { get; set; }
        public DateTime RateAddedOn { get; set; }
        public string RateAddedBy { get; set; }
        public int interviewtimedifference { get; set; }
        public bool IsJoinabilitySelectabilityRequired { get; set; }
        public string ProctorReport { get; set; }
        public bool IsProctor { get; set; }
        public string Cancellationsource { get; set; }
        public string ClientAction { get; set; }
        public int QCPresent { get; set; }
        public int Redflags { get; set; }
        public bool HasDistinctParticipants { get; set; }
        public string TemplateName { get; set; }
        public bool AadhaarVerified { get; set; }
        public bool? IsAadhaarVerificationRequired { get; set; }
        public bool IsProctoring { get; set; } = true;
        public bool RedoInterview { get; set; }
        public string InterviewerVideo { get; set; }
        public string CandidateComment { get; set; }
        public string MeetingPlatform { get; set; }
        public string BackUpLink { get; set; }
        public string BackUpLinkId { get; set; }
        public bool IsClientQCRequired { get; set; }
        public bool IsClientRedoInterview { get; set; }
        public bool IsReportBlocked { get; set; }
        public bool IsSurcharge { get; set; }
        public int Surcharge { get; set; }
        public string SurchargeComment { get; set; }
        public string AudioTranscriptFileLink { get; set; }
        public bool isVerified { get; set; }
        public bool IsEmployed { get; set; }
        public string EmploymentComment { get; set; }
        public string CompanyName { get; set; }

        //For Suspicious Interviewer
        public string SuspiciousName { get; set; }
        public string SuspiciousEmail { get; set; }
        public string InterviewAddedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? Createdon { get; set; }
        public string SuspiciousComment { get; set; }
        public string SuspiciousAddedBy { get; set; }
        public DateTime? SuspiciousAddOn { get; set; }
        public string SuspiciousMobileNumber { get; set; }
        public bool IsActive { get; set; }
        public int CandidateMasterId { get; set; }
        public string scheduleUniqueid { get; set; }
        public int? VideoConfirmation { get; set; }
        public int AssessmentGroupId { get; set; }
        public string InterviewerEmailId { get; set; }
        public string InterviewerName { get; set; }
        public string InterviewerWhatsappNumber { get; set; }
        public string InterviewerResume { get; set; }
        public int Isavailable { get; set; }
        // Interviewer Churn Data: a WhatsApp unsubscribe (source, reason, when); availability is unchanged.
        public string ChurnSource { get; set; }
        public string ChurnReason { get; set; }
        public DateTime? ChurnDate { get; set; }
        public DateTime? AvailabilityResponseDate { get; set; }
        public string candidateEmailId { get; set; }
        //public string EmploymentComment { get; set; }
        //public string CompanyName { get; set; }
        public int RedMarker { get; set; }
        public int PaymentComplianceCount { get; set; }

        public DateTime? PerformedOn { get; set; }
        public bool IRFInterviewerActive { get; set; }
        public bool DMApproved { get; set; }
        public string DMApprovedBy { get; set; }
        public int MailCount { get; set; }
        public int CallsCount { get; set; }
        public int WhatsappCount { get; set; }
        // WhatsApp send buttons: opted out of WhatsApp for this IRF, and sends of this page's stage used (3-per-IRF limit).
        public bool WhatsAppOptedOut { get; set; }
        public int WhatsAppSendsUsed { get; set; }
        public bool IsColdCandidate { get; set; }
        public string ColdMarkedBY { get; set; }
        public DateTime? ColdMarkedDate { get; set; }
        // Call opt-out: ColdSource AICall | HumanCall | Manual (badge on the cold lists), ColdReason the quote / popup reason.
        public string ColdSource { get; set; }
        public string ColdReason { get; set; }
        // Interviewer Churn: AICall | HumanCall when Isavailable = 5 (opted out of calls).
        public string CallOptOutSource { get; set; }
        public int TotalCandidate { get; set; }

        public DateTime? RescheduleDate { get; set; }
        public int CommunicationalProficiency { get; set; }
        public int TotalCompletedInterview { get; set; }
        public bool IsPaused { get; set; }
        public bool IsQC { get; set; }
        public string CodeSnippet { get; set; }
        public string LatestComment { get; set; }
        public bool HasEmailDroppedOrBounced { get; set; }
        public int TimeZoneoffset { get; set; }
        public string TimeZonesValue { get; set; }
        public DateTime interviewDateTimeZone { get; set; }
        public string JDTitleTwo { get; set; }
        public int PerSuccessRate { get; set; }
        public string CodeSnippetTwo { get; set; }
        public string Recommendation { get; set; }
        public string RecommendationComments { get; set; }

        [Display(Name = "Candidate ID Verification Status")]
        public int? CandidateIDVerificationStatus { get; set; }
        public bool IsReportwhitelabel { get; set; }
        public bool IsAITranscriptAnalysisAttempted { get; set; }
        public string AITranscriptVerdict { get; set; }
        public string AIVerdict { get; set; }
        public int AITranscriptAnalysisScore { get; set; }
        public bool ModifiedInterviewStatus { get; set; }
        public string Cancellationreason { get; set; }
        public string CandidateInterviewChecks { get; set; }
        public bool ConsentRequired { get; set; }
        public bool IsAIInterview { get; set; }
        public string SessionId { get; set; }
        public string AssistantId { get; set; }
        public string ThreadId { get; set; } // id of the open ai thread created by the assistant that conducts the interview
        public int TimeLeftInSeconds { get; set; } //applicable only for AI interviews conducted using assistants through sessions/openai-threads
        public string TechSkillsInJson { get; set; }
        //public string JDLink { get; set; } // url to the job description file (pdf/doc) for the skill channel; used to build the adaptive AI interview prompt
        public bool IsAdaptive { get; set; }//refers to the type of ai interview a skill channel is created for (adaptive or preset); applicable only in case of an ai interview skill channel
        public int AIInterviewType { get; set; }
        public string QuestionAnswerSetInJson { get; set; }
        public int AadharVerificationId { get; set; }
        public int MatchScorePercentage { get; set; }
        public string CandidateAnswerVideoLink { get; set; } // in case of ai interview, fetches the link from the table BotInterviewQuestionVideos
        public int CandidateAnswerVideoIdAI { get; set; }
        public string OpenAIAssistantId { get; set; }//assistant Id on skill channel global level (Assistants API legacy)
        public string OpenAIPromptId { get; set; }//Primary Prompt Id (pmpt_…) of the skill channel: adaptive question generator, and report generator when no evaluator prompt is set
        public string OpenAIEvaluatorPromptId { get; set; }//Interview Evaluator Prompt Id (pmpt_…) that generates the candidate report; when null the Primary Prompt Id generates it
        public int AICandidateResponseVideoCount { get; set; }
        public int AITotalQuestionsCount { get; set; }
        public DateTime SkillChannelCreatedOn { get; set; }
        public string CandidateResumeClientHREmailId { get; set; }//used in Emergency dashboards's unprocessed resumes page
        public string TranscriptSummary { get; set; }
        public bool showChatBox { get; set; }
        public bool IsResumeParsed { get; set; }
        public bool IsResumeParsingFailed { get; set; }
        public string ReasonBehindNotProcessed { get; set; }
        public bool isMultiLingual { get; set; }

        /// <summary>
        /// Whether this channel has the candidate review and correct the speech-to-text transcript
        /// before submitting. Null means the channel has not decided, and the environment's
        /// AIInterview:CandidateTranscriptReview setting applies instead.
        /// </summary>
        public bool? CandidateTranscriptReview { get; set; }
        public string AIResumeScreeningVerdict { get; set; }
        public int AIResumeScreeningScore { get; set; }

        /// <summary>
        /// The resume screener's full result, as written by AIScreenerController. Raw JSON:
        /// it has no schema of its own, so it is parsed at the point of use inside a
        /// try/catch rather than being bound here.
        /// </summary>
        public string AIScreeningResultJsonV2 { get; set; }

        /// <summary>
        /// Skill-channel opt-in for the MCQ assessment section served from screen.risebird.io.
        /// The channel being mirrored on the MCQ side is not on its own consent to add a written
        /// test to the interview, so this is an explicit flag - see IRF.HasMcqAssessmentSection.
        /// </summary>
        public bool HasMcqAssessmentSection { get; set; }

        /// <summary>
        /// Comma-separated MCQ section keys in ask order. Null means "let the MCQ side pick its
        /// default set", so a channel can be switched on without choosing sections.
        /// </summary>
        public string McqAssessmentSections { get; set; }

        /// <summary>Total minutes for the MCQ section; null lets the MCQ side sum its own budgets.</summary>
        public int? McqAssessmentMinutes { get; set; }
        public DateTime? CandidateAddedOn { get; set; }
        public string ProjectTitle { get; set; }
        public string HRFirstName { get; set; }
        public string HRLastName { get; set; }
        public bool ReportAI { get; set; }
        public bool TranscritAi { get; set; }
        public bool ContradictionAi { get; set; }
        public int PriorityLevel { get; set; }
        public string PriorityLevelDesc { get; set; }
        public string Riskcategory { get; set; }
        public string NonAIreasoning { get; set; }

        // ---- Potential Risk by Candidate: QC decision on the interviewer (usp_PotentialRiskByCandidate, appended) ----
        /// <summary>Interviewer active (UserList.IsActive); null when not loaded (treated as active).</summary>
        public bool? InterviewerIsActive { get; set; }
        /// <summary>Latest Potential Risk decision: 1 Under review, 2 Reviewed, 3 Removed; null = pending.</summary>
        public int? PotentialRiskStatus { get; set; }
        public string PotentialRiskStatusBy { get; set; }
        public DateTime? PotentialRiskStatusOn { get; set; }
        public string PotentialRiskStatusComment { get; set; }

    }

    public class VedioComments
    {
        public string Comment { get; set; }
        public string UserName { get; set; }
    }
    public class EmailDetails
    {
        public int totalcount { get; set; }
        public string Email { get; set; }
    }
    public class VideoCountdetail
    {
        public string UserName { get; set; }
        public string Count { get; set; }
    }
    public class InterviewerMeetingVM
    {

        public InterviewerMeetingVM()
        {
            SubVideo_List = new List<InterviewerMeetingVM>();
        }
        public List<InterviewerMeetingVM> SubVideo_List { get; set; }
        public int imid { get; set; }

        [Display(Name = "Interview Date")]
        [Required(ErrorMessage = "Please enter Interview Date.")]
        public DateTime interviewDate { get; set; }
        [Display(Name = "Interview Slot")]
        [Required(ErrorMessage = "Please enter Interview Slot.")]
        public string interviewSlot { get; set; }
        [Display(Name = "Interview Duration")]
        [Required(ErrorMessage = "Please enter Interview Duration.")]
        public int interviewDuration { get; set; }

        [Display(Name = "Candidate Email")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        [Required(ErrorMessage = "Please enter Candidate Email Id.")]
        public string candidateEmail { get; set; }
        [Required(ErrorMessage = "Please enter Candidate Name.")]
        [Display(Name = "Candidate Name")]
        public string candidateName { get; set; }
        public string confirmationStatus { get; set; }
        [Display(Name = "Zoom Meeting Link")]
        [Required(ErrorMessage = "Please enter Zoom Meeting Link.")]
        public string zoomLink { get; set; }
        [Display(Name = "Zoom Login Details")]
        //[Required(ErrorMessage = "Please enter Zoom Login Details.")]
        public string zoomDetails { get; set; }
        [Display(Name = "Interviewer")]
        public string interviewer { get; set; }
        public DateTime dtCreatedOn { get; set; }
        [Display(Name = "Confirmation Status")]
        public SelectList ConfirmationStatusList { get; set; }
        [Display(Name = "Interview Slot")]
        public SelectList InterviewSlotList { get; set; }
        public SelectList ISCList { get; set; }
        public string ISCId { get; set; }
        public int iscIntId { get; set; }
        public string ZoomId { get; set; }
        public string PrimarySkill { get; set; }
        public string sCreatedBy { get; set; }
        public bool IsEditable { get; set; }


    }
    public class ZoomMeetingHistoryDetails
    {
        public string Event { get; set; }
        public string ParticipantName { get; set; }
        public DateTime? ActionDate { get; set; }
    }
}
