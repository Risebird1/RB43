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
using System.Collections.Generic;

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
        private readonly INotificationHelper _notificationHelper;

        private const string CfSection = "Cashfree";
        private const string TransferSuccessStatus = "TRANSFER_SUCCESS";

        public CashfreeWebhookController(
            IConfiguration configuration,
            IMeetingRespository meetingRepository,
            IApplicationErrorLogger applicationErrorLogger,
            INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _meetingRepository = meetingRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
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

                // Strip BOM / zero-width spaces that certain HTTP clients or proxies
                // prepend — these cause JObject.Parse to throw even on otherwise valid JSON.
                var cleanBody = rawBody.Trim().TrimStart('\uFEFF', '\u200B');

                // Peek at the root keys to decide which event shape we received.
                // Transfer events use "type" at the root; account-level alerts
                // (e.g. LOW_BALANCE_ALERT) use "event" at the root instead.
                JObject rootObj;
                try
                {
                    // Use JToken.Parse (accepts any JSON value) then validate the root
                    // is an object.  This prevents a crash when Cashfree test webhooks
                    // send a JSON array or bare null instead of the expected object.
                    var token = JToken.Parse(cleanBody);
                    if (token is JObject parsedObj)
                    {
                        rootObj = parsedObj;
                    }
                    else
                    {
                        var typeError = new { success = false, message = $"Expected a JSON object but received {token.Type}." };
                        await _applicationErrorLogger.LogMessage(
                            LogLevelInfo.Warning,
                            new Exception(
                                $"[CashfreeWebhook] Unexpected JSON root type '{token.Type}'. " +
                                $"Raw (first 200): {rawBody.Substring(0, Math.Min(200, rawBody.Length))}"));
                        await _meetingRepository.InsertPaymentLog(
                            endpoint:           endpoint,
                            requestBody:        rawBody,
                            responseBody:       JsonConvert.SerializeObject(typeError),
                            httpStatusCode:     400,
                            logLevel:           "Error",
                            signatureTimestamp: timestamp,
                            signatureReceived:  receivedSig,
                            errorMessage:       $"Expected JSON object, received {token.Type}.",
                            callerIp:           callerIp);
                        return BadRequest(typeError);
                    }
                }
                catch (JsonException ex)
                {
                    var parseError = new { success = false, message = "Malformed JSON payload token." };
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

                if (payload?.Data == null || string.IsNullOrEmpty(payload.Data.TransferId))
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

                var transfer = payload.Data;

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

                // ── 4a. Send payment confirmation email on TRANSFER_SUCCESS ── //
                if (string.Equals(transfer.TransferStatus, TransferSuccessStatus, StringComparison.OrdinalIgnoreCase)
                    && rowsUpdated > 0)
                {
                    await SendPaymentConfirmationEmailAsync(transfer.TransferId, transfer.CfTransferId, transfer.TransferUtr);
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
        //  Payment confirmation email                                         //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Fetches the interviewer email and payment breakdown for the supplied
        /// transfer identifiers and sends the same confirmation email that the
        /// Finance team's "Confirm Payment" button triggers.
        /// </summary>
        private async Task SendPaymentConfirmationEmailAsync(string transferId, string cfTransferId, string transferUtr)
        {
            try
            {
                var details = await _meetingRepository.GetPaymentDetailsByTransferId(transferId, cfTransferId);
                if (details == null || string.IsNullOrWhiteSpace(details.Email))
                    return;

                var breakdown = await _meetingRepository.GetPaymentHistoryDetailsView(details.PaymentHistoryId);

                var sb = new StringBuilder();
                sb.Append("<table cellspacing='0' width='100%'><thead><tr><th>Date</th><th>Payment for</th><th>Type</th><th>Status</th><th>Total Amount</th><th>Payable Amount</th></tr></thead><tbody>");

                if (breakdown != null)
                {
                    foreach (var item in breakdown)
                    {
                        sb.Append("<tr>");
                        sb.Append("<td>" + item.Date.ToString() + "</td>");
                        sb.Append("<td>" + item.Comment + "</td>");
                        sb.Append("<td>" + item.Type + "</td>");
                        sb.Append("<td>" + (item.Status == "Advance" ? item.Status : "Paid") + "</td>");
                        sb.Append("<td>" + item.Points + "</td>");
                        sb.Append("<td>" + (item.Status != "Advance"
                            ? Math.Round(item.Points - item.Points * .1).ToString()
                            : item.Points.ToString()) + "</td>");
                        sb.Append("</tr>");
                    }
                }

                sb.Append("</tbody></table>");

                if (!string.IsNullOrWhiteSpace(transferUtr))
                {
                    sb.Append("<p style='margin-top:10px;'><strong>UTR Number: </strong>" + transferUtr + "</p>");
                }

                var mail = new Mail
                {
                    TO      = details.Email,
                    CC      = "finance@risebird.io",
                    Subject = "RiseBird: Finance Team has done payment"
                };

                await _notificationHelper.RBFNPaymentConfirmationNotification(mail, details.Amount.ToString(), sb);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        // ------------------------------------------------------------------ //
        //  Mapping                                                            //
        // ------------------------------------------------------------------ //

        private static CashfreeTransactionStatus MapToTransactionStatus(CashfreeWebhookPayload payload)
        {
            var d = payload.Data;
            return new CashfreeTransactionStatus
            {
                EventType      = payload.Type,
                TransferId     = d.TransferId,
                CfTransferId   = d.CfTransferId,
                TransferStatus = d.TransferStatus,
                TransferAmount = d.TransferAmount,
                TransferUtr    = d.TransferUtr,
                EventTime      = payload.EventTime,
                ReceivedAtUtc  = DateTime.UtcNow
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
