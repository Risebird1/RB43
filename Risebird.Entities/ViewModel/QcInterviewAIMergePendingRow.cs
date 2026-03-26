namespace Risebird.Entities.ViewModel
{
    /// <summary>Row from usp_GetPendingQcInterviewAIMerge for merging Report AI and Transcript AI via OpenAI assistant.</summary>
    public class QcInterviewAIMergePendingRow
    {
        public int id { get; set; }
        public int InterviewId { get; set; }
        public string ReportAIDetails { get; set; }
        public string TranscritAiDetails { get; set; }
    }
}
