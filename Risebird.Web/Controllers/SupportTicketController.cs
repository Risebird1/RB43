using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// Handles:
    ///   GET  /SupportTicket            → dashboard listing all tickets
    ///   POST /SupportTicket/Webhook    → receives JSON payload from Relay.app,
    ///                                    inserts a new SupportTickets row
    ///
    /// Relay.app sends this JSON structure:
    /// {
    ///   "sender_email":  "customer@example.com",
    ///   "subject":       "Unable to login",
    ///   "mail_content":  "Hi team, I have been unable to login...",
    ///   "received_at":   "2026-03-19T10:30:00Z",
    ///   "intent":        "Login Issue"
    /// }
    ///
    /// Webhook security: validate the X-Relay-Secret header against the
    /// value stored in appsettings.json under RelayWebhook:Secret.
    /// </summary>
    [Authorize]
    public class SupportTicketController : Controller
    {
        private readonly ISupportTicketRepository _repository;
        private readonly IApplicationErrorLogger  _logger;
        private readonly IConfiguration           _configuration;

        public SupportTicketController(
            ISupportTicketRepository repository,
            IApplicationErrorLogger  logger,
            IConfiguration           configuration)
        {
            _repository    = repository;
            _logger        = logger;
            _configuration = configuration;
        }

        // ── Dashboard ─────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var tickets = await _repository.GetAllTicketsAsync();
                return View(tickets);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return View(new System.Collections.Generic.List<SupportTicketVM>());
            }
        }

        // ── Relay webhook ─────────────────────────────────────────────────────

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            try
            {
                // Optional shared-secret validation
                var expectedSecret = _configuration["RelayWebhook:Secret"];
                if (!string.IsNullOrWhiteSpace(expectedSecret))
                {
                    var incomingSecret = Request.Headers["X-Relay-Secret"].ToString();
                    if (incomingSecret != expectedSecret)
                        return Unauthorized(new { error = "Invalid secret." });
                }

                // Read raw body (Relay sends JSON)
                string rawBody;
                using (var reader = new StreamReader(Request.Body))
                    rawBody = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(rawBody))
                    return BadRequest(new { error = "Empty request body." });

                // Relay field names use snake_case
                var payload = JsonConvert.DeserializeObject<RelayEmailWebhookPayload>(rawBody,
                    new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });

                if (payload == null || string.IsNullOrWhiteSpace(payload.SenderEmail))
                    return BadRequest(new { error = "Missing required field: sender_email." });

                // Parse received_at; fall back to now if absent or unparseable
                DateTime? receivedAt = null;
                if (!string.IsNullOrWhiteSpace(payload.ReceivedAt) &&
                    DateTime.TryParse(payload.ReceivedAt, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                    receivedAt = parsed;

                var ticket = new SupportTicket
                {
                    SenderEmail = payload.SenderEmail?.Trim(),
                    Subject     = payload.Subject?.Trim(),
                    MailContent = payload.MailContent?.Trim(),
                    ReceivedAt  = receivedAt ?? DateTime.UtcNow,
                    Intent      = payload.Intent?.Trim()
                };

                var newId = await _repository.InsertTicketAsync(ticket);

                return Ok(new { success = true, ticketId = newId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(500, new { error = "An internal error occurred." });
            }
        }
    }
}
