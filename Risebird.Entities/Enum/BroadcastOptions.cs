using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum BroadcastOptions
    {
        [Description("Interviewer")]
        Interviewer = 1,
        [Description("Candidate")]
        Candidate,
        [Description("Client")]
        Client
    }
}
