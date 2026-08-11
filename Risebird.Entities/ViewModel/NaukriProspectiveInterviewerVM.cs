using Newtonsoft.Json;
using System;

namespace Risebird.Entities.ViewModel
{
    public class NaukriProspectiveInterviewerVM
    {
        public int      Id                          { get; set; }
        public string   SenderEmail                 { get; set; }
        public string   Subject                     { get; set; }
        public string   MailContent                 { get; set; }
        public DateTime? ReceivedAt                 { get; set; }

        public string   CandidateName               { get; set; }
        public string   Location                    { get; set; }
        public string   PastExperience              { get; set; }
        public string   NoticePeriod                { get; set; }
        public string   Education                   { get; set; }
        public string   KeySkills                   { get; set; }
        public string   CurrentRole                 { get; set; }
        public string   ExperienceYears             { get; set; }
        public string   Salary                      { get; set; }
        public string   MagName                     { get; set; }

        public string   ResumeLink                  { get; set; }
        public bool     HasAttachment               { get; set; }

        public string   ParsedEmail                 { get; set; }
        public string   ParsedPhone                 { get; set; }
        public bool?    ResumeParseCompleted        { get; set; }
        public bool?    IsExistingInterviewer       { get; set; }
        public int?     ExistingUserId              { get; set; }
        public int?     ExistingProfileId           { get; set; }

        public bool?    EmploymentVerified          { get; set; }
        public string   EmploymentCompanyName       { get; set; }
        public bool?    EmploymentIsEmployed        { get; set; }
        public string   EmploymentDetails           { get; set; }

        public bool?    ResumeAIAnalysisCompleted   { get; set; }
        public string   ResumeAIAnalysisResult      { get; set; }
        public string   AIVerdict                   { get; set; }
        public int?     AIFitmentScore              { get; set; }

        // LinkedIn background verification (Relay "LinkedIn Verified" variant, captured at ingestion)
        public string   LinkedInUrl                 { get; set; }
        public string   LinkedInMatch               { get; set; }
        public int?     LinkedInConfidence          { get; set; }
        public string   LinkedInReason              { get; set; }

        public bool?    OnboardingCompleted         { get; set; }
        public int?     OnboardedUserId             { get; set; }
        public int?     OnboardedProfileId          { get; set; }
        public int?     AssessmentGroupId           { get; set; }
        public bool?    AcknowledgementEmailSent    { get; set; }

        public int?     SelectedIRFId               { get; set; }
        public string   SelectedIRFName             { get; set; }
        public string   ApplicationLink             { get; set; }
        public bool?    InvitationEmailSent         { get; set; }
        public DateTime? InvitationEmailSentAt      { get; set; }
        public string   ProspectResponse            { get; set; }
        public DateTime? ProspectResponseAt         { get; set; }
        public bool?    SelfRegistrationCompleted   { get; set; }

        // Plivo CX voice agent call tracking
        public string   PlivoCallStatus             { get; set; }
        public string   PlivoConversationId         { get; set; }
        public DateTime? PlivoCallAt                { get; set; }
        public int?     PlivoCallDuration           { get; set; }
        public string   PlivoCallTranscript         { get; set; }
        public string   PlivoCallSummary            { get; set; }
        public string   PlivoProspectDisposition    { get; set; }
        public string   PlivoOnboardingPreference   { get; set; }
        public bool?    PlivoKycAccepted            { get; set; }
        public string   PlivoCalledBy               { get; set; }

        // Unified Intent (populated by both email replies and AI Plivo calls)
        public string   Intent                      { get; set; }
        public string   IntentSource                { get; set; }
        public DateTime? IntentConfirmedAt          { get; set; }

        // Ticket status (operational) is separate from FunnelStage (pipeline position)
        public string   TicketStatus                { get; set; }
        public string   FunnelStage                 { get; set; }

        // Onboarding type for reporting: SelfOnboarded | IntentAutoProcessed | ManuallyOnboarded
        public string   OnboardingType              { get; set; }

        // Ticket assignment — the operator accountable for this prospect (like SupportTicket).
        public int?      AssignedToUserId            { get; set; }
        public string    AssignedToEmail             { get; set; }
        public string    AssignedToName              { get; set; }
        public DateTime? AssignedAt                  { get; set; }

        public string   Status                      { get; set; }
        public string   Source                      { get; set; }
        public string   ProcessedBy                 { get; set; }
        public DateTime CreatedAt                   { get; set; }
        public DateTime? UpdatedAt                  { get; set; }

