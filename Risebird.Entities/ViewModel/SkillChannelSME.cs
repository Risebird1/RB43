using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public  class SkillChannelSME : AssessmentGroupInterviewers
    {
        public bool IsSelectedForIRF { get; set; }//used when adding an existing SME to an IRF

    }
}
