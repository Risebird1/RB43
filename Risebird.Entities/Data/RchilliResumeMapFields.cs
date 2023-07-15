using System.Collections.Generic;

namespace Risebird.Entities.Data
{
    public class RchilliResumeMapFields
    {
        public class Error
        {
            public int errorcode { get; set; }
            public string errormsg { get; set; }
        }
        public class ErrorClass
        {
            public Error error { get; set; }
        }
        public class ResumeLanguage
        {
            public string Language { get; set; }
            public string LanguageCode { get; set; }
        }

        public class CountryCode
        {
            public string IsoAlpha2 { get; set; }
            public string IsoAlpha3 { get; set; }
            public string UNCode { get; set; }
        }

        public class ResumeCountry
        {
            public string Country { get; set; }
            public string Evidence { get; set; }
            public CountryCode CountryCode { get; set; }
        }

        public class Name
        {
            public string FullName { get; set; }
            public string TitleName { get; set; }
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
            public string LastName { get; set; }
            public string FormattedName { get; set; }
            public int ConfidenceScore { get; set; }
        }

        public class LanguageKnown
        {
            public string Language { get; set; }
            public string LanguageCode { get; set; }
        }

        public class PassportDetail
        {
            public string PassportNumber { get; set; }
            public string DateOfExpiry { get; set; }
            public string DateOfIssue { get; set; }
            public string PlaceOfIssue { get; set; }
        }

        public class Email
        {
            public string EmailAddress { get; set; }
            public int ConfidenceScore { get; set; }
        }

        public class PhoneNumber
        {
            public string Number { get; set; }
            public string ISDCode { get; set; }
            public string OriginalNumber { get; set; }
            public string FormattedNumber { get; set; }
            public string Type { get; set; }
            public int ConfidenceScore { get; set; }
        }

        public class WebSite
        {
            public string Type { get; set; }
            public string Url { get; set; }
        }

        public class Address
        {
            public string Street { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string StateIsoCode { get; set; }
            public string Country { get; set; }
            public CountryCode CountryCode { get; set; }
            public string ZipCode { get; set; }
            public string FormattedAddress { get; set; }
            public string Type { get; set; }
            public int ConfidenceScore { get; set; }
        }

        public class CurrentSalary
        {
            public string Amount { get; set; }
            public string Symbol { get; set; }
            public string Currency { get; set; }
            public string Unit { get; set; }
            public string Text { get; set; }
        }

        public class ExpectedSalary
        {
            public string Amount { get; set; }
            public string Symbol { get; set; }
            public string Currency { get; set; }
            public string Unit { get; set; }
            public string Text { get; set; }
        }

        public class Location
        {
            public string City { get; set; }
            public string State { get; set; }
            public string StateIsoCode { get; set; }
            public string Country { get; set; }
            public CountryCode CountryCode { get; set; }
        }

        public class Institution
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public int ConfidenceScore { get; set; }
            public Location Location { get; set; }
        }



        public class SubInstitution
        {
            public string Name { get; set; }
            public int ConfidenceScore { get; set; }
            public string Type { get; set; }
            public Location Location { get; set; }
        }

        public class Degree
        {
            public string DegreeName { get; set; }
            public string NormalizeDegree { get; set; }
            public List<object> Specialization { get; set; }
            public int ConfidenceScore { get; set; }
        }

        public class Aggregate
        {
            public string Value { get; set; }
            public string MeasureType { get; set; }
        }

        public class SegregatedQualification
        {
            public Institution Institution { get; set; }
            public SubInstitution SubInstitution { get; set; }
            public Degree Degree { get; set; }
            public string FormattedDegreePeriod { get; set; }
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public Aggregate Aggregate { get; set; }
        }

        public class SegregatedCertification
        {
            public string CertificationTitle { get; set; }
            public string Authority { get; set; }
            public string CertificationCode { get; set; }
            public string IsExpiry { get; set; }
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public string CertificationUrl { get; set; }
        }

        public class SegregatedSkill
        {
            public string Skill { get; set; }
            public string Type { get; set; }
            public string Evidence { get; set; }
            public int ExperienceInMonths { get; set; }
            public string LastUsed { get; set; }
            public string FormattedName { get; set; }
            public string Alias { get; set; }

            public string Ontology { get; set; }
        }

        public class Employer
        {
            public string EmployerName { get; set; }
            public string FormattedName { get; set; }
            public int ConfidenceScore { get; set; }
        }
        public class RelatedSkills
        {

            /// <summary>
            /// 
            /// </summary>
            public string Skill { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string ProficiencyLevel { get; set; }
        }
        public class JobProfile
        {
            public string Title { get; set; }
            public string FormattedName { get; set; }
            public string Alias { get; set; }
            public List<RelatedSkills> RelatedSkills { get; set; }
            public int ConfidenceScore { get; set; }
        }


        public class Project
        {
            public string UsedSkills { get; set; }
            public string ProjectName { get; set; }
            public string TeamSize { get; set; }
        }

        public class SegregatedExperience
        {
            public Employer Employer { get; set; }
            public JobProfile JobProfile { get; set; }
            public Location Location { get; set; }
            public string JobPeriod { get; set; }
            public string FormattedJobPeriod { get; set; }
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public string IsCurrentEmployer { get; set; }
            public string JobDescription { get; set; }
            public List<Project> Projects { get; set; }
        }

