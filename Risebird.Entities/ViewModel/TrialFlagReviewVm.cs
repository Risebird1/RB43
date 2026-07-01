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
        public const int BackfillCandidateScanSize = 800;   // recent QCDone rows SP scans before filtering
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
        public decimal? CandidateRating { get; set; }
        public decimal? JoinTimeMinutes { get; set; }
        public decimal? InterviewDurationMinutes { get; set; }
        public string OverallVerdict { get; set; }
        public string ReviewStatus { get; set; }
        public DateTime FlaggedAtUtc { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public string ReviewedBy { get; set; }
        public string ReviewerComment { get; set; }
        public string InterviewerName { get; set; }
        public string InterviewerEmail { get; set; }
        public string InterviewerCodedId { get; set; }
        public string IrfName { get; set; }
    }

    public class TrialFlagReviewActionResultVm
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
    }

    /// <summary>Drill-down targets for trial factor review on /Dashboard/TrialFlagReview.</summary>
    public static class TrialFlagDrillDownUrls
    {
        public static string TranscriptQuality(int interviewId, int irfId) =>
            $"/AIScreener/TransciptAnalysis?interviewId={interviewId}&irfId={irfId}";

        public static string ReportQuality(int interviewId) =>
            $"/Interview/QCMergedAIDetails?interviewId={interviewId}";

        public static string DecisionQuality(int interviewId) =>
            $"/Interview/QCMergedAIDetails?interviewId={interviewId}";

        public static string CandidateRating(int interviewId) =>
            $"/Interview/PaymentSlaDetails?interviewId={interviewId}&returnBack=0&actionstatus=SLACompleted";

        public static string InterviewTiming(int interviewId, string irfName)
        {
            var irf = string.IsNullOrWhiteSpace(irfName) ? "" : Uri.EscapeDataString(irfName);
            return $"/RiseTube/VideoPage?id={interviewId}&irfid={irf}";
        }

        public static string TrialStats(int interviewerId, int irfId, string irfName = null)
        {
            var url = $"/Interviewer/TrialStats?irfId={irfId}";
            if (interviewerId > 0)
                url += $"&interviewerId={interviewerId}";
            if (!string.IsNullOrWhiteSpace(irfName))
                url += $"&irfName={Uri.EscapeDataString(irfName)}";
            return url;
        }
    }
}
