using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum FitmentPriorityStatus
    {
        [Display(Name = "High")]
        [Description("High")]
        High = 1,

        [Display(Name = "Medium")]
        [Description("Medium")]
        Medium = 2,

        [Display(Name = "Low")]
        [Description("Low")]
        Low = 3
    }
}
