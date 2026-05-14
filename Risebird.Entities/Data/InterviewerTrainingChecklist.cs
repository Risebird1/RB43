using System;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// Persists interviewer self-review checklist state per training module (e.g. AI report analysis).
    /// One logical row per interviewer + module (upsert via stored procedure).
    /// </summary>
    public class InterviewerTrainingChecklist
    {
        public int Id { get; set; }
        public string InterviewerEmail { get; set; }
        public string TrainingModuleCode { get; set; }
        public string ChecklistStateJson { get; set; }
        public int TotalItems { get; set; }
        public int TotalChecked { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
