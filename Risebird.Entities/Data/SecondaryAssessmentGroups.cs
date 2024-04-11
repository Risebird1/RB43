using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class SecondaryAssessmentGroups
    {
        public int MAGId { get; set; }// using this id, we determine the master assessment group
        public int AssessmentGroupId { get; set; }//note: this is not the id of the mag!!! this is the id of the assessment Group that is the part of the mag
        public string AssessmentGroupNameForMAG { get; set; }
        public string SecondaryAssessmentGroupName { get; set; }
    }
}
