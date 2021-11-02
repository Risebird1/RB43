using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum FitmentStatusEnum
    {
        [Display(Name = "Selected")]
        [Description("Selected")]
        Selected = 1,

        [Display(Name = "Rejected")]
        [Description("Rejected")]
        Rejected = 2,

        [Display(Name = "Border Line")]
        [Description("Border Line")]
        BorderLine = 3,

        [Display(Name = "Proxy")]
        [Description("Proxy")]
        Proxy = 4
    }
}
