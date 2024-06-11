using System;

namespace Risebird.Entities.ViewModel
{
    public class MyInterviewFilter
    {
        public string[] InterviewSlot { get; set; }
        public string[] CandidateName { get; set; }
        public string[] RisebirdHR { get; set; }
        public string[] InterviewDate { get; set; }
        public string[] InterviewDuration { get; set; }
        public string[] JobLevel { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int HdnReturnBack { get; set; }


    }
}
