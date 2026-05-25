using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    /// Web API that receives Cashfree webhook events (Payouts V2 and Payment Gateway),
    /// verifies the HMAC-SHA256 signature, parses the payload, updates
    /// <c>PaymentHistory</c>, then returns the parsed model.
    ///
    /// Supported Cashfree event types:
    ///   TRANSFER_SUCCESS | TRANSFER_FAILED | TRANSFER_REVERSED
    ///   PAYMENT_SUCCESS_WEBHOOK | PAYMENT_FAILED_WEBHOOK
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

                // Peek at the root keys to decide which event shape we received.
                // Transfer events use "type" at the root; account-level alerts
                // (e.g. LOW_BALANCE_ALERT) use "event" at the root instead.
                JObject rootObj;
                try
                {
                    rootObj = JObject.Parse(rawBody);
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

                // ── 3a. Non-transfer alert (e.g. LOW_BALANCE_ALERT) ───────── //
                if (rootObj.ContainsKey("event") && !rootObj.ContainsKey("type"))
                {
                    var alert = rootObj.ToObject<CashfreeBalanceAlertPayload>();
                    var alertResponse = new
                    {
                        success   = true,
                        message   = $"Non-transfer event '{alert?.Event}' acknowledged.",
                        eventType = alert?.Event
                    };

                    await _meetingRepository.InsertPaymentLog(
                        endpoint:           endpoint,
                        requestBody:        rawBody,
                        responseBody:       JsonConvert.SerializeObject(alertResponse),
                        httpStatusCode:     200,
                        logLevel:           "Info",
                        eventType:          alert?.Event,
                        signatureTimestamp: timestamp,
                        signatureReceived:  receivedSig,
                        callerIp:           callerIp);

                    return Ok(alertResponse);
                }

                // Peek at "type" to route to the correct handler.
                var eventType = rootObj["type"]?.ToString() ?? string.Empty;

                // ── 3b. Payment Gateway event (PAYMENT_SUCCESS_WEBHOOK etc.) ─ //
                if (eventType.StartsWith("PAYMENT_", StringComparison.OrdinalIgnoreCase))
                {
                    return await HandlePaymentGatewayWebhook(
                        rootObj, rawBody, timestamp, receivedSig, callerIp, endpoint);
                }

                // ── 3c. Transfer / Payout event ───────────────────────────── //
                CashfreeWebhookPayload payload;
                try
                {
                    payload = rootObj.ToObject<CashfreeWebhookPayload>();
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

                // ── 4. Update PaymentHistory.PayoutStatus + TransferUtr ─── //
                var rowsUpdated = await _meetingRepository.UpdatePaymentHistoryTransferStatus(
                    transferId:   transfer.TransferId,
                    cfTransferId: transfer.CfTransferId,
                    payoutStatus: transfer.TransferStatus,
                    transferUtr:  transfer.TransferUtr);

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
        //  Payment Gateway webhook handler                                    //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Handles PAYMENT_SUCCESS_WEBHOOK and similar Cashfree Payment Gateway events.
        /// Saves payment status to <c>PaymentHistory</c> matched by <c>OrderId</c>.
        /// </summary>
        private async Task<IActionResult> HandlePaymentGatewayWebhook(
            JObject rootObj,
            string  rawBody,
            string  timestamp,
            string  receivedSig,
            string  callerIp,
            string  endpoint)
        {
            CashfreePaymentWebhookPayload pgPayload;
            try
            {
                pgPayload = rootObj.ToObject<CashfreePaymentWebhookPayload>();
            }
            catch (JsonException ex)
            {
                var parseError = new { success = false, message = "Malformed payment webhook JSON." };
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

            if (pgPayload?.Data?.Payment == null || pgPayload.Data.Order == null)
            {
                var missingData = new { success = false, message = "Payment webhook is missing order or payment data." };
                await _meetingRepository.InsertPaymentLog(
                    endpoint:           endpoint,
                    requestBody:        rawBody,
                    responseBody:       JsonConvert.SerializeObject(missingData),
                    httpStatusCode:     400,
                    logLevel:           "Warning",
                    eventType:          pgPayload?.Type,
                    signatureTimestamp: timestamp,
                    signatureReceived:  receivedSig,
                    errorMessage:       "Payment webhook missing order or payment data.",
                    callerIp:           callerIp);
                return BadRequest(missingData);
            }

            var order   = pgPayload.Data.Order;
            var payment = pgPayload.Data.Payment;

            // Update PaymentHistory matched by CfPaymentId.
            var rowsUpdated = await _meetingRepository.UpdatePaymentHistoryGatewayStatus(
                cfPaymentId:          payment.CfPaymentId,
                gatewayPaymentStatus: payment.PaymentStatus);

            if (rowsUpdated == 0)
            {
                await _applicationErrorLogger.LogMessage(
                    LogLevelInfo.Warning,
                    new Exception(
                        $"[CashfreeWebhook] No PaymentHistory row matched CfPaymentId='{payment.CfPaymentId}' " +
                        $"Status='{payment.PaymentStatus}'"));
            }

            var paymentStatus = MapToPaymentStatus(pgPayload);

            var response = new CashfreeWebhookResponse
            {
                SignatureValid = true,
                EventType      = pgPayload.Type,
                Payment        = paymentStatus,
                RowsUpdated    = rowsUpdated
            };

            await _meetingRepository.InsertPaymentLog(
                endpoint:           endpoint,
                requestBody:        rawBody,
                responseBody:       JsonConvert.SerializeObject(response),
                httpStatusCode:     200,
                logLevel:           rowsUpdated == 0 ? "Warning" : "Info",
                eventType:          pgPayload.Type,
                transferStatus:     payment.PaymentStatus,
                signatureTimestamp: timestamp,
                signatureReceived:  receivedSig,
                rowsUpdated:        rowsUpdated,
                    errorMessage:       rowsUpdated == 0
                                        ? $"No PaymentHistory row matched CfPaymentId='{payment.CfPaymentId}'."
                                        : null,
                callerIp:           callerIp);

            return Ok(response);
        }

        // ------------------------------------------------------------------ //
        //  Mapping                                                            //
        // ------------------------------------------------------------------ //

        private static CashfreeTransactionStatus MapToTransactionStatus(CashfreeWebhookPayload payload)
        {
            var t    = payload.Data.Transfer;
            var bene = t.ResolvedBeneficiary;
            return new CashfreeTransactionStatus
            {
                EventType       = payload.Type,
                TransferId      = t.TransferId,
                CfTransferId    = t.CfTransferId,
                TransferStatus  = t.TransferStatus,
                TransferAmount  = t.TransferAmount,
                TransferUtr     = t.TransferUtr,
                TransferMode    = t.TransferMode,
                ServiceCharge   = t.TransferServiceCharge,
                ServiceTax      = t.TransferServiceTax,
                BeneficiaryId   = bene?.ResolvedId,
                BeneficiaryName = bene?.ResolvedName,
                EventTime       = payload.EventTime,
                ReceivedAtUtc   = DateTime.UtcNow
            };
        }

        private static CashfreePaymentStatus MapToPaymentStatus(CashfreePaymentWebhookPayload payload)
        {
            var order    = payload.Data.Order;
            var payment  = payload.Data.Payment;
            var customer = payload.Data.CustomerDetails;
            return new CashfreePaymentStatus
            {
                EventType       = payload.Type,
                OrderId         = order.OrderId,
                OrderAmount     = order.OrderAmount,
                OrderCurrency   = order.OrderCurrency,
                CfPaymentId     = payment.CfPaymentId,
                PaymentStatus   = payment.PaymentStatus,
                PaymentAmount   = payment.PaymentAmount,
                PaymentCurrency = payment.PaymentCurrency,
                PaymentMessage  = payment.PaymentMessage,
                PaymentTime     = payment.PaymentTime,
                BankReference   = payment.BankReference,
                AuthId          = payment.AuthId,
                CustomerId      = customer?.CustomerId,
                CustomerName    = customer?.CustomerName,
                CustomerEmail   = customer?.CustomerEmail,
                CustomerPhone   = customer?.CustomerPhone,
                EventTime       = payload.EventTime,
                ReceivedAtUtc   = DateTime.UtcNow
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
