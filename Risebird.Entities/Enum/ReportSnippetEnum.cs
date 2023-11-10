using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum ReportSnippetEnum
    {
        [Display(Name = "Select Report")]
        [Description("Seelct Report")]
        Select = 0,
        [Display(Name = "Snippet 1")]
        [Description("Snippet 1")]
        Snippet1 = 1,
        [Display(Name = "Snippet 2")]
        [Description("Snippet 2")]
        Snippet2 = 2
    }
}
