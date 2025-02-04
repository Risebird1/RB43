using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    public class AddCandidatesRequest
    {
        //public string skillChannelName { get; set; }
        //public string planType{ get; set; }
        public string requestedBy { get; set; }
        public string jobId { get; set; }
        public List<CandidateQuickAddModel> CandidateList { get; set; }
    }
    public class ScheduleCandidatesRequest
    {
        public string skillChannelId { get; set; }
        public string requestedBy { get; set; }
        public List<ScheculeInterview> CandidateList { get; set; }
    }
    public class ScheduleCandidateList
    {
        public DateTime Interviewdate { get; set; }
        public string CandidateEmail { get; set; }
        public string MobileNumber { get; set; }
        public string Resume { get; set; }
    }
}
