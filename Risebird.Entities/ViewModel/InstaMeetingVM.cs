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
        [Display(Name = "Interview Status")]
        public SelectList InterviewStatusList { get; set; }

        [Display(Name = "Meeting Status")]
        public SelectList MeetingStatusList { get; set; }

        [Display(Name = "Resume File")]
        public IFormFile ResumeFile { get; set; }
        [Display(Name = "JD File")]
        public IFormFile JDFile { get; set; }
        [Display(Name = "Assessment Group List ")]
        public SelectList AssessmentGroupList { get; set; }
        [Display(Name = "Insta Request Name ")]
        public string InstaRequestName { get; set; }
        public IList<InstaTechSkill> TechSkill { get; set; }
        public int RequestId { get; set; }
        public string InterviewerComment { get; set; }
        public string TechSkillComments { get; set; }
        public string VideoLink { get; set; }
        public string PageVideoLink { get; set; }
        public IFormFile VideoFile { get; set; }
        public int TotalCandidate { get; set; }
        public int CompletedInterview { get; set; }
        public int SelectedCandidate { get; set; }
    }
    public class InstaTechSkill
    {
        public int id { get; set; }
        public int InstaRequestId { get; set; }
        public string Level { get; set; }
        public int LevelId { get; set; }
        public int ActualLevelId { get; set; }
        public string SkillName { get; set; }
        public string SkillId { get; set; }
    }
}
