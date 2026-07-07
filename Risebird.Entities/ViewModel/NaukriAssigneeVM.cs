namespace Risebird.Entities.ViewModel
{
    // An operator eligible to own a Naukri onboarding "ticket" (prospect): an active user whose
    // profile is InterviewerManagement or SeniorInterviewerManagement.
    public class NaukriAssigneeVM
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string ProfileName { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Email : $"{Name} ({Email})";
    }
}
