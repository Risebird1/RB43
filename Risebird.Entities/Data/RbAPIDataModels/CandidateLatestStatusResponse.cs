using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data.RbAPIDataModels
{
    public class CandidateLatestStatusResponse
    {
        public string skillChannelId { get; set; }
        public string candidateEmailId { get; set; }
        public string requestedBy { get; set; }
        public string currentStatus { get; set; }
        public string currentStatusUpdatedAt { get; set; }
        public string retrievedAt { get; set; }
        public string interviewDate { get; set; }
    }
}
