namespace Risebird.Entities.Data
{
    public class InterviewerSuccessRateChangeData
    {
        public int UserId { get; set; }
        public float PercentChange { get; set; }
        public string Type { get; set; }
        public int IRFId { get; set; }
    }
}
