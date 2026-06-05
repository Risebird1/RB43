using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    // ──────────────────────────────────────────────────────────────────────────
    //  Cashfree Payouts V2 – Webhook Payload Models
    //  Docs: https://docs.cashfree.com/reference/payout-webhooks
    //
    //  Cashfree POSTs to your URL for each of these event types:
    //    TRANSFER_SUCCESS | TRANSFER_FAILED | TRANSFER_REVERSED
    //
    //  Cashfree Payment Gateway webhooks:
    //    PAYMENT_SUCCESS_WEBHOOK
    //
    //  Signature verification (x-webhook-signature header):
    //    Base64( HMAC-SHA256( x-webhook-timestamp + rawBody , ClientSecret ) )
    // ──────────────────────────────────────────────────────────────────────────

    // ══════════════════════════════════════════════════════════════════════════
    //  Payout webhook models  (TRANSFER_SUCCESS | TRANSFER_FAILED | …)
    //
    //  Actual Cashfree payout payload (flat root-level JSON):
    //  {
    //    "event":        "TRANSFER_SUCCESS",
    //    "transferId":   "RB_362305_20260604173341",
    //    "referenceId":  2328466950,
    //    "acknowledged": 0,
    //    "eventTime":    "2026-06-04 17:48:43",
    //    "utr":          "IDFBH26155274945",
    //    "signature":    "ICxEG81j2sBd..."
    //  }
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Root object POSTed by Cashfree for every payout transfer webhook event
    /// (TRANSFER_SUCCESS | TRANSFER_FAILED | TRANSFER_REVERSED).
    /// All fields are at the root level — there is no nested "data" wrapper.
    /// </summary>
    public class CashfreeWebhookPayload
    {
        /// <summary>Event type: TRANSFER_SUCCESS | TRANSFER_FAILED | TRANSFER_REVERSED</summary>
        [JsonProperty("event")]
        public string Event { get; set; }

        /// <summary>Your internal transfer ID set when the payout was created (e.g. "RB_362305_20260604173341").</summary>
        [JsonProperty("transferId")]
        public string TransferId { get; set; }

        /// <summary>Cashfree's own numeric reference ID for this transfer.</summary>
        [JsonProperty("referenceId")]
        public long? ReferenceId { get; set; }

        /// <summary>0 = not yet acknowledged by beneficiary bank; 1 = acknowledged.</summary>
        [JsonProperty("acknowledged")]
        public int Acknowledged { get; set; }

        /// <summary>Event timestamp as sent by Cashfree, e.g. "2026-06-04 17:48:43".</summary>
        [JsonProperty("eventTime")]
        public string EventTime { get; set; }

        /// <summary>Bank UTR number — populated on TRANSFER_SUCCESS.</summary>
        [JsonProperty("utr")]
        public string Utr { get; set; }

        /// <summary>Cashfree-computed HMAC signature for this payload (sent in body).</summary>
        [JsonProperty("signature")]
        public string Signature { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  Payment Gateway webhook models  (PAYMENT_SUCCESS_WEBHOOK)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Root object for a Cashfree Payment Gateway webhook event.</summary>
    public class CashfreePaymentWebhookPayload
    {
        /// <summary>e.g. PAYMENT_SUCCESS_WEBHOOK | PAYMENT_FAILED_WEBHOOK</summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("event_time")]
        public string EventTime { get; set; }

        [JsonProperty("data")]
        public CashfreePaymentWebhookData Data { get; set; }
    }

    public class CashfreePaymentWebhookData
    {
        [JsonProperty("order")]
        public CashfreePaymentOrder Order { get; set; }

        [JsonProperty("payment")]
        public CashfreePaymentDetails Payment { get; set; }

        [JsonProperty("customer_details")]
        public CashfreeCustomerDetails CustomerDetails { get; set; }
    }

    public class CashfreePaymentOrder
    {
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("order_amount")]
        public decimal OrderAmount { get; set; }

        [JsonProperty("order_currency")]
        public string OrderCurrency { get; set; }
    }

    public class CashfreePaymentDetails
    {
        [JsonProperty("cf_payment_id")]
        public string CfPaymentId { get; set; }

        /// <summary>SUCCESS | FAILED | PENDING | USER_DROPPED</summary>
        [JsonProperty("payment_status")]
        public string PaymentStatus { get; set; }

        [JsonProperty("payment_amount")]
        public decimal PaymentAmount { get; set; }

        [JsonProperty("payment_currency")]
        public string PaymentCurrency { get; set; }

        [JsonProperty("payment_message")]
        public string PaymentMessage { get; set; }

        [JsonProperty("payment_time")]
        public string PaymentTime { get; set; }

        [JsonProperty("bank_reference")]
        public string BankReference { get; set; }

        [JsonProperty("auth_id")]
        public string AuthId { get; set; }

        [JsonProperty("payment_method")]
        public object PaymentMethod { get; set; }
    }

    public class CashfreeCustomerDetails
    {
        [JsonProperty("customer_id")]
        public string CustomerId { get; set; }

        [JsonProperty("customer_name")]
        public string CustomerName { get; set; }

        [JsonProperty("customer_email")]
        public string CustomerEmail { get; set; }

        [JsonProperty("customer_phone")]
        public string CustomerPhone { get; set; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Parsed / normalised response models returned from the API
    // ──────────────────────────────────────────────────────────────────────────

    // ══════════════════════════════════════════════════════════════════════════
    //  Normalised / parsed response models
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Normalised view of a single Cashfree payout webhook event,
    /// returned by POST /api/CashfreeWebhook/TransactionStatus.
    /// </summary>
    public class CashfreeTransactionStatus
    {
        /// <summary>Webhook event type: TRANSFER_SUCCESS | TRANSFER_FAILED | TRANSFER_REVERSED</summary>
        public string EventType { get; set; }

        /// <summary>Your internal transferId sent when the payout was created.</summary>
        public string TransferId { get; set; }

        /// <summary>Cashfree's numeric referenceId for this transfer.</summary>
        public long? ReferenceId { get; set; }

        /// <summary>Terminal event type string, mirrors EventType.</summary>
        public string TransferStatus { get; set; }

        /// <summary>Bank UTR number — populated on TRANSFER_SUCCESS.</summary>
        public string Utr { get; set; }

        /// <summary>0 = not acknowledged; 1 = acknowledged by beneficiary bank.</summary>
        public int Acknowledged { get; set; }

        /// <summary>Event timestamp as sent by Cashfree.</summary>
        public string EventTime { get; set; }

        /// <summary>UTC time this webhook was received by our server.</summary>
        public DateTime ReceivedAtUtc { get; set; }

        /// <summary>True when the transfer reached the SUCCESS terminal state.</summary>
        public bool IsSuccess =>
            string.Equals(EventType, "TRANSFER_SUCCESS", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Flat, normalised view of a single Cashfree Payment Gateway event
    /// (e.g. PAYMENT_SUCCESS_WEBHOOK).
    /// </summary>
    public class CashfreePaymentStatus
    {
        /// <summary>Webhook event type: PAYMENT_SUCCESS_WEBHOOK | PAYMENT_FAILED_WEBHOOK</summary>
        public string EventType { get; set; }

        public string OrderId { get; set; }
        public decimal OrderAmount { get; set; }
        public string OrderCurrency { get; set; }

        /// <summary>Cashfree's own numeric payment reference.</summary>
        public string CfPaymentId { get; set; }

        /// <summary>Terminal payment status: SUCCESS | FAILED | PENDING | USER_DROPPED</summary>
        public string PaymentStatus { get; set; }

        public decimal PaymentAmount { get; set; }
        public string PaymentCurrency { get; set; }
        public string PaymentMessage { get; set; }
        public string PaymentTime { get; set; }
        public string BankReference { get; set; }
        public string AuthId { get; set; }

        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }

        /// <summary>ISO-8601 event timestamp as sent by Cashfree.</summary>
        public string EventTime { get; set; }

        /// <summary>UTC time this webhook was received by our server.</summary>
        public DateTime ReceivedAtUtc { get; set; }

        public bool IsSuccess =>
            string.Equals(PaymentStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Non-transfer event model  (e.g. LOW_BALANCE_ALERT)
    //  Shape: { "event":"LOW_BALANCE_ALERT", "currentBalance":"100.00",
    //           "alertTime":"2026-05-22 14:10:24", "signature":"…" }
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Represents Cashfree account-level alert notifications that are not
    /// associated with a specific transfer (e.g. LOW_BALANCE_ALERT).
    /// These use the <c>event</c> key at the root instead of <c>type</c>.
    /// </summary>
    public class CashfreeBalanceAlertPayload
    {
        /// <summary>Event name, e.g. "LOW_BALANCE_ALERT".</summary>
        [JsonProperty("event")]
        public string Event { get; set; }

        /// <summary>Current wallet balance reported by Cashfree.</summary>
        [JsonProperty("currentBalance")]
        public string CurrentBalance { get; set; }

        /// <summary>Timestamp of the alert as sent by Cashfree, e.g. "2026-05-22 14:10:24".</summary>
        [JsonProperty("alertTime")]
        public string AlertTime { get; set; }

        /// <summary>Cashfree-computed signature for this alert payload.</summary>
        [JsonProperty("signature")]
        public string Signature { get; set; }
    }

    /// <summary>
    /// Envelope returned by POST /api/CashfreeWebhook/TransactionStatus.
    /// Contains the signature check result, event type, DB update count, and the parsed event.
    /// For payout events (TRANSFER_*) the <see cref="Transaction"/> field is populated.
    /// For payment gateway events (PAYMENT_*) the <see cref="Payment"/> field is populated.
    /// The <see cref="Transactions"/> list mirrors <see cref="Transaction"/> for symmetry.
    /// </summary>
    public class CashfreeWebhookResponse
    {
        public bool SignatureValid { get; set; }

        /// <summary>Raw event type string from Cashfree, e.g. "TRANSFER_SUCCESS" or "PAYMENT_SUCCESS_WEBHOOK".</summary>
        public string EventType { get; set; }

        /// <summary>
        /// Number of PaymentHistory rows updated.
        /// 0 means no row matched the identifier.
        /// </summary>
        public int RowsUpdated { get; set; }

        /// <summary>Populated for payout transfer events (TRANSFER_*).</summary>
        public CashfreeTransactionStatus Transaction { get; set; }

        /// <summary>List view — always mirrors <see cref="Transaction"/> for transfer events.</summary>
        public List<CashfreeTransactionStatus> Transactions { get; set; } = new List<CashfreeTransactionStatus>();

        /// <summary>Populated for payment gateway events (PAYMENT_SUCCESS_WEBHOOK etc.).</summary>
        public CashfreePaymentStatus Payment { get; set; }
    }
}
