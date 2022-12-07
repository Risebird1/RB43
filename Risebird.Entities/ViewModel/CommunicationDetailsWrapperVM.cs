using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class CommunicationDetailsWrapperVM
    {
        public IEnumerable<CandidateCommunicationDetailsVM> CommunicationList { get; set; }
        public int CommunicationMedium { get; set; }
    }
}
