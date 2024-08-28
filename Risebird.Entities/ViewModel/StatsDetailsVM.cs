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
        public bool isTechQC { get; set; }
    }
    public class ProjectStatsVM
    {
        public int id { get; set; }
        public string ClientName { get; set; }
        public string ProjectUniqueid { get; set; }
        public string ProjectName { get; set; }
        public int ClientId { get; set; }
        public string ClientCompName { get; set; }
        public int SelectedMeetings { get; set; }
        public int CandidateCount { get; set; }
        public int CompletedInterviews { get; set; }
        public int RejectedMeetings { get; set; }
        public int NotScheduledCount { get; set; }
        public int ReScheduledCount { get; set; }
        public int FutureScheduledMeetings { get; set; }
        public int ColdCandidateCount { get; set; }
        public int SelectedRejectMeetings { get; set; }
        public int RejectSelectedMeetings { get; set; }
        public int SkillSetsevaluate { get; set; }
        public int PendingCount { get; set; }
    }
}
