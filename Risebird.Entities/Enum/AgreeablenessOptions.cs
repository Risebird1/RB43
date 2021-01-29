using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum AgreeablenessOptions
    {
        [Description("Friendly/Compassionate")]
        [Display(Name = "Friendly/Compassionate")]

        FriendlyCompassionate = 1,

        [Description("Challenging/Detached")]
        [Display(Name = "Challenging/Detached")]

        ChallengingDetached = 2
    }
}
