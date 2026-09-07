using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.ViewModel
{
    // ── All-clients wallet summary page ──────────────────────────────────────────
    public class BillingOwnerInterviewCountVM
    {
        public int     ClientId             { get; set; }
        public int     BillingOwnerId       { get; set; }
        public string  BillingOwnerEmail    { get; set; }
        public string  BillingOwnerName     { get; set; }
        public int     MonthInterviewCount  { get; set; }
        public decimal MonthCredit          { get; set; }
        public decimal MonthDebit           { get; set; }
        public decimal MonthNet             => MonthCredit - MonthDebit;
    }

    public class ClientWalletSummaryRowVM
    {
        public int     ClientId            { get; set; }
        public string  ClientName          { get; set; }
        public int     BillingType         { get; set; }   // 1=User Scope, 3=Client Scope
        public int     BillingModel        { get; set; }
        public string  BillingModelDesc    { get; set; }
        public int     PerInterviewRate    { get; set; }
        public int     AIPerInterviewRate  { get; set; }
        public int     MonthInterviewCount { get; set; }
        public decimal MonthCredit         { get; set; }
        public decimal MonthDebit          { get; set; }
        public decimal MonthNet            => MonthCredit - MonthDebit;
        public decimal OverallBalance      { get; set; }
        public int     TransactionCount    { get; set; }
        public string  BillingCurrency     { get; set; } = "INR";
        public string  CurrencySymbol      => BillingCurrency == "USD" ? "$" : "₹";
    }

    public class WalletCurrencyTotalVM
    {
        public string  Currency       { get; set; }
        public string  Symbol         { get; set; }
        public decimal MonthCredit    { get; set; }
        public decimal MonthDebit     { get; set; }
        public decimal MonthNet       => MonthCredit - MonthDebit;
        public decimal OverallBalance { get; set; }
    }

    public class AllClientsWalletVM
    {
        public int    FromMonth          { get; set; }
        public int    FromYear           { get; set; }
        public int    ToMonth            { get; set; }
        public int    ToYear             { get; set; }

        // Legacy aliases — point to the "From" side so existing code keeps compiling
        public int    SelectedMonth      { get => FromMonth; set => FromMonth = value; }
        public int    SelectedYear       { get => FromYear;  set => FromYear  = value; }

        public string PeriodLabel        { get; set; }
        public List<ClientWalletSummaryRowVM>     Clients            { get; set; } = new List<ClientWalletSummaryRowVM>();
        public List<BillingOwnerInterviewCountVM> BillingOwnerCounts { get; set; } = new List<BillingOwnerInterviewCountVM>();

        // Legacy single-currency totals (INR-only, kept for backward compat)
        public decimal TotalMonthCredit    => Clients.Sum(c => c.MonthCredit);
        public decimal TotalMonthDebit     => Clients.Sum(c => c.MonthDebit);
        public decimal TotalMonthNet       => TotalMonthCredit - TotalMonthDebit;
        public decimal TotalOverallBalance => Clients.Sum(c => c.OverallBalance);

        // Per-currency totals for mixed-currency display
        public List<WalletCurrencyTotalVM> TotalsByCurrency =>
            Clients
                .GroupBy(c => string.IsNullOrEmpty(c.BillingCurrency) ? "INR" : c.BillingCurrency)
                .OrderBy(g => g.Key)
                .Select(g => new WalletCurrencyTotalVM
                {
                    Currency       = g.Key,
                    Symbol         = g.Key == "USD" ? "$" : "₹",
                    MonthCredit    = g.Sum(c => c.MonthCredit),
                    MonthDebit     = g.Sum(c => c.MonthDebit),
                    OverallBalance = g.Sum(c => c.OverallBalance)
                })
                .ToList();

        public bool HasMixedCurrencies =>
            Clients.Select(c => string.IsNullOrEmpty(c.BillingCurrency) ? "INR" : c.BillingCurrency)
                   .Distinct().Count() > 1;
    }

    // ── Existing VMs (unchanged) ─────────────────────────────────────────────────
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

        /// <summary>Most recently added agreement for this client. Null when none exist yet.</summary>
        public ClientAgreementVM LatestAgreement { get; set; }
    }

    public class ClientPOVM
    {
        public int id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }     // populated by all-clients SP
        public int BillingType { get; set; }
        public int? BillingOwnerId { get; set; }
        public int BillingModel { get; set; }
        public string PONumber { get; set; }
        public decimal POCreditAmount { get; set; }
        public decimal CashCreditReceived { get; set; }
        public decimal PendingCashCredit => POCreditAmount - CashCreditReceived;
        public int POMonth { get; set; }
        public int POYear { get; set; }
        public int? POQuantity { get; set; }
        /// <summary>User-facing added date. Falls back to CreatedOn when null (legacy rows).</summary>
        public DateTime? AddedDate { get; set; }
        /// <summary>Optional PO expiry date. May be null.</summary>
        public DateTime? ExpireDate { get; set; }
        public string PODocumentUrl { get; set; }
        public int Status { get; set; }
        public string StatusDesc { get; set; }
        public string StatusCss { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        /// <summary>Display date for Added On — prefers AddedDate, else CreatedOn.</summary>
        public DateTime? EffectiveAddedDate => AddedDate ?? CreatedOn;
        public string BillingOwnerEmail { get; set; }
        public string BillingOwnerName { get; set; }
        public string PeriodLabel { get; set; }

        /// <summary>Actual wallet debit (interview charges) consumed against this PO — populated by FIFO distribution in WalletService.</summary>
        public decimal WalletConsumed { get; set; }
        /// <summary>Negative means debits exceeded PO credit (over-consumed). Not clamped to 0.</summary>
        public decimal WalletRemaining => POCreditAmount - WalletConsumed;

        /// <summary>True when ExpireDate is set and before today.</summary>
        public bool IsExpired =>
            ExpireDate.HasValue && ExpireDate.Value.Date < DateTime.Today;

        /// <summary>True when ExpireDate falls within the next 30 days (inclusive).</summary>
        public bool IsExpiringSoon =>
            ExpireDate.HasValue && !IsExpired && ExpireDate.Value.Date <= DateTime.Today.AddDays(30);

        public int? DaysToExpiry =>
            ExpireDate.HasValue ? (int)(ExpireDate.Value.Date - DateTime.Today).TotalDays : null;

        public string ExpiryStatusDesc =>
            !ExpireDate.HasValue ? "No Expiry"
            : IsExpired ? "Expired"
            : IsExpiringSoon ? "Expiring Soon"
            : "Active";

        public string ExpiryStatusCss =>
            !ExpireDate.HasValue ? "badge-secondary"
            : IsExpired ? "badge-danger"
            : IsExpiringSoon ? "badge-warning"
            : "badge-success";

        /// <summary>PO can absorb wallet debits for the given calendar month.</summary>
        public bool IsEligibleForDebitMonth(int year, int month)
        {
            if (!ExpireDate.HasValue) return true;
            return ExpireDate.Value.Date >= new DateTime(year, month, 1);
        }
    }

    /// <summary>Groups all FIFO-computed POs for one client, used on the PO Summary report page.</summary>
    public class ClientPOGroupVM
    {
        public int    ClientId         { get; set; }
        public string ClientName       { get; set; }
        public int    BillingType      { get; set; }   // 1=Billing Owner, 3=Client
        public string BillingModelDesc { get; set; }
        public List<ClientPOVM> POs    { get; set; } = new List<ClientPOVM>();
        public decimal TotalCredit     => POs.Sum(p => p.POCreditAmount);
        public decimal TotalConsumed   => POs.Sum(p => p.WalletConsumed);
        public decimal TotalExpiredNotConsumed => POs.Where(p => p.IsExpired).Sum(p => Math.Max(0m, p.WalletRemaining));
        /// <summary>PO Opening Balance (0 all-time) + Credit − Consumed − Expired Not Consumed.</summary>
        public decimal TotalUsableRemaining => TotalCredit - TotalConsumed - TotalExpiredNotConsumed;
        public decimal TotalRemaining => TotalUsableRemaining;
    }

    /// <summary>
    /// One row on the PO Consumption Alert page — latest PO per billing owner >= threshold consumed.
    /// </summary>
    public class POConsumptionAlertRowVM
    {
        public int    ClientId          { get; set; }
        public string ClientName        { get; set; }
        public int    BillingType       { get; set; }
        public string BillingModelDesc  { get; set; }
        public string BillingTypeName   => BillingType == 1 ? "Billing Owner" : "Client";
        public int?   BillingOwnerId    { get; set; }
        public string BillingOwnerName  { get; set; }
        public string BillingOwnerEmail { get; set; }
        public string  PONumber         { get; set; }
        public int     POMonth          { get; set; }
        public int     POYear           { get; set; }
        public string  POPeriodLabel    { get; set; }
        public decimal POCreditAmount   { get; set; }
        public decimal WalletConsumed   { get; set; }
        /// <summary>Negative means over-consumed. Not clamped to 0.</summary>
        public decimal WalletRemaining  => POCreditAmount - WalletConsumed;

        /// <summary>All-time owner/client totals for Remaining formula (0 opening on reports).</summary>
        public decimal POOpeningBalance { get; set; }
        public decimal OwnerTotalCredit { get; set; }
        public decimal OwnerTotalConsumed { get; set; }
        public decimal ExpiredNotConsumed { get; set; }
        /// <summary>PO Opening Balance + Owner Total Credit − Owner Total Consumed − Expired Not Consumed.</summary>
        public decimal UsableRemaining => POOpeningBalance + OwnerTotalCredit - OwnerTotalConsumed - ExpiredNotConsumed;

        public int     ConsumedPct      => POCreditAmount > 0
                                            ? (int)Math.Round(WalletConsumed / POCreditAmount * 100)
                                            : 0;

        public DateTime? ExpireDate     { get; set; }
        /// <summary>Consumption or Expiry</summary>
        public string  AlertType        { get; set; } = "Consumption";

        public bool IsExpired =>
            ExpireDate.HasValue && ExpireDate.Value.Date < DateTime.Today;

        public bool IsExpiringSoon =>
            ExpireDate.HasValue && !IsExpired && ExpireDate.Value.Date <= DateTime.Today.AddDays(30);

        public string ExpiryStatusDesc =>
            !ExpireDate.HasValue ? "No Expiry"
            : IsExpired ? "Expired"
            : IsExpiringSoon ? "Expiring Soon"
            : "Active";

        public string ExpiryStatusCss =>
            !ExpireDate.HasValue ? "badge-secondary"
            : IsExpired ? "badge-danger"
            : IsExpiringSoon ? "badge-warning"
            : "badge-success";
    }

    /// <summary>Total wallet debits per billing owner (or client) per calendar month, used for FIFO distribution.</summary>
    public class WalletMonthDebitVM
    {
        public int  ClientId       { get; set; }   // populated only for all-clients queries
        public int? BillingOwnerId { get; set; }   // null = client-scope (BillingType 3)
        public int  BillingType    { get; set; }
        public int  Month          { get; set; }
        public int  Year           { get; set; }
        public decimal TotalDebit  { get; set; }
    }

    public class OwnerWalletRowVM
    {
        public int BillingOwnerId { get; set; }
        public string BillingOwnerName { get; set; }
        public string BillingOwnerEmail { get; set; }
        /// <summary>Gross PO credit registered in periods BEFORE the selected month (carry-forward pool).</summary>
        public decimal POOpeningBalance { get; set; }
        public decimal POCredit { get; set; }
        public decimal Consumed { get; set; }
        /// <summary>Set after column values are loaded; mirrors <see cref="UsableRemaining"/>.</summary>
        public decimal Remaining { get; set; }
        /// <summary>Unconsumed PO credit on expired POs (FIFO-computed, all-time).</summary>
        public decimal ExpiredNotConsumed { get; set; }

        /// <summary>PO Opening Balance + PO Credit − Consumed − Expired Not Consumed.</summary>
        public decimal UsableRemaining => POOpeningBalance + POCredit - Consumed - ExpiredNotConsumed;

        public decimal CashBalance { get; set; }
        public string State { get; set; }
        public string StateCss { get; set; }
    }

    public class WalletVM
    {
        public int id { get; set; }
        public int BillingType { get; set; }
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
