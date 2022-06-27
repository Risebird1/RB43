namespace Risebird.Entities.ViewModel
{
    public class CandidatesCount
    {
        public int TotalCandidates { get; set; }
        public int Proxy { get; set; }
        public int Selected { get; set; }
        public int Rejected { get; set; }
        public int BorderLine { get; set; }
        public int Pending { get; set; }
        public int NoShowCount { get; set; }
        public int ScheduledCount { get; set; }
        public int DroppedCount { get; set; }
        //public int QCPending { get; set; }
    }
}
