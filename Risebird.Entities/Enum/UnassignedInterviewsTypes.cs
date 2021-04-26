using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum UnassignedInterviewsTypes
    {
        [Description("Unassigned")]
        Unassigned = 1,
        [Description("Meeting Status Pending")]
        MeetingStatusPending = 2,
        [Description("Interview Status Pending")]
        InterviewStatusPending = 3,
        [Description("Interviewer Comments Pending")]
        InterviewerCommentsPending = 4
    }
}
