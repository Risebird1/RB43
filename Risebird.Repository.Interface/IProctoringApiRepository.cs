using System.Collections.Generic;
using System.Threading.Tasks;
using Risebird.Entities.Data;

namespace Risebird.Repository.Interface
{
    public interface IProctoringApiRepository
    {
        /// <summary>
        /// Validates a mint request's (interviewId, candidateEmail) pair against
        /// InterviewMeetings/InterviewComments — this is the credential check for the
        /// unauthenticated /mint endpoint, standing in for the bearer token a candidate can't supply.
        /// </summary>
        Task<ProctoringCandidateInterviewValidation> ValidateCandidateInterviewAsync(int interviewId, string candidateEmail);

        /// <summary>Creates the session row a launch token was minted for (state = idle). Makes the session token "recognized".</summary>
        Task<bool> RegisterSessionAsync(ProctoringApiSession session);

        Task<ProctoringApiSession> GetSessionByTokenAsync(string sessionToken);

        Task<bool> MarkSessionJoinedAsync(string sessionToken, string candidateEmail);

        Task<bool> MarkSessionStartedAsync(string sessionToken);

        Task<bool> UpdateHeartbeatAsync(string sessionToken, string clientState);

        Task<bool> MarkSessionEndedAsync(string sessionToken);

        /// <summary>Idempotent on (SessionToken, Sequence) — returns false if this event was already stored (redelivered batch).</summary>
        Task<bool> InsertLogIfNewAsync(ProctoringApiLog logEntry);

        /// <summary>Applies a risk-score delta (from newly inserted events) and returns the new clamped [0,100] score.</summary>
        Task<int> AddRiskScoreAsync(string sessionToken, int delta);

        Task<IReadOnlyList<ProctoringApiLog>> GetLogsForCandidateAsync(string candidateEmail);
    }
}
