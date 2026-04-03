using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
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

                // ── HUMAN-IN-THE-LOOP: Store pending resumes for HR review ──
                // No automatic ingestion into CandidateResume table.
                // All resumes are stored as pending and require HR to manually trigger ingestion.
                var pendingResumeResult = new System.Text.StringBuilder();
                
                var isNewCandidateSubmission = string.Equals(ticket.Intent, 
                    "New candidate submission", StringComparison.OrdinalIgnoreCase);
                var isNewInterviewRequest = string.Equals(ticket.Intent, 
                    "New interview request", StringComparison.OrdinalIgnoreCase);
                
                // For "New interview request" with JD attached:
                // This is a request to CREATE a new skill channel — no resume ingestion.
                var isNewChannelRequest = isNewInterviewRequest && ticket.AttachmentJobDescription;
                
                // Check available resume data sources
                var hasResumeAssociations = payload.ResumeAssociations != null && payload.ResumeAssociations.Count > 0;
                var hasLegacyUrls = payload.ResumeDriveUrls != null && payload.ResumeDriveUrls.Count > 0;
                var hasCandidateScheduleData = payload.CandidateScheduleData != null && payload.CandidateScheduleData.Count > 0;
                
                var isResumeIngestionIntent = isNewCandidateSubmission || (isNewInterviewRequest && !isNewChannelRequest);
                var hasResumesToProcess = hasResumeAssociations || hasLegacyUrls || hasCandidateScheduleData;
                
                int pendingCount = 0;
                string fallbackHR = null;
                
                if (isResumeIngestionIntent && hasResumesToProcess)
                {
                    // Process candidate_schedule_data (with scheduling info)
                    if (hasCandidateScheduleData)
                    {
                        foreach (var candidate in payload.CandidateScheduleData)
                        {
                            if (string.IsNullOrWhiteSpace(candidate.ResumeDriveUrl))
                                continue;
                            
                            try
                            {
                                // Parse interview datetime if available
                                DateTime? interviewDateTime = null;
                                string timezone = candidate.Timezone ?? "IST";
                                bool hasScheduling = false;
                                
                                if (!string.IsNullOrWhiteSpace(candidate.InterviewDate) &&
                                    !string.IsNullOrWhiteSpace(candidate.InterviewTime))
                                {
                                    try
                                    {
                                        var dateStr = candidate.InterviewDate + " " + candidate.InterviewTime;
                                        interviewDateTime = DateTime.Parse(dateStr);
                                        hasScheduling = true;
                                    }
                                    catch { /* ignore parse errors */ }
                                }
                                
                                // Resolve IRF for this resume
                                IrfMatchVM irfMatch = null;
                                
                                // Try explicit skill_channel_id first
                                if (!string.IsNullOrWhiteSpace(candidate.SkillChannelId) &&
                                    candidate.SkillChannelId.Trim().Length == 10)
                                {
                                    irfMatch = await _repository.GetIRFByStringIdAsync(candidate.SkillChannelId.Trim());
                                }
                                
                                // Fallback to job title matching
                                if (irfMatch == null && !string.IsNullOrWhiteSpace(ticket.PrimarySpecialization))
                                {
                                    irfMatch = await MatchIRFAsync(
                                        ticket.SenderEmail,
                                        ticket.JobTitle ?? ticket.Subject,
                                        payload.SkillChannelId,
                                        ticket.PrimarySpecialization);
                                }
                                
                                // Insert pending resume
                                var pendingId = await _repository.InsertPendingResumeAsync(
                                    newId,
                                    candidate.ResumeDriveUrl.Trim(),
                                    candidate.CandidateName?.Trim(),
                                    candidate.CandidateEmail?.Trim(),
                                    irfMatch?.Id,
                                    irfMatch?.IRFStringId,
                                    irfMatch?.JDTiltle,
                                    irfMatch?.RisebirdHrEmail,
                                    hasScheduling,
                                    interviewDateTime,
                                    timezone);
                                
                                if (pendingId > 0)
                                {
                                    pendingCount++;
                                    if (fallbackHR == null && !string.IsNullOrWhiteSpace(irfMatch?.RisebirdHrEmail))
                                        fallbackHR = irfMatch.RisebirdHrEmail;
                                }
                            }
                            catch (Exception ex)
                            {
                                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                            }
                        }
                    }
                    
                    // Process resume_associations (per-resume IRF mapping)
                    if (hasResumeAssociations)
                    {
                        foreach (var assoc in payload.ResumeAssociations)
                        {
                            if (string.IsNullOrWhiteSpace(assoc.ResumeUrl))
                                continue;
                            
                            try
                            {
                                IrfMatchVM irfMatch = null;
                                
                                // Try explicit skill_channel_id
                                if (!string.IsNullOrWhiteSpace(assoc.SkillChannelId) &&
                                    assoc.SkillChannelId.Trim().Length == 10)
                                {
                                    irfMatch = await _repository.GetIRFByStringIdAsync(assoc.SkillChannelId.Trim());
                                }
                                
                                // Try job_title from association
                                if (irfMatch == null && !string.IsNullOrWhiteSpace(assoc.JobTitle))
                                {
                                    irfMatch = await MatchIRFAsync(
                                        ticket.SenderEmail,
                                        assoc.JobTitle,
                                        null,
                                        ticket.PrimarySpecialization);
                                }
                                
                                // Fallback to global job title
                                if (irfMatch == null && !string.IsNullOrWhiteSpace(ticket.PrimarySpecialization))
                                {
                                    irfMatch = await MatchIRFAsync(
                                        ticket.SenderEmail,
                                        ticket.JobTitle ?? ticket.Subject,
                                        payload.SkillChannelId,
                                        ticket.PrimarySpecialization);
                                }
                                
                                var pendingId = await _repository.InsertPendingResumeAsync(
                                    newId,
                                    assoc.ResumeUrl.Trim(),
                                    assoc.CandidateName?.Trim(),
                                    null, // no email in resume_associations
                                    irfMatch?.Id,
                                    irfMatch?.IRFStringId,
                                    irfMatch?.JDTiltle,
                                    irfMatch?.RisebirdHrEmail,
                                    false, // no scheduling data
                                    null,
                                    null);
                                
                                if (pendingId > 0)
                                {
                                    pendingCount++;
                                    if (fallbackHR == null && !string.IsNullOrWhiteSpace(irfMatch?.RisebirdHrEmail))
                                        fallbackHR = irfMatch.RisebirdHrEmail;
                                }
                            }
                            catch (Exception ex)
                            {
                                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                            }
                        }
                    }
                    
                    // Process legacy resume_drive_urls (single IRF for all)
                    if (hasLegacyUrls && !hasResumeAssociations && !hasCandidateScheduleData)
                    {
                        IrfMatchVM irfMatch = null;
                        
                        if (!string.IsNullOrWhiteSpace(ticket.PrimarySpecialization))
                        {
                            irfMatch = await MatchIRFAsync(
                                ticket.SenderEmail,
                                ticket.JobTitle ?? ticket.Subject,
                                payload.SkillChannelId,
                                ticket.PrimarySpecialization);
                        }
                        
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
                            try
                            {
                                var pendingId = await _repository.InsertPendingResumeAsync(
                                    newId,
                                    url.Trim(),
                                    null, // no candidate name
                                    null, // no email
                                    irfMatch?.Id,
                                    irfMatch?.IRFStringId,
                                    irfMatch?.JDTiltle,
                                    irfMatch?.RisebirdHrEmail,
                                    false,
                                    null,
                                    null);
                                
                                if (pendingId > 0)
                                {
                                    pendingCount++;
                                    if (fallbackHR == null && !string.IsNullOrWhiteSpace(irfMatch?.RisebirdHrEmail))
                                        fallbackHR = irfMatch.RisebirdHrEmail;
                                }
                            }
                            catch (Exception ex)
                            {
                                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                            }
                        }
                    }
                    
                    // Assign to HR for review
                    var hrEmail = fallbackHR ?? await _repository.GetHRForSenderAsync(ticket.SenderEmail);
                    if (!string.IsNullOrWhiteSpace(hrEmail))
                    {
                        await _repository.UpdateTicketAssignmentAsync(newId, hrEmail, "System");
                    }
                    
                    pendingResumeResult.Append(
                        $"{pendingCount} resume(s) queued for HR review. Assigned to {hrEmail ?? "unassigned"}.");
                }
                else if (isNewChannelRequest)
                {
                    // New channel request - assign to HR
                    var hrEmail = await _repository.GetHRForSenderAsync(ticket.SenderEmail);
                    if (!string.IsNullOrWhiteSpace(hrEmail))
                    {
                        await _repository.UpdateTicketAssignmentAsync(newId, hrEmail, "System");
                        pendingResumeResult.Append($"New channel request — assigned to {hrEmail} for channel creation.");
                    }
                    else
                    {
                        pendingResumeResult.Append("New channel request — no HR resolved; ticket left unassigned.");
                    }
                }
                else if (isNewCandidateSubmission || isNewInterviewRequest)
                {
                    // Intent but no resumes
                    var hrEmail = await _repository.GetHRForSenderAsync(ticket.SenderEmail);
                    if (!string.IsNullOrWhiteSpace(hrEmail))
                    {
                        await _repository.UpdateTicketAssignmentAsync(newId, hrEmail, "System");
                        pendingResumeResult.Append($"No resumes detected — assigned to {hrEmail} for manual review.");
                    }
                    else
                    {
                        pendingResumeResult.Append("No resumes detected — ticket left unassigned.");
                    }
                }
                else
                {
                    pendingResumeResult.Append("N/A");
                }

                return Ok(new
                {
                    success         = true,
                    ticketId        = newId,
                    pendingResumes  = pendingCount,
                    message         = pendingResumeResult.ToString()
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(500, new { error = "An internal error occurred." });
            }
        }

        // ── Pending Resumes Review API ─────────────────────────────────────

        /// <summary>
        /// Gets pending resumes for HR review modal.
        /// Returns all pending resumes with resolved IRF info.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPendingResumes(int ticketId)
        {
            try
            {
                var resumes = await _repository.GetPendingResumesForTicketAsync(ticketId);
                return Json(new { success = true, resumes });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Ingests a pending resume into CandidateResume table.
        /// Optionally schedules the interview if scheduling data is present.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> IngestPendingResume([FromBody] IngestPendingResumeRequest request)
        {
            try
            {
                if (request == null || request.PendingResumeId <= 0)
                    return Json(new { success = false, error = "Invalid request" });

                var pending = await _repository.GetPendingResumeByIdAsync(request.PendingResumeId);
                if (pending == null)
                    return Json(new { success = false, error = "Pending resume not found" });

                if (pending.Status != "Pending")
                    return Json(new { success = false, error = $"Resume already processed (status: {pending.Status})" });

                if (!pending.ResolvedIRFId.HasValue)
                    return Json(new { success = false, error = "No IRF resolved for this resume. Cannot ingest." });

                // Get HR ID from email
                var hrId = 0;
                if (!string.IsNullOrWhiteSpace(pending.ResolvedHREmail))
                {
                    // Attempt to get HR user ID (simplified — in production, query UserList)
                    // For now, we'll use the resolved IRF's HR or fallback to system user
                }

                var processedBy = request.ProcessedByEmail ?? HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                int ingestedResumeId = 0;

                // Insert into CandidateResume
                if (pending.HasSchedulingData && pending.ScheduledInterviewDateTime.HasValue)
                {
                    // Use auto-schedule insert with metadata
                    ingestedResumeId = await _repository.InsertCandidateResumeForAutoScheduleAsync(
                        pending.ResolvedIRFId.Value,
                        pending.ResumeUrl,
                        hrId > 0 ? hrId : 0,
                        pending.ResolvedHREmail,
                        pending.SupportTicketId,
                        pending.CandidateName,
                        pending.CandidateEmail,
                        pending.ScheduledInterviewDateTime);
                }
                else
                {
                    // Standard insert
                    ingestedResumeId = await _repository.InsertCandidateResumeAsync(
                        pending.ResolvedIRFId.Value,
                        pending.ResumeUrl,
                        hrId > 0 ? hrId : 0,
                        pending.ResolvedHREmail,
                        pending.SupportTicketId);
                }

                if (ingestedResumeId <= 0)
                {
                    await _repository.UpdatePendingResumeStatusAsync(pending.Id, "Failed", null, processedBy);
                    return Json(new { success = false, error = "Failed to insert into CandidateResume" });
                }

                // Update pending resume status
                await _repository.UpdatePendingResumeStatusAsync(pending.Id, "Ingested", ingestedResumeId, processedBy);

                // Check if scheduling is requested
                string scheduleMessage = null;
                if (request.AlsoSchedule && pending.HasSchedulingData && pending.ScheduledInterviewDateTime.HasValue)
                {
                    // Wait for resume parsing before scheduling — schedule will be triggered separately
                    scheduleMessage = "Resume ingested. Scheduling will be available after resume parsing completes.";
                }

                // Refresh ticket's pending resume flag
                await _repository.RefreshTicketPendingResumeFlagAsync(pending.SupportTicketId);

                return Json(new
                {
                    success = true,
                    message = $"Resume ingested successfully into IRF {pending.ResolvedIRFStringId}",
                    candidateResumeId = ingestedResumeId,
                    scheduleMessage
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Schedules an interview for an already-ingested candidate resume.
        /// Called after resume parsing is complete and HR verifies the data.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ScheduleIngestedCandidate([FromBody] ScheduleCandidateRequest request)
        {
            try
            {
                if (request == null || request.CandidateResumeId <= 0)
                    return Json(new { success = false, error = "Invalid request" });

                // Get candidate data from auto-schedule view
                var candidates = await _repository.GetAutoScheduleCandidatesForTicketAsync(request.TicketId);
                var candidate = candidates.FirstOrDefault(c => c.CandidateResumeId == request.CandidateResumeId);

                if (candidate == null)
                    return Json(new { success = false, error = "Candidate not found in CandidateResume" });

                if (candidate.ParsingStatus != "Parsed")
                    return Json(new { success = false, error = "Resume parsing not complete. Please wait and try again." });

                if (candidate.IsAlreadyScheduled)
                    return Json(new { success = false, error = "Interview already scheduled for this candidate." });

                if (!candidate.ScheduledInterviewDateTime.HasValue)
                    return Json(new { success = false, error = "No scheduled interview time available." });

                var candidateEmail = !string.IsNullOrWhiteSpace(candidate.ParsedCandidateEmail)
                    ? candidate.ParsedCandidateEmail
                    : candidate.ExtractedCandidateEmail;

                if (string.IsNullOrWhiteSpace(candidateEmail))
                    return Json(new { success = false, error = "No candidate email available" });

                var candidateName = !string.IsNullOrWhiteSpace(candidate.ParsedCandidateName)
                    ? candidate.ParsedCandidateName
                    : candidate.ExtractedCandidateName;

                if (string.IsNullOrWhiteSpace(candidate.TenantId))
                    return Json(new { success = false, error = "Client tenant ID not found" });

                // Call the Schedule API
                const string scheduleApiUrl = "https://api.portal.risebird.io/api/Candidates/Schedule";
                const string scheduleApiKey = "SJJvpXEzy-1fOQN2mygBaTT6gXctL406VhpMwn0BqhKUAzFuBDDP5Q==";

                var scheduleRequest = new
                {
                    skillChannelId = candidate.SkillChannelId,
                    requestedBy = "SupportTicketManualSchedule",
                    requestType = "schedule_supportTicket",
                    CandidateList = new[]
                    {
                        new
                        {
                            EmailId = candidateEmail,
                            Name = candidateName,
                            Interviewdate = candidate.ScheduledInterviewDateTime,
                            Phone = candidate.ParsedCandidatePhone,
                            ResumeLink = candidate.ResumeLink
                        }
                    }
                };

                var restClient = new RestClient(scheduleApiUrl);
                var restRequest = new RestRequest(scheduleApiUrl, Method.Post);
                restRequest.AddHeader("content-type", "application/json");
                restRequest.AddHeader("x-functions-key", scheduleApiKey);
                restRequest.AddHeader("tenant-id", candidate.TenantId);
                restRequest.AddParameter("application/json", JsonConvert.SerializeObject(scheduleRequest), ParameterType.RequestBody);

                var response = await restClient.ExecuteAsync(restRequest);

                if (response.IsSuccessful)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Interview scheduled for {candidateName} ({candidateEmail}) at {candidate.ScheduledInterviewDateTime:g}",
                        apiResponse = response.Content
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        error = $"Schedule API returned {response.StatusCode}",
                        details = response.Content
                    });
                }
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Rejects a pending resume (marks as Rejected, won't be ingested).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RejectPendingResume(int pendingResumeId)
        {
            try
            {
                var processedBy = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                var pending = await _repository.GetPendingResumeByIdAsync(pendingResumeId);
                
                if (pending == null)
                    return Json(new { success = false, error = "Pending resume not found" });

                if (pending.Status != "Pending")
                    return Json(new { success = false, error = $"Resume already processed (status: {pending.Status})" });

                await _repository.UpdatePendingResumeStatusAsync(pendingResumeId, "Rejected", null, processedBy);
                await _repository.RefreshTicketPendingResumeFlagAsync(pending.SupportTicketId);

                return Json(new { success = true, message = "Resume rejected" });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
