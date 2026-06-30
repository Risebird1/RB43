using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class EmailLookupResultVM
    {
        public string SearchEmail { get; set; }
        public bool IsFound { get; set; }

        public int UserId { get; set; }
        public string ProfileType { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }
        public string MobileNumber { get; set; }
        public bool IsActive { get; set; }

        public string PrimarySkill { get; set; }
        public bool IsApproved { get; set; }
        public string CurrentRole { get; set; }
        public decimal Experience { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string CompanyName { get; set; }
        public bool IsLeadInterviewer { get; set; }
        public string NickName { get; set; }
        public bool IsHoldPayment { get; set; }
        public string HoldPaymentReason { get; set; }
        public bool IsInterviewerAccess { get; set; }
        public bool IsBlockedProfile { get; set; }
        public string PAN { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public string UPIId { get; set; }
        public bool IsBankVerified { get; set; }
        public DateTime? BankVerifiedOn { get; set; }
        public string VerifiedAccountNumber { get; set; }
        public string VerifiedAccountName { get; set; }
        public string NameAtBank { get; set; }
        public bool IsPanVerified { get; set; }
        public DateTime? PanVerifiedOn { get; set; }
        public string VerifiedPAN { get; set; }
        public string NameAtPan { get; set; }

        public decimal TotalHistoryPaid { get; set; }
        public List<EmailLookupTdsQuarterVM> TdsQuarters { get; set; } = new List<EmailLookupTdsQuarterVM>();

        public int CandidateTotalCount { get; set; }
        public int CandidateTotalCompleted { get; set; }
        public int CandidateTotalCancelled { get; set; }
        public int CandidateTotalScheduled { get; set; }

        public int InterviewerTotalCount { get; set; }
        public int InterviewerTotalCompleted { get; set; }
        public int InterviewerTotalCancelled { get; set; }
        public int InterviewerTotalScheduled { get; set; }

        public long TotalPaymentAmount { get; set; }
        public long TotalPaidAmount { get; set; }
        public long TotalPendingPayment { get; set; }

        public List<EmailLookupMeetingVM> MeetingsAsCandidate { get; set; } = new List<EmailLookupMeetingVM>();
        public List<EmailLookupMeetingVM> MeetingsAsInterviewer { get; set; } = new List<EmailLookupMeetingVM>();
        public List<EmailLookupPaymentVM> RecentPayments { get; set; } = new List<EmailLookupPaymentVM>();
    }

    public class EmailLookupUserInfoVM
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailId { get; set; }
        public string MobileNumber { get; set; }
        public bool IsActive { get; set; }
        public string ProfileType { get; set; }
    }

    public class EmailLookupInterviewerProfileVM
    {
        public int Id { get; set; }
        public string PrimarySkill { get; set; }
        public bool IsApproved { get; set; }
        public string CurrentRole { get; set; }
        public decimal Experience { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string CompanyName { get; set; }
        public bool IsLeadInterviewer { get; set; }
        public string NickName { get; set; }
        public bool IsHoldPayment { get; set; }
        public string HoldPaymentReason { get; set; }
        public bool IsInterviewerAccess { get; set; }
        public bool IsDeleted { get; set; }
        public string PAN { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string IFSCCode { get; set; }
        public string UPIId { get; set; }
        public bool IsBankVerified { get; set; }
        public DateTime? BankVerifiedOn { get; set; }
        public string VerifiedAccountNumber { get; set; }
        public string VerifiedAccountName { get; set; }
        public string NameAtBank { get; set; }
        public bool IsPanVerified { get; set; }
        public DateTime? PanVerifiedOn { get; set; }
        public string VerifiedPAN { get; set; }
        public string NameAtPan { get; set; }
    }

    public class EmailLookupInterviewStatsVM
    {
        public int TotalCount { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalCancelled { get; set; }
        public int TotalScheduled { get; set; }
    }

    public class EmailLookupPaymentStatsVM
    {
        public long TotalPaymentAmount { get; set; }
        public long TotalPaidAmount { get; set; }
        public long TotalPendingPayment { get; set; }
    }

    public class EmailLookupMeetingVM
    {
        public int Id { get; set; }
        public string IRFId { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string InterviewSlot { get; set; }
        public string Status { get; set; }
        public string CandidateEmail { get; set; }
        public string CandidateName { get; set; }
        public string Interviewer { get; set; }
    }

    public class EmailLookupPaymentVM
    {
        public int Id { get; set; }
        public DateTime? PaymentDate { get; set; }
        public int Amount { get; set; }
        public int TotalAmount { get; set; }
        public string Status { get; set; }
        public string Comment { get; set; }
        public string PayoutStatus { get; set; }
        public DateTime? TransactionDate { get; set; }
    }

    public class EmailLookupHistoryTotalVM
    {
        public decimal TotalHistoryPaid { get; set; }
    }

    public class EmailLookupTdsQuarterVM
    {
        public int ChallanId { get; set; }
        public string FinancialYear { get; set; }
        public string Quarter { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal TdsAmount { get; set; }
        public DateTime? DepositDate { get; set; }
        public bool IsPaid { get; set; }
        public string ChallanSerialNumber { get; set; }
        public string BSRCode { get; set; }
    }
}
