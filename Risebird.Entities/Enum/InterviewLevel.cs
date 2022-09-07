using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum InterviewLevel
    {
        Basic = 0,
        Medium = 1,
        Expert = 2,
        Scholar = 3
    }
    public enum InterviewDurationEnum
    {
        [Description("75 Min")]
        Min75 = 75,
        [Description("60 Min")]
        Min60 = 60,
        [Description("45 Min")]
        Min45 = 45,
        [Description("30 Min")]
        Min30 = 30
    }
}
