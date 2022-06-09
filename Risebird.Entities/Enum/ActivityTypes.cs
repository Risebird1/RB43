using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum ActivityTypes
    {
        [Description("Interview Management")]
        HRComment = 1,

        [Description("Add Skills Channel")]
        AddSkillsChannel = 2,

        [Description("Update Meeting")]
        UpdateMeeting = 3,

        [Description("Update Skills Channel")]
        UpdateSkillsChannel = 4,

        [Description("Create Candidate Feedback")]
        CreateCandidateFeedback = 5,

        [Description("Update Candidate Feedback")]
        UpdateCandidateFeedback = 6,

        [Description("QC Passed")]
        QCPassed = 7,

        [Description("QC Failed")]
        QCFailed = 8,

        [Description("Create ClonableSkillChannel")]
        CreateClonableSkillChannel = 9,

        [Description("Update ClonableSkillChannel")]
        UpdateClonableSkillChannel = 10,

        [Description("Meeting Cancelled")]
        MeetingCancelled = 11


    }
}
