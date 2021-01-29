using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum NeuroticismOptions
    {
        [Description("Sensitive/Nervous")]
        [Display(Name = "Sensitive/Nervous")]

        SensitiveNervous = 1,

        [Description("Secure/Confident")]
        [Display(Name = "Secure/Confident")]

        SecureConfident = 2
    }
}
