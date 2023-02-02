namespace Risebird.Entities.ViewModel
{
    public class TransInterviewer
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int UserId { get; set; }
        public int InterviewerId { get; set; }
        public string PrimarySkill { get; set; }
        public string EmailId { get; set; }
        public int PerSuccessRate { get; set; }
        public bool IsActive { get; set; }
        public bool IsAvailable { get; set; }

        public string ResumeLink { get; set; }
        public string CodedResumeLink { get; set; }
        public string NickName { get; set; }
        public bool IsSelect { get; set; }
        public bool IsSMEVerified { get; set; }
        //public string CodedName { get; set; }
    }
}
