using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class IRFInterviewFilter
    {
        public string[] ClientHR { get; set; }
        public string[] ConfirmationStatus { get; set; }
        public string[] MeetingStatus { get; set; }
        public string[] InterviewStatus { get; set; }
        //public string[] Interviewer { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string[] CandidateName { get; set; }
        public int irfId { get; set; }
        public string IRFName { get; set; }

    }
}
