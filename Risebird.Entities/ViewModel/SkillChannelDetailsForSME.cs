using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class SkillChannelDetailsForSME
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public int IRFId { get; set; }
        public string SkillChannelName { get; set; }
        public string JDTitle { get; set; }
        public string PrimarySkill{ get; set; }
        public string Client { get; set; }
        public string ChannelOwner { get; set; }
        public DateTime AssignedOn { get; set; }
    }
}
