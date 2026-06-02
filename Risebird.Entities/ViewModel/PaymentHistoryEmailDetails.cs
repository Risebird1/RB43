namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Lightweight projection returned by <c>usp_GetPaymentDetailsByTransferId</c>.
    /// Contains the minimum data needed to send an interviewer payment confirmation email
    /// from the Cashfree payout webhook.
    /// </summary>
    public class PaymentHistoryEmailDetails
    {
        public int PaymentHistoryId { get; set; }
        public string Email { get; set; }
        public int Amount { get; set; }
    }
}
