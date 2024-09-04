using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class AddCandidatesResponse
    {
        public string performedAt { get; set; }
        public List<CandidateOnboardingStatus> list { get; set; }
    }

    public class CandidateOnboardingStatus
    {
        public string emailId { get; set; }
        public string onboardingStatus { get; set; }//successful/ duplicate/ User exists with different profile/failed
        public string invitationMailDeliveryStatus { get; set; }//delivered ; not delivered; NA
        public string SchedulingLink { get; set; }//; NA
    }

}
