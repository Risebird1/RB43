using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class CommunicationDetailsWrapperVM
    {
        public IEnumerable<CandidateCommunicationDetailsVM> CommunicationList { get; set; }
        public int CommunicationMedium { get; set; }
    }
}
