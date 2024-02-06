using System;

namespace Risebird.Entities.ViewModel
{
    public class MoneyProtectionVM
    {
        public int id { get; set; }
        public DateTime? AddedOn { get; set; }
        public string Comment { get; set; }
        public bool IsConverted { get; set; }
        public string ClientName { get; set; }
        public int Point { get; set; }
        public int BillingType { get; set; }
        public DateTime? interviewDate { get; set; }
        public string candidateEmail { get; set; }
        public string IRFId { get; set; }
        public int TotalInterviews { get; set; }
        public int Heuristicsscore { get; set; }
        public string HeuristicsscoreDesc { get; set; }
        public int PerInterviewRate { get; set; }
        public int Interviewid { get; set; }
        public int PaymentComplianceCount { get; set; }
        public string ClientHrContact{ get; set; }
    }
}
