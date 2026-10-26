using Risebird.Entities.Data.PlivoCXModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    /// <summary>Result of dialling a batch of campaign candidates (Call now, or one scheduler tick).</summary>
    public class PlivoCXCampaignBatchResult
    {
        public int    Dialled   { get; set; }
        public int    Failed    { get; set; }
        public int    InFlight  { get; set; }
        public int    Remaining { get; set; }
        public string Message   { get; set; }
    }

    /// <summary>
    /// PlivoCX campaigns: the calling engine shared by the web app (Call now, per-row Call, Test
    /// call, the hangup webhook) and the PlivoCXCampaignScheduler timer function.
    /// </summary>
    public interface IPlivoCXCampaignService
    {
        /// <summary>
        /// Inserts a PlivoCXAgent row for each configured PlivoCX trigger URL whose purpose has no
        /// agent yet (Schedule, Reschedule, Confirm attendance, Interviewer availability,
        /// Interviewer onboarding). Cheap after the first call.
        /// </summary>
        Task EnsureAgentsSeededAsync();

        /// <summary>Active agents, seeding them from config first.</summary>
        Task<List<PlivoCXAgent>> GetAgentsAsync();

        /// <summary>
        /// Dials one campaign candidate through the campaign's agent: call log (with CampaignId),
        /// trigger, log update, Attempts + 1. An invalid number makes the candidate Cold; a
        /// schedule-campaign candidate who already has an interview is marked Booked instead.
        /// </summary>
        Task<PlivoCXDialResult> DialCandidateAsync(PlivoCXCampaign campaign, PlivoCXCampaignCandidate candidate, string actor);

        /// <summary>
        /// Dials the due candidates, up to ConcurrentCalls minus the calls still connecting.
        /// <paramref name="respectWindow"/> = false is Call now (dials outside the window and
        /// before StartsOn); attempt caps and Min gap always apply.
        /// </summary>
        /// <remarks>
        /// Also capped by the account-wide campaign line budget (PlivoCX:CampaignConcurrencyBudget):
        /// <paramref name="maxNewCalls"/> is this campaign's share when the scheduler has already
        /// split the free lines; when null the free lines are counted here.
        /// </remarks>
        Task<PlivoCXCampaignBatchResult> DialDueCandidatesAsync(PlivoCXCampaign campaign, string actor, bool respectWindow, int? maxNewCalls = null);

        /// <summary>Lines on the Plivo account that all campaigns together may use (PlivoCX:CampaignConcurrencyBudget).</summary>
        int ConcurrencyBudget { get; }

        /// <summary>Minutes after which a call still initiated / triggered no longer holds a line (PlivoCX:InFlightTimeoutMinutes).</summary>
        int InFlightTimeoutMinutes { get; }

        /// <summary>Dials a typed number through the campaign's agent without touching any candidate row.</summary>
        Task<PlivoCXDialResult> TestCallAsync(PlivoCXCampaign campaign, string phone, string name, string email, string actor);

        /// <summary>One PlivoCXCampaignScheduler tick over every Running campaign.</summary>
        Task RunSchedulerTickAsync();

        /// <summary>
        /// False when the call belongs to a campaign whose agent does not collect a slot
        /// (confirm-only): the hangup webhook then skips booking.
        /// </summary>
        Task<bool> ShouldBookAsync(PlivoCXCallLog callLog);

        /// <summary>
        /// After the hangup webhook has applied the outcome to the call log: moves the campaign
        /// candidate to Booked / NotBooked / Pending (retry after Min gap) / Cold. No-op for calls
        /// outside a campaign.
        /// </summary>
        Task ApplyCallOutcomeAsync(PlivoCXCallLog callLog, bool candidateConfirmed);
    }
}
