using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IPlivoCXCallRepository
    {
        // ── Call logs ────────────────────────────────────────────────────────────
        Task<int>              SaveCallLogAsync(PlivoCXCallLog callLog);
        Task<bool>             UpdateCallLogAsync(PlivoCXCallLog callLog);
        Task<PlivoCXCallLog>   GetCallLogByIdAsync(int id);
        Task<PlivoCXCallLog>   GetCallLogByConversationIdAsync(string conversationId);
        Task<PlivoCXCallLog>   GetCallLogByRequestIdAsync(string requestId);
        Task<List<PlivoCXCallLog>> GetRecentCallLogsAsync(int count = 50);

        // ── Debug logs (raw payload captures) ───────────────────────────────────
        Task<int>                         SaveWebhookDebugLogAsync(PlivoCXWebhookDebugLog log);
        Task<List<PlivoCXWebhookDebugLog>> GetRecentDebugLogsAsync(int count = 50);

        // ── Webhook checkpoints (named processing steps) ─────────────────────────
        Task<int>                              SaveCheckpointAsync(PlivoCXWebhookCheckpoint checkpoint);
        Task<List<PlivoCXWebhookCheckpoint>>   GetRecentCheckpointsAsync(int count = 100);

        // ── Candidate helpers (reuse existing SPs) ───────────────────────────────
        Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForCallsAsync(int count = 10);
        Task IncrementCallAttemptsAsync(int candidateId);
        Task<bool> HasConfirmedInterviewAsync(string candidateEmail, int irfId);
        Task<CandidateDetailsForSchdeule> GetCandidateDetailsForScheduleAsync(int irfId, string candidateEmail);
    }
}
