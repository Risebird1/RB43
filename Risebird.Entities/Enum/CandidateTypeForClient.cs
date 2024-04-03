using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Enum
{
    public enum CandidateTypeForClient
    {
        [Description("Scheduled")]
        Scheduled = 1,

        [Display(Name = "Not Scheduled")]
        [Description("Not Scheduled")]
        NotScheduled = 2,

        [Display(Name = "Cold Candidates")]
        [Description("Cold Candidates")]
        ColdCandidates = 3,

        [Display(Name = "Not Processed Resumes")]
        [Description("Not Processed Resumes")]
        NotProcessedResumes = 4
    }
}
