using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

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
