using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum ProfileEnum
    {
        [Description("Admin")]
        Admin = 1,
        [Description("HR")]
        HR = 2,
        [Description("Interviewer")]
        Interviewer = 3,
        [Description("Client")]
        Client = 4,
        [Description("Quality Controller")]
        QualityController = 5,
        [Description("Trainer")]
        Trainer = 16,
        [Description("Candidate")]
        Candidate = 23,
        [Description("ResumeEvaluator")]
        ResumeEvaluator = 24,
        [Description("Recruiter")]
        Recruiter = 25,
        [Description("CuratedUser")]
        CuratedUser = 26,
        [Description("ExternalUser")]
        ExternalUser = 27,
        [Description("SupportAdmin")]
        SupportAdmin = 32,
    }
}
