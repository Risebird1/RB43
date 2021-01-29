using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum ExtroversionOptions
    {
        [Description("Outgoing/Energetic")]
        [Display(Name = "Outgoing/Energetic")]

        OutgoingEnergetic = 1,

        [Description("Solitary/Reserved")]
        [Display(Name = "Solitary/Reserved")]

        SolitaryReserved = 2
    }
}
