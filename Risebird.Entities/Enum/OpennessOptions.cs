using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum OpennessOptions
    {
        [Description("Inventive/Curious")]
        [Display(Name = "Inventive/Curious")]

        InventiveCurious = 1,

        [Description("Consistent/Cautious")]
        [Display(Name = "Consistent/Cautious")]

        ConsistentCautious = 2
    }
}
