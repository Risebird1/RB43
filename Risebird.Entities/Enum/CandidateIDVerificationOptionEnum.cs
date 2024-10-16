using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Enum
{
    public enum CandidateIDVerificationOptionEnum
    {
        [Description("Verified")]
        [Display(Name = "Verified")]

        Verified = 1,

        [Description("Not Verified")]
        [Display(Name = "Not Verified")]

        NotVerified = 2,
        

        [Description("Candidate Disagreed")]
        [Display(Name = "Candidate Disagreed")]
        CandidateDisagreed = 3,

        [Description("Forgot to Verify")]
        [Display(Name = "Forgot to Verify")]
        ForgotToVerify = 4
    }
}
