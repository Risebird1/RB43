using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IAutoQcRemovalMonitorRepository
    {
        Task<AutoQcRemovalMonitorVm> GetMonitorDataAsync(DateTime? fromUtc, DateTime? toUtc, int? irfId, string source, string verdict);

        Task<int> GetBackfillCandidateCountAsync(DateTime? asOfUtc = null, int windowDays = 60);

        Task<IReadOnlyList<AutoQcBackfillCandidateRow>> GetBackfillCandidatesAsync(DateTime? asOfUtc = null, int windowDays = 60);

        Task<(bool HasRun, Guid? LatestBatchId, DateTime? LatestExecutedAtUtc)> GetBackfillRunStatusAsync();

        Task RecordBackfillRunAsync(Guid batchId, int recordsRemoved, int recordsSkipped, int triggeringInterviewCount, string executedBy);

        Task<AutoQcBackfillRunSummaryVm> GetLatestBackfillSummaryAsync();
    }
}
