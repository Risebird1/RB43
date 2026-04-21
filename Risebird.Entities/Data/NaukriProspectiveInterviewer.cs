using System;

namespace Risebird.Entities.Data
{
    public class NaukriProspectiveInterviewer
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
    }
}
