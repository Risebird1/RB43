using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

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
