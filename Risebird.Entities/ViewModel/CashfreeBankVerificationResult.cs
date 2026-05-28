namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Result returned after Cashfree Bank Account Verification (BAV Sync V2).
    /// </summary>
    public class CashfreeBankVerificationResult
    {
        public bool Success { get; set; }
        /// <summary>True when Cashfree reports account_status = VALID.</summary>
        public bool IsValid { get; set; }
        public string AccountStatus { get; set; }
        public string AccountStatusCode { get; set; }
        public string NameAtBank { get; set; }
        public string NameMatchResult { get; set; }
        public string NameMatchScore { get; set; }
        public string BankName { get; set; }
        public string Branch { get; set; }
        public long? ReferenceId { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
    }
}
