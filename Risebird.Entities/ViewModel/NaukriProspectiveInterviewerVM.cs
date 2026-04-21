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

        public bool?    OnboardingCompleted         { get; set; }
        public int?     OnboardedUserId             { get; set; }
        public int?     OnboardedProfileId          { get; set; }
        public int?     AssessmentGroupId           { get; set; }
        public bool?    AcknowledgementEmailSent    { get; set; }

        public string   Status                      { get; set; }
        public string   ProcessedBy                 { get; set; }
        public DateTime CreatedAt                   { get; set; }
        public DateTime? UpdatedAt                  { get; set; }

        // Resolved from MasterAssessmentGroup / AssessmentGroup
        public int?     ResolvedAssessmentGroupId   { get; set; }
        public string   AssessmentGroupName         { get; set; }

        // Pagination
        public int      TotalCount                  { get; set; }
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

        [JsonProperty("resume_link")]
        public string ResumeLink                { get; set; }

        [JsonProperty("has_attachment")]
        public bool HasAttachment               { get; set; }
    }
}
