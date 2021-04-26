namespace Risebird.Entities.ViewModel
{
    public class CandidateCurrentStatusCount
    {
        public CandidateCurrentStatusCount()
        {
            Proxy = 0;
            Selected = 0;
            Rejected = 0;
            BorderLine = 0;
            NoShow = 0;
            Scheduled = 0;
            Dropped = 0;
            Completed = 0;
            QCPending = 0;
            TotalCount = 0;
        }

        public int? Proxy { get; set; }
        public int? Selected { get; set; }
        public int? Rejected { get; set; }
        public int? BorderLine { get; set; }
        public int? NoShow { get; set; }
        public int? Scheduled { get; set; }
        public int? Dropped { get; set; }
        public int? Completed { get; set; }
        public int? QCPending { get; set; }
        public int? NotYetScheduled { get; set; }
        public int? TotalCount { get; set; }
    }
}
