using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
