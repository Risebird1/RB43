using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IInterviewerCoachingService
    {
        /// <summary>Everything to raise with one interviewer in one call, grouped by the behaviour to change.</summary>
        Task<CoachingBriefVM> GetBriefAsync(string interviewerEmail, DateTime? fromIst, DateTime? toIst);

        /// <summary>The exact email <see cref="SendEmailAsync"/> would send.</summary>
        Task<CoachingEmailPreviewVM> PreviewEmailAsync(CoachingEmailRequestVM request);

        /// <summary>Sends the summary (at most one per interviewer per 12 hours) and records it as a coaching session.</summary>
        Task<CoachingActionResultVM> SendEmailAsync(CoachingEmailRequestVM request, string coachEmail);

        /// <summary>Records a coaching call / meeting. Links the coach's call to the interviewer in the last 3 hours.</summary>
        Task<CoachingActionResultVM> LogSessionAsync(LogCoachingSessionRequestVM request, string coachEmail);
    }
}
