using System;

namespace Risebird.Entities.ViewModel
{
    public class UnapprovedInterviewsFilter
    {
        public string[] PrimarySkill { get; set; }
        public string[] SCID { get; set; }
        public string[] ConfirmationStatus { get; set; }
        public string[] MeetingStatus { get; set; }
        public string[] InterviewStatus { get; set; }

        public string[] Interviewer { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

    }
}
