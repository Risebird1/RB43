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
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Root object POSTed by Cashfree for every payout webhook event.</summary>
    public class CashfreeWebhookPayload
    {
        /// <summary>TRANSFER_SUCCESS | TRANSFER_FAILED | TRANSFER_REVERSED</summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("data")]
        public CashfreeWebhookData Data { get; set; }

        /// <summary>ISO-8601 timestamp sent by Cashfree, e.g. "2024-01-15T10:30:00Z"</summary>
        [JsonProperty("event_time")]
        public string EventTime { get; set; }
    }

    public class CashfreeWebhookData
    {
        [JsonProperty("transfer")]
        public CashfreeWebhookTransfer Transfer { get; set; }
    }

    public class CashfreeWebhookTransfer
    {
        /// <summary>Your internal transfer_id used when the payout was created.</summary>
        [JsonProperty("transfer_id")]
        public string TransferId { get; set; }

        /// <summary>Cashfree's own numeric reference for this transfer.</summary>
        [JsonProperty("cf_transfer_id")]
        public string CfTransferId { get; set; }

        /// <summary>Terminal status: SUCCESS | FAILED | REVERSED</summary>
        [JsonProperty("transfer_status")]
        public string TransferStatus { get; set; }

        [JsonProperty("transfer_amount")]
        public decimal TransferAmount { get; set; }

        /// <summary>Bank UTR — populated only after a successful settlement.</summary>
        [JsonProperty("transfer_utr")]
        public string TransferUtr { get; set; }

        /// <summary>e.g. neft | imps | rtgs | upi | banktransfer</summary>
        [JsonProperty("transfer_mode")]
        public string TransferMode { get; set; }

        [JsonProperty("transfer_service_charge")]
        public decimal TransferServiceCharge { get; set; }

        [JsonProperty("transfer_service_tax")]
        public decimal TransferServiceTax { get; set; }

        /// <summary>Cashfree sends beneficiary info as "bene_details" in Payouts V2.</summary>
        [JsonProperty("bene_details")]
        public CashfreeWebhookBeneficiary BeneDetails { get; set; }

        /// <summary>Legacy alias — some older payloads use "beneficiary_details".</summary>
        [JsonProperty("beneficiary_details")]
        public CashfreeWebhookBeneficiary BeneficiaryDetails { get; set; }

        /// <summary>Returns whichever beneficiary block is populated.</summary>
        [JsonIgnore]
        public CashfreeWebhookBeneficiary ResolvedBeneficiary =>
            BeneDetails ?? BeneficiaryDetails;
    }

    public class CashfreeWebhookBeneficiary
    {
        /// <summary>Cashfree Payouts V2 field name: bene_id</summary>
        [JsonProperty("bene_id")]
        public string BeneId { get; set; }

        /// <summary>Cashfree Payouts V2 field name: bene_name</summary>
        [JsonProperty("bene_name")]
        public string BeneName { get; set; }

        /// <summary>Legacy field name: beneficiary_id</summary>
        [JsonProperty("beneficiary_id")]
        public string BeneficiaryId { get; set; }

        /// <summary>Legacy field name: beneficiary_name</summary>
        [JsonProperty("beneficiary_name")]
        public string BeneficiaryName { get; set; }

        /// <summary>Returns the populated beneficiary id regardless of field name variant.</summary>
        [JsonIgnore]
        public string ResolvedId => BeneId ?? BeneficiaryId;

        /// <summary>Returns the populated beneficiary name regardless of field name variant.</summary>
        [JsonIgnore]
        public string ResolvedName => BeneName ?? BeneficiaryName;
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
    /// Flat, normalised view of a single Cashfree payout transaction event.
    /// All fields map 1-to-1 with the Cashfree webhook payload.
    /// </summary>
    public class CashfreeTransactionStatus
    {
        /// <summary>Webhook event type: TRANSFER_SUCCESS | TRANSFER_FAILED | TRANSFER_REVERSED</summary>
        public string EventType { get; set; }

        /// <summary>Your internal transferId sent when the payout was created.</summary>
        public string TransferId { get; set; }

        /// <summary>Cashfree's own numeric reference.</summary>
        public string CfTransferId { get; set; }

        /// <summary>Terminal status string: SUCCESS | FAILED | REVERSED</summary>
        public string TransferStatus { get; set; }

        /// <summary>Transfer amount in INR.</summary>
        public decimal TransferAmount { get; set; }

        /// <summary>Bank UTR — null until the transfer settles.</summary>
        public string TransferUtr { get; set; }

        /// <summary>Transfer rail: neft | imps | rtgs | upi | banktransfer</summary>
        public string TransferMode { get; set; }

        /// <summary>Gateway service charge in INR.</summary>
        public decimal ServiceCharge { get; set; }

        /// <summary>GST/tax on the service charge in INR.</summary>
        public decimal ServiceTax { get; set; }

        public string BeneficiaryId { get; set; }

        public string BeneficiaryName { get; set; }

        /// <summary>ISO-8601 event timestamp as sent by Cashfree.</summary>
        public string EventTime { get; set; }

        /// <summary>UTC time this webhook was received by our server.</summary>
        public DateTime ReceivedAtUtc { get; set; }

        /// <summary>True when the transfer reached the SUCCESS terminal state.</summary>
        public bool IsSuccess =>
            string.Equals(TransferStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase);
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
