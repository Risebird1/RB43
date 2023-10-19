using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum CountryTypeEnum
    {
        [Display(Name = "Offshore")]
        [Description("Offshore")]
        Offshore = 1,

        [Display(Name = "Onsite")]
        [Description("Onsite")]
        Onsite = 2
    }
}
