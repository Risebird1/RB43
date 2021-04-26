using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
