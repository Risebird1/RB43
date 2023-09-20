using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewersReliabilityScoresVm
    {
        public int InterviewerId { get; set; }
        public int InterviewMeetingId { get; set; }
        public int ReliabilityScoreTypeId { get; set; }
        public int Point { get; set; }
        public DateTime? CreateDate { get; set; }
    }
}
