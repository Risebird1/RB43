namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Result of a Razorpay bank account validation request.
    /// See: https://razorpay.com/docs/api/x/account-validation/bank-account/
    /// </summary>
    public class RazorpayValidationResult
    {
        public bool Success { get; set; }
        public string ValidationId { get; set; }
        /// <summary>created | completed | failed</summary>
        public string Status { get; set; }
        public string RegisteredName { get; set; }
        public string AccountStatus { get; set; }
        public string ErrorMessage { get; set; }
    }
}
