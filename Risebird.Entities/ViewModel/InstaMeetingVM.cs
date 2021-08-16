using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class InstaMeetingVM
    {
        public int id { get; set; }
        [Display(Name = "Required Skill")]
        public string RequiredSkill { get; set; }
        [Display(Name = "Requested Date")]
        public DateTime RequestedDate { get; set; }
        [Display(Name = "Assessment Group")]
        public string AssessmentGroup { get; set; }
        [Display(Name = "Candidate Name")]
        public string CandidateName { get; set; }
        [Display(Name = "Resume Link")]
        public string ResumeLink { get; set; }
        [Display(Name = "JDLink")]
        public string JDLink { get; set; }
        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; }
        public string Interviewer { get; set; }
        public string ZoomLink { get; set; }
        [Display(Name = "Confirmation Status")]
        public string ConfirmationStatus { get; set; }
        [Display(Name = "Meeting Status")]
        public int MeetingStatus { get; set; }
        public string intervieweStatusStatDesc { get; set; }
       
        public string MeetingStatusStatusDesc { get; set; }
        public int InterviewStatus { get; set; }
        [Display(Name = "Resume File")]
        public IFormFile ResumeFile { get; set; }
        [Display(Name = "JD File")]
        public IFormFile JDFile { get; set; }
        [Display(Name = "Assessment Group List ")]
        public SelectList AssessmentGroupList { get; set; }
    }
}
