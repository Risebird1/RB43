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
        Task<PlivoCXCampaignCallStats>       GetCampaignCallStatsAsync(int campaignId);
        Task<PlivoCXCampaignCandidate>       GetCampaignCandidateAsync(int campaignId, int candidateId);

        /// <summary>
        /// Pending candidates that may be dialled now (Min gap, Attempts/day, Total attempts, nothing
        /// still connecting). <paramref name="dayStartUtc"/> is the start of today in the campaign's
        /// time zone, as UTC.
        /// </summary>
        Task<List<PlivoCXCampaignCandidate>> GetDueCandidatesAsync(int campaignId, int take, DateTime nowUtc, DateTime dayStartUtc);

        /// <summary>A call was placed: Attempts + 1, LastCallAt = now, LastCallLogId, NextAttemptAt = now + gap.</summary>
        Task BumpCampaignCandidateAsync(int campaignCandidateId, int callLogId, int minGapMinutes);

        Task SetCampaignCandidateStateAsync(int campaignCandidateId, string state, string disposition, string bookedSlot, DateTime? nextAttemptAtUtc);

        /// <summary>Calls from this campaign still connecting (initiated / triggered in the last 30 minutes).</summary>
        Task<int> CountInFlightAsync(int campaignId);
        Task<int> CountPendingAsync(int campaignId);

        /// <summary>Pending candidates with every attempt used and nothing in flight become Cold. Returns how many.</summary>
        Task<int> RetireExhaustedCandidatesAsync(int campaignId);

        // ── Create-screen sources ────────────────────────────────────────────────
        Task<List<PlivoCXCampaignIRFOption>>    GetActiveIRFsAsync();
        Task<List<PlivoCXCampaignIRFPickerRow>> GetPickerRowsForIRFAsync(int irfId);

        /// <summary>Writes PlivoCXCallLogs.CampaignId (a separate statement so a database without the column still logs calls).</summary>
        Task SetCallLogCampaignAsync(int callLogId, int campaignId);
    }
}
