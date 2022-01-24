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

        [Display(Name = "Skill Channel ID")]
        [Required(ErrorMessage = "Please enter existing IRF Name.")]
        public string IRFId { get; set; }

        public int irfIntId { get; set; }
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
        [Display(Name = "Client HR")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        //[Required(ErrorMessage = "Please enter Client HR Email Id.")]
        public string ClientHrContact { get; set; }

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
        [Display(Name = "Staffing Agency")]

        public string staffingAgency { get; set; }
        [Display(Name = "Expected Salary")]

        public string expectedPerHourRate { get; set; }
        [Display(Name = "Notice Period")]

        public string joiningTime { get; set; }
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
        public long ZoomMeetingId { get; set; }
        public int VideoDuration { get; set; }
        public int Interviewid { get; set; }
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
        public string CalendlyLink { get; set; }
        public bool IsQCRequired { get; set; }
        public bool IsCandidateResponsed { get; set; }
        public bool CandidateResponse { get; set; }
        public int interviewtimedifference { get; set; }
        public bool IsJoinabilitySelectabilityRequired { get; set; }
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
