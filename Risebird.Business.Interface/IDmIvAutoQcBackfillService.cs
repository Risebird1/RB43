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

        Task<IReadOnlyList<IrfAuditDirectoryRowVm>> GetIrfAuditDirectoryAsync(DateTime fromIst, DateTime toIst);

        Task<IReadOnlyList<IrfAuditInterviewerRiskRowVm>> GetIrfAuditInterviewerRiskBreakdownAsync(int irfId, DateTime fromIst, DateTime toIst);

        Task<IReadOnlyList<IrfAuditInterviewerRiskDetailVm>> GetIrfAuditInterviewerRiskInterviewDetailsAsync(int irfId, DateTime fromIst, DateTime toIst, int? transInterviewerId = null);

        /// <param name="ignoreMassFireGuard">
        /// When true, skip the mass-fire circuit breaker (deliberate re-grade after a check fix).
        /// </param>
        Task<IrfAuditRiskBackfillResultVm> BackfillRiskCategoryForIrfAsync(int irfId, DateTime fromIst, DateTime toIst,
            bool ignoreMassFireGuard = false,
            CancellationToken cancellationToken = default);
    }
}
