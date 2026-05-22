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
    //  Signature verification (x-webhook-signature header):
    //    Base64( HMAC-SHA256( x-webhook-timestamp + rawBody , ClientSecret ) )
    // ──────────────────────────────────────────────────────────────────────────

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

        /// <summary>e.g. neft | imps | rtgs | upi</summary>
        [JsonProperty("transfer_mode")]
        public string TransferMode { get; set; }

        [JsonProperty("transfer_service_charge")]
        public decimal TransferServiceCharge { get; set; }

        [JsonProperty("transfer_service_tax")]
        public decimal TransferServiceTax { get; set; }

        [JsonProperty("beneficiary_details")]
        public CashfreeWebhookBeneficiary BeneficiaryDetails { get; set; }
    }

    public class CashfreeWebhookBeneficiary
    {
        [JsonProperty("beneficiary_id")]
        public string BeneficiaryId { get; set; }

        [JsonProperty("beneficiary_name")]
        public string BeneficiaryName { get; set; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Parsed / normalised response models returned from the API
    // ──────────────────────────────────────────────────────────────────────────

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

        /// <summary>Transfer rail: neft | imps | rtgs | upi</summary>
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
    /// Contains the signature check result, event type, DB update count, and the parsed transaction.
    /// The <see cref="Transactions"/> list always contains exactly one entry for
    /// live Cashfree webhooks; it is a list for symmetry with batch test calls.
    /// </summary>
    public class CashfreeWebhookResponse
    {
        public bool SignatureValid { get; set; }

        /// <summary>Raw event type string from Cashfree, e.g. "TRANSFER_SUCCESS".</summary>
        public string EventType { get; set; }

        /// <summary>
        /// Number of PaymentHistory rows updated with the new PayoutStatus.
        /// 0 means no row matched the TransferId / CfTransferId.
        /// </summary>
        public int RowsUpdated { get; set; }

        /// <summary>The single parsed transaction from this webhook call.</summary>
        public CashfreeTransactionStatus Transaction { get; set; }

        /// <summary>List view — always mirrors <see cref="Transaction"/>.</summary>
        public List<CashfreeTransactionStatus> Transactions { get; set; } = new List<CashfreeTransactionStatus>();
    }
}
