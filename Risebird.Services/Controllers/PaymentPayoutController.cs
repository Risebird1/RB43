using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System.Security.Cryptography;
using System.Text;

namespace Risebird.Services.Controllers
{
    /// <summary>
    /// Receives Razorpay payout webhooks and updates payout status in PaymentHistory.
    /// Configure webhook URL in Razorpay Dashboard: https://dashboard.razorpay.com/app/webhooks (Payout events).
    /// Set Razorpay:WebhookSecret in appsettings to the secret from the dashboard for signature verification.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentPayoutController : ControllerBase
    {
        private readonly IMeetingService _meetingService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;

        public PaymentPayoutController(
            IMeetingService meetingService,
            IApplicationErrorLogger applicationErrorLogger,
            IConfiguration configuration)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
        }

        /// <summary>
        /// Razorpay payout status webhook. Receives events like payout.processed, payout.reversed, payout.failed, payout.initiated, payout.queued.
        /// Verifies X-Razorpay-Signature (HMAC-SHA256) and updates PaymentHistory.GatewayStatus for the payout.
        /// </summary>
        [HttpPost("PayoutStatusWebhook")]
        public async Task<IActionResult> PayoutStatusWebhook(CancellationToken cancellationToken = default)
        {
            string rawBody;
            try
            {
                Request.EnableBuffering();
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    rawBody = await reader.ReadToEndAsync(cancellationToken);
                }
                Request.Body.Position = 0;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(500, "Failed to read request body");
            }

            if (string.IsNullOrEmpty(rawBody))
            {
                return BadRequest("Empty body");
            }

            var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault();
            var webhookSecret = _configuration["Razorpay:WebhookSecret"];

            if (string.IsNullOrEmpty(webhookSecret))
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning, new Exception("Razorpay webhook received but Razorpay:WebhookSecret is not configured. Add WebhookSecret in appsettings and in Razorpay Dashboard."));
                return StatusCode(500, "Webhook secret not configured");
            }

            if (string.IsNullOrEmpty(signature) || !VerifyRazorpaySignature(rawBody, signature, webhookSecret))
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning, new Exception("Razorpay webhook signature verification failed."));
                return Unauthorized("Invalid signature");
            }

            string eventName;
            string payoutId;
            string status;
            try
            {
                var root = JObject.Parse(rawBody);
                eventName = root["event"]?.ToString();
                var payload = root["payload"] as JObject;
                if (payload == null)
                {
                    return Ok("Ignored: no payload");
                }

                // Razorpay payout webhook: payload.payout.entity or payload.entity
                var entity = payload["payout"]?["entity"] as JObject ?? payload["entity"] as JObject;
                if (entity == null)
                {
                    return Ok("Ignored: not a payout entity");
                }

                payoutId = entity["id"]?.ToString();
                status = entity["status"]?.ToString();

                if (string.IsNullOrEmpty(payoutId))
                {
                    return Ok("Ignored: no payout id");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return BadRequest("Invalid payload");
            }

            // Only process payout status events we care about for PaymentHistory
            var statusEvents = new[] { "payout.processed", "payout.reversed", "payout.failed", "payout.initiated", "payout.queued", "payout.pending", "payout.rejected", "payout.updated" };
            if (string.IsNullOrEmpty(eventName) || !statusEvents.Contains(eventName, StringComparer.OrdinalIgnoreCase))
            {
                return Ok("Ignored: event " + (eventName ?? "null"));
            }

            if (string.IsNullOrEmpty(status))
            {
                status = eventName.Replace("payout.", "", StringComparison.OrdinalIgnoreCase);
            }

            var updated = await _meetingService.UpdatePaymentHistoryGatewayStatus(payoutId.Trim(), status);
            if (updated)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Info, new Exception($"Razorpay payout webhook: {eventName} payoutId={payoutId} status={status} PaymentHistory updated."));
            }

            return Ok();
        }

        private static bool VerifyRazorpaySignature(string rawBody, string receivedSignature, string webhookSecret)
        {
            if (string.IsNullOrEmpty(receivedSignature) || string.IsNullOrEmpty(webhookSecret))
            {
                return false;
            }

            try
            {
                var keyBytes = Encoding.UTF8.GetBytes(webhookSecret);
                var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
                using var hmac = new HMACSHA256(keyBytes);
                var expectedHash = hmac.ComputeHash(bodyBytes);
                var receivedTrimmed = receivedSignature.Trim();
                byte[] receivedBytes;
                try
                {
                    receivedBytes = Convert.FromHexString(receivedTrimmed);
                }
                catch
                {
                    return false;
                }
                return receivedBytes.Length == expectedHash.Length && CryptographicOperations.FixedTimeEquals(expectedHash, receivedBytes);
            }
            catch
            {
                return false;
            }
        }
    }
}
