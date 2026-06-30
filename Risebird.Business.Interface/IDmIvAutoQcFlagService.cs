using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IDmIvAutoQcFlagService
    {
        /// <summary>
        /// Forward trigger after QCMergedAIDetails verdict finalization. Sets bad-quality flag only — no active/vet changes.
        /// </summary>
        Task TryApplyAutoQcFlagAsync(int interviewId, int irfId, string interviewerSuspicious,
            bool? redoInterview, CancellationToken cancellationToken = default);

        Task<bool> ApplyAutoQcFlagForContextAsync(TransInterviewerInterviewContext ctx, int triggeringInterviewId,
            string verdict, AutoQcFlagOptions options, CancellationToken cancellationToken = default);
    }
}
