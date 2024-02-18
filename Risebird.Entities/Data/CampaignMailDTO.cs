using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data
{
    public class CampaignMailDTO
    {
        public IEnumerable<AssessmentGroupInterviewerDetails> interviewers { get; set; }
        public int campagignId { get; set; }
        public string assessentGroupName { get; set; }
    }
}
