namespace Risebird.Entities.ViewModel
{
    /// <summary>UI model for contradiction report (<see cref="Risebird.Entities.Data.QcInterviewAIevaluation.ContradictionJson"/>).</summary>
    public class QcContradictionReportVM
    {
        public int InterviewId { get; set; }
        public string ContradictionJson { get; set; }
    }
}
