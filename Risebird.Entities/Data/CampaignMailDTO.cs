using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    public class CampaignMailDTO
    {
        public IEnumerable<AssessmentGroupInterviewerDetails> interviewers { get; set; }
        public int campagignId { get; set; }
        public string assessentGroupName { get; set; }
    }
}
