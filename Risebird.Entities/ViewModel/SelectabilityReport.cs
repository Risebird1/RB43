namespace Risebird.Entities.ViewModel
{
    public class SelectabilityReport
    {
        public int id { get; set; }
        public int InterviewId { get; set; }
        public string RequiredLevel { get; set; }
        public int RequiredLevelId { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string ActualLevel { get; set; }
        public int ActualLevelId { get; set; }
        public int WeightageRequired { get; set; }
        public int WeightageActual { get; set; }
        public string Name { get; set; }
    }
}
