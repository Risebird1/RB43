using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    /// <summary>The interviewer's own read-only My Quality page (phase 1).</summary>
    public interface IInterviewerMyQualityService
    {
        /// <summary>Builds the page for the session email. Never throws; sets Error instead.</summary>
        Task<InterviewerMyQualityVM> GetAsync(string sessionEmail, InterviewerInsightsVM lifetime);

        /// <summary>"I've read this" on a coaching session that belongs to the caller.</summary>
        Task<CoachingActionResultVM> AcknowledgeCoachingAsync(int sessionId, string sessionEmail);
    }
}
