using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
