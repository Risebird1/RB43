using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
