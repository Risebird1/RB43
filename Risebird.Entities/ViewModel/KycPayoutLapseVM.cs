using System;

namespace Risebird.Entities.ViewModel
{
    // A KYC-incomplete, 120+ day old (Interviewer, Month) candidate found by the Finance-page scan.
    public class KycLapseCandidate
    {
        public int      Userid        { get; set; }
        public string   Email         { get; set; }
        public string   FirstName     { get; set; }
        public string   LastName      { get; set; }
        public string   Month         { get; set; }
        public decimal  PendingAmount { get; set; }
        public bool     IsBankVerified { get; set; }
        public bool     IsPanVerified  { get; set; }
    }

    // An Active row in KycPayoutLapseTracking, re-checked against current KYC state,
    // used by the daily reminder/lapse job.
    public class KycLapseReminderTarget
    {
        public int      Id              { get; set; }
        public int      Userid          { get; set; }
        public string   Email           { get; set; }
        public string   FirstName       { get; set; }
        public string   LastName        { get; set; }
        public string   Month           { get; set; }
        public decimal  PendingAmount   { get; set; }
        public int      ReminderCount   { get; set; }
        public DateTime CreatedOn       { get; set; }
        public int      DaysInTracking  { get; set; }
        public bool     IsBankVerified  { get; set; }
        public bool     IsPanVerified   { get; set; }
    }

    // Full KycPayoutLapseTracking row (any TrackingStatus), for the Finance "Payment Lapsed" page.
    public class KycPayoutLapseTrackingRow
    {
        public int       Id              { get; set; }
        public int       Userid          { get; set; }
        public string    Email           { get; set; }
        public string    FirstName       { get; set; }
        public string    LastName        { get; set; }
        public string    PAN             { get; set; }
        public string    NameAtPan       { get; set; }
        public string    NameAtBank      { get; set; }
        public string    Month           { get; set; }
        public decimal?  PendingAmount   { get; set; }
        public int       ReminderCount   { get; set; }
        public string    TrackingStatus  { get; set; }
        public string    Remark          { get; set; }
        public DateTime  CreatedOn       { get; set; }
        public string    CreatedBy       { get; set; }
        public DateTime? LastReminderOn  { get; set; }
        public DateTime? KYCCompletedOn  { get; set; }
        public DateTime? LapsedOn        { get; set; }
        public bool      IsBankVerified  { get; set; }
        public bool      IsPanVerified   { get; set; }
    }

    // A (Interviewer, Month) group of InterviewPoints rows with Status = 'Lapsed',
    // for the Finance "Payment Lapsed" page's "Lapsed" tab.
    public class LapsedInterviewPointRow
    {
        public int     Userid         { get; set; }
        public string  Email          { get; set; }
        public string  FirstName      { get; set; }
        public string  LastName       { get; set; }
        public string  PAN            { get; set; }
        public string  NameAtPan      { get; set; }
        public string  NameAtBank     { get; set; }
        public string  Month          { get; set; }
        public decimal LapsedAmount   { get; set; }
        public bool    IsBankVerified { get; set; }
        public bool    IsPanVerified  { get; set; }
        // Reference back to PaymentHistory, same field Paid InterviewPoints rows carry.
        // Lapsed rows are never paid so this is normally null/0, kept for parity with Paid.
        public int?    PaymentHistoryId { get; set; }
    }
}
