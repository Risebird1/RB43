using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum CandidateTypeEnum
    {
        [Description("Candidate")]
        Candidate = 1,
        [Description("Interviewer")]
        Interviewer = 2
    }
}
