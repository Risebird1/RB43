using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class perdaytime
    {
        public string Start { get; set; }
        public string End { get; set; }
    }
    public class daytimes_second
    {
        public string Daytype { get; set; }
        public List<perdaytime> dayvalue { get; set; }
        public bool dayCheck { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string SkillChannel { get; set; }
        public string Uniqueid { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string SkillChannelRelation { get; set; }
        public int Concurrency { get; set; }
        public int BufferTime { get; set; }
        public string MeetingPlatform { get; set; }
        public string schedulertype { get; set; }
        public bool IsSingleUse { get; set; }
        public int duration { get; set; }
    }
    public class daytimes_report
    {
        public int id { get; set; }
        [Display(Name = "Event Name")]
        public string EventName { get; set; }
        public List<daytimes_second> setvalue { get; set; }
        [Display(Name = "Skill Channel")]
        public string SkillChannel { get; set; }
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }
        [Display(Name = "Intervals")]
        public int duration { get; set; }
        public SelectList timeslots { get; set; }
        public string Uniqueid { get; set; }
        public string EventUrl { get; set; }
        public bool IsActive { get; set; }
        public SelectList IRFList { get; set; }
        public SelectList IntervalList { get; set; }
        public int Eventtype { get; set; }
        public int Concurrency { get; set; }
        [Display(Name = "BufferTime(in Hr.)")]
        public int BufferTime { get; set; }
        public string MeetingPlatform { get; set; }
    }
    public class ScheculeInterview
    {
        public string Name { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        [Display(Name = "Interview Channel")]
        public string InterviewChannelId { get; set; }
        [EmailAddress]
        [Display(Name = "Client HR Coordinator")]
        public string HRCoordinator { get; set; }
        public string InterviewDate { get; set; }
        [Display(Name = "Upload Your Resume")]
        public IFormFile ResumeFile { get; set; }
        public string Resume { get; set; }
        [EmailAddress]
        [Display(Name = "Staffing Agencies(Source)")]
        public string StaffingAgiencies { get; set; }
        public string Uniqueid { get; set; }
        public DateTime scheduleDate { get; set; }
        public int offset { get; set; }
        public ScheculeInterviewIRFVM _IRFDetails { get; set; }
        [Display(Name = "Laptop or Machine on which i can code")]
        public bool Checkbox1 { get; set; }
        [Display(Name = "Quality Headphone")]
        public bool Checkbox2 { get; set; }
        [Display(Name = "High Speed Internet")]
        public bool Checkbox3 { get; set; }
        [Display(Name = "Working webcam")]
        public bool Checkbox4 { get; set; }
        [Display(Name = "Chrome Browser")]
        public bool Checkbox5 { get; set; }
        [Display(Name = "Silent place with appropriate lighting")]
        public bool Checkbox6 { get; set; }
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; }
        public string CountryCode { get; set; }
        public string ClientName { get; set; }
        public string MeetingPlatform { get; set; }
        public SelectList _listhr { get; set; }
        public SelectList _liststaffing { get; set; }
        public List<SelectListItem> TimeZonesList { get; set; }
        public string TimeZones { get; set; }
        public string TimeZonesValue { get; set; }
        public int TimeZoneoffset { get; set; }
        public string schedulertype { get; set; }
        public bool reschedulerequest { get; set; }
        public int interviewid { get; set; }
        public int returnvaluetype { get; set; }
    }
    public class TimeslotsVM
    {
        public double id { get; set; }
        public string Name { get; set; }
    }
    public class InterviewSchedulerIRF
    {
        public string IRFId { get; set; }
        public DateTime EndDate { get; set; }
    }
    public class ScheculeInterviewIRFVM
    {
        public string LogoLink { get; set; }
        public string Name { get; set; }
        public string JDTiltle { get; set; }
        public string CompanyLink { get; set; }
        public int inIRFid { get; set; }
    }
    public class CandidateIRFData
    {
        public string CandidateName { get; set; }
        public string candidateEmail { get; set; }
        public string resumeLink { get; set; }
        public string staffingAgency { get; set; }
        public string ClientHrContact { get; set; }
        public string MobileNumber { get; set; }
    }
    public class RescheduleInterviewDetails
    {
        public string uniqueid { get; set; }
        public string ChannelOwner { get; set; }
        public DateTime InterviewDate { get; set; }
        public int? TimeZoneoffset { get; set; }
        public string TimeZonesValue { get; set; }
        public int IRFId { get; set; }
    }
}
