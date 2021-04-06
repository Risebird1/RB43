namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerEducation
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public string EducationType { get; set; }
        public string Degree { get; set; }
        public string EducationMode { get; set; }
        public string CollegeType { get; set; }
        public string UniversityName { get; set; }
        public int? PassOutYear { get; set; }
        public bool AnyGap { get; set; }
    }
}
