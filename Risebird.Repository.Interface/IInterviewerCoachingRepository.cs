using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInterviewerCoachingRepository
    {
        /// <summary>usp_GetInterviewerExposure: profile, risk checks, Payment SLA deductions, trial flags, channel standing.</summary>
        Task<InterviewerExposureVM> GetExposureAsync(string interviewerEmail, DateTime fromIst, DateTime toIst);

        /// <summary>
        /// usp_SaveInterviewerCoachingSession. Result is the new id, -1 when an email summary is inside the
        /// cooldown, -2 when the interviewer email is unknown.
        /// </summary>
        Task<CoachingSaveResultVM> SaveSessionAsync(string interviewerEmail, string coachEmail, string channel, string outcome,
            string themes, string agreed, string emailSubject, int? issueCount, DateTime? fromIst, DateTime? toIst,
            bool linkRecentCall);

        /// <summary>
        /// Removes an Email session saved moments ago when the email itself then failed to send, so the
        /// 12-hour cooldown is not used up by a mail that never went out.
        /// </summary>
        Task<bool> CancelEmailSessionAsync(int sessionId);
    }
}
