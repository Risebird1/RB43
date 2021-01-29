using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

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
