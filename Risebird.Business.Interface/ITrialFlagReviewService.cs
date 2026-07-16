using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ITrialFlagReviewService
    {
        Task<TrialFlagReviewPageVm> GetPageAsync(string tab, bool refreshMetrics = false);

        Task<TrialFlagReviewActionResultVm> KeepOnIrfAsync(int trialFlagId, string reviewerEmail, string comment);

        Task<TrialFlagReviewActionResultVm> RemoveFromIrfAsync(int trialFlagId, string reviewerEmail, string comment);

        Task<IReadOnlyList<InterviewAiRiskSummaryRowVm>> GetInterviewAiRiskSummaryAsync(int interviewId);
    }
}
