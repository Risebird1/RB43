using System;

namespace Risebird.Entities.Data
{
    public class Interviewer
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public long MobileNumber { get; set; }
        public string PrimarySkill { get; set; }
        public string ResumeLink { get; set; }
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
        public int NoOfIntterview { get; set; }
        public double SuccessRate { get; set; }
        public string EmailId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime LastLogout { get; set; }
        public string ImageURL { get; set; }
        public int IdProfile { get; set; }
        public string AssessmentGroupId { get; set; }
        public bool IsLeadInterviewer { get; set; }
        public string NickName { get; set; }
    }
    public class InterviewerDetails
    {
        public string NickName { get; set; }
        public bool IsNotificationAccepted { get; set; }
        public string CodedResumeLink { get; set; }
    }
}
