using System;

namespace Risebird.Entities.ViewModel
{
    public class WalletVM
    {
        public int id { get; set; }
        public int BillingType { get; set; } // 1: Billing Owner (Client Users), 3: Client
        public int? ClientId { get; set; }
        public int? BillingOwnerId { get; set; }
        public int? CreatedBy { get; set; }
        public int Credit { get; set; }
        public int Debit { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string Comment { get; set; }
        public int Balance { get; set; }
        public int? InterviewId { get; set; }
        public int? IRFId { get; set; }
        public string TransactionType { get; set; }
        public string ClientName { get; set; }
        public string BillingOwnerEmail { get; set; }
        public string BillingOwnerName { get; set; }
        public string CreatedOnstring { get; set; }
        public string CandidateEmail { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string IRFName { get; set; }
        public string JobTitle { get; set; }
        public string JobTitleTwo { get; set; }
        public string CandidateName { get; set; }
        public int? MeetingStatus { get; set; }
    }
}

