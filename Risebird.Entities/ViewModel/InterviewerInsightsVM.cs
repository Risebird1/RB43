using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class InterviewerInsightsVM
    {
        public int id { get; set; }
        public int Userid { get; set; }
        public string Emailid { get; set; }
        public string CompanyName { get; set; }
        public bool IsEmployed { get; set; }
        public string MobileNumber { get; set; }
        public string interviewerName { get; set; }
        public DateTime? LastLogin { get; set; }
        public int VideoConfirmation { get; set; }
        public int Isavailable { get; set; }
        public string EmploymentComment { get; set; }
        public long ReliabilityScore { get; set; }
        public int TotalInterviews { get; set; }
        public int CompletedInterviews { get; set; }
        public int SelectedInterviews { get; set; }
        public int RejectedInterviews { get; set; }
        public int NoShowInterviews { get; set; }
        public int NoShowCandidateInterviews { get; set; }
        public int DroppedInterviews { get; set; }
        public bool IsSuspicious { get; set; }
        public int PaymentSLA { get; set; }
        public int Penalty { get; set; }
        public decimal CandidateRating { get; set; }
        public decimal CustomerRating { get; set; }
        public int Redorequests{ get; set; }
        public int RedFlags{ get; set; }
        public int JoinLateMeeting{ get; set; }
        public int JoinLateCandidateNoShow { get; set; }
        public int UnassignInterviews { get; set; }
        public int SuspiciousCount { get; set; }
        public int SelectedComment { get; set; }
        public int RejectedComment { get; set; }
        public int LessQuestions { get; set; }
        public string ResumeLink { get; set; }
        public string uniqueid { get; set; }
        public string CandidateInteractionQuality { get; set; }
        public string LimitedHandoncoding { get; set; }
        public string UnethicalPractices { get; set; }
        public string PoorCommunicationSkills
        {
            get; set;
        }
 public string WeakInfrastructure { get; set; }
        public string TimeManagement { get; set; }

    }
    public class InterviewerInsightsMarkersDetailsVM
    {
        public string Comments { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? Createdon { get; set; }
        public string MeetingStatus { get; set; }
        public int id { get; set; }
    }
    public class InterviewerRedmarkers
    {
        public string EmailId { get; set; }
        public string uniqueid { get; set; }
        public int id { get; set; }
        public string IRFId { get; set; }
        public DateTime? interviewDate { get; set; }
        public string interviewer { get; set; }
        public int RedMarker { get; set; }
        public string candidateName {  get; set; }
        public string candidateEmail { get; set; }
        public string ResumeLink { get; set; }
        public string MobileNumber { get; set; }
        public string CompanyName { get; set; }
        public string ChannelOwner { get; set; }
        public string PrimarySkill { get; set; }
        public int meetingId { get; set; }
        public bool RedmarkerAcknowledge { get; set; }
    }
}
