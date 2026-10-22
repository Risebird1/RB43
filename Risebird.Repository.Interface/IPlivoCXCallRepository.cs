using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Risebird.Repository.Interface
{
    public interface IPlivoCXCallRepository
    {
        // ── Rich call log view (for the Index page) ──────────────────────────────
        Task<List<PlivoCXCallLogDetailVM>> GetCallLogDetailsAsync(
            int page = 1, int pageSize = 50,
            string status = null, string purpose = null,
            string meeting = null, string disposition = null,
            System.DateTime? dateFrom = null, System.DateTime? dateTo = null,
            string clientName = null);

        // ── Call logs ────────────────────────────────────────────────────────────
        Task<int>              SaveCallLogAsync(PlivoCXCallLog callLog);
        Task<bool>             UpdateCallLogAsync(PlivoCXCallLog callLog);
        Task<PlivoCXCallLog>   GetCallLogByIdAsync(int id);
        Task<PlivoCXCallLog>   GetCallLogByConversationIdAsync(string conversationId);
        Task<PlivoCXCallLog>   GetCallLogByRequestIdAsync(string requestId);
        Task<List<PlivoCXCallLog>> GetRecentCallLogsAsync(int count = 50);

        /// <summary>
        /// Recording backfill: recent call logs (ended within <paramref name="maxAgeHours"/>) that
        /// have a Plivo call UUID but no RecordingUrl yet — e.g. the recording was not final at hangup.
        /// </summary>
        Task<List<PlivoCXCallLog>> GetCallLogsMissingRecordingAsync(int maxAgeHours = 24, int count = 50);

        // ── Debug logs (raw payload captures) ───────────────────────────────────
        Task<int>                         SaveWebhookDebugLogAsync(PlivoCXWebhookDebugLog log);
        Task<List<PlivoCXWebhookDebugLog>> GetRecentDebugLogsAsync(int count = 50);

        // ── Webhook checkpoints (named processing steps) ─────────────────────────
        Task<int>                              SaveCheckpointAsync(PlivoCXWebhookCheckpoint checkpoint);
        Task<List<PlivoCXWebhookCheckpoint>>   GetRecentCheckpointsAsync(int count = 100);

        // ── Candidate helpers ─────────────────────────────────────────────────────
        // Eligibility (usp_GetEligibleCandidatesForPlivoCX*). The defaults are the timer jobs' behaviour.
        //   irfId / candidateId       : restrict to one IRF / candidate and return every match (not TOP count)
        //   bypassCaps                : skip the pacing caps (a campaign paces itself from its own policy)
        //   excludeCampaignCandidates : leave out candidates Pending in an open PlivoCX campaign
        Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForCallsAsync(
            int count = 10, int? irfId = null, int? candidateId = null,
            bool bypassCaps = false, bool excludeCampaignCandidates = false);
        Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForRescheduleAsync(
            int count = 10, int? irfId = null, int? candidateId = null,
            bool bypassCaps = false, bool excludeCampaignCandidates = false);
        Task IncrementCallAttemptsAsync(int candidateId);
        Task<bool> HasConfirmedInterviewAsync(string candidateEmail, int irfId);
        Task<CandidateDetailsForSchdeule> GetCandidateDetailsForScheduleAsync(int irfId, string candidateEmail);

        /// <summary>
        /// Fallback: look up a candidate's email from UserList using the Candidate.id
        /// (the id stored in PlivoCXCallLogs.CandidateId).
        /// Used when candidate_email is missing from the Plivo CX hangup payload.
        /// </summary>
        Task<string> GetCandidateEmailByIdAsync(int candidateId);

        /// <summary>
        /// Writes the AI-assessed disposition back to Candidate.PlivoCXDisposition.
        /// Called after every completed Plivo CX call.
        /// </summary>
        Task UpdateCandidateDispositionAsync(int candidateId, string disposition);

        Task<List<string>> GetAllClientNamesAsync();
    }
}
