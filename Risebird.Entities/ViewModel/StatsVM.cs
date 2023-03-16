using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class StatsVM
    {
        public string EmailId { get; set; }
        public string RoleType { get; set; }
        public int TotalMeetings { get; set; }
        public int TotalCompletedMeetings { get; set; }
        public int TotalSelected { get; set; }
        public int TotalRejected { get; set; }
        public int TotalBorderLine { get; set; }
        public int TotalProxy { get; set; }
        public int TotalDropped { get; set; }
        public int TotalInterviewerNoShow { get; set; }
        public int TotalNoShowCandidate { get; set; }
        public int StatusPending { get; set; }
        public int Scheduled { get; set; }
        public string MobileNumber { get; set; }
    }
}
