using System;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewersReliabilityScoresVm
    {
        public int InterviewerId { get; set; }
        public int InterviewMeetingId { get; set; }
        public int ReliabilityScoreTypeId { get; set; }
        public int Point { get; set; }
        public DateTime? CreateDate { get; set; }
        public string InterviewerEmail { get; set; }
        public string ReliabilityScoreType { get; set; }
        public int TotalCount { get; set; }
    }
}
