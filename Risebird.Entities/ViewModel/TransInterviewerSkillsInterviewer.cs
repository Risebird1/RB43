namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerSkillsLeadInterviewer
    {
        public int id { get; set; }
        public int InterviewerSkillID { get; set; }
        public int UserId { get; set; }
        public int InterviewerId { get; set; }
        public bool IsApproved { get; set; }
        public string PrimarySkill { get; set; }
        public string EmailId { get; set; }

    }
}
