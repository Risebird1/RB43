using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>Audit metadata for automated QC verdict removals (item 59).</summary>
    public static class DmIvAutoQcAuditConstants
    {
        public const string Tag = "automated_qc_verdict_removal";
        public const string FlagTag = "automated_qc_verdict_flag";
        public const string SourceForwardTrigger = "forward_trigger";
        public const string SourceRetroactiveBackfill60Day = "retroactive_backfill_60day";
        public const string SourcePerIrfBackfill = "per_irf_backfill";

        /// <summary>Triggers auto-removal (item 59).</summary>
        public const string VerdictSuspicious = "suspicious";
        /// <summary>Triggers auto-removal (item 59).</summary>
        public const string VerdictRedo = "redo";

        public static string ResolveVerdict(string interviewerSuspicious, bool? redoInterview)
        {
            if (string.Equals(interviewerSuspicious, "Yes", StringComparison.OrdinalIgnoreCase))
                return VerdictSuspicious;
            if (redoInterview == true)
                return VerdictRedo;
            return null;
        }

        /// <summary>
        /// QCMergedAIDetails merge verdicts that do NOT trigger removal:
        /// interviewer_suspicious = "No" (or empty), RedoInterview = false, pass/conditional/manual-review style outcomes.
        /// Only normalized "suspicious" and "redo" from <see cref="DmIvAutoQcRemovalService.ResolveVerdict"/> trigger removal.
        /// </summary>
        public static readonly string[] NonTriggeringMergeOutcomes = { "No", "false", "pass", "conditional", "manual review" };

        public static string QcMergedAiDetailsUrl(int interviewId) =>
            $"/Interview/QCMergedAIDetails?interviewId={interviewId}";
    }
}
