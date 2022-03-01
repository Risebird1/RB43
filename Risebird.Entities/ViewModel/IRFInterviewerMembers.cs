namespace Risebird.Entities.ViewModel
{
    public class IRFInterviewerMembers
    {
        public int IRFId { get; set; }

        public string Type { get; set; }
        public string EmailId { get; set; }
        public string ResumeLink { get; set; }
        public int TotalInterviews { get; set; }
        public int TotalQCInterviews { get; set; }
        public string MobileNumber { get; set; }
        public string uniqueid { get; set; }
        public string InterviewerName { get; set; }
        public bool IsActive { get; set; }
        public bool IsAvailable { get; set; }
        public string AvailableStatus { get; set; }
    }
    public class AssessmentIRFListVM
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string PrimarySkillInvite { get; set; }
        public int TotalMeetings { get; set; }
        public int SelectedMeetings { get; set; }
        public int RejectedMeetings { get; set; }
        public int CompletedMeetings { get; set; }
        public int ScheduledMeetings { get; set; }
    }
}
