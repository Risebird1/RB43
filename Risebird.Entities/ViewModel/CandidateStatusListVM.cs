using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class CandidateStatusListVM
    {
        public IEnumerable<CandidateStatus> CandidateStatusList { get; set; }
        public string CandidateName { get; set; }
        public string IRFName { get; set; }
        public int CandidateId { get; set; }
        public int IRFId { get; set; }
        public string Profile { get; set; }
    }
}
