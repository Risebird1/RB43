using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Enum
{
    public enum LevelTechEnum
    {
        [Display(Name = "Novice")]
        [Description("Novice")]
        Novice = 2,

        [Display(Name = "Advanced")]//Advanced Beginner
        [Description("Advanced")]//Advanced Beginner
        Advanced = 3,

        [Display(Name = "Competent")]
        [Description("Competent")]
        Competent = 4,

        [Display(Name = "Proficient")]
        [Description("Proficient")]
        Proficient = 5,

        [Display(Name = "Expert")]
        [Description("Expert")]
        Expert = 6

    }
}
