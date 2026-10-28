using Risebird.Entities.Data.PlivoCXModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>
    /// PlivoCX campaigns: agents, campaigns with their calling policy, and the per-candidate
    /// state machine. Call logs themselves stay in <see cref="IPlivoCXCallRepository"/>.
    /// </summary>
    public interface IPlivoCXCampaignRepository
    {
        // ── Agents ───────────────────────────────────────────────────────────────
        Task<List<PlivoCXAgent>> GetAgentsAsync(bool activeOnly = true);
        Task<PlivoCXAgent>       GetAgentAsync(int id);
        Task<int>                InsertAgentAsync(PlivoCXAgent agent);

        // ── Campaigns ────────────────────────────────────────────────────────────
        Task<int>                     InsertCampaignAsync(PlivoCXCampaign campaign);
        Task<PlivoCXCampaign>         GetCampaignAsync(int id);
        Task<List<PlivoCXCampaignVM>> ListCampaignsAsync(string search);
        Task<List<PlivoCXCampaign>>   GetRunningCampaignsAsync();
        Task UpdateCampaignPolicyAsync(PlivoCXCampaign campaign);
        Task SetCampaignStatusAsync(int id, string status);

        // ── Campaign candidates ──────────────────────────────────────────────────
        /// <summary>Adds the rows, skipping candidates already in the campaign. Returns how many were added.</summary>
        Task<int> AddCampaignCandidatesAsync(int campaignId, IEnumerable<PlivoCXCampaignCandidate> rows);
        Task<List<PlivoCXCampaignCandidate>> GetCampaignCandidatesAsync(int campaignId);
        Task<PlivoCXCampaignCallStats>       GetCampaignCallStatsAsync(int campaignId, int inFlightMinutes = 30);
        Task<PlivoCXCampaignCandidate>       GetCampaignCandidateAsync(int campaignId, int candidateId);

        /// <summary>
        /// Pending candidates that may be dialled now (Min gap, Attempts/day, Total attempts, nothing
        /// still connecting). <paramref name="dayStartUtc"/> is the start of today in the campaign's
        /// time zone, as UTC.
        /// </summary>
        Task<List<PlivoCXCampaignCandidate>> GetDueCandidatesAsync(int campaignId, int take, DateTime nowUtc, DateTime dayStartUtc, int inFlightMinutes = 30);

        /// <summary>
        /// Atomically takes the dial lock on a Pending candidate for <paramref name="seconds"/>.
        /// False when another dial (Call now, a per-row Call or the scheduler) already holds it.
        /// </summary>
        Task<bool> TryClaimForDialAsync(int campaignCandidateId, int seconds);

        /// <summary>Drops the dial lock without recording a call (the dial gave up before logging one).</summary>
        Task ReleaseDialClaimAsync(int campaignCandidateId);

        /// <summary>
        /// The candidate confirmed attendance on a call: marks the meeting as candidate-confirmed
        /// (IsCandidateResponsed = 1, CandidateResponse = 1), as the confirmation email link does,
        /// which takes it off the Unconfirmed list. Only for a live, uncancelled meeting of that
        /// candidate. Returns whether a meeting was updated.
        /// </summary>
        Task<bool> MarkMeetingConfirmedByCandidateAsync(int interviewId, string candidateEmail);

        /// <summary>A call was placed: Attempts + 1, LastCallAt = now, LastCallLogId, NextAttemptAt = now + gap; releases the dial lock.</summary>
        Task BumpCampaignCandidateAsync(int campaignCandidateId, int callLogId, int minGapMinutes);

        Task SetCampaignCandidateStateAsync(int campaignCandidateId, string state, string disposition, string bookedSlot, DateTime? nextAttemptAtUtc);

        /// <summary>Calls from this campaign still connecting (initiated / triggered in the last 30 minutes).</summary>
        Task<int> CountInFlightAsync(int campaignId, int inFlightMinutes = 30);

        /// <summary>
        /// PlivoCX calls of any kind (campaigns, the legacy timer jobs, one-off calls) still initiated /
        /// triggered within <paramref name="inFlightMinutes"/> - the lines in use on the account, whose
        /// concurrency limit is account-wide.
        /// </summary>
        Task<int> CountAllInFlightAsync(int inFlightMinutes);

        /// <summary>
        /// Plivo rejected the call for capacity: give the attempt back (Attempts - 1, not below 0) and
        /// make the candidate due again at <paramref name="nextAttemptAtUtc"/>, still Pending.
        /// </summary>
        Task RefundAttemptAsync(int campaignCandidateId, DateTime nextAttemptAtUtc);
        Task<int> CountPendingAsync(int campaignId);

        /// <summary>Pending candidates with every attempt used and nothing in flight become Cold. Returns how many.</summary>
        Task<int> RetireExhaustedCandidatesAsync(int campaignId, int inFlightMinutes = 30);

        // ── Create-screen sources ────────────────────────────────────────────────
        Task<List<PlivoCXCampaignIRFOption>>    GetActiveIRFsAsync();
        Task<List<PlivoCXCampaignIRFPickerRow>> GetPickerRowsForIRFAsync(int irfId);

        /// <summary>
        /// The Not-scheduled source (usp_GetPlivoCXCampaignNotScheduled): the same candidates as
        /// /Client/NotScheduledMeetings for <paramref name="email"/>, without that page's slow query.
        /// </summary>
        Task<List<PlivoCXCampaignIRFPickerRow>> GetNotScheduledAsync(string email);

        /// <summary>Writes PlivoCXCallLogs.CampaignId (a separate statement so a database without the column still logs calls).</summary>
        Task SetCallLogCampaignAsync(int callLogId, int campaignId);
    }
}
