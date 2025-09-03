using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    public class AddCandidatesResponse
    {
        public AddCandidatesResponse()
        {
            list = new List<CandidateOnboardingStatus>();
        }
        public string performedAt { get; set; }
        public List<CandidateOnboardingStatus> list { get; set; }
    }

    public class CandidateOnboardingStatus
    {
        public string emailId { get; set; }
        public string onboardingStatus { get; set; }//successful/ duplicate/ User exists with different profile/failed
        public string invitationMailDeliveryStatus { get; set; }//delivered ; not delivered; NA
        public string schedulingLink { get; set; }//; NA
    }
    public class CandidateExportStatus
    {
        public CandidateExportStatus()
        {
            list = new List<CandidateExportStatusList>();
        }
        public string message { get; set; }
        public List<CandidateExportStatusList> list { get; set; }
    }
    public class CandidateExportStatusList
    {
        public string emailId { get; set; }
        public string onboardingStatus { get; set; }
    }
    public class CancelCandidatesResponse
    {
        public CancelCandidatesResponse()
        {
            CandidateList = new List<CandidateCancelStatus>();
        }
        public string performedAt { get; set; }
        public List<CandidateCancelStatus> CandidateList { get; set; }
    }
    public class CandidateCancelStatus
    {
        public string candidateemailId { get; set; }
        public string candidatestatus { get; set; }
    }
}
