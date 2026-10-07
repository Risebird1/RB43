using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IQCActivityDashboardRepository
    {
        /// <summary>usp_GetQCActivityDashboardSummary: per-user rows + "open right now" backlog.</summary>
        Task<(IReadOnlyList<QCActivitySummaryRowVM> Rows, QCActivityBacklogVM Backlog)> GetSummaryAsync(
            DateTime fromIst, DateTime toIst, bool onlyQualityController, string actorEmail = null);

        /// <summary>usp_GetQCActivityDashboardDetails: records behind one user's number for one metric (top 2000).</summary>
        Task<IReadOnlyList<QCActivityDetailRowVM>> GetDetailsAsync(
            string actorEmail, string metric, DateTime fromIst, DateTime toIst);

        /// <summary>usp_GetInterviewerQCView: one row per interviewer (top 3000), or just <paramref name="interviewerEmail"/>.</summary>
        Task<IReadOnlyList<InterviewerQCRowVM>> GetInterviewerViewAsync(
            DateTime fromIst, DateTime toIst, string search = null, string interviewerEmail = null);

        /// <summary>usp_GetInterviewerQCTimeline: the interviewer's IRFs and dated events (top 1000).</summary>
        Task<(IReadOnlyList<InterviewerQCIrfVM> Irfs, IReadOnlyList<InterviewerQCEventVM> Events)> GetInterviewerTimelineAsync(
            string interviewerEmail, DateTime fromIst, DateTime toIst);
    }
}
