using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum TaskUpdateStatusOptions
    {
        [Display(Name = "In Progress")]
        [Description("In Progress")]
        InProgress = 1,

        [Display(Name = "Completed")]
        [Description("Completed")]
        Completed = 2
    }
}
