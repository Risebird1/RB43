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
        public const string SourceRiskAuditManual = "risk_audit_manual";
        public const string RiskAuditTag = "irf_audit_risk_removal";

        /// <summary>Max interviews processed per Refresh-risk click.</summary>
        public const int RiskCategoryBackfillBatchSize = 100;
        /// <summary>Max batches per Refresh-risk HTTP call (avoids timeout while filling empty IRFs).</summary>
        public const int RiskCategoryBackfillMaxBatches = 5;

        /// <summary>
        /// Backfill mass-fire circuit breaker: abort before write when any single check
        /// fires on more than this percent of interviews in the run.
        /// </summary>
        public const int MassFireThresholdPct = 40;
        /// <summary>Minimum interviews in a backfill run before the mass-fire guard applies.</summary>
        public const int MassFireMinSample = 20;

        /// <summary>Triggers auto-removal (item 59).</summary>
        public const string VerdictSuspicious = "suspicious";
        /// <summary>Triggers auto-removal (item 59).</summary>
        public const string VerdictRedo = "redo";

        public static string ResolveVerdict(string interviewerSuspicious, bool? redoInterview)
        {
            if (IsAffirmativeSuspicious(interviewerSuspicious))
                return VerdictSuspicious;
            if (redoInterview == true)
                return VerdictRedo;
            return null;
        }

        /// <summary>Matches backfill / IRF audit SQL: Yes, Y, True (case-insensitive).</summary>
        public static bool IsAffirmativeSuspicious(string interviewerSuspicious)
        {
            if (string.IsNullOrWhiteSpace(interviewerSuspicious))
                return false;

            var normalized = interviewerSuspicious.Trim();
            return normalized.Equals("Yes", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Y", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("True", StringComparison.OrdinalIgnoreCase);
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
