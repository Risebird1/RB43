using System;

namespace Risebird.Entities.ViewModel
{
    public class UnassignedInterviewVM
    {
        public int Interviewid { get; set; }
        public int Irfid { get; set; }
        public string CandidateEmail { get; set; }
        public DateTime interviewDate { get; set; }
        public string FirstName { get; set; }
        public int Typefunction { get; set; }
        public int candidateid { get; set; }
        public string uniqueid { get; set; }
        public string staffingAgency { get; set; }
        public string MobileNumber { get; set; }
        public string ClientName { get; set; }
        public string PrimarySkillInvite { get; set; }
        public string MeetingLink { get; set; }
        public string ClientHrContact { get; set; }
    }
}
