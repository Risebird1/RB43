namespace Risebird.Entities.Data.AffIndaModels
{
    public class ResumeData
    {
        public string[] emails { get; set; }
        public string dateOfBirth { get; set; }
        public Name name { get; set; }
        public PhoneNumber[] phoneNumberDetails { get; set; }
        public string totalYearsExperience { get; set; }
        public string professionn { get; set; }
    }
}
