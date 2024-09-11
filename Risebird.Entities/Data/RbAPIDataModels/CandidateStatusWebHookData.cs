using System;

namespace Risebird.Entities.Data.RbAPIDataModels
{
    public class CandidateStatusWebHookData
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string jobId { get; set; }
        public string CandidateEmailId { get; set; }
        public string Status { get; set; }
        public DateTime PerformedAt { get; set; }
    }
}
