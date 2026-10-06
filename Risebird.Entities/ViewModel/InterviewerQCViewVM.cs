using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    /// <summary>One row per interviewer on /QCActivity/Interviewers (usp_GetInterviewerQCView).</summary>
    public class InterviewerQCRowVM
    {
        public int InterviewerUserId { get; set; }
        public string InterviewerEmail { get; set; }
        public string InterviewerName { get; set; }
        public string Mobile { get; set; }
        public bool IsActive { get; set; }
        /// <summary>InterviewerProfile.RedMarkerStatus: 1 under review, 2 reviewed, 3 removed (Total Interviews).</summary>
        public int? TiStatus { get; set; }

        public int ActiveIrfs { get; set; }
        public int CompletedInterviews { get; set; }

        public int QcReviewed { get; set; }
        public int Penalised { get; set; }
        public decimal? AvgPenaltyPercent { get; set; }
        public int AiPenalised { get; set; }

        public int TrialOpen { get; set; }
        public int TrialKept { get; set; }
        public int TrialRemoved { get; set; }

        public int AuditOpen { get; set; }
        public int AuditKept { get; set; }
        public int AuditRemoved { get; set; }

        public int RedMarkers { get; set; }
        public int TrmUnderReviewNow { get; set; }
        public int TrmOpen { get; set; }
        public int TrmRemoved { get; set; }

        public int TiOpen { get; set; }
        public int TiRemoved { get; set; }

        public int Calls { get; set; }
        public int TalkSeconds { get; set; }
        public int DistinctCallers { get; set; }
        public DateTime? LastCallAtIst { get; set; }

        public int CoachingSessions { get; set; }
        public DateTime? LastCoachedAtIst { get; set; }

        public int QcActions { get; set; }
        public DateTime? LastActionAtIst { get; set; }

        public int OpenFlags { get; set; }
        public int IssuesInRange { get; set; }
        public bool HasIssue { get; set; }
        public bool NeedsCall { get; set; }
        public bool RemovedOrInactive { get; set; }
        public int AttentionScore { get; set; }

        public string TiStatusLabel => TiStatus switch
        {
            1 => "Under review",
            2 => "Reviewed",
            3 => "Removed",
            _ => null
        };
    }

    /// <summary>Tile numbers on the Interviewer QC View (each tile is also a filter).</summary>
    public class InterviewerQCTotalsVM
    {
        public int Interviewers { get; set; }
        public int NeedsCall { get; set; }
        public int OpenFlags { get; set; }
        public int Penalised { get; set; }
        public int RemovedOrInactive { get; set; }
        public int WithIssue { get; set; }
        public int WithIssueContacted { get; set; }
        /// <summary>Share of interviewers with an issue who got a QC call or coaching session; null when none have an issue.</summary>
        public int? CoachingCoveragePercent { get; set; }

        public static InterviewerQCTotalsVM From(IReadOnlyCollection<InterviewerQCRowVM> rows)
        {
            rows ??= Array.Empty<InterviewerQCRowVM>();
            var withIssue = rows.Where(r => r.HasIssue).ToList();
            var contacted = withIssue.Count(r => r.Calls > 0 || r.CoachingSessions > 0);
            return new InterviewerQCTotalsVM
            {
                Interviewers = rows.Count,
                NeedsCall = rows.Count(r => r.NeedsCall),
                OpenFlags = rows.Count(r => r.OpenFlags > 0),
                Penalised = rows.Count(r => r.Penalised > 0 || r.AiPenalised > 0),
                RemovedOrInactive = rows.Count(r => r.RemovedOrInactive),
                WithIssue = withIssue.Count,
                WithIssueContacted = contacted,
                CoachingCoveragePercent = withIssue.Count > 0
                    ? (int)Math.Round(100.0 * contacted / withIssue.Count)
                    : (int?)null
            };
        }
    }

    /// <summary>JSON payload of GET /QCActivity/InterviewerList.</summary>
    public class InterviewerQCListResultVM
    {
        public string FromIst { get; set; }
        public string ToIst { get; set; }
        public string Search { get; set; }
        public bool RangeWasCapped { get; set; }
        public IReadOnlyList<InterviewerQCRowVM> Rows { get; set; } = Array.Empty<InterviewerQCRowVM>();
        public InterviewerQCTotalsVM Totals { get; set; } = new InterviewerQCTotalsVM();
        public bool Truncated { get; set; }
        public string Error { get; set; }
        /// <summary>Underlying exception message; filled only when the app runs in Development (cleared by the controller otherwise).</summary>
        public string ErrorDetail { get; set; }
    }

    /// <summary>One IRF in the side panel (usp_GetInterviewerQCTimeline result set 1).</summary>
    public class InterviewerQCIrfVM
    {
        public int IrfId { get; set; }
        public string IrfName { get; set; }
        public string ChannelOwner { get; set; }
        public bool IsActive { get; set; }
        public int? RedMarkers { get; set; }
        /// <summary>TransInterviewerRedMarker.RedMarkerStatus: 1 under review, 2 reviewed, 3 removed.</summary>
        public int? TrmStatus { get; set; }
        public string AutoQcVerdict { get; set; }
        public string TrialStatus { get; set; }
    }

    /// <summary>One timeline event (usp_GetInterviewerQCTimeline result set 2).</summary>
    public class InterviewerQCEventVM
    {
        /// <summary>QC | Removal | Call | Coaching | AI | System</summary>
        public string Kind { get; set; }
        public string Metric { get; set; }
        public DateTime? ActionAtIst { get; set; }
        public string ActorEmail { get; set; }
        public int? InterviewId { get; set; }
        public int? IrfId { get; set; }
        public string IrfName { get; set; }
        public int? Amount { get; set; }
        public string Detail { get; set; }
        public string Status { get; set; }
        public string RecordingUrl { get; set; }
    }

    /// <summary>JSON payload of GET /QCActivity/InterviewerTimeline.</summary>
    public class InterviewerQCTimelineVM
    {
        public string InterviewerEmail { get; set; }
        public string FromIst { get; set; }
        public string ToIst { get; set; }
        /// <summary>Headline numbers (the interviewer's row); null when the email is unknown.</summary>
        public InterviewerQCRowVM Summary { get; set; }
        public IReadOnlyList<InterviewerQCIrfVM> Irfs { get; set; } = Array.Empty<InterviewerQCIrfVM>();
        public IReadOnlyList<InterviewerQCEventVM> Events { get; set; } = Array.Empty<InterviewerQCEventVM>();
        public bool Truncated { get; set; }
        public string Error { get; set; }
        /// <summary>Underlying exception message; filled only when the app runs in Development (cleared by the controller otherwise).</summary>
        public string ErrorDetail { get; set; }
    }

    public static class InterviewerQCViewLimits
    {
        public const int MaxInterviewers = 3000;
        public const int MaxTimelineEvents = 1000;
    }
}
