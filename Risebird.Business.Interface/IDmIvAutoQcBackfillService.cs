using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IDmIvAutoQcBackfillService
    {
        Task<AutoQcPerIrfBackfillPreviewVm> GetPerIrfBackfillPreviewAsync(int irfId, DateTime fromUtc, DateTime toUtc);

        Task<AutoQcPerIrfBackfillResultVm> RunPerIrfBackfillAsync(int irfId, DateTime fromUtc, DateTime toUtc, string executedBy,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<IrfAuditDirectoryRowVm>> GetIrfAuditDirectoryAsync(DateTime fromIst, DateTime toIst, string ownerEmail = null, string profileName = null);

        Task<(IReadOnlyList<IrfAuditInterviewerRiskRowVm> Active, IReadOnlyList<IrfAuditInterviewerRiskRowVm> Removed)> GetIrfAuditInterviewerRiskBreakdownAsync(int irfId, DateTime fromIst, DateTime toIst);

        Task<IReadOnlyList<IrfAuditInterviewerRiskDetailVm>> GetIrfAuditInterviewerRiskInterviewDetailsAsync(int irfId, DateTime fromIst, DateTime toIst, int? transInterviewerId = null);

        Task<IrfAuditWindowQualityVm> GetIrfAuditWindowQualityAsync(int irfId, DateTime fromIst, DateTime toIst);

        /// <summary>IRF Channel Owner email — used for HR/SeniorHR ownership checks on Keep/Remove actions.</summary>
        Task<string> GetIrfChannelOwnerEmailAsync(int irfId);

        Task<InterviewerRiskOverviewPageVm> GetInterviewerRiskOverviewAsync(
            DateTime fromIst, DateTime toIst, string ownerEmail, decimal? minRiskRatio,
            bool pendingReviewOnly, int offset, int pageSize, string sortBy = null, bool needsCall = false);

        Task<IReadOnlyList<InterviewerRiskOverviewChannelRowVm>> GetInterviewerRiskOverviewChannelsAsync(
            int interviewerId, DateTime fromIst, DateTime toIst, string ownerEmail);

        Task<IReadOnlyList<InterviewerRiskOverviewRowVm>> GetInterviewerRiskOverviewExportRowsAsync(
            DateTime fromIst, DateTime toIst, string ownerEmail, decimal? minRiskRatio, bool pendingReviewOnly,
            string sortBy = null, bool needsCall = false);

        /// <summary>QC coverage per High-Risk interviewer (QC Tracking Delta 2); null when the procedure is missing.</summary>
        Task<IReadOnlyList<InterviewerQcCoverageRowVm>> GetInterviewerQcCoverageAsync(IReadOnlyCollection<int> userIds, int days = 90);

        /// <summary>High-Risk Reports tab (QC Tracking Delta 4).</summary>
        Task<HighRiskReportsPageVm> GetHighRiskReportsAsync(
            DateTime fromIst, DateTime toIst, string ownerEmail, string filter, int? interviewerId, int? irfId, int offset, int pageSize);

        /// <summary>PLI / PQI signals per interviewer and channel (QC Tracking Delta 4 / 5).</summary>
        Task<InterviewerProcessSignalsResultVm> GetInterviewerProcessSignalsAsync(
            IReadOnlyCollection<int> userIds, DateTime fromIst, DateTime toIst, string ownerEmail);

        /// <param name="ignoreMassFireGuard">
        /// When true, skip the mass-fire circuit breaker (deliberate re-grade after a check fix).
        /// </param>
        /// <param name="executedBy">Session email/profile — stamped on dbo.IrfAuditRiskBackfillRun.</param>
        Task<IrfAuditRiskBackfillResultVm> BackfillRiskCategoryForIrfAsync(int irfId, DateTime fromIst, DateTime toIst,
            bool ignoreMassFireGuard = false,
            string executedBy = null,
            CancellationToken cancellationToken = default);
    }
}
