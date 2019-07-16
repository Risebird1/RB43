using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    [Serializable]
    public class MeetingVM
    {
        [Display(Name = "IRF Name")]
        [Required(ErrorMessage = "Please enter existing IRF Name.")]
        public string IRFName { get; set; }
        [Display(Name = "Client Name")]
        [Required(ErrorMessage = "Please enter existing Client Name.")]
        public string clientName { get; set; }
        [Display(Name = "Job Role")]
        [Required(ErrorMessage = "Please enter Job Role.")]
        public string JobRole { get; set; }
        [Display(Name = "Interview Date")]
        [Required(ErrorMessage = "Please enter Interview Date.")]
        public DateTime interviewDate { get; set; }
        [Display(Name = "Interview Slot")]
        [Required(ErrorMessage = "Please enter Interview Slot.")]
        public string interviewSlot { get; set; }
        [Display(Name = "Interview Duration")]
        [Required(ErrorMessage = "Please enter Interview Duration.")]
        public string interviewDuration { get; set; }
        [Display(Name = "Primary Skill")]
        [Required(ErrorMessage = "Please enter Primary Skills.")]
        public string primaryskill { get; set; }
        [Display(Name = "Candidate Email")]
        [Required(ErrorMessage = "Please enter Candidate Email Id.")]
        public string candidateEmail { get; set; }
        [Display(Name = "Client HR")]
        [Required(ErrorMessage = "Please enter Client HR Email Id.")]
        public string ClientHrContact { get; set; }
        [Display(Name = "Resume Links")]
        [Required(ErrorMessage = "Please enter Resume Link.")]
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
        [Required(ErrorMessage = "Please enter Interviewer Email Id.")]
        public string interviewer { get; set; }
        public string interviewerComment { get; set; }
        public string intervieweStatus { get; set; }
        public string selectionStatus { get; set; }
        public string videoLink { get; set; }
        public string reportLink { get; set; }
        public string fraudSyncComment { get; set; }
        public string hrCoordinatorComment { get; set; }
        public string trainableComment { get; set; }
        public DateTime dtCreatedOn { get; set; }
    }
}
