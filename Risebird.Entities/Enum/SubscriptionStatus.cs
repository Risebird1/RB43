using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum SubscriptionStatus
    {
        [Description("UnSubscribed")]
        UnSubscribed = 0,
        [Description("Subscribed")]
        Subscribed = 1,
    }
}
