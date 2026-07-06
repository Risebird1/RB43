namespace Risebird.Entities.Data
{
    /// <summary>
    /// Lightweight trigger payload the portal fires at the CampaignBatchCall Azure Function. The portal
    /// only identifies WHICH campaign to call and WHO started it — the function owns everything else:
    /// resolving the campaign, guarding against a running attempt, reading the unresponded interviewers
    /// from the DB, opening + finalizing the attempt log, and placing the paced Plivo calls.
    /// </summary>
    public class CampaignBatchCallRequestDTO
    {
        public int CampaignId { get; set; }
        public string InitiatedBy { get; set; }
        public string Source { get; set; }
    }
}
