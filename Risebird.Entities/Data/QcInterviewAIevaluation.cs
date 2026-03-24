using System;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// Entity for table [dbo].[QcInterviewAIevaluation]. Header record for QC interview AI evaluation.
    /// </summary>
    public class QcInterviewAIevaluation
    {
        public int id { get; set; }
        public int? InterviewId { get; set; }
        public bool? ReportAI { get; set; }
        public bool? TranscritAi { get; set; }
        public int? PenalityApplied { get; set; }
        public bool? QCDone { get; set; }
        public DateTime? QCDoneOn { get; set; }
        public string ReportAIDetails { get; set; }
        public string TranscritAiDetails { get; set; }
        /// <summary>Merged Report AI + Transcript AI assistant output (JSON array) from QcInterviewAIMergeJob.</summary>
        public string MergedAIQCDetails { get; set; }
    }
}
