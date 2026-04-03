using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// View model for the Support Tickets dashboard.
    /// Maps 1:1 to SupportTickets table columns + joined ClientName.
    /// TotalCount is populated by COUNT(*) OVER() in usp_GetSupportTickets.
    /// </summary>
    public class SupportTicketVM
    {
        public int      Id                          { get; set; }
        public string   SenderEmail                 { get; set; }
        public string   Subject                     { get; set; }
        public string   MailContent                 { get; set; }
        public DateTime? ReceivedAt                 { get; set; }
        public string   Intent                      { get; set; }
        public string   PrimarySpecialization       { get; set; }
        public string   MailBodyIntent              { get; set; }
        public bool     AttachmentJobDescription    { get; set; }
        public bool     AttachmentPdf               { get; set; }
        public bool     AttachmentResumes           { get; set; }
        public string   DriveFolderLink             { get; set; }
        public string   SummaryMail                 { get; set; }
        public string   SummaryCandidates           { get; set; }
        public string   SummaryInterviewScores      { get; set; }
        public int?     UserId                      { get; set; }
        public int?     ClientId                    { get; set; }
        public string   AssignedTo                  { get; set; }
        public string   Status                      { get; set; }
        public DateTime CreatedAt                   { get; set; }
        public string   ClientName                  { get; set; }
        public int      TotalCount                  { get; set; }

        /// <summary>IRF integer id (from CandidateResume) for tickets processed by System.</summary>
        public int?     IngestedIRFId               { get; set; }

        /// <summary>IRF string name (from IRF.IRFId) for tickets processed by System.</summary>
        public string   IngestedIRFName             { get; set; }

        /// <summary>JD title of the ingested IRF.</summary>
        public string   IngestedIRFJDTitle          { get; set; }

        /// <summary>True if this ticket has candidates with auto-schedule data (interview slots from email).</summary>
        public bool     HasAutoScheduleCandidates   { get; set; }
    }

    /// <summary>
    /// JSON payload that Relay.app POSTs to the webhook endpoint.
    /// All property names match the snake_case keys defined in the agent prompt.
    /// </summary>
    public class RelayEmailWebhookPayload
    {
        [JsonProperty("sender_email")]
        public string SenderEmail                   { get; set; }

        [JsonProperty("subject")]
        public string Subject                       { get; set; }

        [JsonProperty("mail_content")]
        public string MailContent                   { get; set; }

        [JsonProperty("received_at")]
        public string ReceivedAt                    { get; set; }

        [JsonProperty("webhook_intent")]
        public string WebhookIntent                 { get; set; }

        [JsonProperty("primary_specialization")]
        public string PrimarySpecialization         { get; set; }

        [JsonProperty("mail_body_intent")]
        public string MailBodyIntent                { get; set; }

        [JsonProperty("attachment_job_description")]
        public bool AttachmentJobDescription        { get; set; }

        [JsonProperty("attachment_pdf")]
        public bool AttachmentPdf                   { get; set; }

        [JsonProperty("attachment_resumes")]
        public bool AttachmentResumes               { get; set; }

        [JsonProperty("drive_folder_link")]
        public string DriveFolderLink               { get; set; }

        [JsonProperty("summary_mail")]
        public string SummaryMail                   { get; set; }

        [JsonProperty("summary_candidates")]
        public string SummaryCandidates             { get; set; }

        [JsonProperty("summary_interview_scores")]
        public string SummaryInterviewScores        { get; set; }

        [JsonProperty("job_title")]
        public string JobTitle                      { get; set; }

        /// <summary>
        /// Explicit Risebird Skill Channel ID (IRFId) extracted from the email body.
        /// Exactly 10 alphanumeric characters when present. Allows the server to skip
        /// the scoring algorithm and resolve the IRF directly.
        /// </summary>
        [JsonProperty("skill_channel_id")]
        public string SkillChannelId                { get; set; }

        /// <summary>
        /// Google Drive URLs of individual resume files uploaded during the loop.
        /// Relay passes these as a JSON array: ["url1","url2"].
        /// Populated only when webhook_intent = "New candidate submission".
        /// DEPRECATED: Use resume_associations instead for per-resume IRF mapping.
        /// </summary>
        [JsonProperty("resume_drive_urls")]
        public List<string> ResumeDriveUrls         { get; set; }

        /// <summary>
        /// Per-resume IRF associations. Each entry maps a resume URL to its intended
        /// skill channel/IRF based on context from the email body.
        /// Use this instead of the flat resume_drive_urls + single skill_channel_id.
        /// </summary>
        [JsonProperty("resume_associations")]
        public List<ResumeAssociation> ResumeAssociations { get; set; }

        /// <summary>
        /// Structured candidate scheduling data extracted by Relay AI.
        /// Only populated when intent is "New candidate submission" or "New interview request"
        /// AND the email contains interview scheduling information with 100% confidence.
        /// </summary>
        [JsonProperty("candidate_schedule_data")]
        public List<CandidateScheduleEntry> CandidateScheduleData { get; set; }
    }

    /// <summary>
    /// Maps a single resume to its intended IRF/skill channel.
    /// Used when an email contains multiple resumes intended for different roles.
    /// </summary>
    public class ResumeAssociation
    {
        [JsonProperty("resume_url")]
        public string ResumeUrl               { get; set; }

        [JsonProperty("skill_channel_id")]
        public string SkillChannelId          { get; set; }

        [JsonProperty("job_title")]
        public string JobTitle                { get; set; }

        [JsonProperty("candidate_name")]
        public string CandidateName           { get; set; }
    }

    /// <summary>
    /// Represents one candidate's scheduling data extracted from an email.
    /// All fields require 100% confidence for auto-scheduling to proceed.
    /// </summary>
    public class CandidateScheduleEntry
    {
        [JsonProperty("candidate_name")]
        public string CandidateName           { get; set; }

        [JsonProperty("candidate_email")]
        public string CandidateEmail          { get; set; }

        [JsonProperty("interview_date")]
        public string InterviewDate           { get; set; }

        [JsonProperty("interview_time")]
        public string InterviewTime           { get; set; }

        [JsonProperty("timezone")]
        public string Timezone                { get; set; }

        [JsonProperty("skill_channel_id")]
        public string SkillChannelId          { get; set; }

        [JsonProperty("resume_drive_url")]
        public string ResumeDriveUrl          { get; set; }
    }

    /// <summary>
    /// Result of auto-scheduling attempt for a single candidate.
    /// </summary>
    public class CandidateScheduleResult
    {
        public string CandidateName           { get; set; }
        public string CandidateEmail          { get; set; }
        public string SkillChannelId          { get; set; }
        public DateTime? InterviewDateTime    { get; set; }
        public bool Scheduled                 { get; set; }
        public string Reason                  { get; set; }
        public int? CandidateResumeId         { get; set; }
        public string MeetingLink             { get; set; }
    }

    /// <summary>
    /// Represents one IRF candidate returned by usp_GetIRFsForSender,
    /// used during server-side job-title → IRF matching.
    /// </summary>
    public class IrfMatchVM
    {
        public int       Id              { get; set; }
        public string    IRFStringId     { get; set; }
        public string    JDTiltle        { get; set; }
        public string    SkillCluster    { get; set; }
        public int       RisebirdHrId    { get; set; }
        public string    RisebirdHrEmail { get; set; }
        public DateTime? StartDate       { get; set; }
        public DateTime? EndDate         { get; set; }
        public DateTime? CreatedOn       { get; set; }

        /// <summary>True if this mapping was auto-created by usp_AutoMapUserToIRF.</summary>
        public bool      WasAutoMapped   { get; set; }
    }

    /// <summary>
    /// ViewModel for auto-schedule candidate data shown in the HR review modal.
    /// Contains both extracted (from email) and parsed (from resume) data.
    /// </summary>
    public class AutoScheduleCandidateVM
    {
        public int       CandidateResumeId           { get; set; }
        public int       IRFId                       { get; set; }
        public string    ResumeLink                  { get; set; }
        public DateTime  AddedOn                     { get; set; }
        public bool?     IsProcessed                 { get; set; }
        public bool?     IsResumeParsed              { get; set; }
        public bool?     IsResumeParsingFailed       { get; set; }
        public bool?     IsEmailMissing              { get; set; }
        public string    ReasonBehindNotProcessed    { get; set; }
        public bool      IsAutoScheduled             { get; set; }
        public DateTime? ScheduledInterviewDateTime  { get; set; }
        
        // Extracted data (from email)
        public string    ExtractedCandidateName      { get; set; }
        public string    ExtractedCandidateEmail     { get; set; }
        
        // IRF info
        public string    SkillChannelId              { get; set; }
        public string    JobTitle                    { get; set; }
        public int?      ClientId                    { get; set; }
        public string    ClientName                  { get; set; }
        public string    TenantId                    { get; set; }
        
        // Parsed data (from resume parsing, if completed)
        public int?      CandidateId                 { get; set; }
        public string    ParsedCandidateName         { get; set; }
        public string    ParsedCandidateEmail        { get; set; }
        public string    ParsedCandidatePhone        { get; set; }
        
        // Status indicators
        public string    ParsingStatus               { get; set; }
        public bool      IsAlreadyScheduled          { get; set; }
        public bool?     EmailMatches                { get; set; }
    }

    /// <summary>Request model for scheduling a candidate from the HR review modal.</summary>
    public class ScheduleCandidateRequest
    {
        public int TicketId            { get; set; }
        public int CandidateResumeId   { get; set; }
    }
}
