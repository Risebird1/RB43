using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

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
    }
    public class ScheculeInterview
    {
        public string Name { get; set; }
        public string Email { get; set; }
        [Display(Name = "Interview Channel Id")]
        public string InterviewChannelId { get; set; }
        [EmailAddress]
        [Display(Name = "Client HR Coordinator")]
        public string HRCoordinator { get; set; }
        public string InterviewDate { get; set; }
        [Display(Name = "Upload Your Resume")]
        public IFormFile ResumeFile { get; set; }
        public string Resume { get; set; }
        [Display(Name = "Staffing Agiencies")]
        public string StaffingAgiencies { get; set; }
        public string Uniqueid { get; set; }
        public DateTime scheduleDate { get; set; }
        public int offset { get; set; }
        public ScheculeInterviewIRFVM _IRFDetails { get; set; }
        [Display(Name = "Laptop or machine on which i can code")]
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
        public string MobileNumber { get; set; }
        public string CountryCode { get; set; }
        public string ClientName { get; set; }
    }
    public class TimeslotsVM
    {
        public int id { get; set; }
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
    }
}
