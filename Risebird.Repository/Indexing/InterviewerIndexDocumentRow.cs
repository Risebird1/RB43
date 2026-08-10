using System;

namespace Risebird.Repository.Indexing
{
    /// <summary>Dapper row shape from <c>usp_GetInterviewerIndexBatch</c>.</summary>
    internal sealed class InterviewerIndexDocumentRow
    {
        public int InterviewerProfileId { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }
        public string EmailId { get; set; }
        public string MobileNumber { get; set; }
        public string PrimarySkill { get; set; }
        public string AssessmentGroupId { get; set; }
        public string AssessmentGroupNames { get; set; }
        public string Senioritylevel { get; set; }
        public string PerHourRateIndia { get; set; }
        public string PerHourRateOutside { get; set; }
        public string CompentencyLevel { get; set; }
        public string ProfileType { get; set; }
        public string CompanyType { get; set; }
        public string Country { get; set; }
        public bool IsLeadInterviewer { get; set; }
        public bool IsActive { get; set; }
        public bool IsInterviewerAccess { get; set; }
        public bool IsApproved { get; set; }
        public bool IsInterested { get; set; }
        public bool IsDeleted { get; set; }
        public int Isavailable { get; set; }
        public string AvailabilityStatusValue { get; set; }
        public int VideoConfirmation { get; set; }
        public string ResumeLink { get; set; }
        public string CodedResumeLink { get; set; }
        public string City { get; set; }
        public string CurrentRole { get; set; }
        public decimal Experience { get; set; }
        public string PreferredDomain { get; set; }
        public string uniqueid { get; set; }
        public DateTime? AvailabilityResponseDate { get; set; }
        public bool IsHoldPayment { get; set; }
    }
}
