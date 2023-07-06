using System;

namespace Risebird.Entities.ViewModel
{
    public class StatsDetailsVM
    {
        public DateTime interviewDate { get; set; }
        public string interviewer { get; set; }
        public string candidateEmail { get; set; }
        public string Client { get; set; }
        public string SkillChannel { get; set; }
        public int? MeetingStatus { get; set; }
        public int? InterviewStatus { get; set; }
        public string ResultType { get; set; }
        public int irfId { get; set; }
        public DateTime? InterviewerJoinTime { get; set; }
        public int? interviewId { get; set; }

    }
}
