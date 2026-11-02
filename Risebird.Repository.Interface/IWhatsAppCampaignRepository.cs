using Risebird.Entities.Data.WhatsAppCampaignModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>WhatsApp campaigns: WhatsAppCampaign, WhatsAppCampaignCandidate and their procedures.</summary>
    public interface IWhatsAppCampaignRepository
    {
        // Campaigns
        Task<int> InsertCampaignAsync(WhatsAppCampaign campaign);
        Task<WhatsAppCampaign> GetCampaignAsync(int id);
        Task<List<WhatsAppCampaignVM>> ListCampaignsAsync(string search);
        Task<List<WhatsAppCampaign>> GetRunningCampaignsAsync();
        Task UpdateCampaignPolicyAsync(WhatsAppCampaign campaign);
        Task SetCampaignStatusAsync(int id, string status);

        // Campaign candidates
        Task<int> AddCampaignCandidatesAsync(int campaignId, IEnumerable<WhatsAppCampaignCandidate> rows);
        Task<List<WhatsAppCampaignCandidate>> GetCampaignCandidatesAsync(int campaignId);
        Task<WhatsAppCampaignCandidate> GetCampaignCandidateAsync(int campaignId, int candidateId);
        /// <summary>Pending, due and unlocked rows, locked for this sender (usp_ClaimDueWhatsAppCampaignCandidates).</summary>
        Task<List<WhatsAppCampaignCandidate>> ClaimDueCandidatesAsync(int campaignId, int take, int lockMinutes, int? campaignCandidateId = null);
        /// <summary>Writes one send attempt's outcome and clears the send lock.</summary>
        Task SaveSendResultAsync(int campaignCandidateId, WhatsAppCandidateTransition transition, string lastResult, string lastError);
        /// <summary>Manual state override (Mark…); Pending makes the row due now.</summary>
        Task SetCampaignCandidateStateAsync(int campaignCandidateId, string state, string outcomeNote);
        Task<int> AdvanceResendsAsync(int campaignId);
        Task<int> RefreshOutcomesAsync(int campaignId);
        /// <summary>Rows still Pending or Sent - a Running campaign with none left is Completed.</summary>
        Task<int> CountOpenAsync(int campaignId);
        Task<int> CountPendingAsync(int campaignId);

        // Create-screen and send support
        Task<List<WhatsAppCampaignEligibilityRow>> GetEligibilityAsync(IEnumerable<WhatsAppCampaignEligibilityRow> keys, string eventName);
    }
}
