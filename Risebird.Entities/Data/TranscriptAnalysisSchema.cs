using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// Single red-flag entry from transcript AI analysis. Verdict, parameter name, and observation are persisted to QC tables (observation → Remark).
    /// </summary>
    public class TranscriptRedFlagEntry
    {
        public string redFlagVerdict { get; set; }
        public string observation { get; set; }
    }

    public class TranscriptAnalysisSchema
    {
        public int score { get; set; }
        public string completeAnalysis { get; set; }
        public string verdict { get; set; }
        /// <summary>
        /// Red flags from analysis; key = parameter/flag name, value contains verdict. Only these are saved to QcInterviewAIevaluationDetails.
        /// </summary>
        public Dictionary<string, TranscriptRedFlagEntry> redFlags { get; set; }
    }

    /// <summary>
    /// Model for saving transcript AI evaluation into QcInterviewAIevaluation and QcInterviewAIevaluationDetails. Contains only red-flag data (parameter name + verdict).
    /// </summary>
    public class QcInterviewAIevaluationSaveModel
    {
        public int InterviewId { get; set; }
        public int Score { get; set; }
        public string Verdict { get; set; }
        public bool TranscritAi { get; set; } = true;
        public bool ReportAI { get; set; } = false;
        public int PenalityApplied { get; set; }
        public bool QCDone { get; set; } = true;
        public DateTime? QCDoneOn { get; set; }
        /// <summary>
        /// Red-flag details to persist: parameter name and verdict only.
        /// </summary>
        public List<QcInterviewAIevaluationDetailItem> RedFlagDetails { get; set; } = new List<QcInterviewAIevaluationDetailItem>();
    }

    /// <summary>
    /// Single row for QcInterviewAIevaluationDetails: flag/parameter name, verdict, and observation (saved as Remark).
    /// </summary>
    public class QcInterviewAIevaluationDetailItem
    {
        public string FlagName { get; set; }
        public string Verdict { get; set; }
        public string Remark { get; set; }
    }
}
