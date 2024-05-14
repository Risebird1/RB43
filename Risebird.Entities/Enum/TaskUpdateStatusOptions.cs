using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
