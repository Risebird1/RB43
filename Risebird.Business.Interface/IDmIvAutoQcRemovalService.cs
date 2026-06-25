using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IDmIvAutoQcRemovalService
    {
        /// <summary>
        /// Item 59: when QC merge verdict is suspicious or redo, deactivate interviewer on the skill channel
        /// and record audit + AutoQc fields. No-op when verdict does not require removal.
        /// </summary>
        Task TryApplyAutoQcRemovalAsync(int interviewId, int irfId, string interviewerSuspicious, bool? redoInterview,
            CancellationToken cancellationToken = default);

        Task<bool> ApplyAutoQcRemovalForContextAsync(TransInterviewerInterviewContext ctx, int triggeringInterviewId,
            string verdict, AutoQcRemovalOptions options, CancellationToken cancellationToken = default);
    }
}
