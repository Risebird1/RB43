using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    class ChannelEnum
    {
    }
    public enum Prioritylevels
    {
        [Description("Low")]
        Low = 1,
        [Description("Medium")]
        Medium = 2,
        [Description("High")]
        High = 3,
        [Description("Very High")]
        VeryHigh = 4,
    }
}
