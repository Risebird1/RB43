namespace Risebird.Entities.ViewModel
{
    public class ClonableTemplatesFilter
    {
        //public string[] TemplateName { get; set; }
        //public string[] CompetencyLevel { get; set; }
        //public string[] SeniorityLevel { get; set; }
        //public string[] CountryType { get; set; }
        //public string[] CompanyType { get; set; }
        //public string[] EmploymentType { get; set; }
        //public string[] DifficultyLevel { get; set; }//this is referred to as ProfileType at some places

        public string[] AssessmentGroup { get; set; }
        public string AssessmentGroupTemplate { get; set; }
        public string SeniorityLevel { get; set; }
        public string CountryType { get; set; }
        public string CompanyType { get; set; }
        public string EmploymentType { get; set; }
        public string DifficultyLevel { get; set; }//this is referred to as ProfileType at some places
        public string Category { get; set; }
    }
}
