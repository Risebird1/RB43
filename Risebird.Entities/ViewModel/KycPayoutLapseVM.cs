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
}
