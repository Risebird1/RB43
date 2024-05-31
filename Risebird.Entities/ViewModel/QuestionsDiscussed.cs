namespace Risebird.Entities.ViewModel
{
    public class QuestionsDiscussed
    {
        public int id { get; set; }
        public int InterviewId { get; set; }
        public string Comment { get; set; }
        public string SkillName { get; set; }
        public int RatingId { get; set; }
        public int? Timing { get; set; }
        public string QuestionStatus { get; set; }
        public string VideoLink { get; set; }
        public int Priority { get; set; }
        public int SkillSet { get; set; }

    }
}
