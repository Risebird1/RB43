using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class CandidateTimelineVM
    {
        public string CandidateName { get; set; }
        public string HREmail { get; set; }
        public string CurrentStatus { get; set; }
        public string Report { get; set; }

        public List<TimelineStatus> RisebirdAttempt { get; set; }
        public List<TimelineStatus> MeetingStatus { get; set; }
        public List<TimelineStatus> InterviewStatus { get; set; }

    }

    public class TimelineStatus
    {
        public string Status { get; set; }
        public string StatusDate { get; set; }

      public string Date { get; set; }
        public string Day { get; set; }

    }





}
