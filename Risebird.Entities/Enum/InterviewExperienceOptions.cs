using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum InterviewExperienceOptions
    {
        [Description("Poor")]
        [Display(Name = "Poor")]

        Poor = 1,

        [Description("Good")]
        [Display(Name = "Good")]

        Good = 2,

        [Description("Very Good")]
        [Display(Name = "Very Good")]

        VeryGood = 3
    }
}
