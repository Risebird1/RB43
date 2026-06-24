using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ICashfreePayoutService
    {
        /// <summary>
        /// Creates or finds an existing Cashfree beneficiary and initiates an async bank transfer.
        /// </summary>
        /// <param name="accountName">Beneficiary full name.</param>
        /// <param name="accountNumber">Bank account number.</param>
        /// <param name="ifscCode">Bank IFSC code.</param>
        /// <param name="email">Beneficiary email address.</param>
        /// <param name="phone">Beneficiary mobile number (digits only).</param>
        /// <param name="amount">Amount in rupees (≥ 1.00).</param>
        /// <param name="transferId">Unique alphanumeric transfer identifier (≤ 40 chars).</param>
        /// <param name="remarks">Optional narration shown on bank statement (≤ 70 chars).</param>
        Task<CashfreePayoutResult> CreatePayoutToBankAsync(
            string accountName,
            string accountNumber,
            string ifscCode,
            string email,
            string phone,
            decimal amount,
            string transferId,
            string remarks);

        /// <summary>
        /// Fetches the current status of a previously initiated transfer.
        /// </summary>
        /// <param name="transferId">The transferId used when the payout was created.</param>
        Task<CashfreePayoutResult> GetPayoutStatusAsync(string transferId);

        /// <summary>
        /// Verifies bank account + IFSC via Cashfree BAV Sync V2 (penny-drop style validation).
        /// </summary>
        Task<CashfreeBankVerificationResult> VerifyBankAccountAsync(
            string accountName,
            string accountNumber,
            string ifscCode,
            string phone);
    }
}
