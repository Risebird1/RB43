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
        /// <summary>Full merged assistant JSON (<see cref="QcInterviewAIMergeAssistantResponse"/>).</summary>
        public string MergedAIQCDetails { get; set; }
        /// <summary>JSON from the contradiction AI assistant (reconciling transcript vs report).</summary>
        public string ContradictionJson { get; set; }
        public string ContradictionScore { get; set; }
        public string ContradictionVerdict { get; set; }
        public string ContradictionDirectionVerdict { get; set; }
        public int? AIMergeTotalPenalty { get; set; }
        public string AIMergeInterviewerSuspicious { get; set; }
        public bool? AIMergeRedoInterview { get; set; }
    }
}
