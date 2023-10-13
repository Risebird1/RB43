using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class AvailabilityCheckDTO
    {
        public int id { get; set; } // this is irfId, i.e., skill channel id
        public string IRFId { get; set; } // skill  Channel Name
        public string AssessmentGroupId { get; set; }
        public int FlatRate { get; set; }
        public string PrimarySkillInvite { get; set; }
        public string JDTitle { get; set; }
    }
}
