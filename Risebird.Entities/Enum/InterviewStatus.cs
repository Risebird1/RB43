using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum InterviewStatus
    {
        [Description("Not Appeared")]
        NoShow = 1,
        [Description("Selected")]
        Selected = 2,
        [Description("Rejected")]
        Rejected = 3
    }
}
