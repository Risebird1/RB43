namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Result of a Razorpay payout API call.
    /// </summary>
    public class RazorpayPayoutResult
    {
        public bool Success { get; set; }
        public string PayoutId { get; set; }
        public string Status { get; set; }
        public long? Amount { get; set; }
        public long? Fees { get; set; }
        public string Utr { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
    }
}
