using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    /// <summary>Item B — evaluates first-interview trial outcomes and writes dbo.TrialFlags.</summary>
    public interface ITrialJudgmentService
    {
        Task<TrialJudgmentResultVm> TryEvaluateAndFlagTrialAsync(
            int interviewId,
            int irfId,
            QcInterviewAIMergeAssistantResponse mergeResult,
            CancellationToken cancellationToken = default);

        /// <summary>Evaluate trial flag using stored QC merge result (no re-merge). For backfill.</summary>
        Task<TrialJudgmentResultVm> TryEvaluateAndFlagTrialForCompletedMergeAsync(
            int interviewId,
            CancellationToken cancellationToken = default);

        /// <summary>Backfill TrialFlags for first interviews that already have QCDone=1 but no flag row.</summary>
        Task<TrialFlagBackfillResultVm> BackfillMissingTrialFlagsAsync(
            int? interviewId = null,
            CancellationToken cancellationToken = default);
    }
}
