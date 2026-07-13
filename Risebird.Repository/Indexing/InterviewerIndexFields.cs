using System.Collections.Generic;

namespace Risebird.Repository.Indexing
{
    /// <summary>
    /// Azure AI Search field names for the interviewer roster index (<c>interviewerindex</c>).
    /// </summary>
    internal static class InterviewerIndexFields
    {
        public const string Key = "interviewerProfileId";
        public const string UserId = "userId";
        public const string FirstName = "firstName";
        public const string LastName = "lastName";
        public const string NickName = "nickName";
        public const string EmailId = "emailId";
        public const string MobileNumber = "mobileNumber";
        public const string PrimarySkill = "primarySkill";
        public const string Skills = "skills";
        public const string AssessmentGroupId = "assessmentGroupId";
        public const string AssessmentGroupNames = "assessmentGroupNames";
        public const string Senioritylevel = "senioritylevel";
        public const string PerHourRateIndia = "perHourRateIndia";
        public const string PerHourRateOutside = "perHourRateOutside";
        public const string CompentencyLevel = "compentencyLevel";
        public const string ProfileType = "profileType";
        public const string CompanyType = "companyType";
        public const string Country = "country";
        public const string IsLeadInterviewer = "isLeadInterviewer";
        public const string IsActive = "isActive";
        public const string IsInterviewerAccess = "isInterviewerAccess";
        public const string IsApproved = "isApproved";
        public const string IsInterested = "isInterested";
        public const string IsDeleted = "isDeleted";
        public const string Isavailable = "isavailable";
        public const string AvailabilityStatusValue = "availabilityStatusValue";
        public const string VideoConfirmation = "videoConfirmation";
        public const string ResumeLink = "resumeLink";
        public const string CodedResumeLink = "codedResumeLink";
        public const string City = "city";
        public const string CurrentRole = "currentRole";
        public const string Experience = "experience";
        public const string PreferredDomain = "preferredDomain";
        public const string UniqueId = "uniqueid";
        public const string AvailabilityResponseDate = "availabilityResponseDate";
        public const string IsHoldPayment = "isHoldPayment";

        public static readonly IReadOnlyList<string> ListViewSearch = new[]
        {
            FirstName,
            LastName,
            EmailId,
            NickName,
            MobileNumber,
            PrimarySkill
        };
    }
}
