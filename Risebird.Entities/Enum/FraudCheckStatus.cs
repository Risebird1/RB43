using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum FraudCheckStatus
    {
        [Description("Passed")]
        Passed = 1,
        [Description("Failed")]
        Failed = 2
    }
}
