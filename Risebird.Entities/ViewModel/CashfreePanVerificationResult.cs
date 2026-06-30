namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Result returned after Cashfree PAN Verification.
    /// </summary>
    public class CashfreePanVerificationResult
    {
        public bool Success { get; set; }
        /// <summary>True when Cashfree reports pan_status = VALID.</summary>
        public bool IsValid { get; set; }
        public string PanStatus { get; set; }
        public string NameAtPan { get; set; }
        public string NameMatchResult { get; set; }
        public string NameMatchScore { get; set; }
        public long? ReferenceId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
