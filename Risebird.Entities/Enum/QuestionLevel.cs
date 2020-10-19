using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

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
