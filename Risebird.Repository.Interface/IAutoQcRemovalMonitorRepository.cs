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

        Task<IReadOnlyList<IrfAuditDirectoryRowVm>> GetIrfAuditDirectoryAsync(DateTime fromIst, DateTime toIst, string ownerEmail = null, string profileName = null);

        /// <summary>Active interviewers (with ReviewState) + removed-in-window interviewers, split by IsRemoved.</summary>
        Task<(IReadOnlyList<IrfAuditInterviewerRiskRowVm> Active, IReadOnlyList<IrfAuditInterviewerRiskRowVm> Removed)> GetIrfAuditInterviewerRiskBreakdownAsync(int irfId, DateTime fromIst, DateTime toIst);

        Task<IReadOnlyList<IrfAuditInterviewerRiskDetailVm>> GetIrfAuditInterviewerRiskInterviewDetailsAsync(int irfId, DateTime fromIst, DateTime toIst, int? transInterviewerId = null);

        Task<IReadOnlyList<int>> GetRiskCategoryBackfillCandidatesByIrfAsync(int irfId, DateTime fromIst, DateTime toIst, int maxRows);

        Task<int> SyncTrialFlagsVerdictFromRiskCategoryAsync();

        /// <summary>Interview ids (subset of <paramref name="interviewIds"/>) that already have a RiskCategory row. Used to classify insert vs. update vs. delete around a risk-write batch.</summary>
        Task<IReadOnlyList<int>> GetExistingRiskCategoryInterviewIdsAsync(IEnumerable<int> interviewIds);

        /// <summary>Persists an idempotent stamp of a per-IRF risk-category backfill run (creates dbo.IrfAuditRiskBackfillRun if missing).</summary>
        Task RecordRiskBackfillRunAsync(int irfId, DateTime fromIst, DateTime toIst, int scanned, int processed, int syncUpdates, string executedBy, string message);

        /// <summary>IRF.RisebirdHr → UserList.EmailId — used for HR/SeniorHR ownership checks on Keep/Remove actions.</summary>
        Task<string> GetIrfChannelOwnerEmailAsync(int irfId);

        /// <summary>Window quality strip (TotalCompleted/BadInterviews/BadRatio/P0..P5) for the IRF Audit drill-down.</summary>
        Task<IrfAuditWindowQualityVm> GetIrfAuditWindowQualityAsync(int irfId, DateTime fromIst, DateTime toIst);

        Task<InterviewerRiskOverviewPageVm> GetInterviewerRiskOverviewAsync(
            DateTime fromIst, DateTime toIst, string ownerEmail, decimal? minRiskRatio,
            bool pendingReviewOnly, int offset, int pageSize);

        Task<IReadOnlyList<InterviewerRiskOverviewChannelRowVm>> GetInterviewerRiskOverviewChannelsAsync(
            int interviewerId, DateTime fromIst, DateTime toIst, string ownerEmail);

        Task<IReadOnlyList<InterviewerRiskOverviewRowVm>> GetInterviewerRiskOverviewExportRowsAsync(
            DateTime fromIst, DateTime toIst, string ownerEmail, decimal? minRiskRatio, bool pendingReviewOnly);
    }
}
