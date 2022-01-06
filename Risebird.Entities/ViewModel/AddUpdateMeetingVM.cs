using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class AddUpdateMeetingVM
    {

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

        //[Display(Name = "Current Renumeration")]
        //public double CurrentRenumeration { get; set; }
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

        //[Display(Name = "Zoom Login Details")]
        ////[Required(ErrorMessage = "Please enter Zoom Login Details.")]
        //public string zoomDetails { get; set; }

        [Display(Name = "Risebird HR")]
        [Required(ErrorMessage = "Please enter Zoom login details.")]
        public string risebirdHR { get; set; }

        [Display(Name = "Interviewer")]
        public string interviewer { get; set; }
        public string NewActiveInterviewer { get; set; }

        [Display(Name = "Preferred Location")]
        public string preferredLocation { get; set; }

        [Display(Name = "Staffing Agency")]
        public string staffingAgency { get; set; }

        [Display(Name = "Expected Salary")]
        public string expectedPerHourRate { get; set; }

        [Display(Name = "Notice Period")]
        public string joiningTime { get; set; }


        [Display(Name = "Mobile Number")]
        // [RegularExpression("^[0-9]{1,12}$", ErrorMessage ="Please enter a valid Mobile Number.")]
        //[Required(ErrorMessage = "Please enter a valid Mobile Number.")]
        public string MobileNumber { get; set; }
        public DateTime dtCreatedOn { get; set; }
        [Display(Name = "Upload Resume")]
        //[Required]
        public IFormFile File { get; set; }


        [Display(Name = "Confirmation Status")]
        public SelectList ConfirmationStatusList { get; set; }
        [Display(Name = "Interview Slot")]
        public SelectList InterviewSlotList { get; set; }
        public SelectList IRFList { get; set; }
        public SelectList ClientHrContactList { get; set; }
        public SelectList StaffingAgencyList { get; set; }



        [Display(Name = "Emergency")]

        public bool IsEmergency { get; set; }


        public string QCInterviewer { get; set; }
        public string NewActiveQCInterviewer { get; set; }


        [Display(Name = "Co-ordinating HR")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        public string HRCoordinatorEmailId { get; set; }


        public long ZoomMeetingId { get; set; }

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
        public SelectList InterviewingMembersList { get; set; }
        public SelectList QualityControllersList { get; set; }

        [Display(Name = "Channel Owner")]
        public string ChannelOwner { get; set; }
        public string Message { get; set; }
    }
}
