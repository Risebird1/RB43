using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInterviewerMyQualityRepository
    {
        /// <summary>usp_GetInterviewerMyQuality: tiles, channels, issues, deductions, coaching, wording — the caller's own rows.</summary>
        Task<InterviewerMyQualityDataVM> GetAsync(string email);

        /// <summary>
        /// usp_AcknowledgeInterviewerCoachingSession. 1 acknowledged now, 0 already acknowledged,
        /// -1 not the caller's session, -2 column not deployed.
        /// </summary>
        Task<int> AcknowledgeCoachingAsync(int sessionId, string email);
    }
}
