using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>Payload for saving interviewer training checklist state (JSON form POST).</summary>
    public class TrainingChecklistSubmitVM
    {
        public string TrainingModuleCode { get; set; }
        public Dictionary<string, bool> ChecklistState { get; set; }
        /// <summary>Total checklist rows (e.g. .cl-item count). Sent from the client so sparse state dictionaries still persist correct totals.</summary>
        public int TotalItems { get; set; }
        public int TotalChecked { get; set; }
    }
}
