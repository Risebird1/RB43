using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>Row from usp_GetPendingQcInterviewAIMerge for merging Report AI and Transcript AI via OpenAI assistant.</summary>
    public class QcInterviewAIMergePendingRow
    {
        public int id { get; set; }
        public int InterviewId { get; set; }
        public int irfId { get; set; }
        public string ReportAIDetails { get; set; }
        public string TranscritAiDetails { get; set; }
        public string TranscriptSummary { get; set; }
        public string ContradictionJson { get; set; }
        public DateTime interviewDate { get; set; }
        public DateTime? Joinedon { get; set; }
        public int ActualDuration { get; set; }
        public int RequiredDuration { get; set; }
    }
}
