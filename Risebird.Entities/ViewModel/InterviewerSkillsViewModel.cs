using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerSkillsViewModel
    {
        public IEnumerable<InterviewerSkillsChannelModel> InterviewerSkillsChannelModelList { get; set; }
        public string Profile { get; set; }
        public string Message { get; set; }
        public bool IsClientUser { get; set; }
        public List<string> RoleList { get; set; }
    }
}
