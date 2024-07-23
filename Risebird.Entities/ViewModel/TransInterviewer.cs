namespace Risebird.Entities.ViewModel
{
    public class TransInterviewer
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
        public string ClientAction { get; set; }
        public bool IsResumeRedactAttempted { get; set; }
        public bool IsCampaignActivity { get; set; }
        public string CampaignName { get; set; }
        public string OpenAIScreeningResponse { get; set; }
        public string JDTitle { get; set; }
        public string AIVerdict { get; set; }
        public string ChannelOwner { get; set; }
        public string ClientHr { get; set; }
        public string IRFName { get; set; }
        public int CampaignId { get; set; }
        public bool IsSurchargeApprove { get; set; }

    }
}
