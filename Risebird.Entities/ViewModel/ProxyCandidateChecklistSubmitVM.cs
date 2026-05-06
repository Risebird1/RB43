using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class ProxyCandidateChecklistSubmitVM
    {
        public string InterviewerEmail { get; set; }
        public string CandidateName { get; set; }
        public string RoleApplied { get; set; }
        public Dictionary<string, bool> ChecklistState { get; set; }
    }
}
