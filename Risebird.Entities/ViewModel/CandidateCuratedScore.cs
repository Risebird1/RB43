namespace Risebird.Entities.ViewModel
{
    public class CandidateCuratedScore
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public string CandidateEmailId { get; set; }
        public string CuratedEmailId { get; set; }
        public int Score { get; set; }
        public string Message { get; set; }
        public string IRFDetailsID { get; set; }
    }
}
