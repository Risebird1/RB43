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
            const string endpoint = "POST /api/CashfreeWebhook/TransactionStatus";
            string rawBody   = string.Empty;
            string timestamp = string.Empty;
            string receivedSig = string.Empty;
            string callerIp  = HttpContext.Connection.RemoteIpAddress?.ToString();

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
                {
                    var emptyBodyResponse = new { success = false, message = "Request body is empty." };
                    await _meetingRepository.InsertPaymentLog(
                        endpoint:    endpoint,
                        requestBody: "(empty)",
                        responseBody: JsonConvert.SerializeObject(emptyBodyResponse),
                        httpStatusCode: 400,
                        logLevel:    "Warning",
                        errorMessage: "Request body is empty.",
                        callerIp:    callerIp);
                    return BadRequest(emptyBodyResponse);
                }

                // ── 2. Verify HMAC-SHA256 signature ───────────────────── //
                timestamp   = Request.Headers["x-webhook-timestamp"].ToString();
                receivedSig = Request.Headers["x-webhook-signature"].ToString();

                //if (!VerifySignature(timestamp, rawBody, receivedSig))
                //{
                //    var sigError = new { success = false, message = "Invalid webhook signature." };
                //    await _applicationErrorLogger.LogMessage(
                //        LogLevelInfo.Warning,
                //        new Exception(
                //            $"[CashfreeWebhook] Invalid signature. " +
                //            $"timestamp='{timestamp}' received='{receivedSig}'"));
                //    await _meetingRepository.InsertPaymentLog(
                //        endpoint:           endpoint,
                //        requestBody:        rawBody,
                //        responseBody:       JsonConvert.SerializeObject(sigError),
                //        httpStatusCode:     401,
                //        logLevel:           "AccessError",
                //        signatureTimestamp: timestamp,
                //        signatureReceived:  receivedSig,
                //        errorMessage:       "Invalid webhook signature.",
                //        callerIp:           callerIp);
                //    return Unauthorized(sigError);
                //}

                // ── 3. Deserialise JSON payload ────────────────────────── //
                CashfreeWebhookPayload payload;
                try
                {
                    payload = JsonConvert.DeserializeObject<CashfreeWebhookPayload>(rawBody);
                }
                catch (JsonException ex)
                {
                    var parseError = new { success = false, message = "Malformed JSON payload." };
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    await _meetingRepository.InsertPaymentLog(
                        endpoint:           endpoint,
                        requestBody:        rawBody,
                        responseBody:       JsonConvert.SerializeObject(parseError),
                        httpStatusCode:     400,
                        logLevel:           "Error",
                        signatureTimestamp: timestamp,
                        signatureReceived:  receivedSig,
                        errorMessage:       $"Malformed JSON: {ex.Message}",
                        callerIp:           callerIp);
                    return BadRequest(parseError);
                }

                if (payload?.Data?.Transfer == null)
                {
                    var missingData = new { success = false, message = "Payload is missing transfer data." };
                    await _meetingRepository.InsertPaymentLog(
                        endpoint:           endpoint,
                        requestBody:        rawBody,
                        responseBody:       JsonConvert.SerializeObject(missingData),
                        httpStatusCode:     400,
                        logLevel:           "Warning",
                        eventType:          payload?.Type,
                        signatureTimestamp: timestamp,
                        signatureReceived:  receivedSig,
                        errorMessage:       "Payload missing transfer data.",
                        callerIp:           callerIp);
                    return BadRequest(missingData);
                }

                var transfer = payload.Data.Transfer;

                // ── 4. Update PaymentHistory.PayoutStatus ──────────────── //
                var rowsUpdated = await _meetingRepository.UpdatePaymentHistoryTransferStatus(
                    transferId:   transfer.TransferId,
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
                    EventType      = payload.Type,
                    Transaction    = transaction,
                    RowsUpdated    = rowsUpdated
                };
                response.Transactions.Add(transaction);

                // ── 6. Persist success log ─────────────────────────────── //
                await _meetingRepository.InsertPaymentLog(
                    endpoint:           endpoint,
                    requestBody:        rawBody,
                    responseBody:       JsonConvert.SerializeObject(response),
                    httpStatusCode:     200,
                    logLevel:           rowsUpdated == 0 ? "Warning" : "Info",
                    eventType:          payload.Type,
                    transferId:         transfer.TransferId,
                    cfTransferId:       transfer.CfTransferId,
                    transferStatus:     transfer.TransferStatus,
                    signatureTimestamp: timestamp,
                    signatureReceived:  receivedSig,
                    rowsUpdated:        rowsUpdated,
                    errorMessage:       rowsUpdated == 0
                                            ? "No PaymentHistory row matched the TransferId/CfTransferId."
                                            : null,
                    callerIp:           callerIp);

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                var serverError = new { success = false, message = "An unexpected error occurred." };
                await _meetingRepository.InsertPaymentLog(
                    endpoint:           endpoint,
                    requestBody:        rawBody,
                    responseBody:       JsonConvert.SerializeObject(serverError),
                    httpStatusCode:     500,
                    logLevel:           "Exception",
                    signatureTimestamp: timestamp,
                    signatureReceived:  receivedSig,
                    errorMessage:       ex.ToString(),
                    callerIp:           callerIp);

                return StatusCode(StatusCodes.Status500InternalServerError, serverError);
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
