using System.ComponentModel;

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
