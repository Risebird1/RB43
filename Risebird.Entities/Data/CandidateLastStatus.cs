using System;

namespace Risebird.Entities.Data
{
    public class CandidateLastStatus
    {
        //public int id { get; set; }
        public int IRFintId { get; set; }
        public string IRFId { get; set; }
        public string Comments { get; set; }
        public string StatusValue { get; set; }
        public string candidateEmail { get; set; }
        public string candidateName { get; set; }
        public string JDTitle { get; set; }
        public string Client { get; set; }
        public DateTime? interviewDate { get; set; }
        public DateTime PerformedOn { get; set; }
        //public int? MeetingStatus { get; set; }
        //public int? InterviewStatus { get; set; }
        public int? QCStatus { get; set; }
        public string ResumeLink { get; set; }
        public int? TotalTechScore { get; set; }
        public string MobileNumber { get; set; }
        public int ClientId { get; set; }
    }
}
