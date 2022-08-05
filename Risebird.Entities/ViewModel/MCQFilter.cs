namespace Risebird.Entities.ViewModel
{
    public class MCQFilter
    {
        public string[] PrimarySkill { get; set; }
        public string[] SecondarySkill { get; set; }
        public string SeniorityLevel { get; set; }
        public string CountryType { get; set; }
        public string CompanyType { get; set; }
        public string EmploymentType { get; set; }
        public string DifficultyLevel { get; set; }//this is referred to as ProfileType at some places
    }
}
