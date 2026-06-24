namespace Risebird.Entities.ViewModel
{
    /// <summary>Video KYC fields from InterviewerProfile (DM Approval V4).</summary>
    public partial class TransInterviewer
    {
        public string VideoLink { get; set; }
        public bool IsVideoApproved { get; set; }
    }
}
