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

        public int TotalActions { get; set; }
        public DateTime? LastActionAtIst { get; set; }

        /// <summary>Suspicious marks, blocks, access off, inactivations and payment holds.</summary>
        public int StatusChanges => MarkedSuspicious + Blocked + AccessRevoked + Inactivated + PaymentHolds;

        /// <summary>The undos of <see cref="StatusChanges"/>.</summary>
        public int StatusUndos => SuspiciousUndone + Unblocked + AccessRestored + Reactivated + PaymentHoldsReleased;

        public int? PlivoAvgCallSeconds =>
            PlivoCallsWithDuration > 0 ? (int)Math.Round((double)PlivoTalkSeconds / PlivoCallsWithDuration) : (int?)null;
    }

    /// <summary>"Open right now" strip (not date-filtered).</summary>
    public class QCActivityBacklogVM
    {
        public int PendingReports { get; set; }
        public int PendingTrialFlags { get; set; }
        public int PendingAuditInterviewers { get; set; }
        public int TrmUnderReview { get; set; }
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
