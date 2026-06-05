using System;

namespace Risebird.Entities.ViewModel
{
    public partial class TransInterviewer
    {
        public int id { get; set; }
        public int IRFId { get; set; }
        public int UserId { get; set; }
        public int InterviewerId { get; set; }
        public string PrimarySkill { get; set; }
        public string EmailId { get; set; }
        public int PerSuccessRate { get; set; }
        public bool IsActive { get; set; }
        public bool IsAvailable { get; set; }

        public string ResumeLink { get; set; }
        public string CodedResumeLink { get; set; }
        public string NickName { get; set; }
        public bool IsSelect { get; set; }
        public bool IsSMEVerified { get; set; }
        public int Surcharge { get; set; }
        public string Source { get; set; }
        public string CompanyName { get; set; }
        /// <summary>InterviewerProfile employment note; Insights uses when CompanyName is empty.</summary>
        public string EmploymentComment { get; set; }
        public string ClientAction { get; set; }
        public bool IsResumeRedactAttempted { get; set; }
        public bool IsCampaignActivity { get; set; }
        public string CampaignName { get; set; }
        public string OpenAIScreeningResponse { get; set; }
        public string JDTitle { get; set; }
        public string JDLink { get; set; }
        public string AIVerdict { get; set; }
        public string ChannelOwner { get; set; }
        public string ClientHr { get; set; }
        public string IRFName { get; set; }
        public int CampaignId { get; set; }
        public bool IsSurchargeApprove { get; set; }
        public string ConvertedFromDocResumeLink { get; set; }
        public DateTime CreatedOn { get; set; }
        public string InterviewerProfileAddedBy { get; set; }
        public string SkillChannelAssessmentGroup { get; set; }
        public int AIScreeningScore { get; set; }
        public int RedMarker { get; set; }
        public int TotalFuture { get; set; }
        public int TotalInterview { get; set; }
        public string RedMarkerReviewComment { get; set; }
        public int NegativeAIVerdictStatus { get; set; }

        /// <summary>NULL = no cap configured (unlimited). 0 = explicit zero cap. &gt;0 = limit.</summary>
        public int? InterviewCapacity { get; set; }

        public DateTime? ModifiedAt { get; set; }

    }
}
