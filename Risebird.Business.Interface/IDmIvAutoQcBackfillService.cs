using Risebird.Entities.ViewModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IDmIvAutoQcBackfillService
    {
        Task<int> GetPreDeploymentCandidateCountAsync(DateTime? asOfUtc = null, int windowDays = 60);

        Task<AutoQcBackfillResultVm> RunBackfillAsync(string executedBy, bool forceRerun = false, CancellationToken cancellationToken = default);

        Task<(bool HasRun, Guid? LatestBatchId, DateTime? LatestExecutedAtUtc)> GetRunStatusAsync();
    }
}
