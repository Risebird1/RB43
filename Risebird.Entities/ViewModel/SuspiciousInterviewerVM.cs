using System;

namespace Risebird.Entities.ViewModel
{
    public class SuspiciousInterviewerVM
    {
        public string EmailId { get; set; }
        public string Comment { get; set; }
        public DateTime Createddate { get; set; }
        public string CreatedBy { get; set; }
        public bool SuspiciousStatus { get; set; }
        /// <summary>Page the mark was made from (TotalInterviews, TRM, PotentialRiskCandidate, TrialFlagReview, PaymentSla, HighRisk, 911:&lt;action&gt;).</summary>
        public string SourcePage { get; set; }
    }
}
