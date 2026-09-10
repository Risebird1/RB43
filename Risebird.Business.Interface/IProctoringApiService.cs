using System;
using System.Threading.Tasks;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Interface
{
    public interface IProctoringApiService
    {
        /// <summary>Null means interviewId/candidateEmail didn't validate against a real, joinable interview — caller returns 401.</summary>
        Task<ProctoringMintSessionResponse> MintSessionAsync(ProctoringMintSessionRequest request);

        /// <summary>Null means the session token is unrecognized — caller returns 401.</summary>
        Task<ProctoringSessionResponse> JoinAsync(ProctoringJoinRequest request);

        /// <summary>Null means the token is invalid or the session wasn't joined first — caller returns 401.</summary>
        Task<ProctoringSessionResponse> StartAsync(string sessionToken);

        /// <summary>Null means the session token is unrecognized — caller returns 401.</summary>
        Task<ProctoringHeartbeatResponse> HeartbeatAsync(ProctoringHeartbeatRequest request);

        Task<ProctoringPolicyResponse> GetPolicyAsync();

        /// <summary>Null means the session token is unrecognized — caller returns 401.</summary>
        Task<ProctoringEventsResponse> SubmitEventsAsync(ProctoringEventsRequest request);

        /// <summary>False means the session token is unrecognized — caller returns 401.</summary>
        Task<bool> EndAsync(string sessionToken);

        Task<System.Collections.Generic.IReadOnlyList<ProctoringLogEntry>> GetLogsForCandidateAsync(string candidateEmail);
    }
}
