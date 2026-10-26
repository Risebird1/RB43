using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    /// <summary>
    /// Interview alignment (the interviewer's pre-call page, switched on per channel by
    /// IRF.IsInterviewerAlignmentRequired) and the per-question code sessions minted from it. Callers are
    /// responsible for the ownership check (IMeetingService.IsInterviewerForInterview) and the rollout
    /// check (IsAlignmentRequiredAsync); the stored procedures re-check ownership for writes.
    /// </summary>
    public interface IInterviewAlignmentService
    {
        /// <summary>
        /// The rollout switch for one interview, read server-side from that interview's own channel - never
        /// from anything the caller supplies. False when off, NULL, or unreadable.
        /// </summary>
        Task<bool> IsAlignmentRequiredAsync(int interviewId);

        /// <summary>
        /// Switches the alignment page on or off for one channel. Writes only IRF.IsInterviewerAlignmentRequired:
        /// it never re-saves the channel, so interviewers' active status and interview assignments are untouched.
        /// True when saved, false when there is no such channel, null when the database call failed.
        /// </summary>
        Task<bool?> SetAlignmentRequiredAsync(int irfId, bool isRequired);

        /// <summary>
        /// Builds the alignment page model. Returns null if anything needed is unavailable (for example the
        /// database objects are not deployed yet, or the channel's switch is off), and the interviewer then
        /// gets today's Interview Details.
        /// </summary>
        Task<InterviewAlignmentViewModel> BuildAlignmentAsync(int interviewId, string interviewerEmail, Meeting meeting,
            int defaultCodingTimeLimitInMinutes, int maxQuestionLength);

        Task<bool> AcknowledgeTrialOnboardingAsync(string interviewerEmail);

        Task<bool> SaveJoinGateOverrideAsync(int interviewId, string interviewerEmail, IEnumerable<string> outstandingRequirements);

        Task<(IList<InterviewerCodeQuestion> Questions, IList<InterviewCodeSession> Sessions)> GetCodeQuestionsAsync(int interviewId, string interviewerEmail);

        Task<CreateInterviewCodeSessionResult> CreateCodeSessionAsync(string interviewerEmail, CreateCodeSessionRequestVM request);

        /// <summary>The candidate page for a token, or null when the token does not exist.</summary>
        Task<CodeSessionPageViewModel> GetCandidatePageAsync(Guid token);

        /// <summary>Runs the candidate's code for a live session and persists what came back.</summary>
        Task<(bool Accepted, string Message, CodeExecutionResult Result)> RunAsync(CodeSessionRunRequestVM request);

        Task<(bool Accepted, string Message, DateTime? SubmittedOn)> SubmitAsync(CodeSessionSubmitRequestVM request);

        Task<IList<InterviewCodeSession>> GetSessionsForReportAsync(int interviewId);

        /// <summary>True while a token's interview has not yet ended (plus grace). Exposed for tests.</summary>
        bool IsSessionLive(InterviewCodeSession session, DateTime now);
    }
}
