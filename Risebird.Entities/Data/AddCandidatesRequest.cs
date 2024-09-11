using Risebird.Entities.ViewModel;
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
}
