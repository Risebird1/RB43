namespace Risebird.Entities.ViewModel
{
    public class SelectabilityEvaluatorReport
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int CandidateId { get; set; }
        public string RequiredLevel { get; set; }
        public int RequiredLevelId { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string ActualLevel { get; set; }
        public int ActualLevelId { get; set; }
        public float WeightageRequired { get; set; }
        public float WeightageActual { get; set; }
        public string Name { get; set; }
        public string MCQId { get; set; }
        public string CMCQId { get; set; }
        public string CandidateEmail { get; set; }
        public string LevelId { get; set; }
        public string ApplyingCompany { get; set; }
    }
}
