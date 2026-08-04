using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class ChannelVM
    {
        public string Client { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Please select a client")]
        [Display(Name = "Client")]
        public int ClientId { get; set; }
        [Display(Name = "Plan Type")]
        public int PlanType { get; set; }
        [Display(Name = "Client Segment")]
        public int ClientType { get; set; }
        [Display(Name = "Requester Name")]
        public string Requester { get; set; }
        [Display(Name = "Primary Skill")]
        [Required]
        public string PrimarySkill { get; set; }
        public string EndClient { get; set; }
        [Display(Name = "Job Country and Location")]
        [Required]
        public string Location { get; set; }
        [Display(Name = "Expected Volume")]
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
        [Display(Name = "Skill Channel")]
        public int IRFID { get; set; }
        [Display(Name = "IRFID")]
        public string IRFIDs { get; set; }
        public string Priority { get; set; }
        public SelectList Prioritylist { get; set; }
        public SelectList ClientList { get; set; }
        public SelectList CreatedByList { get; set; }
        public SelectList PlanTypeList { get; set; }
        public SelectList ClientTypeList { get; set; }
        [Display(Name = "Your Email Adress")]
        [Required(ErrorMessage = "Please select a Risebird user")]
        public string CreatedBy { get; set; }
        public int id { get; set; }
        [Required]
        [Display(Name = "Requester EmailAddress")]
        public string RequesterEmailid { get; set; }
        [Display(Name = "Requester Mobile Number")]
        public string RequesterMobile { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        [Display(Name = "Seniority Level")]
        public string SeniorityLevel { get; set; }
        [Required]
        [Display(Name = "Turn Around Time in days")]
        public string TurnAroundTimeindays { get; set; }
        public string Comments { get; set; }
        [Required]
        [Display(Name = "Number of Interviews")]
        public int NumberofInterviews { get; set; }
        public string Origin { get; set; }
        /// <summary>'Verified' | 'External' — set by public intake / Add client user promotion.</summary>
        public string RequesterType { get; set; }
        public SelectList Originlist { get; set; }
        [Display(Name = " Engagement Type")]
        public string EngagementType { get; set; }
        public SelectList EngagementTypelist { get; set; }
        public string Stage { get; set; }
        public SelectList stagelist { get; set; }
        [Display(Name = "Expected Price")]
        public int ExpectedPrice { get; set; }
        [Display(Name = "Expected revenue")]
        public int Expectedrevenue { get; set; }
        public string PaymentStatus { get; set; }
        public SelectList PaymentStatuslist { get; set; }
        public string ConfirmationStatus { get; set; }
        public SelectList ConfirmationStatuslist { get; set; }
        /// <summary>Optional insert override (public intake sets Pending).</summary>
        public string Status { get; set; }
        public int? MAGId { get; set; }
        public int? TemplateId { get; set; }
    }
    public class ChannelManagementVM
    {
        public int id { get; set; }
        public string Client { get; set; }
        public string EndClient { get; set; }
        public string Status { get; set; }
        public int InterviewerCount { get; set; }
        public string IRFIDs { get; set; }
        public string PrimarySkill { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Title { get; set; }
        public string RequesterEmailid { get; set; }
        public int? MAGId { get; set; }
        public int? TemplateId { get; set; }
        /// <summary>'Verified' | 'External'</summary>
        public string RequesterType { get; set; }
        public string Origin { get; set; }
        public bool IsProcessed =>
            (MAGId.HasValue || TemplateId.HasValue) && !string.IsNullOrWhiteSpace(IRFIDs);
    }

    public class SetRequirementDetailsVM
    {
        public int Id { get; set; }
        public int? MAGId { get; set; }
        public int? TemplateId { get; set; }
        public string IRFIDs { get; set; }
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
        [Required]
        public string Mobile { get; set; }
        public IFormFile CV { get; set; }
        public string CVLink { get; set; }
        public IFormFile CodedCV { get; set; }
        public string CodedCVLink { get; set; }
        [Required]
        public string PrimarySKill { get; set; }
        [Required]
        public string Role { get; set; }
        public string Priority { get; set; }
        [Required]
        public string Comment { get; set; }
        public int Approved { get; set; }
        [Display(Name = "DM Call Completed")]
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
        [Display(Name = "Training Time")]
        public DateTime TrainingTime { get; set; }
        [Display(Name = "Call Time")]
        public DateTime CallTime { get; set; }
        public bool isEditable { get; set; }
    }
    public class ChannelApprovedInterviewer
    {
        public int InterviewerProfileId { get; set; }
        public int id { get; set; }
        public int UserId { get; set; }
        public int Channelid { get; set; }
        public string CurrentRole { get; set; }
        public string ResumeLink { get; set; }
        public string PublicUrl { get; set; }
        public string FirstName { get; set; }
        public string EmailId { get; set; }
        public string SkillName { get; set; }
        public bool IsAdded { get; set; }
        public string status { get; set; }
        public bool Isclient { get; set; }
    }
    public class AddChannelInterviewersVM
    {
        public IList<ChannelApprovedInterviewer> ChannelInterviewersCollection { get; set; }
        public int ChannelId { get; set; }
        public string ChannelTitle { get; set; }
    }
}
