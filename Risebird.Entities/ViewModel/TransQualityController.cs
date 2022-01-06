namespace Risebird.Entities.ViewModel
{
    public class TransQualityController
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int InterviewerId { get; set; }
        public int UserId { get; set; }

        public string PrimarySkill { get; set; }
        public string EmailId { get; set; }
        public int PerSuccessRate { get; set; }
        public bool IsActive { get; set; }

    }
    //public class QualityController
    //{
    //    public int id { get; set; }
    //    public int UserId { get; set; }
    //    public string PrimarySkill { get; set; }
    //    public string ResumeLink { get; set; }
    //}
}
