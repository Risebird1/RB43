using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>
    /// Repository interface for Vapi call operations
    /// </summary>
    public interface IVapiCallRepository
    {
        /// <summary>
        /// Saves a new Vapi call log
        /// </summary>
        Task<int> SaveCallLogAsync(VapiCallLog callLog);

        /// <summary>
        /// Updates an existing Vapi call log
        /// </summary>
        Task<bool> UpdateCallLogAsync(VapiCallLog callLog);

        /// <summary>
        /// Gets call log by Vapi call ID
        /// </summary>
        Task<VapiCallLog> GetCallLogByVapiCallIdAsync(string vapiCallId);

        /// <summary>
        /// Gets call log by internal ID
        /// </summary>
        Task<VapiCallLog> GetCallLogByIdAsync(int id);

        /// <summary>
        /// Gets all call logs for an interview
        /// </summary>
        Task<List<VapiCallLog>> GetCallLogsByInterviewIdAsync(int interviewId);

        /// <summary>
        /// Gets all call logs for a recipient
        /// </summary>
        Task<List<VapiCallLog>> GetCallLogsByRecipientIdAsync(int recipientId);

        /// <summary>
        /// Gets recent call logs
        /// </summary>
        Task<List<VapiCallLog>> GetRecentCallLogsAsync(int count = 50);

        /// <summary>
        /// Updates call booking status
        /// </summary>
        Task<bool> UpdateBookingStatusAsync(string vapiCallId, bool isBooked, DateTime? bookedDateTime, string confirmationDetails);
        Task<CandidateDetailsForSchdeule> GetCandidateDetailsForSchedule(int irfid, string CandidateEmail);

        /// <summary>
        /// Gets eligible candidates for Vapi automated interview calls
        /// Returns candidates who need interview booking calls (max 2 attempts per candidate)
        /// </summary>
        Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForVapiCallsAsync(int count = 10);

        /// <summary>
        /// Atomically increments Candidate.VapiCallAttempts for the given candidate.
        /// Must be called immediately after every successful Vapi call initiation.
        /// </summary>
        Task IncrementVapiCallAttemptsAsync(int candidateId);

        /// <summary>
        /// Returns true if a confirmed interview already exists in InterviewMeetings
        /// for the given candidate email and IRF (identified by IRF.id).
        /// Mirrors the eligibility check in usp_GetEligibleCandidatesForVapiCalls.
        /// </summary>
        Task<bool> HasConfirmedInterviewAsync(string candidateEmail, int irfId);
    }
}
