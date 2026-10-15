using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Rules for "Request review" on a deduction (My Quality, phase 2). Starting values come from the spec;
    /// each can be overridden in appsettings under "DeductionReview" (e.g. "DeductionReview:MinDeductionPct").
    /// </summary>
    public class DeductionReviewSettings
    {
        /// <summary>Only deductions at or above this % can be sent for review.</summary>
        public int MinDeductionPct { get; set; } = 3;
        /// <summary>Days from the deduction during which a review can be requested.</summary>
        public int WindowDays { get; set; } = 7;
        /// <summary>Review requests per interviewer per calendar month (IST).</summary>
        public int MonthlyCap { get; set; } = 3;
        public int NoteMinLength { get; set; } = 30;
        public int NoteMaxLength { get; set; } = 500;
        /// <summary>Working days (Mon-Fri) QC has to decide; unanswered requests close as "deduction stands".</summary>
        public int ReplyWorkingDays { get; set; } = 5;
        /// <summary>Comma-separated QC lead emails told about requests that expired unanswered.</summary>
        public string QcLeadEmails { get; set; }
        /// <summary>Upheld rate (0-100) at or above which a check is flagged as a candidate to take out of auto-approval.</summary>
        public int HighUpheldRatePct { get; set; } = 50;
        /// <summary>Decided reviews a check needs before its upheld rate is trusted.</summary>
        public int MinDecisionsForRate { get; set; } = 5;
    }

    public static class DeductionReviewConstants
    {
        public const string StatusOpen = "Open";
        public const string StatusStands = "Stands";
        public const string StatusReduced = "Reduced";
        public const string StatusRefunded = "Refunded";
        public const string StatusExpired = "Expired";

        public const string DecisionStands = "Stands";
        public const string DecisionReduce = "Reduce";
        public const string DecisionRefund = "Refund";

        public const int DecisionNoteMinLength = 5;
        public const int DecisionNoteMaxLength = 500;

        /// <summary>InterviewerActionLog.ActionType for a QC decision (counts on the QC Activity dashboard).</summary>
        public const string ActionLogType = "DeductionReview";

        /// <summary>Reason categories the interviewer picks from (key → label).</summary>
        public static readonly IReadOnlyDictionary<string, string> ReasonCategories =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["WrongStatus"] = "Wrong interview status",
                ["Technical"] = "Technical issue (audio, video, platform)",
                ["CandidateCaused"] = "Candidate caused it",
                ["ReportSavedIncorrectly"] = "Report saved incorrectly",
                ["Other"] = "Other"
            };

        /// <summary>AI check keys (dbo.fn_DeductionAiCheck) → label. Must match the SQL function.</summary>
        public static readonly IReadOnlyDictionary<string, string> AiChecks =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["LateJoin"] = "Late join",
                ["ShortComment"] = "Short comment",
                ["Duration"] = "Duration / time management",
                ["Questions"] = "Question relevance",
                ["Decision"] = "Decision accuracy",
                ["Rubric"] = "Scoring rubric",
                ["StatusUpdate"] = "Status update",
                ["CameraOff"] = "Camera off",
                ["Proctoring"] = "Proctoring",
                ["CandidateInteraction"] = "Candidate interaction",
                ["Communication"] = "Communication",
                ["Infrastructure"] = "Infrastructure",
                ["HandsOn"] = "Hands-on coding",
                ["Conduct"] = "Conduct",
                ["Other"] = "Other"
            };

        public static string CanonicalKey(IReadOnlyDictionary<string, string> map, string value) =>
            string.IsNullOrWhiteSpace(value) ? null : map.Keys.FirstOrDefault(k => string.Equals(k, value.Trim(), StringComparison.OrdinalIgnoreCase));

        public static string Label(IReadOnlyDictionary<string, string> map, string key) =>
            key != null && map.TryGetValue(key.Trim(), out var label) ? label : key;

        /// <summary>Adds working days (Mon-Fri) to a UTC instant, judged on the IST calendar.</summary>
        public static DateTime AddWorkingDaysUtc(DateTime utc, int workingDays)
        {
            var ist = utc.AddMinutes(330);
            var added = 0;
            while (added < Math.Max(0, workingDays))
            {
                ist = ist.AddDays(1);
                if (ist.DayOfWeek != DayOfWeek.Saturday && ist.DayOfWeek != DayOfWeek.Sunday)
                    added++;
            }
            return ist.AddMinutes(-330);
        }
    }

    // ---------- Interviewer side ----------

    public class DeductionReviewRequestInputVM
    {
        public int InterviewId { get; set; }
        public string ReasonCategory { get; set; }
        public string Note { get; set; }
    }

    // ---------- QC queue ----------

    /// <summary>Row of usp_GetDeductionReviewQueue (open requests, oldest first, or recent decisions).</summary>
    public class DeductionReviewQueueRowVM
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public int? IrfId { get; set; }
        public string IrfName { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string InterviewerEmail { get; set; }
        public string InterviewerName { get; set; }
        public string InterviewerCodedId { get; set; }
        public int DeductionPct { get; set; }
        public decimal? DeductionAmount { get; set; }
        public string ReasonCategory { get; set; }
        public string Note { get; set; }
        public string AiCheck { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public DateTime DueAtUtc { get; set; }
        public string Status { get; set; }
        public int? NewDeductionPct { get; set; }
        public decimal? RefundedAmount { get; set; }
        public string DecidedBy { get; set; }
        public DateTime? DecidedAtUtc { get; set; }
        public string DecisionNote { get; set; }
        public DateTime? LeadNotifiedAtUtc { get; set; }
        /// <summary>The interviewer was already paid for the interview when QC decided: Finance settles the difference.</summary>
        public bool PayoutAlreadyPaid { get; set; }
        /// <summary>Payment SLA recommendation / reason text, for QC context.</summary>
        public string Recommendation { get; set; }
        public string RecommendationComments { get; set; }

        // Display helpers filled by the service.
        public string ReasonLabel { get; set; }
        public string AiCheckLabel { get; set; }
        public bool IsOverdue { get; set; }
    }

    /// <summary>Upheld rate per AI check (usp_GetDeductionReviewQueue result set 3 + policy).</summary>
    public class DeductionReviewCheckStatVM
    {
        public string AiCheck { get; set; }
        public string AiCheckLabel { get; set; }
        public int Requests { get; set; }
        public int Decided { get; set; }
        public int Upheld { get; set; }
        public int Stands { get; set; }
        public int Open { get; set; }
        /// <summary>Upheld / Decided, whole percent; null with no decisions.</summary>
        public int? UpheldRatePct { get; set; }
        public bool ExcludeFromAutoApprove { get; set; }
        public string PolicyModifiedBy { get; set; }
        public DateTime? PolicyModifiedAtUtc { get; set; }
        /// <summary>Enough decisions and an upheld rate at or above the threshold.</summary>
        public bool SuggestExclude { get; set; }
    }

    public class DeductionReviewQueueVM
    {
        public IReadOnlyList<DeductionReviewQueueRowVM> Open { get; set; } = Array.Empty<DeductionReviewQueueRowVM>();
        public IReadOnlyList<DeductionReviewQueueRowVM> RecentDecided { get; set; } = Array.Empty<DeductionReviewQueueRowVM>();
        public IReadOnlyList<DeductionReviewCheckStatVM> Checks { get; set; } = Array.Empty<DeductionReviewCheckStatVM>();
        public DeductionReviewSettings Settings { get; set; } = new DeductionReviewSettings();
        public bool CanManagePolicy { get; set; }
        public int ExpiredJustNow { get; set; }
        public string Error { get; set; }
    }

    public class DeductionReviewDecisionInputVM
    {
        public int RequestId { get; set; }
        /// <summary>Stands | Reduce | Refund.</summary>
        public string Decision { get; set; }
        /// <summary>Required for Reduce: 1 .. current % - 1.</summary>
        public int? NewDeductionPct { get; set; }
        public string Note { get; set; }
    }

    /// <summary>Row returned by usp_DecideDeductionReviewRequest.</summary>
    public class DeductionReviewDecideResultVM
    {
        /// <summary>1 ok; -1 not found; -2 already decided; -3 invalid %; -4 table missing.</summary>
        public int Result { get; set; }
        public string Status { get; set; }
        public int? NewDeductionPct { get; set; }
        public decimal? RefundedAmount { get; set; }
        public bool PayoutAlreadyPaid { get; set; }
    }

    /// <summary>Request expired unanswered (usp_ExpireDeductionReviewRequests) — told to the QC lead.</summary>
    public class DeductionReviewExpiredRowVM
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public string InterviewerEmail { get; set; }
        public string IrfName { get; set; }
        public int DeductionPct { get; set; }
        public string AiCheck { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public DateTime DueAtUtc { get; set; }
    }

    /// <summary>High-Risk header, per window: review requests received, upheld %, refunded amount.</summary>
    public class DeductionReviewWindowStatsVM
    {
        public int RequestsReceived { get; set; }
        public int Decided { get; set; }
        public int Upheld { get; set; }
        public int? UpheldRatePct { get; set; }
        public decimal? RefundedAmount { get; set; }
        public int Open { get; set; }
    }
}
