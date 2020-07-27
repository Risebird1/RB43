namespace Risebird.Entities.ViewModel
{
    public class TechSkillEvaluatorReport
    {
        public int id { get; set; }
        public int CandidateId { get; set; }
        public int IRFId { get; set; }
        public string RequiredLevel { get; set; }
        public int RequiredLevelId { get; set; }
        public string SkillName { get; set; }
        public int SkillId { get; set; }
        public string ActualLevel { get; set; }
        public int ActualLevelId { get; set; }
        public int WeightageRequired { get; set; }
        public int WeightageActual { get; set; }
        //public bool IsEdited { get;set; }
    }
}
