using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum ClientTypeEnum
    {
        [Display(Name = "Service")]
        [Description("Service")]
        Service = 1,

        [Display(Name = "Product")]
        [Description("Product")]
        Product = 2,

        [Display(Name = "Elite")]
        [Description("Elite")]
        Elite = 3,
    }
}
