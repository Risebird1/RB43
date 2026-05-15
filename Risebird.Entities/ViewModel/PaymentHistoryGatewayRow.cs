namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Lightweight projection of a PaymentHistory row used for idempotency checks.
    /// </summary>
    public class PaymentHistoryGatewayRow
    {
        public int Id { get; set; }
        public string PayoutReferenceId { get; set; }
        public string GatewayStatus { get; set; }
    }
}
