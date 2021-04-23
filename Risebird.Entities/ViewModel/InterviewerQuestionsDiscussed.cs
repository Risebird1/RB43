namespace Risebird.Entities.ViewModel
{
    public class InterviewerQuestionsDiscussed
    {
        public int id { get; set; }
        public int InterviewId { get; set; }
        public string Question { get; set; }
        public int RatingId { get; set; }
        public int Timing { get; set; }
    }
}
