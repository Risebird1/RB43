using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class InterviewFilter
    {
        public string[] PrimarySkill { get; set; }
        public string[] RFID { get; set; }
        public string[] ClientHR { get; set; }
        public string[] ConfirmationStatus { get; set; }
        public string[] MeetingStatus { get; set; }
        public string[] InterviewStatus { get; set; }
        public string[] Interviewer { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
