using System.Threading.Tasks;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Interface
{
    /// <summary>
    /// Service to create payouts via Razorpay X Payouts API (bank transfer).
    /// </summary>
    public interface IRazorpayPayoutService
    {
        /// <summary>
        /// Create a payout to a bank account using Razorpay composite payout API.
        /// </summary>
        /// <param name="beneficiaryName">Account holder name.</param>
        /// <param name="beneficiaryAccountNumber">Beneficiary bank account number.</param>
        /// <param name="ifscCode">11-character IFSC code.</param>
        /// <param name="contactEmail">Contact email (for Razorpay contact).</param>
        /// <param name="amountInPaise">Amount in paise (e.g. 100000 = ₹1000).</param>
        /// <param name="referenceId">Our reference id (idempotency / tracking), max 40 chars.</param>
        /// <param name="narration">Narration for bank statement, max 30 chars.</param>
        /// <param name="contactPhone">Optional contact phone.</param>
        /// <returns>Payout result with PayoutId and Status on success.</returns>
        Task<RazorpayPayoutResult> CreatePayoutToBankAsync(
            string beneficiaryName,
            string beneficiaryAccountNumber,
            string ifscCode,
            string contactEmail,
            long amountInPaise,
            string referenceId,
            string narration,
            string contactPhone = null);

        /// <summary>
        /// Fetch payout status by payout ID.
        /// See: https://razorpay.com/docs/api/x/payouts/fetch-with-id/
        /// </summary>
        /// <param name="payoutId">Razorpay payout ID (e.g. pout_xxx).</param>
        /// <returns>Payout result with PayoutId, Status, Amount, Utr (if processed) on success.</returns>
        Task<RazorpayPayoutResult> GetPayoutStatusAsync(string payoutId);

        Task<RazorpayValidationResult> ValidateBankAccountAsync(
            string beneficiaryName,
            string beneficiaryAccountNumber,
            string ifscCode,
            string contactEmail,
            string contactPhone = null);
    }
}
