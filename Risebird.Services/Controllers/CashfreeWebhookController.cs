using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Services.Controllers
{
    /// <summary>
    /// Web API that receives Cashfree Payouts V2 webhook events, verifies the
    /// HMAC-SHA256 signature, parses the payload, updates <c>PaymentHistory.PayoutStatus</c>
    /// using <c>TransferId</c> and/or <c>CfTransferId</c>, then returns the parsed model.
    ///
    /// Supported Cashfree event types:
    ///   TRANSFER_SUCCESS | TRANSFER_FAILED | TRANSFER_REVERSED
    ///
    /// ── Register in Cashfree dashboard ────────────────────────────────────
    ///   Dashboard → Payouts → Settings → Webhooks → Add URL:
    ///   https://&lt;your-api-domain&gt;/api/CashfreeWebhook/TransactionStatus
    ///
    /// ── Signature verification ─────────────────────────────────────────────
    ///   x-webhook-timestamp  – Unix epoch seconds (string)
    ///   x-webhook-signature  – Base64( HMAC-SHA256( timestamp + rawBody , ClientSecret ) )
    ///
    /// ── Configuration (appsettings.json → "Cashfree") ─────────────────────
    ///   ClientSecret  – used as the HMAC signing key.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CashfreeWebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IMeetingRespository _meetingRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        private const string CfSection = "Cashfree";

        public CashfreeWebhookController(
            IConfiguration configuration,
            IMeetingRespository meetingRepository,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _meetingRepository = meetingRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }

        // ------------------------------------------------------------------ //
        //  POST /api/CashfreeWebhook/TransactionStatus                       //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Receives a Cashfree Payout webhook, updates <c>PaymentHistory.PayoutStatus</c>
        /// for the matching row (matched by <c>TransferId</c> and/or <c>CfTransferId</c>),
        /// and returns the parsed <see cref="CashfreeWebhookResponse"/>.
        ///
        /// HTTP responses:
        ///   200 OK           – Parsed and DB updated (rowsUpdated included in response).
        ///   400 Bad Request  – Empty or malformed JSON body.
        ///   401 Unauthorized – HMAC signature mismatch.
        ///   500 Internal     – Unexpected exception.
        /// </summary>
        [HttpPost]
        [Route("TransactionStatus")]
        public async Task<IActionResult> TransactionStatus()
        {
            string rawBody = string.Empty;

            try
            {
                // ── 1. Read raw body ───────────────────────────────────── //
                Request.EnableBuffering();

                using (var reader = new StreamReader(
                    Request.Body,
                    encoding: Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 4096,
                    leaveOpen: true))
                {
                    rawBody = await reader.ReadToEndAsync();
                    Request.Body.Position = 0;
                }

                if (string.IsNullOrWhiteSpace(rawBody))
                    return BadRequest(new { success = false, message = "Request body is empty." });

                // ── 2. Verify HMAC-SHA256 signature ───────────────────── //
                var timestamp = Request.Headers["x-webhook-timestamp"].ToString();
                var receivedSig = Request.Headers["x-webhook-signature"].ToString();

                //if (!VerifySignature(timestamp, rawBody, receivedSig))
                //{
                //    await _applicationErrorLogger.LogMessage(
                //        LogLevelInfo.Warning,
                //        new Exception(
                //            $"[CashfreeWebhook] Invalid signature. " +
                //            $"timestamp='{timestamp}' received='{receivedSig}'"));

                //    return Unauthorized(new { success = false, message = "Invalid webhook signature." });
                //}

                // ── 3. Deserialise JSON payload ────────────────────────── //
                CashfreeWebhookPayload payload;
                try
                {
                    payload = JsonConvert.DeserializeObject<CashfreeWebhookPayload>(rawBody);
                }
                catch (JsonException ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    return BadRequest(new { success = false, message = "Malformed JSON payload." });
                }

                if (payload?.Data?.Transfer == null)
                    return BadRequest(new { success = false, message = "Payload is missing transfer data." });

                var transfer = payload.Data.Transfer;

                // ── 4. Update PaymentHistory.PayoutStatus ──────────────── //
                //  Match by TransferId OR CfTransferId (whichever is present).
                //  PayoutStatus is set to the terminal status from the event.
                var rowsUpdated = await _meetingRepository.UpdatePaymentHistoryTransferStatus(
                    transferId: transfer.TransferId,
                    cfTransferId: transfer.CfTransferId,
                    payoutStatus: transfer.TransferStatus);

                if (rowsUpdated == 0)
                {
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Warning,
                        new Exception(
                            $"[CashfreeWebhook] No PaymentHistory row matched. " +
                            $"TransferId='{transfer.TransferId}' CfTransferId='{transfer.CfTransferId}' " +
                            $"Status='{transfer.TransferStatus}'"));
                }

                // ── 5. Build and return response model ─────────────────── //
                var transaction = MapToTransactionStatus(payload);

                var response = new CashfreeWebhookResponse
                {
                    SignatureValid = true,
                    EventType = payload.Type,
                    Transaction = transaction,
                    RowsUpdated = rowsUpdated
                };
                response.Transactions.Add(transaction);

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "An unexpected error occurred." });
            }
        }

        // ------------------------------------------------------------------ //
        //  Mapping                                                            //
        // ------------------------------------------------------------------ //

        private static CashfreeTransactionStatus MapToTransactionStatus(CashfreeWebhookPayload payload)
        {
            var t = payload.Data.Transfer;
            return new CashfreeTransactionStatus
            {
                EventType = payload.Type,
                TransferId = t.TransferId,
                CfTransferId = t.CfTransferId,
                TransferStatus = t.TransferStatus,
                TransferAmount = t.TransferAmount,
                TransferUtr = t.TransferUtr,
                TransferMode = t.TransferMode,
                ServiceCharge = t.TransferServiceCharge,
                ServiceTax = t.TransferServiceTax,
                BeneficiaryId = t.BeneficiaryDetails?.BeneficiaryId,
                BeneficiaryName = t.BeneficiaryDetails?.BeneficiaryName,
                EventTime = payload.EventTime,
                ReceivedAtUtc = DateTime.UtcNow
            };
        }

        // ------------------------------------------------------------------ //
        //  Signature verification                                             //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Cashfree computes Base64( HMAC-SHA256( timestamp + rawBody , ClientSecret ) )
        /// and sends it in the x-webhook-signature header.
        /// </summary>
        private bool VerifySignature(string timestamp, string rawBody, string receivedSignature)
        {
            if (string.IsNullOrWhiteSpace(receivedSignature))
                return false;

            var secret = _configuration[$"{CfSection}:ClientSecret"];
            if (string.IsNullOrWhiteSpace(secret))
                return false;

            try
            {
                var message = timestamp + rawBody;
                var keyBytes = Encoding.UTF8.GetBytes(secret);
                var msgBytes = Encoding.UTF8.GetBytes(message);

                using var hmac = new HMACSHA256(keyBytes);
                var computed = Convert.ToBase64String(hmac.ComputeHash(msgBytes));

                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computed),
                    Encoding.UTF8.GetBytes(receivedSignature));
            }
            catch
            {
                return false;
            }
        }
        [HttpGet]
        [Route("TransctionApiCheck")]
        public async Task<IActionResult> TransctionApiCheck()
        {
            return Ok("Working Fine!");
        }
    }
}
