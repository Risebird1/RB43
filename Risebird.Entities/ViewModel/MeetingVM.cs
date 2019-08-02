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
        public int imid { get; set; }

        [Display(Name = "IRF ID")]
        [Required(ErrorMessage = "Please enter existing IRF Name.")]
        public string IRFId { get; set; }
       
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
        public string candidateName { get; set; }
        [Display(Name = "Client HR")]
        [RegularExpression(@"^([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,4})+$",
        ErrorMessage = "Please Enter Correct Email Address")]
        //[Required(ErrorMessage = "Please enter Client HR Email Id.")]
        public string ClientHrContact { get; set; }
        
        public string resumeLink { get; set; }
        [Display(Name = "Confirmation Status")]
        [Required(ErrorMessage = "Please enter Confirmation Status.")]
        public string comfirmationStatus { get; set; }
        [Display(Name = "Zoom Meeting Link")]
        [Required(ErrorMessage = "Please enter Zoom Meeting Link.")]
        public string zoomLink { get; set; }
        [Display(Name = "Zoom Login Details")]
        [Required(ErrorMessage = "Please enter Zoom Login Details.")]
        public string zoomDetails { get; set; }
        [Display(Name = "Risebird HR")]
        [Required(ErrorMessage = "Please enter Risebird HR Email Id.")]
        public string risebirdHR { get; set; }
        [Display(Name = "Interviewer")]
        public string interviewer { get; set; }
        [Display(Name = "Preffered Location")]
        [Required(ErrorMessage = "Please enter Preffered Location.")]
        public string prefferedLocation { get; set; }
        [Display(Name = "Staffing Agency")]
        [Required(ErrorMessage = "Please enter Staffing Agency.")]
        public string staffingAgency { get; set; }
        [Display(Name = "Expected Per Hour Rate")]
        [Required(ErrorMessage = "Please enter Expected Per Hour Rate.")]
        public string expectedPerHourRate { get; set; }
        [Display(Name = "Expected Joining Time")]
        [Required(ErrorMessage = "Please enter Joining Time.")]
        public string joiningTime { get; set; }
        public string interviewerComment { get; set; }
        public string intervieweStatus { get; set; }
        public string selectionStatus { get; set; }
        public string videoLink { get; set; }
        public string reportLink { get; set; }
        public string fraudSyncComment { get; set; }
        public string hrCoordinatorComment { get; set; }
        public string trainableComment { get; set; }
        public DateTime dtCreatedOn { get; set; }
        [Display(Name = "Upload Resume")]
        public IFormFile File { get; set; }

        public int? L2InterviewStatus { get; set; }
        public int? ClientInterviewStatus { get; set; }
        public string L2InterviewStatDesc { get; set; }
        public string ClientInterviewStatDesc { get; set; }
    }
}
