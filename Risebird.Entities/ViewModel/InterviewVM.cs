using System;

namespace Risebird.Entities.ViewModel
{
    public class InterviewVM
    {
        public string IRFID { get; set; }
        public DateTime interviewDate { get; set; }
        public int interviewSlot { get; set; }
        public string interviewer { get; set; }
        public int interviewStatus { get; set; }
        public string InterviewerComment { get; set; }
        public string VideoLink { get; set; }
        public int QCInterview { get; set; }

    }
    public class CommonInterviewDetailsVM
    {
        public string EmailId { get; set; }
        public int Total { get; set; }
        public string uniqueid { get; set; }
        public string IRFId { get; set; }
        public string PrimarySkill { get; set; }
        public string Name { get; set; }
        public string ClientName { get; set; }
        public int JobCountry { get; set; }
    }
}
