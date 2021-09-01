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
        public string AccountEmail { get; set; }
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
    public class InstaZoomMeetingVM
    {
        public InstaZoomMeetingVM()
        {
            settings = new InstaZoomSetting();
        }
        public string topic { get; set; }
        public int type { get; set; }
        public string start_time { get; set; }
        public int duration { get; set; }
        public string timezone { get; set; }
        public InstaZoomSetting settings { get; set; }
    }
    public class InstaZoomSetting
    {
        public bool host_video { get; set; }
        public bool participant_video { get; set; }
        public bool join_before_host { get; set; }
        public int jbh_time { get; set; }
        public string auto_recording { get; set; }//cloud
    }
    public class InstaZoomMeetingResponse
    {
        public long id { get; set; }
        public string join_url { get; set; }
        public DateTime InterviewDate { get; set; }
    }
    public class ZoomAccountDetailsVM
    {
        public int id { get; set; }
        public string AccountEmailid { get; set; }
        public bool Active { get; set; }
    }
}
