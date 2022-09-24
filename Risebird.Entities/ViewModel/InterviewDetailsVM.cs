using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class InterviewDetailsVM
    {
        public string interviewer { get; set; }
        public string NickName { get; set; }
        public int InterviewerProfileId { get; set; }
        public int TotalInterview { get; set; }
        public int Reject { get; set; }
        public int Selected { get; set; }

        public DateTime? interviewDate { get; set; }
        public string candidateEmail { get; set; }
        public string candidateName { get; set; }
        public int? MeetingStatus { get; set; }
        public int? InterviewStatus { get; set; }
        public int confirmationStatus { get; set; }
        public string MobileNumber { get; set; }
        public string intervieweStatusStatDesc { get; set; }
        public string MeetingStatusStatusDesc { get; set; }
        public string confirmationStatusStatusDesc { get; set; }

        public string ClientHRAttemptContact { get; set; }
        public string ClientName { get; set; }
        public string IRFId { get; set; }
    }
    public class ClientInterviewMapped
    {
        public string EmailId { get; set; }
        public int TotalCompletedInterview { get; set; }
        public string ClientName { get; set; }
    }
}
