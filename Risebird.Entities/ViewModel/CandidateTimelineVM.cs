using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class CandidateTimelineVM
    {
        public string CandidateName { get; set; }
        public string CandidateEmailId { get; set; }
        public string HREmail { get; set; }
        public string CurrentStatus { get; set; }
        public string Report { get; set; }

        public List<TimelineStatus> RisebirdAttempt { get; set; }
        public List<TimelineStatus> MeetingStatus { get; set; }
        public List<TimelineStatus> InterviewStatus { get; set; }
        public List<TimelineStatus> ScheduleStatus { get; set; }
        //public TimelineStatus OnboardingDetails { get; set; }
        //public TimelineStatus CurrentStatusDetails { get; set; }

        public int? InterviewId { get; set; }
        public int IRFId { get; set; }
        public int CandidateId { get; set; }
        public int CandidateMasterId { get; set; }
        public int? CurrentInterviewStatus { get; set; }
        public int? QCStatus { get; set; }
        public int? CurrentMeetingStatus { get; set; }
        public int? confirmationStatus { get; set; }
        public string PrimarySkill { get; set; }


    }

    public class TimelineStatus
    {
        public string Status { get; set; }
        public string StatusDate { get; set; }

        public string Date { get; set; }
        public string Day { get; set; }

    }





}
