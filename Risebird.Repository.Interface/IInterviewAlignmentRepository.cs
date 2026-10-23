using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>
    /// Interview alignment page (Interview/InterviewAlignment, behind IRF.IsInterviewerAlignmentRequired) and
    /// the per-question code sessions minted from it. Every read returns null / empty on failure so the
    /// interviewer falls back to today's Interview Details.
    /// </summary>
    public interface IInterviewAlignmentRepository
    {
        /// <summary>IRF.IsInterviewerAlignmentRequired for the interview's own channel; false when off, NULL or unknown.</summary>
        Task<bool> IsAlignmentRequiredAsync(int interviewId);

        /// <summary>
        /// Writes IRF.IsInterviewerAlignmentRequired for one channel and nothing else (usp_SetInterviewerAlignmentRequired).
        /// True when the channel row was updated, false when no channel has that id, null when the database call failed
        /// (for example usp_SetInterviewerAlignmentRequired is not deployed yet).
        /// </summary>
        Task<bool?> SetAlignmentRequiredAsync(int irfId, bool isRequired);

        Task<InterviewAlignmentContext> GetAlignmentContextAsync(int interviewId, string interviewerEmail);

        /// <summary>
        /// The interviewer's own flagged past interviews on this interview's channel (read-only,
        /// usp_GetInterviewAlignmentFlaggedInterviews). Null when the database call failed.
        /// </summary>
        Task<IList<AlignmentFlaggedInterview>> GetFlaggedInterviewsAsync(int interviewId, string interviewerEmail);

        Task<bool> AcknowledgeTrialOnboardingAsync(string interviewerEmail, string policyVersion, DateTime now);

        Task<bool> SaveJoinGateOverrideAsync(int interviewId, string interviewerEmail, string outstandingRequirements, DateTime now);

        Task<IList<InterviewerCodeQuestion>> GetInterviewerCodeQuestionsAsync(int interviewId, string interviewerEmail);

        Task<CreateInterviewCodeSessionResult> CreateCodeSessionAsync(int interviewId, string interviewerEmail, int questionId,
            string questionText, string skillName, int? timeLimitInMinutes, Guid token, DateTime now);

        Task<IList<InterviewCodeSession>> GetCodeSessionsAsync(int interviewId);

        Task<InterviewCodeSession> GetCodeSessionByTokenAsync(Guid token);

        Task<DateTime?> MarkCodeSessionOpenedAsync(Guid token, DateTime now);

        Task<bool> SaveCodeSessionRunAsync(Guid token, string languageId, string source, string output, string status, DateTime now);

        Task<bool> SubmitCodeSessionAsync(Guid token, string languageId, string source, DateTime now);
    }
}
