using Newtonsoft.Json;
using System;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerEmailAvailabilityVM
    {
        public int      Id                          { get; set; }
        public string   SenderEmail                 { get; set; }
        public int?     UserId                      { get; set; }
        public int?     InterviewerProfileId        { get; set; }
        public string   Subject                     { get; set; }
        public string   MailContent                 { get; set; }
        public DateTime? ReceivedAt                 { get; set; }
        public string   JobTitle                    { get; set; }
        public string   InterviewerName             { get; set; }
        public string   Phone                       { get; set; }
        public string   MagName                     { get; set; }
        public string   IRFReference                { get; set; }
        public string   HasLoginCredentials         { get; set; }
        public string   SlotsPerDay                 { get; set; }
        public string   TimeFrame                   { get; set; }
        public string   AvailabilityIntent          { get; set; }
        public string   MailBodyIntent              { get; set; }
        public string   ConcernsOrNotInterested     { get; set; }
        public string   ResumeFitment               { get; set; }
        public string   ResumeLink                  { get; set; }
        public bool     HasAttachment               { get; set; }
        public string   Status                      { get; set; }
        public bool?    EmploymentVerified          { get; set; }
        public bool?    ResumeAIAnalysisCompleted   { get; set; }
        public string   ResumeAIAnalysisResult      { get; set; }
        public int?     AssessmentGroupId           { get; set; }
        public DateTime? AssessmentGroupAssignedAt  { get; set; }
        public string   ProcessedBy                 { get; set; }
        public DateTime CreatedAt                   { get; set; }
        public DateTime? UpdatedAt                  { get; set; }

        // Joined from InterviewerProfile
        public string   InterviewerNickName         { get; set; }
        public string   CurrentRole                 { get; set; }
        public decimal? Experience                  { get; set; }
        public bool?    IsProfileVerified           { get; set; }
        public bool?    IsEmployed                  { get; set; }
        public string   CompanyName                 { get; set; }
        public string   ProfileResumeLink           { get; set; }
        public string   ExistingAssessmentGroupIds  { get; set; }
        public string   UserEmail                   { get; set; }
        public long?    UserMobileNumber            { get; set; }

        // Joined from MasterAssessmentGroup / AssessmentGroup
        public string   MasterAssessmentGroupName   { get; set; }
        public int?     ResolvedAssessmentGroupId   { get; set; }
        public string   AssessmentGroupName         { get; set; }

        // Pagination
        public int      TotalCount                  { get; set; }
    }

    public class RelayInterviewerAvailabilityPayload
    {
        [JsonProperty("sender_email")]
        public string SenderEmail               { get; set; }

        [JsonProperty("subject")]
        public string Subject                   { get; set; }

        [JsonProperty("mail_content")]
        public string MailContent               { get; set; }

        [JsonProperty("received_at")]
        public string ReceivedAt                { get; set; }

        [JsonProperty("job_title")]
        public string JobTitle                  { get; set; }

        [JsonProperty("name")]
        public string Name                      { get; set; }

        [JsonProperty("phone")]
        public string Phone                     { get; set; }

        [JsonProperty("mag_name")]
        public string MagName                   { get; set; }

        [JsonProperty("irf_id")]
        public string IRFId                     { get; set; }

        [JsonProperty("has_login_credentials")]
        public string HasLoginCredentials       { get; set; }

        [JsonProperty("slots_per_day")]
        public string SlotsPerDay               { get; set; }

        [JsonProperty("time_frame")]
        public string TimeFrame                 { get; set; }

        [JsonProperty("availability_intent")]
        public string AvailabilityIntent        { get; set; }

        [JsonProperty("mail_body_intent")]
        public string MailBodyIntent            { get; set; }

        [JsonProperty("concerns_or_not_interested")]
        public string ConcernsOrNotInterested   { get; set; }

        [JsonProperty("resume_fitment")]
        public string ResumeFitment             { get; set; }

        [JsonProperty("resume_link")]
        public string ResumeLink                { get; set; }

        [JsonProperty("has_attachment")]
        public bool HasAttachment               { get; set; }
    }
}
