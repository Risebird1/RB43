using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum ClientFeedbackType
    {
        [Description("Audio")]
        Audio = 1,
        [Description("Broadcast")]
        Broadcast = 2,
        [Description("Rating")]
        Rating = 3
    }
}
