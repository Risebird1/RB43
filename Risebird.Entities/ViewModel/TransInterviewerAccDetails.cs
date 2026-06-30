using System;

namespace Risebird.Entities.ViewModel
{
    public class TransInterviewerAccDetails
    {
        public int id { get; set; }
        public int InterviewerProfileId { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public string UPIId { get; set; }
        public string PAN { get; set; }
        public string AccountLocation { get; set; }

        // Bank verification
        public bool IsBankVerified { get; set; }
        public DateTime? VerifiedOn { get; set; }
        public string VerifiedAccountNumber { get; set; }
        public string VerifiedIFSCCode { get; set; }
        public string VerifiedAccountName { get; set; }
        public string NameAtBank { get; set; }
        public long? CashfreeReferenceId { get; set; }

        // PAN verification
        public bool IsPanVerified { get; set; }
        public DateTime? PanVerifiedOn { get; set; }
        public string VerifiedPAN { get; set; }
        public string NameAtPan { get; set; }
        public long? PanVerificationReferenceId { get; set; }
    }
}
