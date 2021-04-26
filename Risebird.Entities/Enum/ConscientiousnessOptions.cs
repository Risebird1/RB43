using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum ConscientiousnessOptions
    {
        [Description("Efficient/Organized")]
        [Display(Name = "Efficient/Organized")]

        EfficientOrganized = 1,

        [Description("Easy-going/Careless")]
        [Display(Name = "Easy-going/Careless")]

        EasygoingCareless = 2
    }
}
