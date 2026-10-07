using Risebird.Entities.Data;
using System;

namespace Risebird.Entities.ViewModel
{
    public class MailRecipientActivityDetailsVM : SendgridEventData
    {
        public int? InterviewId { get; set; }
        public int? IRFId { get; set; }
        public string SkillChannelName { get; set; }
        public DateTime? interviewDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string MailSubject { get; set; }
        public string MailBody { get; set; }
        public string SentBy { get; set; }
        public int partitionNumber { get; set; }
        public string eventType { get; set; }
        public string latestEvent { get; set; }
        public DateTime ActivityDate { get; set; }
        public DateTime SentOn { get; set; }
        public string MobileNumber { get; set; }
        public string RecipientName { get; set; }
        public string CompanyName { get; set; }
        public string ResumeLink { get; set; }
        public int? VideoConfirmation { get; set; }
        public int RedMarker { get; set; }
        public string interviewer { get; set; }
        public int TotalInterviews { get; set; }
        public string ChannelOwner { get; set; }
        public string SkillChannel { get; set; }
        public string candidateEmail { get; set; }
        public string candidateName { get; set; }
        public string CalendlyLink { get; set; }
        public string CandidateClientHR { get; set; }
        public DateTime EndDate { get; set; }
        // public bool IsActive { get; set; }
        public string IsAvailable { get; set; }
        public string CampaignName { get; set; }
        public string SchedulerUniqueid { get; set; }
        public int? InterviewerId { get; set; }
        public int? CandidateId { get; set; }
        public int SendgridEventId { get; set; }
        public int SentMailDetailsId { get; set; }
        public string BounceReason { get; set; }
        public int BounceCount { get; set; }
        /// <summary>Latest MailBounceResolution.Status: EmailCorrected | Resent | Resolved | Unreachable; null = Open.</summary>
        public string ResolutionStatus { get; set; }
        /// <summary>Domain of the skill channel's billing owner; not allowed for a candidate email.</summary>
        public string ClientDomain { get; set; }
        /// <summary>Temporary (4.x.x, mailbox full, quota, try again) or Permanent.</summary>
        public string BounceKind { get; set; }

        /// <summary>
        /// The bounced address has been fixed: either this row was corrected, or the candidate's current
        /// address no longer matches the bounced one (another row of the same address was corrected).
        /// </summary>
        public bool EmailCorrected =>
            ResolutionStatus == "EmailCorrected"
            || (!string.IsNullOrWhiteSpace(candidateEmail) && !string.IsNullOrWhiteSpace(email)
                && !string.Equals(candidateEmail.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));

        public bool IsResolved =>
            ResolutionStatus == "Resolved" || ResolutionStatus == "Unreachable" || ResolutionStatus == "Resent"
            || ResolutionStatus == "RetriedSameAddress";
    }
}
