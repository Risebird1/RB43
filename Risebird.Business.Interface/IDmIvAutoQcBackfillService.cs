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