        public class WorkedPeriod
        {
            public string TotalExperienceInMonths { get; set; }
            public string TotalExperienceInYear { get; set; }
            public string TotalExperienceRange { get; set; }
        }

        public class SegregatedPublication
        {
            public string PublicationTitle { get; set; }
            public string Publisher { get; set; }
            public string PublicationNumber { get; set; }
            public string PublicationUrl { get; set; }
            public string Authors { get; set; }
            public string Description { get; set; }
        }


        public class CurrentLocation
        {
            public string City { get; set; }
            public string State { get; set; }
            public string StateIsoCode { get; set; }
            public string Country { get; set; }
            public CountryCode CountryCode { get; set; }
        }


        public class PreferredLocation
        {
            public string City { get; set; }
            public string State { get; set; }
            public string StateIsoCode { get; set; }
            public string Country { get; set; }
            public CountryCode CountryCode { get; set; }
        }

        public class SegregatedAchievement
        {
            public string AwardTitle { get; set; }
            public string Issuer { get; set; }
            public string AssociatedWith { get; set; }
            public string IssuingDate { get; set; }
            public string Description { get; set; }
        }

        public class EmailInfo
        {
            public string EmailTo { get; set; }
            public string EmailBody { get; set; }
            public string EmailReplyTo { get; set; }
            public string EmailSignature { get; set; }
            public string EmailFrom { get; set; }
            public string EmailSubject { get; set; }
            public string EmailCC { get; set; }
        }

        public class Recommendation
        {
            public string PersonName { get; set; }
            public string CompanyName { get; set; }
            public string Relation { get; set; }
            public string PositionTitle { get; set; }
            public string Description { get; set; }
        }

        public class CandidateImage
        {
            public string CandidateImageData { get; set; }
            public string CandidateImageFormat { get; set; }
        }

        public class TemplateOutput
        {
            public string TemplateOutputFileName { get; set; }
            public string TemplateOutputData { get; set; }
        }

        public class ApiInfo
        {
            public string Metered { get; set; }
            public string CreditLeft { get; set; }
            public string AccountExpiryDate { get; set; }
            public string BuildVersion { get; set; }
        }

        public class ResumeParserData
        {
            public string ResumeFileName { get; set; }
            public ResumeLanguage ResumeLanguage { get; set; }
            public string ParsingDate { get; set; }
            public ResumeCountry ResumeCountry { get; set; }
            public Name Name { get; set; }
            public string DateOfBirth { get; set; }
            public string Gender { get; set; }
            public string FatherName { get; set; }
            public string MotherName { get; set; }
            public string MaritalStatus { get; set; }
            public string Nationality { get; set; }
            public List<LanguageKnown> LanguageKnown { get; set; }
            public string UniqueID { get; set; }
            public string LicenseNo { get; set; }
            public PassportDetail PassportDetail { get; set; }
            public string PanNo { get; set; }
            public string VisaStatus { get; set; }
            public List<Email> Email { get; set; }
            public List<PhoneNumber> PhoneNumber { get; set; }
            public List<WebSite> WebSite { get; set; }
            public List<Address> Address { get; set; }
            public string Category { get; set; }
            public string SubCategory { get; set; }
            public CurrentSalary CurrentSalary { get; set; }
            public ExpectedSalary ExpectedSalary { get; set; }
            public string Qualification { get; set; }
            public List<SegregatedQualification> SegregatedQualification { get; set; }
            public string Certification { get; set; }
            public List<SegregatedCertification> SegregatedCertification { get; set; }
            public string SkillBlock { get; set; }
            public string SkillKeywords { get; set; }
            public List<SegregatedSkill> SegregatedSkill { get; set; }
            public string Experience { get; set; }
            public List<SegregatedExperience> SegregatedExperience { get; set; }
            public string CurrentEmployer { get; set; }
            public string JobProfile { get; set; }
            public WorkedPeriod WorkedPeriod { get; set; }
            public string GapPeriod { get; set; }
            public string AverageStay { get; set; }
            public string LongestStay { get; set; }
            public string Summary { get; set; }
            public string ExecutiveSummary { get; set; }
            public string ManagementSummary { get; set; }
            public string Coverletter { get; set; }
            public string Publication { get; set; }
            public List<SegregatedPublication> SegregatedPublication { get; set; }
            public List<CurrentLocation> CurrentLocation { get; set; }
            public List<PreferredLocation> PreferredLocation { get; set; }
            public string Availability { get; set; }
            public string Hobbies { get; set; }
            public string Objectives { get; set; }
            public string Achievements { get; set; }
            public List<SegregatedAchievement> SegregatedAchievement { get; set; }
            public string References { get; set; }
            public string CustomFields { get; set; }
            public EmailInfo EmailInfo { get; set; }
            public List<Recommendation> Recommendations { get; set; }
            public string DetailResume { get; set; }
            public string HtmlResume { get; set; }
            public CandidateImage CandidateImage { get; set; }
            public TemplateOutput TemplateOutput { get; set; }
            public ApiInfo ApiInfo { get; set; }
        }

        public class RchilliMapFields
        {
            public ResumeParserData ResumeParserData { get; set; }
        }
    }
}
