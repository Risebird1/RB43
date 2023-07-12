namespace Risebird.Entities.ViewModel
{
    public class InterviewerStatsVM
    {
        public string InterviewerEmailId { get; set; }
        public int UserId { get; set; }
        public int TotalMeetings { get; set; }
        public int TotalCompletedMeetings { get; set; }
        public int TotalSelected { get; set; }
        public int TotalRejected { get; set; }
        public int TotalBorderLine { get; set; }
        public int TotalProxy { get; set; }
        public int TotalDropped { get; set; }
        public int TotalSelfNoShow { get; set; }
        public int TotalNoShowCandidate { get; set; }
        public int StatusPending { get; set; }
        public int Scheduled { get; set; }
        public int TotalPenalties { get; set; }
        public string ResumeLink { get; set; }
        public string InterviewerPhone { get; set; }
        public string AverageClientRating { get; set; }
        public string NickName { get; set; }//CodedName
        public string CodedResumeLink { get; set; }
    }
}
