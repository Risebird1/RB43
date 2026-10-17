using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    public class AutoQcRemovalMonitorVm
    {
        public int TodayRemovals { get; set; }
        public int Rolling7DayRemovals { get; set; }
        public int Rolling30DayRemovals { get; set; }
        public int Rolling30DayReactivations { get; set; }
        public decimal ReactivationRatePercent { get; set; }
        public int ForwardTriggerCount { get; set; }
        public int BackfillCount { get; set; }
        public int SuspiciousCount { get; set; }
        public int RedoCount { get; set; }
        public AutoQcBackfillRunSummaryVm LatestBackfill { get; set; }
        public List<AutoQcRemovalMonitorRowVm> Removals { get; set; } = new List<AutoQcRemovalMonitorRowVm>();
        public List<AutoQcRemovalMonitorIrfBreakdownVm> IrfBreakdown { get; set; } = new List<AutoQcRemovalMonitorIrfBreakdownVm>();
        public DateTime? FilterFrom { get; set; }
        public DateTime? FilterTo { get; set; }
        public int? FilterIrfId { get; set; }
        public string FilterSource { get; set; }
        public string FilterVerdict { get; set; }
    }

    public class AutoQcRemovalMonitorRowVm
    {
        public int AuditLogId { get; set; }
        public DateTime RemovedAt { get; set; }
        public string InterviewerCodedId { get; set; }
        public int IrfId { get; set; }
        public string IrfName { get; set; }
        public int? TriggeringInterviewId { get; set; }
        public string Verdict { get; set; }
        public string Source { get; set; }
        public Guid? BackfillBatchId { get; set; }
        public string ExtraMetadata { get; set; }
        public string QcDetailsUrl { get; set; }
        public bool WasReactivated { get; set; }
    }

    public class AutoQcRemovalMonitorIrfBreakdownVm
    {
        public int IrfId { get; set; }
        public string IrfName { get; set; }
        public int Removals { get; set; }
        public int Reactivations { get; set; }
        public int SuspiciousRemovals { get; set; }
        public int RedoRemovals { get; set; }
    }

    public class AutoQcBackfillRunSummaryVm
    {
        public Guid BatchId { get; set; }
        public DateTime ExecutedAtUtc { get; set; }
        public int RecordsRemoved { get; set; }
        public int TriggeringInterviewCount { get; set; }
        public string ExecutedBy { get; set; }
    }

    public class AutoQcBackfillResultVm
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public Guid? BatchId { get; set; }
        public int RecordsRemoved { get; set; }
        public int RecordsSkipped { get; set; }
        public int TriggeringInterviewCount { get; set; }
        public int PreDeploymentCandidateCount { get; set; }
    }

    public class AutoQcBackfillCandidateRow
    {
        public int InterviewId { get; set; }
        public int TransInterviewerId { get; set; }
        public int IrfId { get; set; }
        public string Verdict { get; set; }
        public DateTime? VerdictFinalizedAt { get; set; }
        public bool AlreadyFlagged { get; set; }
        public string InterviewerEmail { get; set; }
        public int UserId { get; set; }
        public string InterviewerCodedId { get; set; }
        public string CurrentAutoQcVerdict { get; set; }
    }

    public class AutoQcPerIrfBackfillPreviewVm
    {
        public int IrfId { get; set; }
        public int VerdictCount { get; set; }
        public int WouldFlagCount { get; set; }
        public int AlreadyFlaggedCount { get; set; }
        public int UnmappedVerdictCount { get; set; }
        public int QcMergedInRangeCount { get; set; }
        public DateTime? OldestVerdictDate { get; set; }
        public DateTime? NewestVerdictDate { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class AutoQcPerIrfBackfillResultVm
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public Guid? RunId { get; set; }
        public int VerdictsScanned { get; set; }
        public int NewFlagsSet { get; set; }
        public int AlreadyFlaggedSkipped { get; set; }
    }

    public class IrfAuditDirectoryRowVm
    {
        public int IrfId { get; set; }
        public string IrfName { get; set; }
        public int TotalCompletedLifetime { get; set; }
        public int CompletedLast90Days { get; set; }
        public int SuspiciousRedoLast90Days { get; set; }
        public int PendingFlagsCount { get; set; }
        public DateTime? LastBackfillAtUtc { get; set; }
        public string ChannelOwnerEmail { get; set; }
        public string ChannelOwnerDisplay { get; set; }
        /// <summary>Computed client-facing label, e.g. "2026-08-06 17:53 IST · 07 Jul–06 Aug".</summary>
        public string LastBackfillLabel { get; set; }
        public DateTime? LastRiskBackfillFromIst { get; set; }
        public DateTime? LastRiskBackfillToIst { get; set; }
    }

    public class IrfAuditInterviewerRiskRowVm
    {
        public int TransInterviewerId { get; set; }
        public int InterviewerId { get; set; }
        /// <summary>UserList.id — use for TrialStats / InterviewerStatsDetails drill-through.</summary>
        public int UserId { get; set; }
        public string InterviewerCodedId { get; set; }
        public string InterviewerEmail { get; set; }
        public int IrfId { get; set; }
        public string IrfName { get; set; }
        /// <summary>Completed interviews in the selected date window.</summary>
        public int TotalCompleted { get; set; }
        /// <summary>Lifetime completed on this IRF (display only — not used in ratios).</summary>
        public int LifetimeCompleted { get; set; }
        public int RiskInterviews { get; set; }
        public int P0Count { get; set; }
        public int P1Count { get; set; }
        public int P2Count { get; set; }
        public int P3Count { get; set; }
        public int P4Count { get; set; }
        public int P5Count { get; set; }
        public decimal? RiskRatio { get; set; }
        public decimal? HighSevRatio { get; set; }
        public int? WorstSeverityInterviewId { get; set; }
        public string Recommendation { get; set; }
        public string QcMergeVerdict { get; set; }
        public int? QcMergeScore { get; set; }
        public int PaymentDeductionTotal { get; set; }
        public string PaymentDeductionReasons { get; set; }

        /// <summary>Removed | Kept | NeedsReReview | PendingReview | None (display as '—').</summary>
        public string ReviewState { get; set; }
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        /// <summary>True when this row is inactive-with-RiskAuditRemoval (rendered in the "Removed in this window" section).</summary>
        public bool IsRemoved { get; set; }
    }

    /// <summary>IRF-window quality strip — completed vs. bad (RiskCategory P0–P5) for the selected IST window.</summary>
    public class IrfAuditWindowQualityVm
    {
        public int TotalCompleted { get; set; }
        public int BadInterviews { get; set; }
        public decimal? BadRatio { get; set; }
        public int P0Count { get; set; }
        public int P1Count { get; set; }
        public int P2Count { get; set; }
        public int P3Count { get; set; }
        public int P4Count { get; set; }
        public int P5Count { get; set; }
    }

    /// <summary>
    /// Per-interview RiskCategory detail for IRF audit drill-down (Severity + Description + SeverityBreakdown).
    /// </summary>
    public class IrfAuditInterviewerRiskDetailVm
    {
        public int TransInterviewerId { get; set; }
        public int InterviewId { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string InterviewerRaw { get; set; }
        public string Severity { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SeverityBreakdown { get; set; }
    }

    public class IrfAuditRiskBackfillResultVm
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public int Processed { get; set; }
        public int Remaining { get; set; }
        /// <summary>True when the mass-fire circuit breaker aborted before any write.</summary>
        public bool Aborted { get; set; }
        public string AbortedCheckName { get; set; }
        public double? AbortedFireRatePct { get; set; }

        /// <summary>Candidates found in window before the per-call cap was applied.</summary>
        public int Scanned { get; set; }
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Deleted { get; set; }
        /// <summary>Always 0 today — TrialFlags backfill is not IRF-scoped, so it is intentionally skipped here (see BackfillRiskCategoryForIrfAsync).</summary>
        public int TrialFlagsInserted { get; set; }
        public int SyncUpdates { get; set; }
        public int Skipped { get; set; }
        public string SkipReasons { get; set; }
        public DateTime? FromIst { get; set; }
        public DateTime? ToIst { get; set; }
        /// <summary>Client-facing label, e.g. "2026-08-06 17:53 IST · 07 Jul–06 Aug".</summary>
        public string LastBackfillLabel { get; set; }
    }

    public class IrfAuditRiskRemoveResultVm
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
    }

    public class IrfAuditRiskKeepResultVm
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
    }

    /// <summary>One row on the High-Risk Interviewers overview (canonical UserList.id).</summary>
    public class InterviewerRiskOverviewRowVm
    {
        public int InterviewerId { get; set; }
        public string CodedId { get; set; }
        public string InterviewerName { get; set; }
        public string InterviewerEmail { get; set; }
        public string MobileNumber { get; set; }
        public int ChannelsActive { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalBad { get; set; }
        public decimal? OverallRiskRatio { get; set; }
        public string WorstSeverity { get; set; }
        public string PriorityTier { get; set; }
        public int HighRiskChannels { get; set; }
        public int PendingReviewChannels { get; set; }
        public bool CoverageIncomplete { get; set; }
        public int P0Count { get; set; }
        public int P1Count { get; set; }
        public int P2Count { get; set; }
        public int P3Count { get; set; }
        public int P4Count { get; set; }
        public int P5Count { get; set; }
        public int TotalRows { get; set; }

        // ---- Profile (usp_GetInterviewerRiskOverview, page rows only) ----
        public string ResumeLink { get; set; }
        /// <summary>CompanyName, else EmploymentComment (same label as the Insights popup / DM page).</summary>
        public string CompanyName { get; set; }
        public bool IsEmployed { get; set; }
        public bool HasVideoKyc { get; set; }
        public string VideoLink { get; set; }
        public bool IsVideoApproved { get; set; }
        /// <summary>"yes" | "no" | null.</summary>
        public string AcknowledgementStatus { get; set; }
        /// <summary>InterviewerProfile.Isavailable: 1 yes, 2 not available, 3 need clarity, 4 drop my profile.</summary>
        public int? IsAvailable { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsHoldPayment { get; set; }
        public bool HasPortalAccess { get; set; }

        // ---- Activity ----
        public DateTime? LastInterviewAt { get; set; }
        public int FutureInterviews { get; set; }
        public bool ActiveSuspicious { get; set; }

        // ---- Lifetime (same definitions as usp_GetInterviewerInsights) ----
        public int LifetimeInterviews { get; set; }
        public int LifetimeCompleted { get; set; }
        public int LifetimeSelected { get; set; }
        public int LifetimeRejected { get; set; }
        public int InterviewerNoShows { get; set; }
        public int CandidateNoShows { get; set; }
        public int RedoRequests { get; set; }
        public int AiTranscriptFlagged { get; set; }
        public int LateJoins { get; set; }
        public int Penalties { get; set; }
        public int ReliabilityScore { get; set; }

        // ---- AI penalties applied automatically, window ----
        public int AiPenalised { get; set; }
        public int AiAutoApproved { get; set; }
        public int AiHumanReviewed { get; set; }
        public decimal? AiAvgPct { get; set; }
        // The money-protection wallet amount (AiAmount) was removed from this page (QC Tracking Fix 4A).

        // ---- QC Tracking: High-Risk decision on the interviewer (latest RedMarkerStatusDetails Type HighRisk) ----
        /// <summary>1 Under review, 2 Reviewed, 3 Removed; null = no decision yet.</summary>
        public int? HighRiskStatus { get; set; }
        public string HighRiskStatusBy { get; set; }
        public DateTime? HighRiskStatusOn { get; set; }
        public string HighRiskStatusComment { get; set; }
        /// <summary>UserList.IsActive; null when an older procedure did not return it (treated as active).</summary>
        public bool? InterviewerIsActive { get; set; }

        // ---- QC Tracking Delta 4: overall level ----
        /// <summary>Bad interviews x severity weight (P0 = 5, P1 = 3, P2 = 2, P3-P5 = 1); default sort.</summary>
        public int Impact { get; set; }
        /// <summary>In-scope channels the interviewer is on ("Removed from <see cref="ChannelsRemoved"/> of N channels").</summary>
        public int ChannelsTotal { get; set; }
        public int ChannelsRemoved { get; set; }
        /// <summary>Completed / bad interviews in the window of the same length just before this one (Trend).</summary>
        public int PrevCompleted { get; set; }
        public int PrevBad { get; set; }

        /// <summary>Bad % in this window (0-100); null with no completed interview.</summary>
        public decimal? BadPct => TotalCompleted > 0 ? Math.Round(TotalBad * 100m / TotalCompleted, 1) : (decimal?)null;
        /// <summary>Bad % in the previous window of the same length; null with no completed interview there.</summary>
        public decimal? PrevBadPct => PrevCompleted > 0 ? Math.Round(PrevBad * 100m / PrevCompleted, 1) : (decimal?)null;
        /// <summary>"up" (worse), "down" (better), "flat", or null when either window has no interviews.</summary>
        public string Trend => BadPct == null || PrevBadPct == null ? null
            : BadPct.Value > PrevBadPct.Value ? "up" : BadPct.Value < PrevBadPct.Value ? "down" : "flat";
    }

    public class InterviewerRiskOverviewSummaryVm
    {
        public int InterviewersAboveThreshold { get; set; }
        public int ChannelsAffected { get; set; }
        public int PendingReviewInterviewers { get; set; }

        /// <summary>Every completed interview in scope and window (all interviewers) with an AISystem deduction above 0.</summary>
        public int AiPenalisedReports { get; set; }
        public int AiAutoApprovedReports { get; set; }
        public int AiHumanReviewedReports { get; set; }
        public decimal? AiAvgPct { get; set; }

        // ---- Deduction review requests in the window (My Quality phase 2) ----
        public int ReviewRequestsReceived { get; set; }
        public int ReviewRequestsDecided { get; set; }
        /// <summary>Reduced + Refunded (the review agreed with the interviewer).</summary>
        public int ReviewRequestsUpheld { get; set; }
        public decimal? ReviewUpheldPct { get; set; }
        public decimal? ReviewRefundedAmount { get; set; }
        /// <summary>Upheld rate per AI check for the same requests (result set 3).</summary>
        public List<DeductionReviewCheckStatVM> ReviewChecks { get; set; } = new List<DeductionReviewCheckStatVM>();
    }

    public class InterviewerRiskOverviewPageVm
    {
        public IReadOnlyList<InterviewerRiskOverviewRowVm> Rows { get; set; }
            = Array.Empty<InterviewerRiskOverviewRowVm>();
        public InterviewerRiskOverviewSummaryVm Summary { get; set; } = new InterviewerRiskOverviewSummaryVm();
        public int TotalRows { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
    }

    /// <summary>Per-channel row under an expanded High-Risk interviewer.</summary>
    public class InterviewerRiskOverviewChannelRowVm
    {
        public int TransInterviewerId { get; set; }
        public int IrfId { get; set; }
        public string IrfName { get; set; }
        public string ChannelOwnerEmail { get; set; }
        public string ChannelOwnerDisplay { get; set; }
        public int Completed { get; set; }
        public int Bad { get; set; }
        public decimal? RiskRatio { get; set; }
        public int P0Count { get; set; }
        public int P1Count { get; set; }
        public int P2Count { get; set; }
        public int P3Count { get; set; }
        public int P4Count { get; set; }
        public int P5Count { get; set; }
        public string Recommendation { get; set; }
        public string ReviewState { get; set; }
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int? WorstSeverityInterviewId { get; set; }
        public DateTime? LastBackfillAtUtc { get; set; }
        public DateTime? LastBackfillFromIst { get; set; }
        public DateTime? LastBackfillToIst { get; set; }
        public bool CoverageIncomplete { get; set; }

        // ---- Window outcomes (OutcomeAgg) ----
        public int Selected { get; set; }
        public int Rejected { get; set; }
        public int ReportsPenalised { get; set; }
        public decimal? AvgPenaltyPct { get; set; }
        /// <summary>TransInterviewer.AIVerdict for this interviewer on this channel.</summary>
        public string AiVerdict { get; set; }

        // ---- AI penalties applied automatically (AiAgg) ----
        public int AiPenalised { get; set; }
        public int AiAutoApproved { get; set; }
        public int AiHumanReviewed { get; set; }
        public decimal? AiAvgPct { get; set; }

        // ---- QC Tracking Delta 4: IRF level (appended to usp_GetInterviewerRiskOverviewChannels) ----
        /// <summary>Process lapses on this channel in the window (PLI): the sum of the five kinds below.</summary>
        public int ProcessLapses { get; set; }
        public int ShortSelectComments { get; set; }
        public int ShortRejectComments { get; set; }
        public int FewQuestions { get; set; }
        public int ShortCalls { get; set; }
        public int LateJoins { get; set; }
        /// <summary>Outcome skew (PQI): candidate ratings below PqiNegativeRating (default 4).</summary>
        public int NegativeRatings { get; set; }
        /// <summary>Select % at or above PqiSelectPct (default 40) with at least PqiMinInterviews completed.</summary>
        public bool HighSelection { get; set; }
        /// <summary>Reject % at or above PqiRejectPct (default 80) with at least PqiMinInterviews completed.</summary>
        public bool HighRejection { get; set; }
        /// <summary>This channel's bad reports and the interviewer's bad reports across the listed channels ("4 of 5").</summary>
        public int ShareOfBad { get; set; }
        public int InterviewerBadTotal { get; set; }
        /// <summary>When the row entered PendingReview / Watch / Remove (first bad report since the last decision).</summary>
        public DateTime? PendingSince { get; set; }
        /// <summary>Whole days since <see cref="PendingSince"/>; red after 7 days.</summary>
        public int? PendingDays { get; set; }
    }

    /// <summary>One graded report (P0-P5) on the High-Risk Reports tab (usp_GetHighRiskReports; QC Tracking Delta 4).</summary>
    public class HighRiskReportRowVm
    {
        public int InterviewId { get; set; }
        public DateTime? InterviewDateIst { get; set; }
        public int? IrfIntId { get; set; }
        public string IRFId { get; set; }
        public string ClientName { get; set; }
        public bool? IsAIInterview { get; set; }
        public string SessionId { get; set; }
        public string CandidateName { get; set; }
        public string CandidateEmail { get; set; }
        public int? InterviewerUserId { get; set; }
        public string InterviewerEmail { get; set; }
        public string InterviewerName { get; set; }
        public string CodedId { get; set; }
        /// <summary>InterviewComments.InterviewStatus (2 / 4 selected, 3 / 5 rejected).</summary>
        public int? InterviewStatus { get; set; }
        /// <summary>Readable outcome for the redo form (filled by the controller).</summary>
        public string InterviewStatusText { get; set; }
        public string AITranscriptVerdict { get; set; }
        public string Severity { get; set; }
        public string RiskName { get; set; }
        /// <summary>AISystem Payment SLA deduction %; null when the AI review wrote none.</summary>
        public int? AiDeductionPct { get; set; }
        public string AiRecommendation { get; set; }
        public int? CandidateRating { get; set; }
        public bool ShortComment { get; set; }
        public bool FewQuestions { get; set; }
        public bool ShortCall { get; set; }
        public bool LateJoin { get; set; }
        public int? QuestionCount { get; set; }
        public int? CommentChars { get; set; }
        public int? VideoMinutes { get; set; }
        public int? PlannedMinutes { get; set; }
        public int? LateJoinMinutes { get; set; }
        /// <summary>False when the signals procedure was missing (flags blank, not "none").</summary>
        public bool SignalsLoaded { get; set; }
        /// <summary>Not reviewed / Reviewed / Penalised x% / Redo / Proxy.</summary>
        public string QcStatus { get; set; }
        public bool IsReviewed { get; set; }
        public bool HumanReviewed { get; set; }
        public int? QcDeductionPct { get; set; }
        public string QcReviewedBy { get; set; }
        public DateTime? QcReviewedAtUtc { get; set; }
        public int? IsApproved { get; set; }
        public bool HasRedo { get; set; }
        public bool HasProxy { get; set; }
        /// <summary>AI penalised but the QC save is 0%, or the reverse: a second look is needed.</summary>
        public bool AiQcDisagree { get; set; }
        public int TotalRows { get; set; }
    }

    public class HighRiskReportsPageVm
    {
        public IReadOnlyList<HighRiskReportRowVm> Rows { get; set; } = Array.Empty<HighRiskReportRowVm>();
        public int TotalRows { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        /// <summary>False when usp_GetHighRiskReports is not deployed (the tab shows a hint, the page still loads).</summary>
        public bool Available { get; set; } = true;
    }

    /// <summary>Report-tab filter keys accepted by usp_GetHighRiskReports @Filter.</summary>
    public static class HighRiskReportFilters
    {
        public static readonly IReadOnlyList<string> Keys = new[]
        {
            "NotReviewed", "P0P1", "P0", "P1", "P2", "P3", "P4", "P5", "AiQcDisagree", "HasProcessFlags",
            "ShortComment", "FewQuestions", "ShortCall", "LateJoin", "Selected", "Rejected"
        };

        /// <summary>Keeps known keys only (case-insensitive, de-duplicated) as a comma list; null when none.</summary>
        public static string Normalize(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return null;
            var keys = filter.Split(',')
                .Select(k => Keys.FirstOrDefault(x => string.Equals(x, k.Trim(), StringComparison.OrdinalIgnoreCase)))
                .Where(k => k != null)
                .Distinct()
                .ToList();
            return keys.Count == 0 ? null : string.Join(",", keys);
        }
    }

    /// <summary>PLI / PQI signals rolled up per interviewer (usp_GetInterviewerProcessSignals result set 3).</summary>
    public class InterviewerProcessSignalVm
    {
        public int InterviewerUserId { get; set; }
        public int Channels { get; set; }
        public int Completed { get; set; }
        public int Selected { get; set; }
        public int Rejected { get; set; }
        public decimal? SelectPct { get; set; }
        public decimal? RejectPct { get; set; }
        public int ShortSelectComments { get; set; }
        public int ShortRejectComments { get; set; }
        public int ShortComments { get; set; }
        public int FewQuestions { get; set; }
        public int ShortCalls { get; set; }
        public int LateJoins { get; set; }
        public int NegativeRatings { get; set; }
        public int ProcessLapses { get; set; }
        public bool HighSelection { get; set; }
        public bool HighRejection { get; set; }
        /// <summary>Graded bad reports (RiskCategory P0-P5) among these interviews (share of bad).</summary>
        public int Bad { get; set; }
    }

    /// <summary>PLI / PQI signals per interviewer x channel (usp_GetInterviewerProcessSignals result set 2).</summary>
    public class InterviewerProcessSignalChannelVm
    {
        public int InterviewerUserId { get; set; }
        public int? IrfIntId { get; set; }
        public string IRFId { get; set; }
        public int Completed { get; set; }
        public int Selected { get; set; }
        public int Rejected { get; set; }
        public decimal? SelectPct { get; set; }
        public decimal? RejectPct { get; set; }
        public int ShortSelectComments { get; set; }
        public int ShortRejectComments { get; set; }
        public int FewQuestions { get; set; }
        public int ShortCalls { get; set; }
        public int LateJoins { get; set; }
        public int NegativeRatings { get; set; }
        public int ProcessLapses { get; set; }
        public bool HighSelection { get; set; }
        public bool HighRejection { get; set; }
        /// <summary>Graded bad reports (RiskCategory P0-P5) among these interviews (share of bad).</summary>
        public int Bad { get; set; }
    }

    /// <summary>Result of usp_GetInterviewerProcessSignals for a set of interviewers.</summary>
    public class InterviewerProcessSignalsResultVm
    {
        public IReadOnlyList<InterviewerProcessSignalVm> Interviewers { get; set; } = Array.Empty<InterviewerProcessSignalVm>();
        public IReadOnlyList<InterviewerProcessSignalChannelVm> Channels { get; set; } = Array.Empty<InterviewerProcessSignalChannelVm>();
        /// <summary>False when the procedure is not deployed.</summary>
        public bool Available { get; set; } = true;
    }

    /// <summary>QC coverage of one interviewer (usp_GetInterviewerQcCoverage): has QC dealt with this person?</summary>
    public class InterviewerQcCoverageRowVm
    {
        public int UserId { get; set; }
        /// <summary>Latest human QC touch in the look-back (call, coaching, status move, action log, suspicious, audit, trial).</summary>
        public DateTime? LastTouchAtUtc { get; set; }
        public string LastTouchLabel { get; set; }
        public string LastTouchBy { get; set; }
        /// <summary>Latest connected call (talk time at least QCActivitySetting.MinCallSeconds).</summary>
        public DateTime? LastConnectedCallAtUtc { get; set; }
        public int? LastConnectedCallSeconds { get; set; }
        public string LastConnectedCallBy { get; set; }
        public int Touches30d { get; set; }
        public int OpenTrialFlags { get; set; }
        public int OpenTrmUnderReview { get; set; }
        public int SlaPending { get; set; }
        public int OpenDeductionReviews { get; set; }
        public int ActiveSuspicious { get; set; }
        /// <summary>Look-back in days the procedure used (90).</summary>
        public int LookbackDays { get; set; }
    }
}
