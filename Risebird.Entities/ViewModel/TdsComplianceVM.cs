using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Risebird.Entities.ViewModel
{
    // -----------------------------------------------------------------------
    // FIXED CONSTANTS — NEVER allow the UI to override these
    // -----------------------------------------------------------------------
    public static class TdsConstants
    {
        public const string DeducteeCode   = "01";
        public const string SectionCode    = "194JB";
        public const decimal Rate          = 10m;
        public const string MinorHead      = "200";
        public const string ReturnType     = "26Q";
        public const string DeductorName   = "ARHAT TALENT SOLUTIONS PRIVATE LIMITED";
        public const string CompanyTAN     = "BLRA32127E";
    }

    // -----------------------------------------------------------------------
    // Master VM for the QuarterlyTds view
    // -----------------------------------------------------------------------
    public class TdsComplianceVM
    {
        public string SelectedYear     { get; set; }
        public string SelectedQuarter  { get; set; }
        public string QuarterRange     { get; set; }

        public List<SelectListItem> FinancialYears { get; set; } = new();
        public List<SelectListItem> Quarters       { get; set; } = new();

        // Tab 1 — wallet-verified interviewers WITH PAN included in filing
        public IList<InterviewerQuarterlyTdsRow> Deductees       { get; set; } = new List<InterviewerQuarterlyTdsRow>();
        // Tab 2 — excluded (unverified wallet OR no PAN)
        public IList<InterviewerQuarterlyTdsRow> LapsedDeductees { get; set; } = new List<InterviewerQuarterlyTdsRow>();

        public IList<TdsChallanVM>      Challans      { get; set; } = new List<TdsChallanVM>();
        public IList<WeeklyPayoutVM>    WeeklyPayouts { get; set; } = new List<WeeklyPayoutVM>();
        public IList<PendingTdsWeekVM>  PendingByWeek { get; set; } = new List<PendingTdsWeekVM>();
        public TdsReturnFilingVM        Filing        { get; set; } = new TdsReturnFilingVM();

        // Reconciliation bar
        public int     InterviewerPaidTotal { get; set; }   // Σ gross, verified only
        public int     InterviewerTdsTotal  { get; set; }   // Σ TDS (10%), verified only
        public decimal ChallanTdsTotal      { get; set; }   // Σ challan TDS amounts
        public int     ChallanCount         { get; set; }

        public decimal Difference   => InterviewerTdsTotal - ChallanTdsTotal;
        public bool    IsReconciled => Difference == 0;

        // Pre-filled challan defaults
        public string DefaultTAN      => TdsConstants.CompanyTAN;
        public string DefaultDeductor => TdsConstants.DeductorName;
    }

    // -----------------------------------------------------------------------
    // One row per interviewer in the quarterly TDS table
    // -----------------------------------------------------------------------
    public class InterviewerQuarterlyTdsRow
    {
        public int      Userid    { get; set; }
        public string   FirstName { get; set; }
        public string   MiddleName { get; set; } = "";  // TODO: add MiddleName to UserList if required by tax authority
        public string   LastName  { get; set; }
        public string   Email     { get; set; }
        public string   PAN       { get; set; }
        public string   AccountNumber { get; set; }
        public string   IFSCCode      { get; set; }
        public string   AccountName   { get; set; }

        public bool IsWalletVerified { get; set; }

        // Fixed constants
        public string  DeducteeCode => TdsConstants.DeducteeCode;
        public string  SectionCode  => TdsConstants.SectionCode;
        public decimal Rate         => TdsConstants.Rate;

        public int       TotalPaid { get; set; }
        public int       Tds       { get; set; }
        public DateTime? DatePaid  { get; set; }

        // Linked challan serial numbers (all challans this quarter for this interviewer)
        public List<string> ChallanNumbers { get; set; } = new();

        // Individual payment rows for the quarter (used by Date Paid popover)
        public List<PaymentHistoryItem> PaymentItems { get; set; } = new();

        // Form 16A certificate blob URL (from InterviewerCertificates table)
        public string Form16Link { get; set; }

        public bool HasPan          => !string.IsNullOrWhiteSpace(PAN);
        public bool IncludeInFiling => IsWalletVerified && HasPan;
    }

    // -----------------------------------------------------------------------
    // Challan (one per weekly govt deposit)
    // -----------------------------------------------------------------------
    public class TdsChallanVM
    {
        public int    id           { get; set; }
        public string FinancialYear { get; set; }
        public string Quarter       { get; set; }
        public int    WeekNumber    { get; set; }
        public int    SrNo         { get; set; }

        public string  SectionCode  { get; set; } = TdsConstants.SectionCode;
        public string  TANNumber    { get; set; } = TdsConstants.CompanyTAN;
        public string  Deductor     { get; set; } = TdsConstants.DeductorName;
        public decimal TdsAmount    { get; set; }
        public string  BSRCode      { get; set; }
        public DateTime? DepositDate { get; set; }
        public string  ChallanSerialNumber    { get; set; }
        public string  MinorHead              { get; set; } = TdsConstants.MinorHead;
        public string  ChallanReferenceNumber { get; set; }
        public string  ChallanPdfPath         { get; set; }
        public string  PayoutReference        { get; set; }
        public DateTime? PaymentDate          { get; set; }
        public bool    IsPaid                 { get; set; }

        public List<ChallanInterviewerVM> Interviewers { get; set; } = new();
    }

    public class ChallanInterviewerVM
    {
        public int     Userid     { get; set; }
        public string  FullName   { get; set; }
        public string  PAN        { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal TdsAmount  { get; set; }
    }

    // -----------------------------------------------------------------------
    // Weekly payout status (Tab 3)
    // -----------------------------------------------------------------------
    public class WeeklyPayoutVM
    {
        public int    WeekNumber       { get; set; }
        public string WeekRange        { get; set; }
        public int    TotalPaid        { get; set; }
        public int    TdsDue           { get; set; }
        public int    InterviewerCount { get; set; }
        public bool   IsPaid           { get; set; }
        public int?   ChallanId        { get; set; }
    }

    // -----------------------------------------------------------------------
    // Pending TDS tracker (Tab 5)
    // -----------------------------------------------------------------------
    public class PendingTdsWeekVM
    {
        public int    WeekNumber    { get; set; }
        public string WeekRange     { get; set; }
        public int    TdsDue        { get; set; }
        public int    TdsDeposited  { get; set; }
        public int    Outstanding   { get; set; }
        public bool   IsPaid        { get; set; }
    }

    // -----------------------------------------------------------------------
    // Return filing (Tab 6)
    // -----------------------------------------------------------------------
    public class TdsReturnFilingVM
    {
        public int      id                    { get; set; }
        public string   FinancialYear         { get; set; }
        public string   Quarter               { get; set; }
        public string   ReturnType            { get; set; } = TdsConstants.ReturnType;
        public DateTime? FilingDate           { get; set; }
        public string   AcknowledgementNumber { get; set; }
        public string   TokenNumber           { get; set; }
        public string   Status                { get; set; }
        public string   Remarks               { get; set; }
    }

    // -----------------------------------------------------------------------
    // Form 16A upload result (Tab 7)
    // -----------------------------------------------------------------------
    public class Form16UploadResultVM
    {
        public List<string> Matched   { get; set; } = new();
        public List<string> Unmatched { get; set; } = new();
        public List<string> Skipped   { get; set; } = new();
    }

    // -----------------------------------------------------------------------
    // Interviewer info from PAN lookup
    // -----------------------------------------------------------------------
    public class InterviewerPanInfo
    {
        public int    Userid    { get; set; }
        public string Email     { get; set; }
        public string FirstName { get; set; }
        public string LastName  { get; set; }
        public string PAN       { get; set; }
    }

    // -----------------------------------------------------------------------
    // Used by the lapse reminder job
    // -----------------------------------------------------------------------
    public class UnverifiedWalletInterviewer
    {
        public int      Userid                  { get; set; }
        public string   Email                   { get; set; }
        public string   FirstName               { get; set; }
        public string   LastName                { get; set; }
        public DateTime FirstUnclaimedDate      { get; set; }
        public decimal  TotalUnpaid             { get; set; }
        public int      DaysSinceFirstUnclaimed { get; set; }
        public string   PaymentReference        { get; set; }
    }

    // -----------------------------------------------------------------------
    // Individual payment row (for the Date Paid popover breakdown)
    // -----------------------------------------------------------------------
    public class PaymentHistoryItem
    {
        public int       Userid               { get; set; }
        public DateTime? PaymentDate          { get; set; }
        public int       TotalAmount          { get; set; }
        public string    Comment              { get; set; }
        public string    ChallanSerialNumber  { get; set; }

        // Replaces "M/d/yyyy h:mm:ss tt" datetime substrings in Comment with "dd-MMM-yyyy".
        // Uses InvariantCulture + TryParseExact so month/day order is always treated as M/d/yyyy
        // regardless of the server's regional settings (e.g. "5/22/2021" = 22-May-2021).
        public string FormattedComment
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Comment)) return "";
                return Regex.Replace(
                    Comment,
                    @"\d{1,2}/\d{1,2}/\d{4} \d{1,2}:\d{2}:\d{2} [AP]M",
                    m => DateTime.TryParseExact(
                             m.Value,
                             "M/d/yyyy h:mm:ss tt",
                             CultureInfo.InvariantCulture,
                             DateTimeStyles.None,
                             out var dt)
                         ? dt.ToString("dd-MMM-yyyy")
                         : m.Value);
            }
        }
    }

    // -----------------------------------------------------------------------
    // Challan number link (for building ChallanNumbers per interviewer)
    // -----------------------------------------------------------------------
    public class InterviewerChallanLink
    {
        public int    Userid                  { get; set; }
        public string PAN                     { get; set; }
        public string ChallanSerialNumber     { get; set; }
        public string ChallanReferenceNumber  { get; set; }
    }
}
