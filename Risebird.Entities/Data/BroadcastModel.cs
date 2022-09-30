namespace Risebird.Entities.Data
{
    public class BroadcastModel
    {
        public string BroadcastReciever { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }

        public int IRFId { get; set; }
        public string SentBy { get; set; }
        public string Profile { get; set; }
        public int? InterviewId { get; set; }
        //public bool shouldSendEmail { get; set; }
    }
}
