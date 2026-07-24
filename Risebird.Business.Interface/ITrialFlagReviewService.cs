using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ITrialFlagReviewService
    {
        /// <summary>Page shell only — diagnostics + tab counts; no list query.</summary>
        Task<TrialFlagReviewPageVm> GetPageShellAsync(string tab);

        /// <summary>Legacy alias for shell (list is AJAX).</summary>
        Task<TrialFlagReviewPageVm> GetPageAsync(string tab, bool refreshMetrics = false);

        Task<TrialFlagReviewListResultVm> GetListAsync(TrialFlagReviewListRequest request);

        Task<TrialFlagReviewRowVm> GetDetailAsync(int trialFlagId);

        Task<TrialFlagReviewActionResultVm> KeepOnIrfAsync(int trialFlagId, string reviewerEmail, string comment);

        Task<TrialFlagReviewActionResultVm> RemoveFromIrfAsync(int trialFlagId, string reviewerEmail, string comment);

        Task<IReadOnlyList<InterviewAiRiskSummaryRowVm>> GetInterviewAiRiskSummaryAsync(int interviewId);

        /// <summary>Optional: recompute risk for interview ids on the current list page.</summary>
        Task RefreshLiveMetricsForInterviewIdsAsync(IReadOnlyList<int> interviewIds);
    }
}
