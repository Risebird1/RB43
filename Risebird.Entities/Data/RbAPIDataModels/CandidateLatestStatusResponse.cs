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
        public string interviewer { get; set; }
        public string reportUrl { get; set; }
        public DateTime retrievedAt { get; set; }
        public DateTime? interviewDate { get; set; }
    }
    public class CandidateInfoResponse
    {
        public CandidateInfoResponse()
        {
            result = new List<CandidateStatusResponse>();
        }
        public string skillChannelId { get; set; }
        public string requestedBy { get; set; }
        public string retrievedAt { get; set; }
        public List<CandidateStatusResponse> result { get; set; }
    }
    public class CandidateStatusResponse
    {
        public string candidateEmailId { get; set; }
        public string currentStatus { get; set; }
        public string currentStatusUpdatedAt { get; set; }
        public string interviewDate { get; set; }
        public string interviewer { get; set; }
        public string reportUrl { get; set; }
    }
}
