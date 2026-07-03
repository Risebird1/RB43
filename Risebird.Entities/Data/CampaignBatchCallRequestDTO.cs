namespace Risebird.Entities.Data
{
    /// <summary>
    /// Lightweight payload the portal fires (fire-and-forget) to the CampaignBatchCall Azure Function.
    /// The portal has already opened the attempt log row (BatchCallLogId / AttemptName); the function
    /// re-reads the unresponded interviewers straight from the DB by <see cref="CampaignId"/> (so the
    /// portal never has to serialize a large interviewer list into the request body), does the actual
    /// paced Plivo calling, logs each call's outcome, and finalizes the attempt log.
    /// </summary>
    public class CampaignBatchCallRequestDTO
    {
        public int BatchCallLogId { get; set; }
        public int CampaignId { get; set; }
        public string CampaignName { get; set; }
        public string MagName { get; set; }
        public int AttemptNumber { get; set; }
        public string AttemptName { get; set; }
        public string CallerId { get; set; }
        public string Source { get; set; }
    }
}
