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
        public async Task<IActionResult> Index(
            string status      = null,
            string intent      = null,
            string senderEmail = null,
            string dateFrom    = null,
            string dateTo      = null,
            int    page        = 1,
            int    pageSize    = 25)
        {
            try
            {
                if (page     < 1)   page     = 1;
                if (pageSize < 10)  pageSize = 10;
                if (pageSize > 200) pageSize = 200;

                DateTime? dtFrom = null;
                DateTime? dtTo   = null;

                if (!string.IsNullOrWhiteSpace(dateFrom) &&
                    DateTime.TryParse(dateFrom, out var pFrom)) dtFrom = pFrom;
                if (!string.IsNullOrWhiteSpace(dateTo) &&
                    DateTime.TryParse(dateTo,   out var pTo))   dtTo   = pTo;

                var tickets = await _repository.GetTicketsAsync(
                    status, intent, senderEmail, dtFrom, dtTo, page, pageSize);

                int totalCount = tickets.Count > 0 ? tickets[0].TotalCount : 0;
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                ViewBag.Page        = page;
                ViewBag.PageSize    = pageSize;
                ViewBag.TotalCount  = totalCount;
                ViewBag.TotalPages  = totalPages;
                ViewBag.Status      = status      ?? string.Empty;
                ViewBag.Intent      = intent      ?? string.Empty;
                ViewBag.SenderEmail = senderEmail ?? string.Empty;
                ViewBag.DateFrom    = dateFrom    ?? string.Empty;
                ViewBag.DateTo      = dateTo      ?? string.Empty;

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
                // Shared-secret validation
                var expectedSecret = _configuration["RelayWebhook:Secret"];
                if (!string.IsNullOrWhiteSpace(expectedSecret))
                {
                    var incoming = Request.Headers["X-Relay-Secret"].ToString();
                    if (incoming != expectedSecret)
                        return Unauthorized(new { error = "Invalid secret." });
                }

                string rawBody;
                using (var reader = new StreamReader(Request.Body))
                    rawBody = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(rawBody))
                    return BadRequest(new { error = "Empty request body." });

                var payload = JsonConvert.DeserializeObject<RelayEmailWebhookPayload>(rawBody,
                    new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });

                if (payload == null || string.IsNullOrWhiteSpace(payload.SenderEmail))
                    return BadRequest(new { error = "Missing required field: sender_email." });

                DateTime? receivedAt = null;
                if (!string.IsNullOrWhiteSpace(payload.ReceivedAt) &&
                    DateTime.TryParse(payload.ReceivedAt, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                    receivedAt = parsed;

                var ticket = new SupportTicket
                {
                    SenderEmail              = payload.SenderEmail?.Trim(),
                    Subject                  = payload.Subject?.Trim(),
                    MailContent              = payload.MailContent?.Trim(),
                    ReceivedAt               = receivedAt ?? DateTime.UtcNow,
                    Intent                   = payload.WebhookIntent?.Trim(),
                    PrimarySpecialization    = payload.PrimarySpecialization?.Trim(),
                    MailBodyIntent           = payload.MailBodyIntent?.Trim(),
                    AttachmentJobDescription = payload.AttachmentJobDescription,
                    AttachmentPdf            = payload.AttachmentPdf,
                    AttachmentResumes        = payload.AttachmentResumes,
                    DriveFolderLink          = payload.DriveFolderLink?.Trim(),
                    SummaryMail              = payload.SummaryMail?.Trim(),
                    SummaryCandidates        = payload.SummaryCandidates?.Trim(),
                    SummaryInterviewScores   = payload.SummaryInterviewScores?.Trim()
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
