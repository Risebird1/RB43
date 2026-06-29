using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// Payload the portal fires (fire-and-forget) to the CampaignBatchCall Azure Function. The portal
    /// has already opened the attempt log row (BatchCallLogId / AttemptName); the function does the
    /// actual paced Plivo calling, logs each call's outcome, and finalizes the attempt log.
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
        public List<CampaignBatchCallInterviewerDTO> Interviewers { get; set; } = new List<CampaignBatchCallInterviewerDTO>();
    }

    public class CampaignBatchCallInterviewerDTO
    {
        public int InterviewerProfileId { get; set; }
        public string InterviewerName { get; set; }
        public string SenderEmail { get; set; }
        public string Phone { get; set; }
    }
}