        // Automation tracking
        public string   AutomationStatus            { get; set; }
        public DateTime? AutomationLastProcessedAt  { get; set; }
        public int      PlivoCallAttemptsTotal      { get; set; }
        public int      PlivoCallAttemptsToday      { get; set; }
        public DateTime? PlivoCallLastAttemptDate   { get; set; }

        // Resolved from MasterAssessmentGroup / AssessmentGroup
        public int?     ResolvedAssessmentGroupId   { get; set; }
        public string   AssessmentGroupName         { get; set; }

        public bool HasResumeLink => !string.IsNullOrWhiteSpace(ResumeLink);

        // Pagination
        public int      TotalCount                  { get; set; }

        // Global status breakdown (across all matched records, not just this page).
        // Open + InProgress + Closed + Onboarded == TotalCount.
        public int      OpenCount                   { get; set; }
        public int      InProgressCount             { get; set; }
        public int      ClosedCount                 { get; set; }
        public int      OnboardedCount              { get; set; }
    }

    public class RelayNaukriPayload
    {
        [JsonProperty("sender_email")]
        public string SenderEmail               { get; set; }

        [JsonProperty("subject")]
        public string Subject                   { get; set; }

        [JsonProperty("mail_content")]
        public string MailContent               { get; set; }

        [JsonProperty("received_at")]
        public string ReceivedAt                { get; set; }

        [JsonProperty("candidate_name")]
        public string CandidateName             { get; set; }

        [JsonProperty("location")]
        public string Location                  { get; set; }

        [JsonProperty("past_experience")]
        public string PastExperience            { get; set; }

        [JsonProperty("notice_period")]
        public string NoticePeriod              { get; set; }

        [JsonProperty("education")]
        public string Education                 { get; set; }

        [JsonProperty("key_skills")]
        public string KeySkills                 { get; set; }

        [JsonProperty("current_role")]
        public string CurrentRole               { get; set; }

        [JsonProperty("experience_years")]
        public string ExperienceYears           { get; set; }

        [JsonProperty("salary")]
        public string Salary                    { get; set; }

        [JsonProperty("mag_name")]
        public string MagName                   { get; set; }

        [JsonProperty("candidate_email")]
        public string CandidateEmail            { get; set; }

        [JsonProperty("candidate_phone")]
        public string CandidatePhone            { get; set; }

        [JsonProperty("resume_link")]
        public string ResumeLink                { get; set; }

        [JsonProperty("has_attachment")]
        public bool HasAttachment               { get; set; }

        // LinkedIn background-verification verdict (from the "LinkedIn Verified" Relay variant).
        [JsonProperty("linkedin_url")]
        public string LinkedInUrl               { get; set; }

        [JsonProperty("linkedin_match")]
        public string LinkedInMatch             { get; set; }

        [JsonProperty("linkedin_confidence")]
        public int? LinkedInConfidence          { get; set; }

        [JsonProperty("linkedin_reason")]
        public string LinkedInReason            { get; set; }

        // Automation platform that produced this payload ('Relay' | 'n8n'). Absent on legacy
        // Relay.app calls, which are treated as 'Relay'. Distinct from the prospect's channel
        // Source, which stays 'Naukri' regardless of which platform ran the workflow.
        [JsonProperty("source")]
        public string Source                    { get; set; }
    }

    public class RelayLinkedInPayload
    {
        [JsonProperty("sender_email")]
        public string SenderEmail               { get; set; }

        [JsonProperty("subject")]
        public string Subject                   { get; set; }

        [JsonProperty("mail_content")]
        public string MailContent               { get; set; }

        [JsonProperty("received_at")]
        public string ReceivedAt                { get; set; }

        [JsonProperty("candidate_name")]
        public string CandidateName             { get; set; }

        [JsonProperty("job_title")]
        public string JobTitle                  { get; set; }

        [JsonProperty("headline")]
        public string Headline                  { get; set; }

        [JsonProperty("location")]
        public string Location                  { get; set; }

        [JsonProperty("past_experience")]
        public string PastExperience            { get; set; }

        [JsonProperty("skills_match")]
        public string SkillsMatch               { get; set; }

        [JsonProperty("candidate_email")]
        public string CandidateEmail            { get; set; }

        [JsonProperty("candidate_phone")]
        public string CandidatePhone            { get; set; }

        [JsonProperty("resume_link")]
        public string ResumeLink                { get; set; }

        [JsonProperty("has_attachment")]
        public bool HasAttachment               { get; set; }
    }
}
