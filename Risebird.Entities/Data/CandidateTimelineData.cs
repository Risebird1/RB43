using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class CandidateTimelineData
    {
        public int CandidateId { get; set; }
        public string EmailId { get; set; }
        public string Name { get; set; }
        public string ClientHR { get; set; }
        public int IRFId { get; set; }
        public int InterviewId { get; set; }
        public DateTime OnboardingDate { get; set; }
        public DateTime InvitationDate { get; set; }
        public string MeetingStatus { get; set; }
        public DateTime MeetingStatusDate { get; set; }
        public string InterviewStatus { get; set; }
        public DateTime InterviewStatusDate { get; set; }
        public string CurrentStatus { get; set; }
        public DateTime CurrentStatusDate { get; set; }
    }
}
