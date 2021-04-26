using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum ProfileScoreOptions
    {
        [Description("Poor")]
        Poor = 1,

        [Description("Good")]
        Good = 2,

        [Description("Very Good")]
        [Display(Name = "Very Good")]

        VeryGood = 3,

        [Description("Excellent")]
        Excellent = 4,

        [Description("Outstanding")]
        Outstanding = 5,
    }
}
