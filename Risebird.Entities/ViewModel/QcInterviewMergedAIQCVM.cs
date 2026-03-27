using Risebird.Entities.Data;

namespace Risebird.Entities.ViewModel
{
    /// <summary>UI model for merged AI QC details (<see cref="QcInterviewAIevaluation.MergedAIQCDetails"/>).</summary>
    public class QcInterviewMergedAIQCVM
    {
        public int InterviewId { get; set; }
        public QcInterviewAIMergeAssistantResponse Merged { get; set; }
        public string RawMergedJson { get; set; }
        public int? AIMergeTotalPenalty { get; set; }
        public string AIMergeInterviewerSuspicious { get; set; }
        public bool? AIMergeRedoInterview { get; set; }
    }
}
