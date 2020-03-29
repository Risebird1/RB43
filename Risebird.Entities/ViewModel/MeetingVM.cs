using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Risebird.Entities.ViewModel
{
    [Serializable]
    public class MeetingVM
    {

        public InterviewReport IreportModel { get; set; }

        public MeetingVM()
        {
          SubVideo_List = new List<MeetingVM>();
        }
        public List<MeetingVM> SubVideo_List { get; set; }
        public int imid { get; set; }

        [Display(Name = "IRF ID")]
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
        [Required(ErrorMessage = "Please enter Preffered Location.")]
        public string preferredLocation { get; set; }
        [Display(Name = "Staffing Agency")]
       
        public string staffingAgency { get; set; }
        [Display(Name = "Expected Per Hour Rate")]
        [Required(ErrorMessage = "Please enter Expected Per Hour Rate.")]
        public string expectedPerHourRate { get; set; }
        [Display(Name = "Expected Joining Time")]
        [Required(ErrorMessage = "Please enter Joining Time.")]
        public string joiningTime { get; set; }
        public string InterviewerComment { get; set; }
        public int? InterviewStatus { get; set; }
        public string InterviewStatusDescription { get; set; }

        public string intervieweStatusStatDesc { get; set; }
        public string selectionStatus { get; set; }
        public string VideoLink { get; set; }
        public string reportLink { get; set; }
        public string fraudSyncComment { get; set; }
        public string hrCoordinatorComment { get; set; }
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
        public bool IsEmergency { get; set; }
        public bool IsBiometricEnabled { get; set; }
        public int CandidateId { get; set; }
        public double InterviewBiometryScore { get; set; }
        public double JoiningBiometryScore { get; set; }

        [Display(Name = "Candidate Visa")]
        public string CandidateVisa { get; set; }
    }
}
