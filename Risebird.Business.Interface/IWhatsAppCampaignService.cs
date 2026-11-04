using Risebird.Entities.Data.WhatsAppCampaignModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    /// <summary>
    /// The WhatsApp campaign sending engine: sends C1, C2 or C3 to a campaign's due candidates through the same
    /// helpers as the single sends (so opt-out and the 3-per-IRF limit apply), records each result, re-sends on
    /// the campaign's schedule and settles who booked or confirmed.
    /// </summary>
    public interface IWhatsAppCampaignService
    {
        /// <summary>WhatsAppCampaign:SchedulerEnabled - off until the templates are live in Interakt.</summary>
        bool SchedulerEnabled { get; }

        /// <summary>The 3-per-IRF limit for the purpose's event (C1 / C2); 0 for C3.</summary>
        int SendLimitFor(string purpose);

        /// <summary>
        /// Refreshes outcomes, advances re-sends, then sends to up to BatchSize due candidates.
        /// <paramref name="respectWindow"/> false (Send now) ignores the window and dates.
        /// </summary>
        Task<WhatsAppCampaignSendResult> SendDueAsync(WhatsAppCampaign campaign, string actor, bool respectWindow);

        /// <summary>Re-sends to one candidate now (Re-send); limits still apply.</summary>
        Task<WhatsAppCampaignSendResult> SendCandidateAsync(WhatsAppCampaign campaign, WhatsAppCampaignCandidate row, string actor);

        /// <summary>Sends the campaign's template to one team member's address (Test send); no candidate row changes.</summary>
        Task<string> TestSendAsync(WhatsAppCampaign campaign, string email, string actor);

        /// <summary>Adds the WhatsApp facts (number, opt-out, n/3 used, other campaign) to the picker rows.</summary>
        Task<List<WhatsAppCampaignPickerItem>> AnnotateAsync(IEnumerable<WhatsAppCampaignPickerItem> items, string purpose);

        /// <summary>One scheduler pass over every Running campaign.</summary>
        Task RunSchedulerTickAsync();
    }
}
