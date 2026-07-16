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

        Task<AutoQcPerIrfBackfillPreviewVm> GetPerIrfBackfillPreviewAsync(int irfId, DateTime fromUtc, DateTime toUtc);

        Task<IReadOnlyList<AutoQcBackfillCandidateRow>> GetPerIrfBackfillCandidatesAsync(int irfId, DateTime fromUtc, DateTime toUtc);

        Task RecordPerIrfBackfillRunAsync(Guid runId, int irfId, DateTime fromUtc, DateTime toUtc, int verdictsScanned, int newFlagsSet, int alreadyFlaggedSkipped, string executedBy);

        Task<IReadOnlyList<IrfAuditDirectoryRowVm>> GetIrfAuditDirectoryAsync(DateTime fromIst, DateTime toIst);

        Task<IReadOnlyList<IrfAuditInterviewerRiskRowVm>> GetIrfAuditInterviewerRiskBreakdownAsync(int irfId, DateTime fromIst, DateTime toIst);

        Task<IReadOnlyList<IrfAuditInterviewerRiskDetailVm>> GetIrfAuditInterviewerRiskInterviewDetailsAsync(int irfId, DateTime fromIst, DateTime toIst, int? transInterviewerId = null);

        Task<IReadOnlyList<int>> GetRiskCategoryBackfillCandidatesByIrfAsync(int irfId, DateTime fromIst, DateTime toIst, int maxRows);

        Task<int> SyncTrialFlagsVerdictFromRiskCategoryAsync();
    }
}
