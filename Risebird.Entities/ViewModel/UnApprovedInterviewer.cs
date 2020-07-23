namespace Risebird.Entities.ViewModel
{
    public class UnApprovedInterviewer
    {
        public int InterviewerProfileId { get; set; }
        public int UserId { get; set; }
        public string CurrentRole { get; set; }
        public string ResumeLink { get; set; }
        public string PreferredDomain { get; set; }
        public string FirstName { get; set; }
        public string EmailId { get; set; }
        public string SkillName { get; set; }
        public bool IsAdded { get; set; }
    }
}
