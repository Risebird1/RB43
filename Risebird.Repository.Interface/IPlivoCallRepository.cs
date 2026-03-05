using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoModels;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>
    /// Repository interface for Plivo call log and assistant config persistence
    /// </summary>
    public interface IPlivoCallRepository
    {
        // ── Call logs ────────────────────────────────────────────────────────────
        Task<int> SaveCallLogAsync(PlivoCallLog callLog);
        Task<bool> UpdateCallLogAsync(PlivoCallLog callLog);
        Task<PlivoCallLog> GetCallLogByIdAsync(int id);
        Task<PlivoCallLog> GetCallLogByCallUUIDAsync(string callUUID);
        Task<PlivoCallLog> GetCallLogByRequestUUIDAsync(string requestUUID);
        Task<List<PlivoCallLog>> GetCallLogsByInterviewIdAsync(int interviewId);
        Task<List<PlivoCallLog>> GetCallLogsByRecipientIdAsync(int recipientId);
        Task<List<PlivoCallLog>> GetRecentCallLogsAsync(int count = 50);
        Task<bool> UpdateBookingStatusAsync(string callUUID, bool isBooked, DateTime? bookedDateTime, string confirmationDetails);

        // ── Assistant config ─────────────────────────────────────────────────────
        /// <summary>Returns the active assistant config (IsActive = 1)</summary>
        Task<PlivoAssistantConfig> GetActiveAssistantConfigAsync();
        Task<PlivoAssistantConfig> GetAssistantConfigByNameAsync(string name);
        Task<List<PlivoAssistantConfig>> GetAllAssistantConfigsAsync();
        Task<int> SaveAssistantConfigAsync(PlivoAssistantConfig config);
        Task<bool> UpdateAssistantConfigAsync(PlivoAssistantConfig config);
        Task<bool> SetActiveAssistantAsync(int id);

        // ── Candidate helpers ────────────────────────────────────────────────────
        Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForCallsAsync(int count = 10);
        Task IncrementCallAttemptsAsync(int candidateId);
        Task<bool> HasConfirmedInterviewAsync(string candidateEmail, int irfId);
        Task<CandidateDetailsForSchdeule> GetCandidateDetailsForScheduleAsync(int irfId, string candidateEmail);
    }
}
