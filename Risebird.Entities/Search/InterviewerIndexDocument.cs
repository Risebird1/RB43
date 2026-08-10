using System;
using System.Collections.Generic;

namespace Risebird.Entities.Search
{
    /// <summary>
    /// Denormalized interviewer roster document for Azure AI Search index <c>interviewerindex</c>.
    /// Mirrors <c>usp_GetInterviewerIndexBatch</c> / <c>/Interviewer/Index</c> list row shape.
    /// </summary>
    public class InterviewerIndexDocument
    {
        /// <summary>Azure Search document key (<c>InterviewerProfile.Id</c>).</summary>
        public int InterviewerProfileId { get; set; }

        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }
        public string EmailId { get; set; }
        public string MobileNumber { get; set; }

        /// <summary>Comma-separated skill names (same as <c>usp_GetInterviewerDetails</c>).</summary>
        public string PrimarySkill { get; set; }

        /// <summary>Skill names as a collection for OData <c>any</c> filters.</summary>
        public IList<string> Skills { get; set; } = new List<string>();

        /// <summary>Comma-separated assessment group ids (raw, resolved to names in the UI).</summary>
        public string AssessmentGroupId { get; set; }

        /// <summary>Resolved assessment group names for search/filter.</summary>
        public IList<string> AssessmentGroupNames { get; set; } = new List<string>();

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
