using Risebird.Entities.Data.RbAPIDataModels;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>
    /// Call opt-out / opt-in (UCC consent) and pre-dial checks. Interviewer opt-outs are platform level,
    /// candidate opt-outs are IRF level; the two never touch each other. Opt-out writes throw on a DB error
    /// so the webhook can return 500 and the provider retries.
    /// </summary>
    public interface ICallPreferenceRepository
    {
        Task<CallOptOutResult> RecordInterviewerCallOptOut(InterviewerCallOptOutRequest request);

        Task<CallOptOutResult> RecordCandidateCallOptOut(CandidateCallOptOutRequest request);

        /// <param name="personType">Interviewer | Candidate</param>
        /// <param name="irfId">IRF.id; required for candidates</param>
        Task<CanCallResult> CanCallContact(string personType, string phone, int? irfId = null);

        Task<CallConsentResult> RecordInterviewerCallConsent(int userId, string ipAddress, string userAgent);

        Task<bool> UndoInterviewerCallOptOut(int userId, string performedBy);
    }
}
