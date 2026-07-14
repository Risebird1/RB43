using Risebird.Business.Interface;
using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ITrialFlagReviewService
    {
        Task<TrialFlagReviewPageVm> GetPageAsync(string tab, bool refreshMetrics = false);

        Task<TrialFlagReviewActionResultVm> KeepOnIrfAsync(int trialFlagId, string reviewerEmail, string comment);

        Task<TrialFlagReviewActionResultVm> RemoveFromIrfAsync(int trialFlagId, string reviewerEmail, string comment);
    }
}
