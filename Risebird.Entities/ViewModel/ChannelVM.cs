using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class ChannelVM
    {
        [Required]
        public string Client { get; set; }
        [Required]
        [EmailAddress]
        [Display(Name = "Recruiter Email Address")]
        public string Recruiter { get; set; }
        [Display(Name = "Primary Skill")]
        [Required]
        public string PrimarySkill { get; set; }
        [Required]
        public string EndClient { get; set; }
        [Display(Name = "Job Country and Location")]
        [Required]
        public string Location { get; set; }
        [Display(Name = "Expected Volume")]
        [Required]
        public int ExpectedVolume { get; set; }
        public IFormFile Jdfile { get; set; }
        public string JdLink { get; set; }
        public bool isEditable { get; set; }
        [Display(Name = "Weekend Drive")]
        public int WeekendDrive { get; set; }
        [Display(Name = "PO Generated")]
        public int POGenerated { get; set; }
        [Display(Name = "IPS Created")]
        public int IPSCreated { get; set; }
        [Display(Name = "Calendly Link Created")]
        public int CalendlyLinkCreated { get; set; }
        [Display(Name = "Candidate Sourcing")]
        public int CandidateSourcing { get; set; }
        [Display(Name = "Additional Comment")]
        public string AdditionalComment { get; set; }
        [Display(Name = "HR Coordinator")]
        public string HRCoordinator { get; set; }
        [Display(Name = "Program Manager")]
        public string ProgramManager { get; set; }
        public int IRFID { get; set; }
        public string IRFIDs { get; set; }
        public string Priority { get; set; }
        public SelectList Prioritylist { get; set; }
        public string CreatedBy { get; set; }
        public int id { get; set; }
        [Required]
        [Display(Name = "Requester EmailAddress")]
        public string RequesterEmailid { get; set; }
        [Required]
        [Display(Name = "Requester Mobile Number")]
        public string RequesterMobile { get; set; }
    }
    public class ChannelManagementVM
    {
        public int id { get; set; }
        public string Client { get; set; }
        public string Status { get; set; }
        public int InterviewCount { get; set; }
        public string IRFIDs { get; set; }
        public string PrimarySkill { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class ChannelInterviewerVM
    {
        public int id { get; set; }
        public DateTime ProcessedDate { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }
        public string Mobile { get; set; }
        public IFormFile CV { get; set; }
        public string CVLink { get; set; }
        public IFormFile CodedCV { get; set; }
        public string CodedCVLink { get; set; }
        public string PrimarySKill { get; set; }
        public string Role { get; set; }
        public string Priority { get; set; }
        public string Comment { get; set; }
        public int Approved { get; set; }
        [Display(Name = "DM Call Availability")]
        public int DMCallAvailability { get; set; }
        [Display(Name = "Added over Portal")]
        public int AddedoverPortal { get; set; }
        [Display(Name = "Added over SKill Channel")]
        public int AddedoverSKillChannel { get; set; }
        public int QC { get; set; }
        public int Emergency { get; set; }
        public int Training { get; set; }
        public int AdvancePayment { get; set; }
        public int CallAttended { get; set; }
        public int Channelid { get; set; }
        public SelectList Prioritylist { get; set; }
        public string CreatedBy { get; set; }
        [Display(Name = "Per Interview Rate")]
        public int PerInterviewrate { get; set; }
        [Display(Name = "Training mail send")]
        public int Trainingmailsend { get; set; }
    }
}
