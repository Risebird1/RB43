using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class AssessmentGroupInterviewers
    {
        public int InterviewerProfileId { get; set; }
        public int UserId { get; set; }
        public int AssessmentGroupId { get; set; }
        public string EmailId { get; set; }
        public string InterviewerName { get; set; }
        public bool IsSME { get; set; }
    }
}
