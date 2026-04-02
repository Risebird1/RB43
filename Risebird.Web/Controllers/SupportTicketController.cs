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
            string tab         = "all",
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

                List<SupportTicketVM> tickets;
                int totalCount;
                int totalPages;

                if (tab == "system")
                {
                    // System tab — tickets assigned ONLY to System (never to human HR)
                    tickets    = await _repository.GetSystemAssignedTicketsAsync(page, pageSize);
                    totalCount = tickets.Count > 0 ? tickets[0].TotalCount : 0;
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                    // Clear filters (they don't apply)
                    status = intent = senderEmail = dateFrom = dateTo = null;
                }
                else if (tab == "hybrid")
                {
                    // Hybrid tab — tickets assigned to BOTH System AND human HR
                    tickets    = await _repository.GetHybridTicketsAsync(page, pageSize);
                    totalCount = tickets.Count > 0 ? tickets[0].TotalCount : 0;
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                    // Clear filters (they don't apply)
                    status = intent = senderEmail = dateFrom = dateTo = null;
                }
                else
                {
                    // Manual Review tab — tickets never assigned to System
                    DateTime? dtFrom = null;
                    DateTime? dtTo   = null;

                    if (!string.IsNullOrWhiteSpace(dateFrom) &&
                        DateTime.TryParse(dateFrom, out var pFrom)) dtFrom = pFrom;
                    if (!string.IsNullOrWhiteSpace(dateTo) &&
                        DateTime.TryParse(dateTo,   out var pTo))   dtTo   = pTo;

                    tickets    = await _repository.GetTicketsAsync(
                        status, intent, senderEmail, dtFrom, dtTo, page, pageSize);
                    totalCount = tickets.Count > 0 ? tickets[0].TotalCount : 0;
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                }

                // Fetch counts for tab badges
                var systemTicketsForCount = await _repository.GetSystemAssignedTicketsAsync(1, 1);
                int systemCount = systemTicketsForCount.Count > 0 ? systemTicketsForCount[0].TotalCount : 0;

                var hybridTicketsForCount = await _repository.GetHybridTicketsAsync(1, 1);
                int hybridCount = hybridTicketsForCount.Count > 0 ? hybridTicketsForCount[0].TotalCount : 0;

                ViewBag.Tab           = tab;
                ViewBag.Page          = page;
                ViewBag.PageSize      = pageSize;
                ViewBag.TotalCount    = totalCount;
                ViewBag.TotalPages    = totalPages;
                ViewBag.SystemCount   = systemCount;
                ViewBag.HybridCount   = hybridCount;
                ViewBag.Status        = status      ?? string.Empty;
                ViewBag.Intent        = intent      ?? string.Empty;
                ViewBag.SenderEmail   = senderEmail ?? string.Empty;
                ViewBag.DateFrom      = dateFrom    ?? string.Empty;
                ViewBag.DateTo        = dateTo      ?? string.Empty;

                return View(tickets);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return View(new List<SupportTicketVM>());
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
        /// Resolves the IRF for resume ingestion.
        ///   1. If skill_channel_id is a 10-char code, usp_GetIRFByStringId confirms it
        ///      against IRF.IRFId; on match, scoring is skipped.
        ///   2. Otherwise: sender's eligible IRFs via TransClientUser, then score
        ///      JDTiltle then SkillCluster; tiebreak by EndDate, StartDate, CreatedOn.
        /// </summary>
        /// <summary>
        /// Matches sender to the best IRF based on job title and specialization.
        /// Priority: explicit skill_channel_id > scoring (JDTitle + SkillCluster vs jobTitle + specialization) > recency.
        /// </summary>
        private async Task<IrfMatchVM> MatchIRFAsync(
            string senderEmail,
            string jobTitle,
            string skillChannelId = null,
            string specialization = null)
        {
            // Explicit Skill Channel ID from Relay: confirm it exists in IRF (string
            // IRFId column). No sender / TransClientUser filter — if valid, skip scoring.
            if (!string.IsNullOrWhiteSpace(skillChannelId)
                && skillChannelId.Trim().Length == 10)
            {
                var byDb = await _repository.GetIRFByStringIdAsync(skillChannelId.Trim());
                if (byDb != null)
                    return byDb;
            }

            var irfs = await _repository.GetIRFsForSenderAsync(senderEmail);
            
            // If no existing mappings found, try auto-mapping based on client and job title
            if (irfs == null || irfs.Count == 0)
            {
                // Attempt to auto-map: finds matching IRF for user's client and creates mapping
                var autoMapped = await _repository.AutoMapUserToIRFAsync(
                    senderEmail, 
                    jobTitle, 
                    specialization);
                
                if (autoMapped != null)
                {
                    // Auto-mapping succeeded — return the newly mapped IRF
                    return autoMapped;
                }
                
                // Auto-mapping failed — no matching IRF found
                return null;
            }

            // Build combined search text from jobTitle and specialization
            var searchText = string.Join(" ",
                new[] { jobTitle, specialization }
                    .Where(s => !string.IsNullOrWhiteSpace(s)))
                .Trim();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                // No search criteria — return most recent, or single IRF
                return irfs.Count == 1 ? irfs[0] : BestByRecency(irfs);
            }

            // Score each mapped IRF: combine JDTiltle and SkillCluster into one field to match against
            const int StrongMatchThreshold = 80; // Score >= 70 = confident match
            
            var scored = irfs
                .Select(irf => new
                {
                    irf,
                    score = ScoreIRF(
                        (irf.JDTiltle + " " + irf.SkillCluster).Trim(),
                        searchText)
                })
                .OrderByDescending(x => x.score)
                .ThenByDescending(x => x.irf.EndDate)
                .ThenByDescending(x => x.irf.StartDate)
                .ThenByDescending(x => x.irf.CreatedOn)
                .ToList();

            var bestMappedScore = scored[0].score;

            // If mapped IRF has a strong match (>= threshold), use it directly
            if (bestMappedScore >= StrongMatchThreshold)
                return scored[0].irf;

            // Mapped IRFs don't have a strong match — search ALL client IRFs for a better fit
            // This handles cases where sender is mapped to wrong IRF but correct one exists for same client
            var clientBestMatch = await _repository.FindBestIRFForClientAsync(
                senderEmail,
                jobTitle,
                specialization,
                createMapping: true);

            if (clientBestMatch != null)
            {
                // Found a strong matching IRF among all client IRFs — use it
                return clientBestMatch;
            }

            // No strong match found anywhere
            // If mapped IRF has at least some match (> 0), use it as fallback
            if (bestMappedScore > 0)
                return scored[0].irf;

            // No match at all — return null for manual review
            return null;
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

        // ── Sensitive email detection ─────────────────────────────────────────
        
        private static readonly HashSet<string> SensitiveSenderDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            // Banks (India)
            "hdfcbank.com", "icicibank.com", "sbi.co.in", "axisbank.com", "kotak.com",
            "yesbank.in", "indusind.com", "rbl.co.in", "idfcfirstbank.com", "bankofbaroda.co.in",
            // Banks (International)
            "chase.com", "bankofamerica.com", "wellsfargo.com", "citi.com", "hsbc.com",
            // HR/Payroll systems
            "greythr.com", "darwinbox.com", "keka.com", "adp.com", "paylocity.com",
            "gusto.com", "rippling.com", "bamboohr.com", "zenefits.com", "namely.com",
            // Tax/Government
            "incometax.gov.in", "irs.gov", "gst.gov.in",
            // Insurance
            "licindia.in", "iciciprulife.com", "hdfclife.com", "starhealth.in",
            // Credit cards
            "americanexpress.com", "visa.com", "mastercard.com"
        };

        private static readonly string[] SensitiveKeywords = new[]
        {
            // Salary/Payroll
            "salary credited", "salary slip", "payslip", "pay slip", "salary statement",
            "salary transfer", "salary deposited", "monthly salary", "net pay",
            "gross salary", "salary credit", "payroll",
            // Bank
            "bank statement", "account statement", "transaction alert", "balance update",
            "credited to your account", "debited from your account", "fund transfer",
            "neft", "imps", "rtgs", "upi transaction", "account balance",
            // Tax
            "form 16", "form-16", "form16", "itr", "income tax return", "tax return",
            "tds certificate", "w-2", "w2 form", "1099", "tax statement",
            // Medical
            "medical report", "lab report", "health report", "prescription",
            "diagnosis", "patient report", "test results", "medical records",
            // Password/Security
            "password reset", "reset your password", "otp", "one time password",
            "verification code", "security code", "2fa", "two factor",
            // Identity
            "aadhaar", "pan card", "passport", "ssn", "social security",
            "identity verification", "kyc",
            // Credit/Loans
            "credit card statement", "loan statement", "emi", "loan approved",
            "credit limit", "card statement",
            // Insurance
            "policy document", "insurance premium", "claim status", "policy renewal",
            // Personal bills
            "electricity bill", "phone bill", "utility bill", "mobile bill"
        };

        private static (bool IsSensitive, string Reason) DetectSensitiveEmail(
            string senderEmail, string subject, string body, string intent)
        {
            // Check if Relay already identified it as sensitive
            if (string.Equals(intent, "Sensitive personal email", StringComparison.OrdinalIgnoreCase))
                return (true, "Identified by Relay as sensitive personal email");

            // Check sender domain
            if (!string.IsNullOrWhiteSpace(senderEmail))
            {
                var atIndex = senderEmail.IndexOf('@');
                if (atIndex > 0 && atIndex < senderEmail.Length - 1)
                {
                    var domain = senderEmail.Substring(atIndex + 1).ToLowerInvariant();
                    if (SensitiveSenderDomains.Contains(domain))
                        return (true, $"Sender domain blocked: {domain}");
                }
            }

            // Check subject and body for sensitive keywords
            var combined = ((subject ?? "") + " " + (body ?? "")).ToLowerInvariant();
            foreach (var keyword in SensitiveKeywords)
            {
                if (combined.Contains(keyword))
                    return (true, $"Sensitive keyword detected: {keyword}");
            }

            return (false, null);
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

                // ── SENSITIVE EMAIL CHECK (Privacy Protection) ──────────────────
                var (isSensitive, sensitiveReason) = DetectSensitiveEmail(
                    payload.SenderEmail,
                    payload.Subject,
                    payload.MailContent,
                    payload.WebhookIntent);

                if (isSensitive)
                {
                    await _logger.LogMessage(LogLevelInfo.Warning,
                        $"BLOCKED sensitive email from {payload.SenderEmail}: {sensitiveReason}");
                    
                    return Ok(new
                    {
                        success = false,
                        blocked = true,
                        reason  = "Sensitive personal email detected and blocked for privacy protection.",
                        detail  = sensitiveReason
                    });
                }

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

                // Correct attachment flags based on actual resume_drive_urls content
                // GPT may not set these correctly, so we override based on actual data
                bool hasResumeUrls = payload.ResumeDriveUrls != null
                                  && payload.ResumeDriveUrls.Any(u => !string.IsNullOrWhiteSpace(u));
                if (hasResumeUrls)
                {
                    ticket.AttachmentResumes = true;
                    // If there are resume URLs, there's likely at least one PDF
                    ticket.AttachmentPdf = true;
                }

                var newId = await _repository.InsertTicketAsync(ticket);

                // ── Resume ingestion logic ──
                var resumeIngestionResult = new System.Text.StringBuilder();
                
                var isNewCandidateSubmission = string.Equals(ticket.Intent, 
                    "New candidate submission", StringComparison.OrdinalIgnoreCase);
                var isNewInterviewRequest = string.Equals(ticket.Intent, 
                    "New interview request", StringComparison.OrdinalIgnoreCase);
                
                // For "New interview request" with JD attached:
                // This is a request to CREATE a new skill channel — skip resume ingestion
                // and assign to HR for manual channel creation.
                var isNewChannelRequest = isNewInterviewRequest && ticket.AttachmentJobDescription;
                
                // Resume ingestion applies to:
                // 1. "New candidate submission" (always for existing channels)
                // 2. "New interview request" WITHOUT JD attached (resumes for existing channel)
                var shouldAttemptIngestion = 
                    (isNewCandidateSubmission || (isNewInterviewRequest && !isNewChannelRequest))
                    && payload.ResumeDriveUrls != null
                    && payload.ResumeDriveUrls.Count > 0;

                if (shouldAttemptIngestion)
                {
                    IrfMatchVM irfMatch = null;
                    int        inserted = 0;

                    try
                    {
                        irfMatch = await MatchIRFAsync(
                            ticket.SenderEmail,
                            ticket.JobTitle ?? ticket.Subject,
                            payload.SkillChannelId,
                            ticket.PrimarySpecialization);

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
                                    ticket.SenderEmail,
                                    newId);
                                if (rid > 0) inserted++;
                            }
                        }
                    }
                    catch (Exception rex)
                    {
                        await _logger.LogMessage(LogLevelInfo.Exception, rex);
                    }

                    // For resume ingestion intents, AssignedTo was left NULL by the SP.
                    // Now assign based on the outcome:
                    if (inserted > 0)
                    {
                        // Ingestion succeeded — assign to System and close
                        await _repository.UpdateTicketAssignmentAsync(newId, "System", "System");
                        await _repository.UpdateTicketStatusAsync(newId, "Closed", "System");

                        resumeIngestionResult.Append(
                            $"Resumes ingested: {inserted}, IRF: {irfMatch.IRFStringId}, ticket auto-closed");
                    }
                    else
                    {
                        // Ingestion failed — assign to the resolved HR for manual review
                        var fallbackHR = irfMatch?.RisebirdHrEmail;
                        if (!string.IsNullOrWhiteSpace(fallbackHR))
                        {
                            await _repository.UpdateTicketAssignmentAsync(newId, fallbackHR, "System");
                            resumeIngestionResult.Append(
                                $"Ingestion failed — assigned to {fallbackHR} for manual review.");
                        }
                        else
                        {
                            resumeIngestionResult.Append(
                                "Ingestion failed — no IRF or HR resolved; ticket left unassigned.");
                        }
                    }
                }
                else if (isNewChannelRequest)
                {
                    // "New interview request" with JD attached = request to CREATE a new skill channel
                    // Don't attempt resume ingestion — assign directly to an HR for manual channel creation
                    // No need to match an IRF (it doesn't exist yet) — just find the HR for this client
                    var hrEmail = await _repository.GetHRForSenderAsync(ticket.SenderEmail);

                    if (!string.IsNullOrWhiteSpace(hrEmail))
                    {
                        await _repository.UpdateTicketAssignmentAsync(newId, hrEmail, "System");
                        resumeIngestionResult.Append(
                            $"New channel request (JD attached) — assigned to {hrEmail} for channel creation.");
                    }
                    else
                    {
                        resumeIngestionResult.Append(
                            "New channel request (JD attached) — no HR resolved; ticket left unassigned.");
                    }
                }
                else if (isNewCandidateSubmission || isNewInterviewRequest)
                {
                    // Resume ingestion intent but no resumes attached or no drive URLs
                    // Still need to assign to an HR since the SP left it NULL
                    // No IRF matching needed — just find the HR for this client directly
                    var hrEmail = await _repository.GetHRForSenderAsync(ticket.SenderEmail);

                    if (!string.IsNullOrWhiteSpace(hrEmail))
                    {
                        await _repository.UpdateTicketAssignmentAsync(newId, hrEmail, "System");
                        resumeIngestionResult.Append(
                            $"No resumes to ingest — assigned to {hrEmail} for manual review.");
                    }
                    else
                    {
                        resumeIngestionResult.Append(
                            "No resumes to ingest and no HR resolved; ticket left unassigned.");
                    }
                }
                else
                {
                    // Non-resume-ingestion intents (e.g., "Candidate status update", "Stop interviews", etc.)
                    // The SP already handled HR assignment for these intents.
                    // No additional action needed here.
                    resumeIngestionResult.Append("N/A");
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
