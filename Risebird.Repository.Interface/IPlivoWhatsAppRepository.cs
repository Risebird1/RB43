using Risebird.Entities.Data.PlivoWhatsAppModels;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IPlivoWhatsAppRepository
    {
        // ── Log CRUD ──────────────────────────────────────────────────────────
        Task<int>                       SaveLogAsync(PlivoWhatsAppLog log);
        Task<bool>                      UpdateLogAsync(PlivoWhatsAppLog log);
        Task<PlivoWhatsAppLog>          GetLogByIdAsync(int id);
        Task<PlivoWhatsAppLog>          GetLogByConversationIdAsync(string conversationId);
        Task<List<PlivoWhatsAppLog>>    GetRecentLogsAsync(int count = 50);

        // ── Debug logs (raw trigger request/response + callback payloads) ────
        Task<int>                           SaveDebugLogAsync(PlivoWhatsAppDebugLog log);
        Task<List<PlivoWhatsAppDebugLog>>   GetRecentDebugLogsAsync(int count = 50);

        // ── Processing checkpoints ────────────────────────────────────────────
        Task<int>                               SaveCheckpointAsync(PlivoWhatsAppCheckpoint checkpoint);
        Task<List<PlivoWhatsAppCheckpoint>>     GetRecentCheckpointsAsync(int count = 100);

        // ── Dashboard view ────────────────────────────────────────────────────
        Task<List<PlivoWhatsAppLogDetailVM>> GetLogDetailsAsync(
            int page = 1, int pageSize = 50,
            string status = null, string purpose = null,
            string meeting = null, string disposition = null);

        // ── Candidate eligibility ─────────────────────────────────────────────
        Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForScheduleAsync(int count = 10);
        Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForRescheduleAsync(int count = 10);

        // ── Shared helpers (reuse same SPs/queries as voice channel) ──────────
        Task<Risebird.Entities.Data.CandidateDetailsForSchdeule>
            GetCandidateDetailsForScheduleAsync(int irfId, string candidateEmail);
        Task<bool>   HasConfirmedInterviewAsync(string candidateEmail, int irfId);
        Task<string> GetCandidateEmailByIdAsync(int candidateId);
        Task         UpdateCandidateDispositionAsync(int candidateId, string disposition);
        Task         IncrementCallAttemptsAsync(int candidateId);
    }
}
