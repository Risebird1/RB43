using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    // ---------- Raw rows (usp_GetInterviewerMyQuality) ----------

    /// <summary>Result set 1: tiles for the current and previous 30 days, this and last month.</summary>
    public class MyQualityTilesRowVM
    {
        public int CurCompleted { get; set; }
        public int CurIssueFree { get; set; }
        public int PrevCompleted { get; set; }
        public int PrevIssueFree { get; set; }
        public int CurJoinTracked { get; set; }
        public int CurOnTime { get; set; }
        public int PrevJoinTracked { get; set; }
        public int PrevOnTime { get; set; }
        public int MonthDeductionCount { get; set; }
        public decimal? MonthDeductionAvgPct { get; set; }
        public decimal? MonthDeductionAmount { get; set; }
        public int PrevMonthDeductionCount { get; set; }
        public int ActiveChannels { get; set; }
        public int PausedChannels { get; set; }
        /// <summary>Deduction review requests made this calendar month (IST), for the monthly cap.</summary>
        public int ReviewRequestsThisMonth { get; set; }
    }

    /// <summary>Result set 2: issue-free share per channel (IRF code only).</summary>
    public class MyQualityChannelRowVM
    {
        public int? IrfId { get; set; }
        public string IrfName { get; set; }
        public int Completed { get; set; }
        public int IssueFree { get; set; }
        /// <summary>Active | Paused | empty.</summary>
        public string Standing { get; set; }
    }

    /// <summary>Result set 4: Payment SLA deduction (reason columns inherited from the exposure row).</summary>
    public class MyQualityDeductionRowVM : ExposurePaymentRowVM
    {
        public decimal? Amount { get; set; }
        /// <summary>1 applied, 2 not applied, 0 still being checked.</summary>
        public int ApprovalState { get; set; }
        /// <summary>When the deduction was applied (first wallet row, else the Payment SLA save time).</summary>
        public DateTime? AppliedAt { get; set; }
        // Review request on this deduction (null when none).
        public string ReviewStatus { get; set; }
        public int? ReviewOriginalPct { get; set; }
        public int? ReviewNewPct { get; set; }
        public DateTime? ReviewRequestedAtUtc { get; set; }
        public DateTime? ReviewDueAtUtc { get; set; }
        public DateTime? ReviewDecidedAtUtc { get; set; }
        public string ReviewDecisionNote { get; set; }
    }

    /// <summary>Result set 5: a coaching session (no coach name, no outcome).</summary>
    public class MyQualityCoachingRowVM
    {
        public int SessionId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string Channel { get; set; }
        public string Themes { get; set; }
        public DateTime? AcknowledgedAtUtc { get; set; }
    }

    /// <summary>Result set 6: InterviewerFeedbackText wording.</summary>
    public class InterviewerFeedbackTextVM
    {
        public string ThemeKey { get; set; }
        public string Title { get; set; }
        public string WhatToDo { get; set; }
    }

    public class InterviewerMyQualityDataVM
    {
        public MyQualityTilesRowVM Tiles { get; set; }
        public IReadOnlyList<MyQualityChannelRowVM> Channels { get; set; } = Array.Empty<MyQualityChannelRowVM>();
        public IReadOnlyList<ExposureRiskRowVM> Issues { get; set; } = Array.Empty<ExposureRiskRowVM>();
        public IReadOnlyList<MyQualityDeductionRowVM> Deductions { get; set; } = Array.Empty<MyQualityDeductionRowVM>();
        public IReadOnlyList<MyQualityCoachingRowVM> Coaching { get; set; } = Array.Empty<MyQualityCoachingRowVM>();
        public IReadOnlyList<InterviewerFeedbackTextVM> Wording { get; set; } = Array.Empty<InterviewerFeedbackTextVM>();
    }

    // ---------- Page view model (InterviewerMyQualityService) ----------

    public class MyQualityTileVM
    {
        public int Count { get; set; }
        public int Total { get; set; }
        /// <summary>Whole percent, null when there is nothing to measure.</summary>
        public int? Percent { get; set; }
        public int? PreviousPercent { get; set; }
        /// <summary>Up | Down | Same | null.</summary>
        public string Trend { get; set; }
    }

    public class MyQualityEvidenceVM
    {
        public int InterviewId { get; set; }
        public int? IrfId { get; set; }
        public string IrfName { get; set; }
        /// <summary>yyyy-MM-dd (IST).</summary>
        public string DateIst { get; set; }
    }

    public class MyQualityFocusVM
    {
        public string ThemeKey { get; set; }
        public string Title { get; set; }
        public string WhatToDo { get; set; }
        /// <summary>Distinct interviews behind this item in the last 30 days.</summary>
        public int Count { get; set; }
        public IReadOnlyList<MyQualityEvidenceVM> Evidence { get; set; } = Array.Empty<MyQualityEvidenceVM>();
    }

    public class MyQualityDeductionVM
    {
        public int InterviewId { get; set; }
        public int? IrfId { get; set; }
        public string IrfName { get; set; }
        public string DateIst { get; set; }
        public int Percent { get; set; }
        public decimal? Amount { get; set; }
        public string Reason { get; set; }
        /// <summary>Applied | Being checked | Not applied.</summary>
        public string Status { get; set; }

        // ---- Request review (phase 2) ----
        /// <summary>The Request review button is shown.</summary>
        public bool CanRequestReview { get; set; }
        /// <summary>Grey text when it is not: "Below 3%", "Older than 7 days", "Monthly limit reached".</summary>
        public string ReviewBlockedReason { get; set; }
        /// <summary>"Under review · reply by 21 Oct" or "Reviewed · deduction stands / reduced to n% / refunded".</summary>
        public string ReviewLabel { get; set; }
        /// <summary>Open | Stands | Reduced | Refunded | Expired, or null.</summary>
        public string ReviewStatus { get; set; }
        /// <summary>QC decision note shown with the outcome (no QC name).</summary>
        public string ReviewDecisionNote { get; set; }
    }

    public class MyQualityChannelVM
    {
        public string IrfName { get; set; }
        public int Completed { get; set; }
        public int IssueFree { get; set; }
        public int? Percent { get; set; }
        public string Standing { get; set; }
    }

    public class MyQualityCoachingVM
    {
        public int SessionId { get; set; }
        /// <summary>yyyy-MM-dd (IST).</summary>
        public string DateIst { get; set; }
        /// <summary>Call | Email | WhatsApp | Video meeting | Other.</summary>
        public string Channel { get; set; }
        public IReadOnlyList<string> Topics { get; set; } = Array.Empty<string>();
        public bool Acknowledged { get; set; }
    }

    public class MyQualityRecordVM
    {
        public int Completed { get; set; }
        public int? SelectPercent { get; set; }
        public int? RejectPercent { get; set; }
        public int NoShows { get; set; }
        public int LateJoins { get; set; }
    }

    public class InterviewerMyQualityVM
    {
        public const string Attribution = "Generated by Risebird quality system";

        public MyQualityTileVM IssueFree { get; set; } = new MyQualityTileVM();
        public MyQualityTileVM OnTime { get; set; } = new MyQualityTileVM();
        public int MonthDeductionCount { get; set; }
        public decimal? MonthDeductionAvgPct { get; set; }
        public decimal? MonthDeductionAmount { get; set; }
        /// <summary>Up | Down | Same | null, against last month.</summary>
        public string DeductionTrend { get; set; }
        public int ActiveChannels { get; set; }
        public int PausedChannels { get; set; }
        public IReadOnlyList<MyQualityFocusVM> TopFocus { get; set; } = Array.Empty<MyQualityFocusVM>();
        public IReadOnlyList<MyQualityDeductionVM> Deductions { get; set; } = Array.Empty<MyQualityDeductionVM>();
        public IReadOnlyList<MyQualityChannelVM> Channels { get; set; } = Array.Empty<MyQualityChannelVM>();
        public IReadOnlyList<MyQualityCoachingVM> Coaching { get; set; } = Array.Empty<MyQualityCoachingVM>();
        public MyQualityRecordVM Record { get; set; }
        public string Error { get; set; }

        // ---- Request review (phase 2) ----
        public DeductionReviewSettings ReviewSettings { get; set; } = new DeductionReviewSettings();
        public int ReviewRequestsThisMonth { get; set; }
        public int ReviewRequestsLeft => System.Math.Max(0, (ReviewSettings?.MonthlyCap ?? 0) - ReviewRequestsThisMonth);
    }

    /// <summary>JSON for the small card on My Interviews.</summary>
    public class InterviewerMyQualitySummaryVM
    {
        public bool Ok { get; set; }
        public int FocusCount { get; set; }
        public int MonthDeductionCount { get; set; }
    }
}
