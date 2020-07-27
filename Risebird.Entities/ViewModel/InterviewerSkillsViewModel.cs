using System.Collections.Generic;

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
