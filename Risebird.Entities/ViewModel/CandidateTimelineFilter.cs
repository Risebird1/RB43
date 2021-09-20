using System;

namespace Risebird.Entities.ViewModel
{
    public class CandidateTimelineFilter
    {
        public string[] MeetingStatus { get; set; }
        public string[] InterviewStatus { get; set; }
        public string[] ScheduleStatus { get; set; }
        public string[] HREmail { get; set; }
        public string[] ProfileSource { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? InterviewStartDate { get; set; }
        public DateTime? InterviewEndDate { get; set; }
    }
}
