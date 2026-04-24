using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    /// <summary>
    /// Runs the QC Interview AI merge assistant (same behavior as the <c>QcInterviewAIMergeJob</c> timer).
    /// </summary>
    public interface IQcInterviewAIMergeJobService
    {
        /// <summary>Processes all rows returned by <see cref="IMeetingService.GetPendingQcInterviewAIMerge"/>.</summary>
        Task<(int processed, int failed)> ProcessAllPendingAsync(CancellationToken cancellationToken = default);

        /// <summary>Processes a single interview if it appears in the pending-merge queue.</summary>
        Task<(bool ok, string message)> ProcessInterviewAsync(int interviewId, CancellationToken cancellationToken = default);
    }
}
