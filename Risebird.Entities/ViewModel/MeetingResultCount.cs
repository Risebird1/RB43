namespace Risebird.Entities.ViewModel
{
    public class MeetingResultCount
    {
        public int ProxyCount { get; set; }
        public int SelectedCount { get; set; }
        public int RejectedCount { get; set; }
        public int BorderLineCount { get; set; }
        public int NoShowCount { get; set; }
        public int ScheduledCount { get; set; }
        public int DroppedCount { get; set; }
        public int Pending { get; set; }
        //public int NoResultCount { get; set; }
        public int TotalMeetingsCount { get; set; }
    }
}
