using Risebird.Entities.ViewModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IQCActivityDashboardService
    {
        /// <summary>
        /// Per-user QC activity for an inclusive IST date range (default last 7 days, max 92).
        /// When <paramref name="restrictToActorEmail"/> is set only that user's row is returned
        /// (a Quality Controller viewing their own activity).
        /// A cancelled <paramref name="cancellationToken"/> stops the query and is not logged as a load error.
        /// </summary>
        Task<QCActivityDashboardDataVM> GetSummaryAsync(DateTime? fromIst, DateTime? toIst, bool onlyQualityController,
            string restrictToActorEmail = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records behind one number. Unknown metric keys return an empty list without querying.
        /// When <paramref name="restrictToActorEmail"/> is set it replaces <paramref name="actorEmail"/>.
        /// </summary>
        Task<QCActivityDetailsResultVM> GetDetailsAsync(string actorEmail, string metric, DateTime? fromIst, DateTime? toIst,
            string restrictToActorEmail = null);

        /// <summary>
        /// Interviewer QC View list: one row per interviewer (top 3000 by attention), with tile totals.
        /// <paramref name="minInterviews"/> defaults to 3 and is clamped to 1..50; <paramref name="includeOpenFlags"/>
        /// also lists interviewers whose only signal is an older open flag.
        /// </summary>
        Task<InterviewerQCListResultVM> GetInterviewerViewAsync(DateTime? fromIst, DateTime? toIst, string search = null,
            int? minInterviews = null, bool includeOpenFlags = false);

        /// <summary>Data check for the QC Activity page: per source, who the rows in the range are stored under.</summary>
        Task<QCActivityDiagnosticsResultVM> GetDiagnosticsAsync(DateTime? fromIst, DateTime? toIst);

        /// <summary>Side panel for one interviewer: headline numbers, IRFs and the dated timeline.</summary>
        Task<InterviewerQCTimelineVM> GetInterviewerTimelineAsync(string interviewerEmail, DateTime? fromIst, DateTime? toIst);

        /// <summary>
        /// One report edit with the full text before and after (ReportEditLog). Returns null for an id of 0 or less
        /// (without querying) and when the edit does not exist; logs and rethrows on failure. Access checks are the caller's.
        /// </summary>
        Task<ReportEditVM> GetReportEditAsync(int id);

        /// <summary>
        /// Flags panel (bursts, reviewed with no call, reversed by another QC, short calls, kept despite a P0,
        /// actions without evidence). <paramref name="actorEmail"/> null = everyone; when
        /// <paramref name="restrictToActorEmail"/> is set only that person's flags are returned.
        /// </summary>
        Task<QCActivityFlagsResultVM> GetFlagsAsync(string actorEmail, DateTime? fromIst, DateTime? toIst, string restrictToActorEmail = null);

        /// <summary>High-Risk interviewers in the window, how many any QC person touched, and the untouched list (top 50).</summary>
        Task<QCActivityCoverageVM> GetCoverageAsync(DateTime? fromIst, DateTime? toIst);
    }
}
