using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public static class TrialFlagConstants
    {
        public const string VerdictGoodTrial = "good_trial";
        public const string VerdictBadTrial = "bad_trial";

        public const string ReviewPending = "pending";
        public const string ReviewKept = "kept";
        public const string ReviewRemoved = "removed";

        // Trial judgment thresholds (Item B producer)
        public const decimal MinTranscriptScore = 4m;       // AITranscriptAnalysisScore 0–10 scale (ops uses <4 / <6 as bad)
        public const decimal MinReportScore = 60m;          // AIReportQCAnalysisScore 0–100
        public const decimal MinCandidateRating = 3m;       // 0–5
        public const decimal MaxJoinTimeMinutes = 5m;       // lateness vs scheduled start
        public const decimal MinDurationRatio = 0.80m;      // actual / required when required > 0
        public const int BackfillBatchSize = 25;            // max inserts per backfill HTTP request
        public const int BackfillCandidateFetchSize = 100;  // candidates to return from SP per click
        /// <summary>Active interviewer–IRF pairs need a completed interview within this many days to be backfill candidates.</summary>
        public const int BackfillActivityWindowDays = 14;

        // Trial severity (P0 = worst … P5 = clean)
        public const string SeverityP0 = "P0";
        public const string SeverityP4 = "P4";
        public const string SeverityClean = "P5";
    }

    public class TrialJudgmentContextVm
    {
        public bool IsEligible { get; set; }
        public bool IsFirstCompletedInterview { get; set; }
        public bool AlreadyFlagged { get; set; }
        public int ChannelId { get; set; }
        public int InterviewerId { get; set; }
        public int IrfId { get; set; }
        public DateTime? TriggeringInterviewDate { get; set; }
        public decimal? TranscriptQualityScore { get; set; }
        public decimal? ReportQualityScore { get; set; }
        public decimal? CandidateRating { get; set; }
        public decimal? JoinTimeMinutes { get; set; }
        public decimal? InterviewDurationMinutes { get; set; }
        public int? RequiredDurationMinutes { get; set; }
    }

    public class TrialJudgmentResultVm
    {
        public bool Inserted { get; set; }
        public int? TrialFlagId { get; set; }
        public string OverallVerdict { get; set; }
        public bool TranscriptQualityPass { get; set; }
        public bool ReportQualityPass { get; set; }
        public bool DecisionQualityPass { get; set; }
        public bool CandidateRatingPass { get; set; }
        public bool JoinTimePass { get; set; }
        public bool InterviewDurationPass { get; set; }
        public string Severity { get; set; }
        public string SkipReason { get; set; }
    }

    public class TrialFlagBackfillCandidateVm
    {
        public int InterviewId { get; set; }
        public int IrfId { get; set; }
    }

    public class TrialFlagBackfillResultVm
    {
        public int Inserted { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public IReadOnlyList<string> Messages { get; set; } = System.Array.Empty<string>();
    }

    public class TrialFlagReviewPageVm
    {
        public string ActiveTab { get; set; } = TrialFlagConstants.ReviewPending;
        public IReadOnlyList<TrialFlagReviewRowVm> Rows { get; set; } = Array.Empty<TrialFlagReviewRowVm>();
        public string LoadError { get; set; }
        public int PendingCount { get; set; }
        public int KeptCount { get; set; }
        public int RemovedCount { get; set; }
        public bool TrialFlagsTableExists { get; set; }
        public int TotalRowsInTable { get; set; }
        public string EmptyStateHint { get; set; }
        public bool LiveMetrics { get; set; }
    }

    public class TrialFlagReviewRowVm
    {
        public int TrialFlagId { get; set; }
        public int InterviewerId { get; set; }
        public int IrfId { get; set; }
        public int ChannelId { get; set; }
        public int TriggeringInterviewId { get; set; }
        public DateTime TriggeringInterviewDate { get; set; }
        public bool TranscriptQualityPass { get; set; }
        public bool ReportQualityPass { get; set; }
        public bool DecisionQualityPass { get; set; }
        public bool CandidateRatingPass { get; set; }
        public bool JoinTimePass { get; set; }
        public bool InterviewDurationPass { get; set; }
        public decimal? TranscriptQualityScore { get; set; }
        public decimal? ReportQualityScore { get; set; }
        public decimal? CandidateRating { get; set; }
        public decimal? JoinTimeMinutes { get; set; }
        public decimal? InterviewDurationMinutes { get; set; }
        public string OverallVerdict { get; set; }
        /// <summary>Read-time verdict: bad_trial only when live RiskCategory.Severity is non-null.</summary>
        public string DisplayVerdict { get; set; }
        public string ReviewStatus { get; set; }
        public DateTime FlaggedAtUtc { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public string ReviewedBy { get; set; }
        public string ReviewerComment { get; set; }
        public string InterviewerName { get; set; }
        public string InterviewerEmail { get; set; }
        public string InterviewerCodedId { get; set; }
        public string IrfName { get; set; }
        public string Severity { get; set; }
        public string RiskCategoryLabels { get; set; }
        public string RiskCategoryReasons { get; set; }
        public string SeverityBreakdown { get; set; }
        /// <summary>Candidate feedback traits / comment text when CandidateRating risk is present.</summary>
        public string CandidateFeedbackComment { get; set; }
        public string QcMergeVerdict { get; set; }
        /// <summary>Total penalty (AIMergeTotalPenalty / MergedAIQCDetails.total_penalty) — same as QCMergedAIDetails "Total Penalty".</summary>
        public int? QcMergeTotalPenalty { get; set; }
        /// <summary>QC score (qcmergescore / MergedAIQCDetails.total_qcmerge_score) — same as QCMergedAIDetails "Score" / QC Score.</summary>
        public int? QcMergeQcScore { get; set; }
        /// <summary>Legacy alias — prefer <see cref="QcMergeTotalPenalty"/>.</summary>
        public int? QcMergeScore { get; set; }
        /// <summary>Raw JSON array from MergedAIQCDetails.flags (category + flag + penalty).</summary>
        public string QcMergeFlagsJson { get; set; }
        public int PaymentDeduction { get; set; }
        /// <summary>JSON object of PaymentSLA reason column → raw value (filtered at render via TrialFlagEvidenceBadgeHelper).</summary>
        public string PaymentReasonValuesJson { get; set; }
        /// <summary>Legacy CSV of reason labels; prefer PaymentReasonValuesJson + helper.</summary>
        public string PaymentDeductionReasons { get; set; }
        public string PaymentRecommendation { get; set; }
        public string PaymentRecommendationComments { get; set; }
        public bool IsSuboptimalQuality =>
            !string.IsNullOrWhiteSpace(RiskCategoryLabels)
            || (!string.IsNullOrWhiteSpace(Severity)
                && !string.Equals(Severity, TrialFlagConstants.SeverityClean, StringComparison.OrdinalIgnoreCase));

        /// <summary>Badge text for verdict column — prefers DisplayVerdict.</summary>
        public string EffectiveVerdict =>
            !string.IsNullOrWhiteSpace(DisplayVerdict) ? DisplayVerdict : OverallVerdict;
    }

    public class TrialFlagReviewActionResultVm
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
    }

    /// <summary>Drill-down targets for trial factor review on /Dashboard/TrialFlagReview.</summary>
    public static class TrialFlagDrillDownUrls
    {
        /// <summary>Client / interview report (Trial Interview ID column).</summary>
        public static string ClientReport(int interviewId, int irfId) =>
            $"/Interview/ClientReport?interviewId={interviewId}&irfId={irfId}";

        public static string TranscriptQuality(int interviewId, int irfId) =>
            $"/AIScreener/TransciptAnalysis?interviewId={interviewId}&irfId={irfId}";

        /// <summary>Report AI verdict page (AI Links → Report).</summary>
        public static string ReportAiVerdict(int interviewId) =>
            $"/Interview/ReportAIDetailsReport?interviewId={interviewId}";

        /// <summary>Alias for Report AI verdict — not QC Merge.</summary>
        public static string ReportQuality(int interviewId) =>
            ReportAiVerdict(interviewId);

        /// <summary>QC Merge details page.</summary>
        public static string QcMergedAiDetails(int interviewId) =>
            $"/Interview/QCMergedAIDetails?interviewId={interviewId}";

        /// <summary>Contradiction report — same target as RiskCategory / PaymentSLA / Stats evidence.</summary>
        public static string ContradictionReport(int interviewId) =>
            $"/Interview/ContradictionReport?interviewId={interviewId}";

        public static string DecisionQuality(int interviewId) =>
            ContradictionReport(interviewId);

        public static string CandidateRating(int interviewId) =>
            $"/Interview/PaymentSlaDetails?interviewId={interviewId}&returnBack=0&actionstatus=SLACompleted";

        public static string InterviewTiming(int interviewId, string irfName)
        {
            var irf = string.IsNullOrWhiteSpace(irfName) ? "" : Uri.EscapeDataString(irfName);
            return $"/RiseTube/VideoPage?id={interviewId}&irfid={irf}";
        }

        public static string TrialStats(int interviewerId, int irfId, string irfName = null, string fromIst = null, string toIst = null)
        {
            var url = $"/Interviewer/TrialStats?irfId={irfId}";
            if (interviewerId > 0)
                url += $"&interviewerId={interviewerId}";
            if (!string.IsNullOrWhiteSpace(irfName))
                url += $"&irfName={Uri.EscapeDataString(irfName)}";
            if (!string.IsNullOrWhiteSpace(fromIst))
                url += $"&from={Uri.EscapeDataString(fromIst)}";
            if (!string.IsNullOrWhiteSpace(toIst))
                url += $"&to={Uri.EscapeDataString(toIst)}";
            return url;
        }
    }

    public class InterviewAiRiskSummaryRowVm
    {
        public string SourceName { get; set; }
        public string RawScore { get; set; }
        public bool Triggered { get; set; }
        public string PLevel { get; set; }
    }
}
