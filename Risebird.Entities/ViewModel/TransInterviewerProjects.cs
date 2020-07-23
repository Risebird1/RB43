namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerProjects
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public string TechnologyEnvironment { get; set; }
        public bool IsPrimarySkillUsed { get; set; }
        public string Description { get; set; }
        public bool IsCommercialProject { get; set; }
        public string NatureOfProject { get; set; }
    }
}
