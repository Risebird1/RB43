using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    /// <summary>One row per user on /QCActivity (usp_GetQCActivityDashboardSummary result set 1).</summary>
    public class QCActivitySummaryRowVM
    {
        public string ActorEmail { get; set; }
        public string ActorName { get; set; }
        public string ProfileName { get; set; }
        public bool IsActive { get; set; }

        public int ReportsReviewed { get; set; }
        public int PenaltiesApplied { get; set; }
        /// <summary>Average deduction % across the penalised reports; null when none were penalised.</summary>
        public decimal? AvgPenaltyPercent { get; set; }
        public int SlaApproved { get; set; }
        public int SlaNotApproved { get; set; }
        public int QcForms { get; set; }
        public int QcFormRedFlags { get; set; }

        public int TrialKept { get; set; }
        public int TrialRemoved { get; set; }

        /// <summary>Distinct IRFs across backfills and keep/remove decisions.</summary>
        public int AuditIrfs { get; set; }
        public int AuditBackfills { get; set; }
        public int AuditKept { get; set; }
        public int AuditRemoved { get; set; }

        public int TrmUnderReview { get; set; }
        public int TrmReviewed { get; set; }
        public int TrmRemoved { get; set; }

        public int TiUnderReview { get; set; }
        public int TiReviewed { get; set; }
        public int TiRemoved { get; set; }

        public int PlivoCalls { get; set; }
        public int PlivoInterviewers { get; set; }
        public int PlivoTalkSeconds { get; set; }
        /// <summary>Calls that have a duration (callback data arrived); the average call uses these only.</summary>
        public int PlivoCallsWithDuration { get; set; }

        public int CoachingSessions { get; set; }

        /// <summary>Report edits (ReportEditLog, plus older edits rebuilt from InterviewCommentsHistory).</summary>
        public int ReportsEdited { get; set; }

        // Interviewer status changes (SuspiciousInterviewer and InterviewerActionLog); each action with its undo.
        public int MarkedSuspicious { get; set; }
        public int SuspiciousUndone { get; set; }
        public int Blocked { get; set; }
        public int Unblocked { get; set; }
        public int AccessRevoked { get; set; }
        public int AccessRestored { get; set; }
        public int Inactivated { get; set; }
        public int Reactivated { get; set; }
        public int PaymentHolds { get; set; }
        public int PaymentHoldsReleased { get; set; }
        /// <summary>Decisions on interviewers' deduction review requests (InterviewerActionLog DeductionReview).</summary>
        public int DeductionReviews { get; set; }

        public int TotalActions { get; set; }
        public DateTime? LastActionAtIst { get; set; }

        /// <summary>Suspicious marks, blocks, access off, inactivations and payment holds.</summary>
        public int StatusChanges => MarkedSuspicious + Blocked + AccessRevoked + Inactivated + PaymentHolds;

        /// <summary>The undos of <see cref="StatusChanges"/>.</summary>
        public int StatusUndos => SuspiciousUndone + Unblocked + AccessRestored + Reactivated + PaymentHoldsReleased;

        public int? PlivoAvgCallSeconds =>
            PlivoCallsWithDuration > 0 ? (int)Math.Round((double)PlivoTalkSeconds / PlivoCallsWithDuration) : (int?)null;

        // ---- Part 2: authentic QC work (usp_GetQCActivityDashboardSummary) ----
        /// <summary>Answered calls to interviewers lasting at least MinCallSeconds.</summary>
        public int ConnectedCalls { get; set; }
        /// <summary>Every logged call to an interviewer.</summary>
        public int DialledCalls { get; set; }
        public int ConnectedTalkSeconds { get; set; }
        /// <summary>Distinct interviewers reached by a connected call or a coaching session.</summary>
        public int InterviewersReached { get; set; }
        public int ActionsTotalInterviews { get; set; }
        public int ActionsTrm { get; set; }
        public int ActionsIrfAudit { get; set; }
        public int ActionsTrial { get; set; }
        public int ActionsSuspicious { get; set; }
        public int ActionsReportsSla { get; set; }
        public int ProxyForms { get; set; }
        public int RedoRequests { get; set; }
        public int TrialBackfills { get; set; }
        /// <summary>Actions scored for authenticity (everything except calls, coaching and backfill runs).</summary>
        public int ScoredActions { get; set; }
        /// <summary>Scored actions with a real comment and evidence on the interviewer.</summary>
        public int AuthenticActions { get; set; }
        public int NoEvidenceActions { get; set; }
        /// <summary>High-Risk interviewers this person actioned who were also called or coached (by anyone) in the window.</summary>
        public int FollowThroughDone { get; set; }
        /// <summary>High-Risk interviewers this person actioned.</summary>
        public int FollowThroughOf { get; set; }
        public int FlagCount { get; set; }

        /// <summary>Actions taken on the six pages and Reports &amp; SLA (calls and coaching excluded).</summary>
        public int ActionsTaken => ActionsTotalInterviews + ActionsTrm + ActionsIrfAudit + ActionsTrial + ActionsSuspicious + ActionsReportsSla;

        /// <summary>Authenticity score: authentic ÷ scored actions, whole percent; null with no scored action.</summary>
        public int? AuthenticPct => ScoredActions > 0 ? (int)Math.Round(AuthenticActions * 100.0 / ScoredActions, MidpointRounding.AwayFromZero) : (int?)null;

        public int? FollowThroughPct => FollowThroughOf > 0 ? (int)Math.Round(FollowThroughDone * 100.0 / FollowThroughOf, MidpointRounding.AwayFromZero) : (int?)null;

        public int? ConnectedAvgCallSeconds => ConnectedCalls > 0 ? (int)Math.Round((double)ConnectedTalkSeconds / ConnectedCalls) : (int?)null;
    }

    /// <summary>"Open right now" strip (not date-filtered).</summary>
    public class QCActivityBacklogVM
    {
        public int PendingReports { get; set; }
        public int PendingTrialFlags { get; set; }
        public int PendingAuditInterviewers { get; set; }
        public int TrmUnderReview { get; set; }
        /// <summary>Deduction review requests waiting for a QC decision.</summary>
        public int PendingDeductionReviews { get; set; }
        /// <summary>High-risk coverage for the window (summary result sets 3 and 4); filled by the repository.</summary>
        public QCActivityCoverageVM Coverage { get; set; }
    }

    /// <summary>High-Risk interviewers in the window and how many any QC person touched (call, coaching or action).</summary>
    public class QCActivityCoverageVM
    {
        public int HighRiskTotal { get; set; }
        public int HighRiskTouched { get; set; }
        public decimal? CoveragePct { get; set; }
        public IReadOnlyList<QCActivityUntouchedVM> Untouched { get; set; } = Array.Empty<QCActivityUntouchedVM>();
        public string Error { get; set; }
        public string ErrorDetail { get; set; }
    }

    /// <summary>A High-Risk interviewer nobody called, coached or actioned in the window.</summary>
    public class QCActivityUntouchedVM
    {
        public int InterviewerId { get; set; }
        public string CodedId { get; set; }
        public string InterviewerName { get; set; }
        public string InterviewerEmail { get; set; }
        public decimal? OverallRiskRatio { get; set; }
        public string WorstSeverity { get; set; }
        public string PriorityTier { get; set; }
        public int? TotalCompleted { get; set; }
        public int? TotalBad { get; set; }
    }

    /// <summary>One flag for one person (usp_GetQCActivityFlags result set 1). A prompt to look, not a verdict.</summary>
    public class QCActivityFlagVM
    {
        public string Flag { get; set; }
        public string ActorEmail { get; set; }
        public int Count { get; set; }
        public DateTime? FirstAt { get; set; }
        public DateTime? LastAt { get; set; }
        public string Detail { get; set; }
        /// <summary>Comma-separated metric keys for the existing drill-down.</summary>
        public string Metrics { get; set; }
        /// <summary>Readable title (filled by the service).</summary>
        public string Title { get; set; }
        /// <summary>The rule with the thresholds in force (filled by the service from QCActivitySetting).</summary>
        public string Rule { get; set; }
        public IReadOnlyList<QCActivityFlagItemVM> Items { get; set; } = Array.Empty<QCActivityFlagItemVM>();
    }

    /// <summary>A row behind a flag (usp_GetQCActivityFlags result set 2).</summary>
    public class QCActivityFlagItemVM
    {
        public string Flag { get; set; }
        public string ActorEmail { get; set; }
        public DateTime? ActionAtIst { get; set; }
        public string Metric { get; set; }
        public int? InterviewId { get; set; }
        public int? IrfId { get; set; }
        public string IrfName { get; set; }
        public string InterviewerEmail { get; set; }
        public string InterviewerName { get; set; }
        public string Detail { get; set; }
        public int? SourceId { get; set; }
        public string Note { get; set; }
    }

    /// <summary>JSON payload of GET /QCActivity/Flags.</summary>
    public class QCActivityFlagsResultVM
    {
        public string ActorEmail { get; set; }
        public string FromIst { get; set; }
        public string ToIst { get; set; }
        public IReadOnlyList<QCActivityFlagVM> Flags { get; set; } = Array.Empty<QCActivityFlagVM>();
        public QCActivitySettingsVM Settings { get; set; } = new QCActivitySettingsVM();
        public string Error { get; set; }
        public string ErrorDetail { get; set; }
    }

    /// <summary>Name / Value row of dbo.QCActivitySetting.</summary>
    public class QCActivitySettingRowVM
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// QC Activity authenticity thresholds (dbo.QCActivitySetting; the QC lead tunes them without a deploy).
    /// Defaults are the spec's start values and match the SQL defaults in usp_QCActivity_CollectRows /
    /// usp_QCActivity_ComputeFlags.
    /// </summary>
    public class QCActivitySettingsVM
    {
        public int MinCallSeconds { get; set; } = 120;
        public int MinCommentChars { get; set; } = 20;
        public IReadOnlyList<string> StockComments { get; set; } =
            new[] { "ok", "okay", "done", "fine", "checked", "check", "na", "n/a", "nil", "none", "reviewed", "good", "noted", "yes", "no", "-", "." };
        public int BurstCount { get; set; } = 10;
        public int BurstMinutes { get; set; } = 10;
        public int ReversalDays { get; set; } = 7;
        public int ShortCallSeconds { get; set; } = 30;
        public int ShortCallPct { get; set; } = 50;

        /// <summary>Builds the settings from table rows; a missing or invalid row keeps its default.</summary>
        public static QCActivitySettingsVM From(IEnumerable<QCActivitySettingRowVM> rows)
        {
            var s = new QCActivitySettingsVM();
            foreach (var r in rows ?? Enumerable.Empty<QCActivitySettingRowVM>())
            {
                if (r == null || string.IsNullOrWhiteSpace(r.Name)) continue;
                var name = r.Name.Trim();
                var value = r.Value?.Trim();
                if (string.Equals(name, "StockComments", StringComparison.OrdinalIgnoreCase))
                {
                    if (value != null)
                        s.StockComments = value.Split(',').Select(w => w.Trim().ToLowerInvariant()).Where(w => w.Length > 0).Distinct().ToList();
                    continue;
                }
                if (!int.TryParse(value, out var n) || n < 0) continue;
                switch (name.ToLowerInvariant())
                {
                    case "mincallseconds": s.MinCallSeconds = n; break;
                    case "mincommentchars": s.MinCommentChars = n; break;
                    case "burstcount": s.BurstCount = Math.Max(2, n); break;
                    case "burstminutes": s.BurstMinutes = Math.Max(1, n); break;
                    case "reversaldays": s.ReversalDays = n; break;
                    case "shortcallseconds": s.ShortCallSeconds = n; break;
                    case "shortcallpct": s.ShortCallPct = n; break;
                }
            }
            return s;
        }

        /// <summary>True when the comment is only a stock word ("ok", "done", ...), ignoring case and "." / "!".</summary>
        public bool IsStockComment(string comment)
        {
            var c = (comment ?? "").Replace(".", "").Replace("!", "").Trim().ToLowerInvariant();
            return StockComments.Contains(c) || StockComments.Contains((comment ?? "").Trim().ToLowerInvariant());
        }

        /// <summary>
        /// The authentic-action rule (same as usp_QCActivity_CollectRows section 16): a comment of at least
        /// MinCommentChars that is not a stock word, and evidence on the interviewer in the window.
        /// </summary>
        public bool IsAuthenticAction(string comment, bool hasEvidence) =>
            hasEvidence && (comment ?? "").Trim().Length >= MinCommentChars && !IsStockComment(comment);

        /// <summary>Why an action is not authentic, in plain words; null when it is.</summary>
        public string NotAuthenticReason(string comment, bool hasEvidence)
        {
            var reasons = new List<string>();
            var c = (comment ?? "").Trim();
            if (c.Length == 0) reasons.Add("no comment");
            else if (IsStockComment(c)) reasons.Add("stock comment (\"" + c + "\")");
            else if (c.Length < MinCommentChars) reasons.Add("comment under " + MinCommentChars + " characters");
            if (!hasEvidence) reasons.Add("no evidence on the interviewer in the window");
            return reasons.Count == 0 ? null : string.Join("; ", reasons);
        }

        /// <summary>A connected coaching call: answered, to an interviewer, at least MinCallSeconds long.</summary>
        public bool IsConnectedCall(int? durationSeconds, string status)
        {
            if (!durationSeconds.HasValue || durationSeconds.Value < MinCallSeconds) return false;
            var st = (status ?? "").ToLowerInvariant();
            return !(st.Contains("busy") || st.Contains("no-answer") || st.Contains("no_answer") || st.Contains("fail") || st.Contains("cancel"));
        }
    }

    /// <summary>Totals for the tiles, computed from the listed rows.</summary>
    public class QCActivityTotalsVM
    {
        public int ReportsReviewed { get; set; }
        public int PenaltiesApplied { get; set; }
        public int TrialKept { get; set; }
        public int TrialRemoved { get; set; }
        public int AuditIrfs { get; set; }
        public int AuditRemoved { get; set; }
        public int TrmRemoved { get; set; }
        public int TiRemoved { get; set; }
        public int PlivoCalls { get; set; }
        public int PlivoInterviewers { get; set; }
        public int PlivoTalkSeconds { get; set; }
        public int ReportsEdited { get; set; }
        public int StatusChanges { get; set; }
        public int StatusUndos { get; set; }
        public int DeductionReviews { get; set; }
        // Part 2 tiles
        public int ConnectedCalls { get; set; }
        public int DialledCalls { get; set; }
        public int ConnectedTalkSeconds { get; set; }
        /// <summary>Sum over people (an interviewer reached by two people counts twice).</summary>
        public int InterviewersReached { get; set; }
        public int ActionsTaken { get; set; }
        public int ScoredActions { get; set; }
        public int AuthenticActions { get; set; }
        public int? AuthenticPct => ScoredActions > 0 ? (int)Math.Round(AuthenticActions * 100.0 / ScoredActions, MidpointRounding.AwayFromZero) : (int?)null;
        public int FlagCount { get; set; }
        public int UsersListed { get; set; }
        public int UsersWithNoActivity { get; set; }

        public static QCActivityTotalsVM From(IReadOnlyCollection<QCActivitySummaryRowVM> rows)
        {
            rows ??= Array.Empty<QCActivitySummaryRowVM>();
            return new QCActivityTotalsVM
            {
                ReportsEdited = rows.Sum(r => r.ReportsEdited),
                StatusChanges = rows.Sum(r => r.StatusChanges),
                StatusUndos = rows.Sum(r => r.StatusUndos),
                DeductionReviews = rows.Sum(r => r.DeductionReviews),
                ReportsReviewed = rows.Sum(r => r.ReportsReviewed),
                PenaltiesApplied = rows.Sum(r => r.PenaltiesApplied),
                TrialKept = rows.Sum(r => r.TrialKept),
                TrialRemoved = rows.Sum(r => r.TrialRemoved),
                AuditIrfs = rows.Sum(r => r.AuditIrfs),
                AuditRemoved = rows.Sum(r => r.AuditRemoved),
                TrmRemoved = rows.Sum(r => r.TrmRemoved),
                TiRemoved = rows.Sum(r => r.TiRemoved),
                PlivoCalls = rows.Sum(r => r.PlivoCalls),
                PlivoInterviewers = rows.Sum(r => r.PlivoInterviewers),
                PlivoTalkSeconds = rows.Sum(r => r.PlivoTalkSeconds),
                ConnectedCalls = rows.Sum(r => r.ConnectedCalls),
                DialledCalls = rows.Sum(r => r.DialledCalls),
                ConnectedTalkSeconds = rows.Sum(r => r.ConnectedTalkSeconds),
                InterviewersReached = rows.Sum(r => r.InterviewersReached),
                ActionsTaken = rows.Sum(r => r.ActionsTaken),
                ScoredActions = rows.Sum(r => r.ScoredActions),
                AuthenticActions = rows.Sum(r => r.AuthenticActions),
                FlagCount = rows.Sum(r => r.FlagCount),
                UsersListed = rows.Count,
                UsersWithNoActivity = rows.Count(r => r.TotalActions == 0)
            };
        }
    }

    /// <summary>JSON payload of GET /QCActivity/Summary.</summary>
    public class QCActivityDashboardDataVM
    {
        public string FromIst { get; set; }
        public string ToIst { get; set; }
        public bool OnlyQualityController { get; set; }
        /// <summary>True when the range asked for was longer than the cap and was cut.</summary>
        public bool RangeWasCapped { get; set; }
        public IReadOnlyList<QCActivitySummaryRowVM> Rows { get; set; } = Array.Empty<QCActivitySummaryRowVM>();
        public QCActivityBacklogVM Backlog { get; set; } = new QCActivityBacklogVM();
        public QCActivityTotalsVM Totals { get; set; } = new QCActivityTotalsVM();
        /// <summary>High-risk coverage for the window (page level).</summary>
        public QCActivityCoverageVM Coverage { get; set; } = new QCActivityCoverageVM();
        public string Error { get; set; }
        /// <summary>Underlying exception message; filled only when the app runs in Development (cleared by the controller otherwise).</summary>
        public string ErrorDetail { get; set; }
    }

    /// <summary>One record behind a dashboard number (usp_GetQCActivityDashboardDetails).</summary>
    public class QCActivityDetailRowVM
    {
        public string Metric { get; set; }
        public string ActorEmail { get; set; }
        public DateTime? ActionAtIst { get; set; }
        public int? InterviewId { get; set; }
        public int? IrfId { get; set; }
        public string IrfName { get; set; }
        public string InterviewerEmail { get; set; }
        public string InterviewerName { get; set; }
        public string Phone { get; set; }
        /// <summary>Penalty % (reports / SLA), red flags (QC form), talk seconds (Plivo), processed / new flags (backfill).</summary>
        public int? Amount { get; set; }
        public string Detail { get; set; }
        public string Status { get; set; }
        public string RecordingUrl { get; set; }
        public int? SourceId { get; set; }
        /// <summary>ReportEditLog.Id for log-based ReportEdited rows (the "View change" link); null otherwise.</summary>
        public int? RefId { get; set; }

        // ---- Part 2: authenticity ----
        /// <summary>The action's own comment (red-marker, audit, trial, suspicious, status-action or SLA comment).</summary>
        public string Comment { get; set; }
        public bool? HasEvidence { get; set; }
        /// <summary>True / false for scored actions and calls; null for rows that are not scored.</summary>
        public bool? IsAuthentic { get; set; }
        public string PageGroup { get; set; }
        /// <summary>FollowThrough drill-down only: called or coached by anyone in the window.</summary>
        public bool? FollowedUp { get; set; }
        /// <summary>Why a scored action is not authentic (filled by the service); null otherwise.</summary>
        public string NotAuthenticReason { get; set; }
    }

    /// <summary>One report edit with the full text before and after (usp_GetReportEdit, GET /QCActivity/ReportEdit).</summary>
    public class ReportEditVM
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public string FieldName { get; set; }
        /// <summary>Text before the edit, HTML-encoded as the app saves it.</summary>
        public string OldValue { get; set; }
        /// <summary>Text after the edit, HTML-encoded as the app saves it.</summary>
        public string NewValue { get; set; }
        public string EditedBy { get; set; }
        public string EditorName { get; set; }
        public DateTime? EditedAtIst { get; set; }
        public string InterviewerEmail { get; set; }
        public string InterviewerName { get; set; }
        public int? IrfId { get; set; }
        public string IrfName { get; set; }
        /// <summary>Set (and every other field empty) when the edit cannot be shown.</summary>
        public string Error { get; set; }
    }

    /// <summary>JSON payload of GET /QCActivity/Details.</summary>
    public class QCActivityDetailsResultVM
    {
        public string ActorEmail { get; set; }
        public string Metric { get; set; }
        public string MetricLabel { get; set; }
        public string FromIst { get; set; }
        public string ToIst { get; set; }
        public IReadOnlyList<QCActivityDetailRowVM> Rows { get; set; } = Array.Empty<QCActivityDetailRowVM>();
        /// <summary>True when the row cap was reached; narrow the range to see everything.</summary>
        public bool Truncated { get; set; }
        public string Error { get; set; }
        /// <summary>Underlying exception message; filled only when the app runs in Development (cleared by the controller otherwise).</summary>
        public string ErrorDetail { get; set; }
    }

    /// <summary>Metric keys accepted by /QCActivity/Details. Anything else returns an empty list without querying.</summary>
    public static class QCActivityMetrics
    {
        public const int MaxDetailRows = 2000;
        public const int DefaultRangeDays = 7;
        public const int MaxRangeDays = 92;

        public const string ReportReviewed = "ReportReviewed";
        public const string PenaltyApplied = "PenaltyApplied";
        public const string SlaApproved = "SlaApproved";
        public const string SlaNotApproved = "SlaNotApproved";
        public const string QCForm = "QCForm";
        public const string TrialKept = "TrialKept";
        public const string TrialRemoved = "TrialRemoved";
        public const string Trial = "Trial";
        public const string AuditBackfill = "AuditBackfill";
        public const string AuditKept = "AuditKept";
        public const string AuditRemoved = "AuditRemoved";
        public const string AuditActions = "AuditActions";
        public const string TrmUnderReview = "TrmUnderReview";
        public const string TrmReviewed = "TrmReviewed";
        public const string TrmRemoved = "TrmRemoved";
        public const string Trm = "Trm";
        public const string TiUnderReview = "TiUnderReview";
        public const string TiReviewed = "TiReviewed";
        public const string TiRemoved = "TiRemoved";
        public const string Ti = "Ti";
        public const string PlivoCall = "PlivoCall";
        public const string Coaching = "Coaching";
        public const string ReportEdited = "ReportEdited";
        public const string Suspicious = "Suspicious";
        public const string SuspiciousUndone = "SuspiciousUndone";
        public const string Blocked = "Blocked";
        public const string Unblocked = "Unblocked";
        public const string AccessRevoked = "AccessRevoked";
        public const string AccessRestored = "AccessRestored";
        public const string Inactivated = "Inactivated";
        public const string Reactivated = "Reactivated";
        public const string PaymentHold = "PaymentHold";
        public const string PaymentHoldReleased = "PaymentHoldReleased";
        public const string DeductionReview = "DeductionReview";
        // Part 2: authentic QC work
        public const string ProxyForm = "ProxyForm";
        public const string RedoRequested = "RedoRequested";
        public const string TrialBackfill = "TrialBackfill";
        public const string PageTotalInterviews = "PageTotalInterviews";
        public const string PageTrm = "PageTrm";
        public const string PageIrfAudit = "PageIrfAudit";
        public const string PageTrial = "PageTrial";
        public const string PageSuspicious = "PageSuspicious";
        public const string PageReportsSla = "PageReportsSla";
        public const string ScoredActions = "ScoredActions";
        public const string AuthenticActions = "AuthenticActions";
        public const string NotAuthentic = "NotAuthentic";
        public const string NoEvidence = "NoEvidence";
        public const string ConnectedCall = "ConnectedCall";
        public const string Reached = "Reached";
        public const string FollowThrough = "FollowThrough";
        public const string ActionsTaken = "ActionsTaken";
        /// <summary>Group keys: an action together with its undo (StatusActions = all ten).</summary>
        public const string StatusActions = "StatusActions";
        public const string SuspiciousAll = "SuspiciousAll";
        public const string BlockedAll = "BlockedAll";
        public const string AccessAll = "AccessAll";
        public const string ActiveAll = "ActiveAll";
        public const string PaymentHoldAll = "PaymentHoldAll";
        public const string All = "All";

        private static readonly IReadOnlyDictionary<string, string> Labels =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ReportReviewed] = "Reports reviewed",
                [PenaltyApplied] = "Penalties applied",
                [SlaApproved] = "Payment SLA approved",
                [SlaNotApproved] = "Payment SLA not approved",
                [QCForm] = "QC forms",
                [TrialKept] = "Trial interviewers kept",
                [TrialRemoved] = "Trial interviewers removed",
                [Trial] = "Trial interviewer decisions",
                [AuditBackfill] = "IRF audit backfills",
                [AuditKept] = "IRF audit: interviewers kept",
                [AuditRemoved] = "IRF audit: interviewers removed",
                [AuditActions] = "IRF audit actions",
                [TrmUnderReview] = "Total Red Marker: moved to under review",
                [TrmReviewed] = "Total Red Marker: marked reviewed",
                [TrmRemoved] = "Total Red Marker: removed",
                [Trm] = "Total Red Marker actions",
                [TiUnderReview] = "Total Interviews: moved to under review",
                [TiReviewed] = "Total Interviews: marked reviewed",
                [TiRemoved] = "Total Interviews: removed",
                [Ti] = "Total Interviews actions",
                [PlivoCall] = "Plivo calls to interviewers",
                [Coaching] = "Coaching sessions",
                [ReportEdited] = "Reports edited",
                [Suspicious] = "Interviewers marked suspicious",
                [SuspiciousUndone] = "Suspicious marks undone",
                [Blocked] = "Interviewers blocked",
                [Unblocked] = "Interviewers unblocked",
                [AccessRevoked] = "Portal access turned off",
                [AccessRestored] = "Portal access turned on",
                [Inactivated] = "Interviewers made inactive",
                [Reactivated] = "Interviewers made active",
                [PaymentHold] = "Payments put on hold",
                [PaymentHoldReleased] = "Payment holds released",
                [DeductionReview] = "Deduction review decisions",
                [ProxyForm] = "Proxy forms saved",
                [RedoRequested] = "Redo requests",
                [TrialBackfill] = "Trial-flag backfill runs",
                [PageTotalInterviews] = "Actions on Total Interviews",
                [PageTrm] = "Actions on Total Red Marker",
                [PageIrfAudit] = "Actions on IRF Audit",
                [PageTrial] = "Actions on Trial Flag Review",
                [PageSuspicious] = "Suspicious marks and undos",
                [PageReportsSla] = "Reports & Payment SLA actions",
                [ScoredActions] = "Actions scored for authenticity",
                [AuthenticActions] = "Authentic actions (comment + evidence)",
                [NotAuthentic] = "Actions not counted as authentic",
                [NoEvidence] = "Actions without evidence",
                [ConnectedCall] = "Connected coaching calls",
                [Reached] = "Interviewers reached (connected calls + coaching)",
                [FollowThrough] = "Actions on High-Risk interviewers (follow-through)",
                [ActionsTaken] = "Actions taken (all pages)",
                [StatusActions] = "Interviewer status changes",
                [SuspiciousAll] = "Suspicious: marked and undone",
                [BlockedAll] = "Blocked and unblocked",
                [AccessAll] = "Portal access: off and on",
                [ActiveAll] = "Made inactive and active",
                [PaymentHoldAll] = "Payment hold: put on and released",
                [All] = "All actions"
            };

        public static IEnumerable<string> Keys => Labels.Keys;

        /// <summary>Returns the canonical key (case-insensitive match) or null when the key is not whitelisted.</summary>
        public static string Normalize(string metric)
        {
            if (string.IsNullOrWhiteSpace(metric))
                return null;
            var trimmed = metric.Trim();
            return Labels.Keys.FirstOrDefault(k => string.Equals(k, trimmed, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsValid(string metric) => Normalize(metric) != null;

        public static string Label(string metric)
        {
            var key = Normalize(metric);
            return key != null ? Labels[key] : metric;
        }
    }
}
