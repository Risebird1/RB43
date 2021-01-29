using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum EmployerQualityOptions
    {
        [Description("Relevant")]
        [Display(Name = "Relevant")]

        Relevant = 1,

        [Description("Not Relevant")]
        [Display(Name = "Not Relevant")]

        NotRelevant = 2
    }
}
