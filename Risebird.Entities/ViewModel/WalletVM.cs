using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class ClientWalletDashboardVM
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public int BillingType { get; set; }
        public int BillingModel { get; set; }
        public string BillingModelDesc { get; set; }
        public int SelectedMonth { get; set; }
        public int SelectedYear { get; set; }
        public string PeriodLabel { get; set; }
        public decimal CashCreditBalance { get; set; }
        public decimal CashCreditAdded { get; set; }
        public decimal CashDebitConsumed { get; set; }
        public decimal CashRemaining { get; set; }
        public decimal POCreditLimit { get; set; }
        public decimal POCashReceived { get; set; }
        public decimal POCashRemaining { get; set; }
        public decimal POConsumed { get; set; }
        public decimal PORemaining { get; set; }
        public decimal MonthCredit { get; set; }
        public decimal MonthDebit { get; set; }
        public decimal OverallCredit { get; set; }
        public decimal OverallDebit { get; set; }
        public int POCreditUtilizationPercent { get; set; }
        public bool SupportsPO { get; set; }
        public bool IsAllDetailsView { get; set; }
        public string DetailsPeriodLabel { get; set; }
        public List<OwnerWalletRowVM> OwnerWallets { get; set; } = new List<OwnerWalletRowVM>();
        public List<ClientPOVM> PORecords { get; set; } = new List<ClientPOVM>();
    }

    public class ClientPOVM
    {
        public int id { get; set; }
        public int ClientId { get; set; }
        public int BillingType { get; set; }
        public int? BillingOwnerId { get; set; }
        public int BillingModel { get; set; }
        public string PONumber { get; set; }
        public decimal POCreditAmount { get; set; }
        public decimal CashCreditReceived { get; set; }
        public decimal PendingCashCredit => POCreditAmount - CashCreditReceived;
        public int POMonth { get; set; }
        public int POYear { get; set; }
        public string PODocumentUrl { get; set; }
        public int Status { get; set; }
        public string StatusDesc { get; set; }
        public string StatusCss { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string BillingOwnerEmail { get; set; }
        public string BillingOwnerName { get; set; }
        public string PeriodLabel { get; set; }
    }

    public class OwnerWalletRowVM
    {
        public int BillingOwnerId { get; set; }
        public string BillingOwnerName { get; set; }
        public string BillingOwnerEmail { get; set; }
        public decimal POCredit { get; set; }
        public decimal Consumed { get; set; }
        public decimal Remaining { get; set; }
        public decimal CashBalance { get; set; }
        public string State { get; set; }
        public string StateCss { get; set; }
    }

    public class WalletVM
    {
        public int id { get; set; }
        public int BillingType { get; set; } // 1: Billing Owner (Client Users), 3: Client
        public int ClientId { get; set; }
        public int BillingOwnerId { get; set; }
        public string CreatedBy { get; set; }
        public int Credit { get; set; }
        public int Debit { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string Comment { get; set; }
        public int Balance { get; set; }
        public int InterviewId { get; set; }
        public int IRFId { get; set; }
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
        public int MeetingStatus { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string TransactionId { get; set; }
        public string TransactionRefNumber { get; set; }
        public string InvoicePDF { get; set; }
        public int? ClientPOId { get; set; }
        public string PONumber { get; set; }
    }
}

