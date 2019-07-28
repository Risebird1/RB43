using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum InterviewStatus
    {
        [Description("No Show")]
        NoShow = 1,
        [Description("Selected")]
        Selected = 2,
        [Description("Rejected")]
        Rejected = 3,
        [Description("Border Line")]
        BorderLine = 4

    }

    public enum L2InterviewStatus
    {
        [Description("Selected")]
        Selected = 1,
        [Description("Rejected")]
        Rejected = 2
    }

    public enum ClientInterviewStatus
    {

        [Description("Offered")]
        Offered = 1,
        [Description("Not Offered")]
        NotOffered = 2
    }
}
