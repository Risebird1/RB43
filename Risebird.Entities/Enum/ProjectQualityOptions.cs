using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.Enum
{
    public enum ProjectQualityOptions
    {
        [Description("Relevant")]
        [Display(Name = "Relevant")]

        Relevant = 1,

        [Description("Not Relevant")]
        [Display(Name = "Not Relevant")]

        NotRelevant = 2
    }
}
