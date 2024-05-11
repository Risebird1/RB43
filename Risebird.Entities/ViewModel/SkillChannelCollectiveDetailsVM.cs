using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class SkillChannelCollectiveDetailsVM
    {
        public int IRFId { get; set; }
        public string type { get; set; }

        public int NeedClarity { get; set; }
        public int MissedCandidate { get; set; }

        public int HighSelectCount { get; set; }
        public int HighRejectCount { get; set; }
        public int HighNegCandidateRating { get; set; }
        public int ProbableNoShowCount { get; set; }
        public int NotProcessedResumeCount { get; set; }

        public bool CalibrationCall { get; set; }
        public bool DMApproved { get; set; }
        public int ReportPending { get; set; }

        public int ProxyCount { get; set; }
    }
    public class SkillChannelTaskSpeedDetailsVM
    {
        public DateTime interviewDate { get; set; }
        public string interviewer { get; set; }
        public string candidateEmail { get; set; }
        public int imid { get; set; }
        public int irfIntId { get; set; }
    }
}
