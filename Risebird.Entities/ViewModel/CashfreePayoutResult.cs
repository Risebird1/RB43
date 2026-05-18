namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Result returned by CashfreePayoutService after a bank payout attempt or status query.
    /// </summary>
    public class CashfreePayoutResult
    {
        public bool Success { get; set; }
        /// <summary>Cashfree transferId used as the payout reference.</summary>
        public string PayoutId { get; set; }
        /// <summary>Cashfree transfer status e.g. ACCEPTED, SUCCESS, FAILED.</summary>
        public string Status { get; set; }
        /// <summary>Amount in rupees.</summary>
        public decimal Amount { get; set; }
        /// <summary>Service fee charged by Cashfree (rupees). Zero until settled.</summary>
        public decimal Fees { get; set; }
        /// <summary>UTR (Unique Transaction Reference) populated once the transfer settles.</summary>
        public string Utr { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public string CFPayoutId { get; set; }
    }
}
