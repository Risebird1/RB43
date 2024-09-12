using System;

namespace Risebird.Entities.Data.RbAPIDataModels
{
    public class CandidateLatestStatusRequest
    {
        public string skillChannelId { get; set; }
        public string candidateEmailId { get; set; }
        public string requestedBy { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }
        public string jobId { get; set; }
    }
}
