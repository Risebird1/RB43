using System;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// Row in InterviewerProxyCandidateChecklist — one row per interviewer,
    /// storing their latest proxy-detection checklist state.
    /// </summary>
    public class InterviewerProxyCandidateChecklist
    {
        public int Id { get; set; }
        /// <summary>Logged-in interviewer's session email — acts as the unique key.</summary>
        public string InterviewerEmail { get; set; }
        /// <summary>Full checklist state serialised as JSON — key is section-item index (e.g. "pre-setup-0"), value is true/false.</summary>
        public string ChecklistStateJson { get; set; }
        public int TotalItems { get; set; }
        public int TotalChecked { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
