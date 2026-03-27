using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        // ── Status update ─────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int    id,
            string status,
            string returnStatus      = null,
            string returnIntent      = null,
            string returnSenderEmail = null,
            string returnDateFrom    = null,
            string returnDateTo      = null,
            int    returnPage        = 1,
            int    returnPageSize    = 25)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var allowed = new[] { "Open", "InProgress", "Closed" };
                if (id <= 0 || !Array.Exists(allowed, s => s == status))
                {
                    TempData["Error"] = "Invalid status update request.";
                }
                else
                {
                    var changedBy = HttpContext.Session.GetString("email").Trim('"');

                    var ok = await _repository.UpdateTicketStatusAsync(id, status, changedBy);
                    TempData[ok ? "Success" : "Error"] = ok
                        ? $"Ticket #{id} status updated to '{status}'."
                        : $"Could not update ticket #{id}. Please try again.";
                }

                return RedirectToAction("Index", new
                {
                    status      = returnStatus,
                    intent      = returnIntent,
                    senderEmail = returnSenderEmail,
                    dateFrom    = returnDateFrom,
                    dateTo      = returnDateTo,
                    page        = returnPage,
                    pageSize    = returnPageSize
                });
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        // ── Eligible assignees (JSON) ──────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetEligibleAssignees(int id)
        {
            if (HttpContext.Session.GetString("email") == null ||
                HttpContext.Session.GetString("profileName") == null)
                return Json(new string[0]);

            try
            {
                var emails = await _repository.GetEligibleAssigneesAsync(id);
                return Json(emails);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new string[0]);
            }
        }

        // ── Update assignment (AJAX POST) ──────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAssignment(int id, string assignedTo)
        {
            if (HttpContext.Session.GetString("email") != null &&
                HttpContext.Session.GetString("profileName") != null)
            {
                if (id <= 0 || string.IsNullOrWhiteSpace(assignedTo))
                    return Json(new { success = false, message = "Invalid request." });

                var changedBy = HttpContext.Session.GetString("email").Trim('"');
                var ok        = await _repository.UpdateTicketAssignmentAsync(id, assignedTo.Trim(), changedBy);
                return Json(new { success = ok });
            }
            else
            {
                return Json(new { success = false, message = "Session expired." });
            }
        }

        // ── Ticket history (JSON) ──────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetHistory(int id)
        {
            try
            {
                var history = await _repository.GetTicketHistoryAsync(id);
                return Json(history.Select(h => new
                {
                    h.Id,
                    h.TicketId,
                    fromStatus = h.FromStatus ?? "—",
                    toStatus   = h.ToStatus,
                    changedBy  = h.ChangedBy,
                    changedAt  = h.ChangedAt.ToString("dd MMM yyyy, HH:mm") + " UTC",
                    eventType  = h.EventType ?? "StatusChange"
                }));
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new object[0]);
            }
        }

        // ── IRF matching (server-side, no extra Relay step) ───────────────────

        /// <summary>
        /// Matches the extracted job title to the best candidate IRF for the sender.
        ///
        /// Matching priority:
        ///   1. Score against IRF.JDTitleTwo (the role-specific JD title — most precise)
        ///   2. If all JDTitleTwo scores are 0, fall back to IRF.SkillCluster
        ///
        /// Scoring tiers (per field):
        ///   100 — direct string containment (one contains the other)
        ///   1–90 — word-overlap ratio
        ///   0    — no shared words
        ///
        /// Tiebreaker: lowest IRF.id (oldest / most established channel).
        /// </summary>
        private async Task<IrfMatchVM> MatchIRFAsync(
            string senderEmail,
            string jobTitle,
            string skillChannelId = null)
        {
            var irfs = await _repository.GetIRFsForSenderAsync(senderEmail);
            if (irfs == null || irfs.Count == 0) return null;
            if (irfs.Count == 1)                 return irfs[0];

            // Shortcut: if the sender included an explicit Skill Channel ID in the
            // email body, try to resolve it directly from the sender's eligible IRF
            // list. This avoids running the scoring algorithm entirely.
            // A Skill Channel ID is exactly 10 alphanumeric characters.
            if (!string.IsNullOrWhiteSpace(skillChannelId)
                && skillChannelId.Trim().Length == 10)
            {
                var direct = irfs.FirstOrDefault(irf =>
                    string.Equals(irf.IRFStringId, skillChannelId.Trim(),
                        StringComparison.OrdinalIgnoreCase));
                if (direct != null) return direct;
                // ID not in sender's eligible list — fall through to scoring
            }

            if (string.IsNullOrWhiteSpace(jobTitle))
                return BestByRecency(irfs);

            // Pass 1: score against JDTiltle
            var pass1 = irfs
                .Select(irf => new { irf, score = ScoreIRF(irf.JDTiltle, jobTitle) })
                .OrderByDescending(x => x.score)
                .ThenByDescending(x => x.irf.EndDate)
                .ThenByDescending(x => x.irf.StartDate)
                .ThenByDescending(x => x.irf.CreatedOn)
                .ToList();

            if (pass1[0].score > 0)
                return pass1[0].irf;

            // Pass 2: JDTiltle produced no match — fall back to SkillCluster
            var pass2 = irfs
                .Select(irf => new { irf, score = ScoreIRF(irf.SkillCluster, jobTitle) })
                .OrderByDescending(x => x.score)
                .ThenByDescending(x => x.irf.EndDate)
                .ThenByDescending(x => x.irf.StartDate)
                .ThenByDescending(x => x.irf.CreatedOn)
                .ToList();

            return pass2[0].score > 0 ? pass2[0].irf : BestByRecency(irfs);
        }

        private static IrfMatchVM BestByRecency(List<IrfMatchVM> irfs) =>
            irfs
                .OrderByDescending(x => x.EndDate)
                .ThenByDescending(x => x.StartDate)
                .ThenByDescending(x => x.CreatedOn)
                .First();

        private static int ScoreIRF(string irfField, string jobTitle)
        {
            if (string.IsNullOrWhiteSpace(irfField) || string.IsNullOrWhiteSpace(jobTitle))
                return 0;

            var f  = irfField.ToLowerInvariant();
            var jt = jobTitle.ToLowerInvariant();

            if (jt.Contains(f) || f.Contains(jt)) return 100;

            var sep     = new[] { ' ', ',', '/', '+', '-', '.', '_', '(', ')' };
            var fWords  = f.Split(sep,  StringSplitOptions.RemoveEmptyEntries)
                           .Where(w => w.Length > 2).ToHashSet();
            var jtWords = jt.Split(sep, StringSplitOptions.RemoveEmptyEntries)
                            .Where(w => w.Length > 2).ToHashSet();

            if (fWords.Count == 0 || jtWords.Count == 0) return 0;

            int matches = fWords.Count(w => jtWords.Contains(w));
            if (matches == 0) return 0;

            return (int)((double)matches / Math.Max(fWords.Count, jtWords.Count) * 90);
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
                    SummaryInterviewScores   = payload.SummaryInterviewScores?.Trim(),
                    JobTitle                 = payload.JobTitle?.Trim()
                };

                var newId = await _repository.InsertTicketAsync(ticket);

                // ── Resume ingestion ("New candidate submission" OR "New interview request") ──
                var resumeIngestionResult = new System.Text.StringBuilder();
                var isResumeIngestionIntent =
                    string.Equals(ticket.Intent, "New candidate submission",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ticket.Intent, "New interview request",
                        StringComparison.OrdinalIgnoreCase);

                if (isResumeIngestionIntent
                    && ticket.AttachmentResumes
                    && payload.ResumeDriveUrls != null
                    && payload.ResumeDriveUrls.Count > 0)
                {
                    IrfMatchVM irfMatch = null;
                    int        inserted = 0;

                    try
                    {
                        irfMatch = await MatchIRFAsync(
                            ticket.SenderEmail,
                            ticket.JobTitle ?? ticket.Subject,
                            payload.SkillChannelId);

                        if (irfMatch != null)
                        {
                            // Relay sometimes sends all URLs joined by whitespace in a
                            // single array element instead of one element per URL.
                            // Flatten by splitting every element on whitespace so both
                            // formats (proper array and space-joined string) are handled.
                            var individualUrls = payload.ResumeDriveUrls
                                .Where(u => !string.IsNullOrWhiteSpace(u))
                                .SelectMany(u => u.Split(
                                    new[] { ' ', '\n', '\r', '\t' },
                                    StringSplitOptions.RemoveEmptyEntries))
                                .Where(u => u.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                .Distinct()
                                .ToList();

                            foreach (var url in individualUrls)
                            {
                                var rid = await _repository.InsertCandidateResumeAsync(
                                    irfMatch.Id,
                                    url.Trim(),
                                    irfMatch.RisebirdHrId,
                                    ticket.SenderEmail);
                                if (rid > 0) inserted++;
                            }
                        }
                    }
                    catch (Exception rex)
                    {
                        await _logger.LogMessage(LogLevelInfo.Exception, rex);
                    }

                    if (inserted > 0)
                    {
                        // Ingestion succeeded — system did the work, reassign away from HR
                        await _repository.UpdateTicketAssignmentAsync(newId, "System", "System");
                        resumeIngestionResult.Append(
                            $"Resumes ingested: {inserted}, IRF: {irfMatch.IRFStringId}");
                    }
                    else
                    {
                        // Ingestion failed — the ticket keeps its naturally resolved HR
                        // assignment so they can handle it manually. No update needed.
                        var fallbackHR = irfMatch?.RisebirdHrEmail;
                        resumeIngestionResult.Append(
                            !string.IsNullOrWhiteSpace(fallbackHR)
                                ? $"Ingestion failed — assigned to {fallbackHR} for manual review."
                                : "Ingestion failed — no IRF resolved; ticket assigned to default HR.");
                    }
                }

                return Ok(new
                {
                    success          = true,
                    ticketId         = newId,
                    resumeIngestion  = resumeIngestionResult.Length > 0
                                       ? resumeIngestionResult.ToString()
                                       : "N/A"
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(500, new { error = "An internal error occurred." });
            }
        }
    }
}
