using System;

namespace Risebird.Entities.Data
{
    public class InterviewerEmailAvailability
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
        public string   Intent                      { get; set; }
        public string   IntentSource                { get; set; }
        public DateTime? IntentConfirmedAt          { get; set; }
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

        public string   Source                      { get; set; }
        public bool?    IsAutoClosed                { get; set; }
        public string   AutoClosedBy                { get; set; }
        public DateTime? AutoClosedAt               { get; set; }
    }
}
