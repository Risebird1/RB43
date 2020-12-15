using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class CandidateTimelineData
    {
        public int CandidateId { get; set; }
        public int CandidateMasterId { get; set; }
        public string EmailId { get; set; }
        public string Name { get; set; }
        public string HREmail { get; set; }
        public int IRFId { get; set; }
        public int InterviewId { get; set; }
        public string MeetingStatus { get; set; }
        public DateTime MeetingStatusDate { get; set; }
        public string InterviewStatus { get; set; }
        public DateTime InterviewStatusDate { get; set; }
        public string CurrentStatus { get; set; }
        //public DateTime CurrentStatusDate { get; set; }
        public int? CurrentInterviewStatus { get; set; }
        public int? QCStatus { get; set; }
        public int? CurrentMeetingStatus { get; set; }
        public int? confirmationStatus { get; set; }
        public string ScheduleStatusComments { get; set; }
        public string ScheduleStatusValue { get; set; }
        public DateTime ScheduleStatusDate { get; set; }
    }
}
