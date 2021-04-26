using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum QuestionLevel
    {
        [Display(Name = "Easy")]
        [Description("Easy")]
        Easy = 1,

        [Display(Name = "Medium")]
        [Description("Medium")]
        Medium = 2,

        [Display(Name = "Difficult")]
        [Description("Difficult")]
        Difficult = 3
    }
}
