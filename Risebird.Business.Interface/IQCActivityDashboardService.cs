using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IQCActivityDashboardService
    {
        /// <summary>
        /// Per-user QC activity for an inclusive IST date range (default last 7 days, max 92).
        /// When <paramref name="restrictToActorEmail"/> is set only that user's row is returned
        /// (a Quality Controller viewing their own activity).
        /// </summary>
        Task<QCActivityDashboardDataVM> GetSummaryAsync(DateTime? fromIst, DateTime? toIst, bool onlyQualityController,
            string restrictToActorEmail = null);

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
    }
}
