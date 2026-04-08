using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Risebird.Business.Interface;
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
        private readonly IResumeParsingService    _resumeParser;
        private readonly IIRFService              _irfService;
        private readonly IClonableSkillChannelTemplateService _templateService;
        private readonly IClientService           _clientService;
        private readonly IAzureStorageHelper      _azureStorageHelper;
        private readonly IFileMimeTypeHelper      _fileMimeTypeHelper;

        public SupportTicketController(
            ISupportTicketRepository repository,
            IApplicationErrorLogger  logger,
            IConfiguration           configuration,
            IResumeParsingService    resumeParser = null,
            IIRFService              irfService = null,
            IClonableSkillChannelTemplateService templateService = null,
            IClientService           clientService = null,
            IAzureStorageHelper      azureStorageHelper = null,
            IFileMimeTypeHelper      fileMimeTypeHelper = null)
        {
            _repository         = repository;
            _logger             = logger;
            _configuration      = configuration;
            _resumeParser       = resumeParser;
            _irfService         = irfService;
            _templateService    = templateService;
            _clientService      = clientService;
            _azureStorageHelper = azureStorageHelper;
            _fileMimeTypeHelper = fileMimeTypeHelper;
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

                // Fetch counts for tab badges (optimized single query)
                var counts = await _repository.GetTicketCountsAsync();
                int systemCount = counts.SystemOnlyCount;
                int hybridCount = counts.HybridCount;

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
                
                // Check available resume data sources
                var hasResumeAssociations = payload.ResumeAssociations != null && payload.ResumeAssociations.Count > 0;
                var hasLegacyUrls = payload.ResumeDriveUrls != null && payload.ResumeDriveUrls.Count > 0;
                var hasCandidateScheduleData = payload.CandidateScheduleData != null && payload.CandidateScheduleData.Count > 0;
                var hasResumesToProcess = hasResumeAssociations || hasLegacyUrls || hasCandidateScheduleData;
                
                // Also check if AttachmentResumes flag indicates resumes exist in the drive folder
                var hasResumeAttachments = ticket.AttachmentResumes;
                
                // For "New interview request" with JD attached BUT NO resumes:
                // This is a request to CREATE a new skill channel — no resume ingestion needed.
                // If there ARE resumes (in URLs or indicated by AttachmentResumes), process them.
                var isNewChannelRequestOnly = isNewInterviewRequest && 
                    ticket.AttachmentJobDescription && 
                    !hasResumesToProcess && 
                    !hasResumeAttachments;
                
                // Process resumes for:
                // 1. "New candidate submission" intent
                // 2. "New interview request" that has resumes (even if JD is also attached)
                var isResumeIngestionIntent = isNewCandidateSubmission || 
                    (isNewInterviewRequest && !isNewChannelRequestOnly);
                
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
                                
                                // Handle case where resume_filename might contain multiple names concatenated
                                var cleanFilename = candidate.ResumeFileName?.Trim();
                                if (!string.IsNullOrWhiteSpace(cleanFilename) && cleanFilename.Contains('\n'))
                                {
                                    cleanFilename = cleanFilename.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                        .FirstOrDefault()?.Trim();
                                }
                                
                                // Insert pending resume
                                var pendingId = await _repository.InsertPendingResumeAsync(
                                    newId,
                                    candidate.ResumeDriveUrl.Trim(),
                                    cleanFilename,
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
                                
                                // Handle case where resume_filename might contain multiple names concatenated
                                // Just take the first one if there are newlines
                                var cleanFilename = assoc.ResumeFileName?.Trim();
                                if (!string.IsNullOrWhiteSpace(cleanFilename) && cleanFilename.Contains('\n'))
                                {
                                    cleanFilename = cleanFilename.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                        .FirstOrDefault()?.Trim();
                                }
                                
                                var pendingId = await _repository.InsertPendingResumeAsync(
                                    newId,
                                    assoc.ResumeUrl.Trim(),
                                    cleanFilename,
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
                    // Uses parallel resume_filenames array if available
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
                        
                        // Build list of (url, filename) pairs
                        // resume_filenames is a parallel array - index 0 filename goes with index 0 URL
                        // Also handle case where filenames are concatenated (space/newline separated)
                        var urlFilePairs = new List<(string Url, string Filename)>();
                        var filenames = payload.ResumeFilenames ?? new List<string>();
                        
                        // First, flatten all URLs - some entries might have multiple URLs concatenated
                        var allUrls = new List<string>();
                        foreach (var rawUrl in payload.ResumeDriveUrls)
                        {
                            if (string.IsNullOrWhiteSpace(rawUrl))
                                continue;
                            
                            var urls = rawUrl.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                .Where(u => u.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                .Select(u => u.Trim())
                                .ToList();
                            allUrls.AddRange(urls);
                        }
                        
                        // Similarly, flatten all filenames - some entries might have multiple names concatenated
                        var allFilenames = new List<string>();
                        foreach (var rawFilename in filenames)
                        {
                            if (string.IsNullOrWhiteSpace(rawFilename))
                            {
                                allFilenames.Add(null);
                                continue;
                            }
                            
                            // Split by newlines or common separators, but preserve filenames with spaces
                            // Try newline first, then fall back to common patterns
                            var splitNames = rawFilename.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(n => n.Trim())
                                .Where(n => !string.IsNullOrWhiteSpace(n))
                                .ToList();
                            
                            if (splitNames.Count > 0)
                            {
                                allFilenames.AddRange(splitNames);
                            }
                            else
                            {
                                allFilenames.Add(rawFilename.Trim());
                            }
                        }
                        
                        // Pair URLs with filenames by index
                        for (int i = 0; i < allUrls.Count; i++)
                        {
                            var url = allUrls[i];
                            var filename = i < allFilenames.Count ? allFilenames[i] : null;
                            urlFilePairs.Add((url, filename));
                        }
                        
                        // Remove duplicates (keep first occurrence with filename if any)
                        urlFilePairs = urlFilePairs
                            .GroupBy(p => p.Url)
                            .Select(g => {
                                var withFilename = g.FirstOrDefault(p => !string.IsNullOrEmpty(p.Filename));
                                return !string.IsNullOrEmpty(withFilename.Filename) ? withFilename : g.First();
                            })
                            .ToList();
                        
                        foreach (var (url, filename) in urlFilePairs)
                        {
                            try
                            {
                                var pendingId = await _repository.InsertPendingResumeAsync(
                                    newId,
                                    url,
                                    filename, // original filename from parallel array
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
                else if (isResumeIngestionIntent && hasResumeAttachments && pendingCount == 0)
                {
                    // Resumes exist in drive folder but we couldn't extract individual URLs
                    // Create a placeholder pending resume pointing to the drive folder
                    IrfMatchVM irfMatch = null;
                    if (!string.IsNullOrWhiteSpace(ticket.PrimarySpecialization))
                    {
                        irfMatch = await MatchIRFAsync(
                            ticket.SenderEmail,
                            ticket.JobTitle ?? ticket.Subject,
                            payload.SkillChannelId,
                            ticket.PrimarySpecialization);
                    }
                    
                    // Use drive folder link as the "resume URL" for placeholder
                    var driveLink = ticket.DriveFolderLink;
                    if (!string.IsNullOrWhiteSpace(driveLink))
                    {
                        try
                        {
                            await _repository.InsertPendingResumeAsync(
                                newId,
                                driveLink,
                                "Drive Folder (multiple resumes)", // placeholder filename
                                null, // no candidate name
                                null, // no email
                                irfMatch?.Id,
                                irfMatch?.IRFStringId,
                                irfMatch?.JDTiltle,
                                irfMatch?.RisebirdHrEmail,
                                false,
                                null,
                                null);
                            pendingCount = 1;
                        }
                        catch (Exception ex)
                        {
                            await _logger.LogMessage(LogLevelInfo.Exception, ex);
                        }
                    }
                    
                    var hrEmail = irfMatch?.RisebirdHrEmail ?? await _repository.GetHRForSenderAsync(ticket.SenderEmail);
                    if (!string.IsNullOrWhiteSpace(hrEmail))
                    {
                        await _repository.UpdateTicketAssignmentAsync(newId, hrEmail, "System");
                    }
                    
                    pendingResumeResult.Append(
                        $"Resumes detected in drive folder (URLs not extracted). Queued for HR review. Assigned to {hrEmail ?? "unassigned"}.");
                }
                else if (isNewChannelRequestOnly)
                {
                    // New channel request (JD only, no resumes) - assign to HR
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
        /// Returns all pending resumes with resolved IRF info plus ticket metadata.
        /// Use ticketId = -1 for test data.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPendingResumes(int ticketId)
        {
            try
            {
                // TEST MODE: Return hardcoded data for ticketId = -1
                if (ticketId == -1)
                {
                    var testResumes = GetTestPendingResumes();
                    return Json(new { 
                        success = true, 
                        resumes = testResumes,
                        ticket = new {
                            id = -1,
                            driveFolderLink = "https://drive.google.com/drive/folders/test123",
                            attachmentResumes = true,
                            primarySpecialization = "Test Specialization",
                            senderEmail = "test@example.com"
                        }
                    });
                }

                var resumes = await _repository.GetPendingResumesForTicketAsync(ticketId);
                var ticket = await _repository.GetTicketByIdAsync(ticketId);
                
                return Json(new {
                    success = true,
                    resumes,
                    ticket = ticket != null ? new {
                        id = ticket.Id,
                        senderEmail = ticket.SenderEmail,
                        subject = ticket.Subject,
                        intent = ticket.Intent,
                        primarySpecialization = ticket.PrimarySpecialization,
                        receivedAt = ticket.ReceivedAt,
                        summaryMail = ticket.SummaryMail,
                        driveFolderLink = ticket.DriveFolderLink,
                        jobTitle = ticket.JobTitle,
                        attachmentJobDescription = ticket.AttachmentJobDescription,
                        attachmentPdf = ticket.AttachmentPdf,
                        attachmentResumes = ticket.AttachmentResumes
                    } : null
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Processes the drive folder for a ticket and creates pending resume records.
        /// Called when user clicks "Process Drive Folder" in the modal.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessDriveFolderForResumes(int ticketId)
        {
            try
            {
                var ticket = await _repository.GetTicketByIdAsync(ticketId);
                if (ticket == null)
                    return Json(new { success = false, error = "Ticket not found" });

                if (string.IsNullOrWhiteSpace(ticket.DriveFolderLink))
                    return Json(new { success = false, error = "No drive folder link available for this ticket" });

                // Try to match an IRF for the resumes
                IrfMatchVM irfMatch = null;
                if (!string.IsNullOrWhiteSpace(ticket.PrimarySpecialization))
                {
                    irfMatch = await MatchIRFAsync(
                        ticket.SenderEmail,
                        ticket.JobTitle ?? ticket.Subject,
                        null,
                        ticket.PrimarySpecialization);
                }

                // Create a placeholder pending resume pointing to the drive folder
                var pendingId = await _repository.InsertPendingResumeAsync(
                    ticketId,
                    ticket.DriveFolderLink,
                    "Drive Folder (manual processing)", 
                    null, 
                    null, 
                    irfMatch?.Id,
                    irfMatch?.IRFStringId,
                    irfMatch?.JDTiltle,
                    irfMatch?.RisebirdHrEmail,
                    false,
                    null,
                    null);

                if (pendingId > 0)
                {
                    // Update ticket to indicate it has pending resumes
                    await _repository.RefreshTicketPendingResumeFlagAsync(ticketId);
                    
                    // Fetch the newly created pending resume
                    var resumes = await _repository.GetPendingResumesForTicketAsync(ticketId);
                    
                    return Json(new { 
                        success = true, 
                        message = "Drive folder queued for resume processing.",
                        resumes,
                        pendingCount = resumes.Count
                    });
                }
                else
                {
                    return Json(new { success = false, error = "Failed to create pending resume record" });
                }
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Returns hardcoded test data for UI testing.
        /// </summary>
        private List<PendingResumeVM> GetTestPendingResumes()
        {
            var now = DateTime.UtcNow;
            return new List<PendingResumeVM>
            {
                new PendingResumeVM
                {
                    Id = 1001,
                    SupportTicketId = -1,
                    ResumeUrl = "https://drive.google.com/file/d/abc123/view",
                    CandidateName = "Rahul Sharma",
                    CandidateEmail = "rahul.sharma@gmail.com",
                    ResolvedIRFId = 24500,
                    ResolvedIRFStringId = "AzureDevEn",
                    ResolvedJobTitle = "Azure DevOps Engineer",
                    ResolvedHREmail = "hr.lead@risebird.io",
                    ResolvedHRName = "Priya Mehta",
                    HasSchedulingData = true,
                    ScheduledInterviewDateTime = now.AddDays(2).AddHours(10),
                    ScheduleTimezone = "IST",
                    Status = "Pending",
                    CreatedAt = now.AddHours(-2),
                    IRFJobTitle = "Azure DevOps Engineer",
                    IRFSkillCluster = "Cloud & DevOps",
                    IRFStartDate = now.AddMonths(-1),
                    IRFEndDate = now.AddMonths(2),
                    IRFPositions = 3,
                    IRFJDLink = "https://risebirdportal.blob.core.windows.net/resumes/Azure_DevOps.docx",
                    ClientName = "TechCorp Solutions",
                    TenantId = "tenant-123",
                    TicketSenderEmail = "recruiter@techcorp.com",
                    TicketSubject = "Resume Submission - Azure DevOps Engineers for Q2 Hiring",
                    TicketMailContent = "Hi Team,\n\nPlease find attached resumes for the Azure DevOps Engineer position.\n\nCandidate 1: Rahul Sharma - 5 years experience in Azure, CI/CD, Kubernetes\nAvailable: April 2nd, 10:00 AM IST\n\nCandidate 2: Ananya Patel - 4 years experience, strong in Terraform\nNo scheduling preference\n\nCandidate 3: Vikram Singh - 6 years, AWS + Azure certified\n\nPlease schedule interviews at the earliest.\n\nThanks,\nRecruiter Team",
                    TicketIntent = "New candidate submission",
                    TicketPrimarySpecialization = "Azure DevOps",
                    TicketMailBodyIntent = "Resume submission with scheduling request",
                    TicketJobTitle = "Azure DevOps Engineer",
                    TicketReceivedAt = now.AddHours(-2),
                    TicketDriveFolderLink = "https://drive.google.com/drive/folders/xyz789",
                    TicketSummaryMail = "Recruiter from TechCorp submitting 3 candidates for Azure DevOps role. One candidate has specific availability for April 2nd morning.",
                    TicketSummaryCandidates = "3 candidates submitted: Rahul Sharma (5 yrs), Ananya Patel (4 yrs), Vikram Singh (6 yrs)",
                    TicketHasJD = false,
                    TicketHasPdf = true,
                    TicketHasResumes = true
                },
                new PendingResumeVM
                {
                    Id = 1002,
                    SupportTicketId = -1,
                    ResumeUrl = "https://drive.google.com/file/d/def456/view",
                    CandidateName = "Ananya Patel",
                    CandidateEmail = "ananya.patel@outlook.com",
                    ResolvedIRFId = 24500,
                    ResolvedIRFStringId = "AzureDevEn",
                    ResolvedJobTitle = "Azure DevOps Engineer",
                    ResolvedHREmail = "hr.lead@risebird.io",
                    ResolvedHRName = "Priya Mehta",
                    HasSchedulingData = false,
                    Status = "Pending",
                    CreatedAt = now.AddHours(-2),
                    IRFJobTitle = "Azure DevOps Engineer",
                    IRFSkillCluster = "Cloud & DevOps",
                    IRFStartDate = now.AddMonths(-1),
                    IRFEndDate = now.AddMonths(2),
                    IRFPositions = 3,
                    IRFJDLink = "https://risebirdportal.blob.core.windows.net/resumes/Azure_DevOps.docx",
                    ClientName = "TechCorp Solutions",
                    TenantId = "tenant-123",
                    TicketSenderEmail = "recruiter@techcorp.com",
                    TicketSubject = "Resume Submission - Azure DevOps Engineers for Q2 Hiring",
                    TicketMailContent = "Hi Team,\n\nPlease find attached resumes for the Azure DevOps Engineer position.\n\nCandidate 1: Rahul Sharma - 5 years experience in Azure, CI/CD, Kubernetes\nAvailable: April 2nd, 10:00 AM IST\n\nCandidate 2: Ananya Patel - 4 years experience, strong in Terraform\nNo scheduling preference\n\nCandidate 3: Vikram Singh - 6 years, AWS + Azure certified\n\nPlease schedule interviews at the earliest.\n\nThanks,\nRecruiter Team",
                    TicketIntent = "New candidate submission",
                    TicketPrimarySpecialization = "Azure DevOps",
                    TicketMailBodyIntent = "Resume submission with scheduling request",
                    TicketJobTitle = "Azure DevOps Engineer",
                    TicketReceivedAt = now.AddHours(-2),
                    TicketDriveFolderLink = "https://drive.google.com/drive/folders/xyz789",
                    TicketSummaryMail = "Recruiter from TechCorp submitting 3 candidates for Azure DevOps role. One candidate has specific availability for April 2nd morning.",
                    TicketSummaryCandidates = "3 candidates submitted: Rahul Sharma (5 yrs), Ananya Patel (4 yrs), Vikram Singh (6 yrs)",
                    TicketHasJD = false,
                    TicketHasPdf = true,
                    TicketHasResumes = true
                },
                new PendingResumeVM
                {
                    Id = 1003,
                    SupportTicketId = -1,
                    ResumeUrl = "https://drive.google.com/file/d/ghi789/view",
                    CandidateName = "Vikram Singh",
                    CandidateEmail = null,
                    ResolvedIRFId = null,
                    ResolvedIRFStringId = null,
                    ResolvedJobTitle = null,
                    ResolvedHREmail = null,
                    HasSchedulingData = false,
                    Status = "Pending",
                    CreatedAt = now.AddHours(-2),
                    ClientName = null,
                    TicketSenderEmail = "recruiter@techcorp.com",
                    TicketSubject = "Resume Submission - Azure DevOps Engineers for Q2 Hiring",
                    TicketMailContent = "Hi Team,\n\nPlease find attached resumes for the Azure DevOps Engineer position.\n\nCandidate 1: Rahul Sharma - 5 years experience in Azure, CI/CD, Kubernetes\nAvailable: April 2nd, 10:00 AM IST\n\nCandidate 2: Ananya Patel - 4 years experience, strong in Terraform\nNo scheduling preference\n\nCandidate 3: Vikram Singh - 6 years, AWS + Azure certified\n\nPlease schedule interviews at the earliest.\n\nThanks,\nRecruiter Team",
                    TicketIntent = "New candidate submission",
                    TicketPrimarySpecialization = "Azure DevOps",
                    TicketMailBodyIntent = "Resume submission with scheduling request",
                    TicketJobTitle = "Azure DevOps Engineer",
                    TicketReceivedAt = now.AddHours(-2),
                    TicketDriveFolderLink = "https://drive.google.com/drive/folders/xyz789",
                    TicketSummaryMail = "Recruiter from TechCorp submitting 3 candidates for Azure DevOps role. One candidate has specific availability for April 2nd morning.",
                    TicketSummaryCandidates = "3 candidates submitted: Rahul Sharma (5 yrs), Ananya Patel (4 yrs), Vikram Singh (6 yrs)",
                    TicketHasJD = false,
                    TicketHasPdf = true,
                    TicketHasResumes = true
                },
                new PendingResumeVM
                {
                    Id = 1004,
                    SupportTicketId = -1,
                    ResumeUrl = "https://drive.google.com/file/d/jkl012/view",
                    CandidateName = "Sneha Reddy",
                    CandidateEmail = "sneha.reddy@yahoo.com",
                    ResolvedIRFId = 24501,
                    ResolvedIRFStringId = "JavaSpring",
                    ResolvedJobTitle = "Java Spring Boot Developer",
                    ResolvedHREmail = "hr.lead@risebird.io",
                    ResolvedHRName = "Priya Mehta",
                    // IRF was changed by HR (example of tracking)
                    OriginalResolvedIRFId = 24500,
                    OriginalResolvedIRFStringId = "AzureDevEn",
                    OriginalIRFJobTitle = "Azure DevOps Engineer",
                    IRFChangedByHR = true,
                    IRFChangedAt = now.AddMinutes(-35),
                    IRFChangeReason = "Candidate has Java background, not DevOps",
                    HasSchedulingData = true,
                    ScheduledInterviewDateTime = now.AddDays(3).AddHours(14).AddMinutes(30),
                    ScheduleTimezone = "IST",
                    Status = "Ingested",
                    IngestedResumeId = 55001,
                    ProcessedAt = now.AddMinutes(-30),
                    ProcessedBy = "reviewer@risebird.io",
                    CreatedAt = now.AddHours(-3),
                    IRFJobTitle = "Java Spring Boot Developer",
                    IRFSkillCluster = "Backend Development",
                    IRFStartDate = now.AddMonths(-2),
                    IRFEndDate = now.AddMonths(1),
                    IRFPositions = 2,
                    ClientName = "TechCorp Solutions",
                    ClientId = 100,
                    TenantId = "tenant-456",
                    TicketSenderEmail = "recruiter@techcorp.com",
                    TicketSubject = "Resume Submission - Azure DevOps Engineers for Q2 Hiring",
                    TicketMailContent = "Hi Team,\n\nPlease find attached resumes for the Azure DevOps Engineer position.\n\nCandidate 1: Rahul Sharma - 5 years experience in Azure, CI/CD, Kubernetes\nAvailable: April 2nd, 10:00 AM IST\n\nCandidate 2: Ananya Patel - 4 years experience, strong in Terraform\nNo scheduling preference\n\nCandidate 3: Vikram Singh - 6 years, AWS + Azure certified\n\nPlease schedule interviews at the earliest.\n\nThanks,\nRecruiter Team",
                    TicketIntent = "New candidate submission",
                    TicketPrimarySpecialization = "Azure DevOps",
                    TicketMailBodyIntent = "Resume submission with scheduling request",
                    TicketJobTitle = "Azure DevOps Engineer",
                    TicketReceivedAt = now.AddHours(-2),
                    TicketDriveFolderLink = "https://drive.google.com/drive/folders/xyz789",
                    TicketSummaryMail = "Recruiter from TechCorp submitting 3 candidates for Azure DevOps role.",
                    TicketHasJD = false,
                    TicketHasPdf = true,
                    TicketHasResumes = true
                },
                new PendingResumeVM
                {
                    Id = 1005,
                    SupportTicketId = -1,
                    ResumeUrl = "https://drive.google.com/file/d/mno345/view",
                    CandidateName = "Arjun Kapoor",
                    CandidateEmail = "arjun.k@gmail.com",
                    ResolvedIRFId = 24502,
                    ResolvedIRFStringId = "ReactFront",
                    ResolvedJobTitle = "React Frontend Developer",
                    ResolvedHREmail = "support@risebird.io",
                    ResolvedHRName = "Amit Kumar",
                    HasSchedulingData = false,
                    Status = "Rejected",
                    ProcessedAt = now.AddMinutes(-15),
                    ProcessedBy = "reviewer@risebird.io",
                    CreatedAt = now.AddHours(-4),
                    IRFJobTitle = "React Frontend Developer",
                    IRFSkillCluster = "Frontend Development",
                    IRFStartDate = now.AddMonths(-3),
                    IRFEndDate = now.AddDays(15),
                    IRFPositions = 1,
                    ClientName = "StartupXYZ",
                    TenantId = "tenant-789",
                    TicketSenderEmail = "recruiter@techcorp.com",
                    TicketSubject = "Resume Submission - Azure DevOps Engineers for Q2 Hiring",
                    TicketMailContent = "Hi Team,\n\nPlease find attached resumes for the Azure DevOps Engineer position.\n\nCandidate 1: Rahul Sharma - 5 years experience in Azure, CI/CD, Kubernetes\nAvailable: April 2nd, 10:00 AM IST\n\nCandidate 2: Ananya Patel - 4 years experience, strong in Terraform\nNo scheduling preference\n\nCandidate 3: Vikram Singh - 6 years, AWS + Azure certified\n\nPlease schedule interviews at the earliest.\n\nThanks,\nRecruiter Team",
                    TicketIntent = "New candidate submission",
                    TicketPrimarySpecialization = "Azure DevOps",
                    TicketMailBodyIntent = "Resume submission with scheduling request",
                    TicketJobTitle = "Azure DevOps Engineer",
                    TicketReceivedAt = now.AddHours(-2),
                    TicketDriveFolderLink = "https://drive.google.com/drive/folders/xyz789",
                    TicketSummaryMail = "Recruiter from TechCorp submitting 3 candidates for Azure DevOps role.",
                    TicketHasJD = false,
                    TicketHasPdf = true,
                    TicketHasResumes = true
                }
            };
        }

        /// <summary>
        /// Gets all active IRFs for a client (for the IRF dropdown).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetIRFsForClient(string senderEmail)
        {
            try
            {
                // TEST MODE
                if (string.IsNullOrWhiteSpace(senderEmail) || senderEmail == "recruiter@techcorp.com")
                {
                    var testIRFs = GetTestIRFOptions();
                    return Json(new { success = true, irfs = testIRFs });
                }

                var irfs = await _repository.GetIRFsForClientAsync(senderEmail);
                return Json(new { success = true, irfs });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Returns test IRF options for UI testing.
        /// </summary>
        private List<IRFOptionVM> GetTestIRFOptions()
        {
            var now = DateTime.UtcNow;
            return new List<IRFOptionVM>
            {
                new IRFOptionVM { Id = 24500, IRFStringId = "AzureDevEn", JDTitle = "Azure DevOps Engineer", SkillCluster = "Cloud & DevOps", StartDate = now.AddMonths(-1), EndDate = now.AddMonths(2), Positions = 3, RisebirdHrEmail = "hr.lead@risebird.io", RisebirdHrName = "Priya Mehta", ClientName = "TechCorp Solutions", ClientId = 100 },
                new IRFOptionVM { Id = 24501, IRFStringId = "JavaSpring", JDTitle = "Java Spring Boot Developer", SkillCluster = "Backend Development", StartDate = now.AddMonths(-2), EndDate = now.AddMonths(1), Positions = 2, RisebirdHrEmail = "hr.lead@risebird.io", RisebirdHrName = "Priya Mehta", ClientName = "TechCorp Solutions", ClientId = 100 },
                new IRFOptionVM { Id = 24502, IRFStringId = "ReactFront", JDTitle = "React Frontend Developer", SkillCluster = "Frontend Development", StartDate = now.AddMonths(-3), EndDate = now.AddDays(15), Positions = 1, RisebirdHrEmail = "support@risebird.io", RisebirdHrName = "Amit Kumar", ClientName = "TechCorp Solutions", ClientId = 100 },
                new IRFOptionVM { Id = 24503, IRFStringId = "PythonML01", JDTitle = "Python ML Engineer", SkillCluster = "Machine Learning", StartDate = now.AddMonths(-1), EndDate = now.AddMonths(3), Positions = 4, RisebirdHrEmail = "hr.lead@risebird.io", RisebirdHrName = "Priya Mehta", ClientName = "TechCorp Solutions", ClientId = 100 },
                new IRFOptionVM { Id = 24504, IRFStringId = "NodeBacken", JDTitle = "Node.js Backend Developer", SkillCluster = "Backend Development", StartDate = now.AddMonths(-2), EndDate = now.AddMonths(2), Positions = 2, RisebirdHrEmail = "support@risebird.io", RisebirdHrName = "Amit Kumar", ClientName = "TechCorp Solutions", ClientId = 100 },
                new IRFOptionVM { Id = 24505, IRFStringId = "AWSArchite", JDTitle = "AWS Solutions Architect", SkillCluster = "Cloud & DevOps", StartDate = now.AddDays(-15), EndDate = now.AddMonths(3), Positions = 2, RisebirdHrEmail = "hr.lead@risebird.io", RisebirdHrName = "Priya Mehta", ClientName = "TechCorp Solutions", ClientId = 100 },
            };
        }

        /// <summary>
        /// Ingests a pending resume into CandidateResume table.
        /// Optionally schedules the interview if scheduling data is present.
        /// Supports IRF override by HR with change tracking.
        /// Use PendingResumeId >= 1000 for test mode (returns mock success).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> IngestPendingResume([FromBody] IngestPendingResumeRequest request)
        {
            var overallStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var stepStopwatch = new System.Diagnostics.Stopwatch();
            int? ticketId = null;
            int? pendingId = request?.PendingResumeId;
            int? candidateResumeId = null;
            var processedBy = "System";

            try
            {
                // CP01: Request Received
                stepStopwatch.Restart();
                await _repository.LogProcessingCheckpointAsync(
                    null, pendingId, null,
                    "CP01_REQUEST_RECEIVED", "Started",
                    $"Ingest request received. AlsoSchedule={request?.AlsoSchedule}, OverrideIRF={request?.OverrideIRFId}, OverrideTime={request?.OverrideInterviewDateTime}",
                    JsonConvert.SerializeObject(request),
                    processedBy);

                if (request == null || request.PendingResumeId <= 0)
                {
                    await _repository.LogProcessingCheckpointAsync(null, pendingId, null, "CP01_REQUEST_RECEIVED", "Failed", "Invalid request - null or invalid PendingResumeId");
                    return Json(new { success = false, error = "Invalid request" });
                }

                // TEST MODE: Return mock success for test IDs (>= 1000)
                if (request.PendingResumeId >= 1000)
                {
                    await Task.Delay(500);
                    var irfChanged = request.OverrideIRFId.HasValue;
                    await _repository.LogProcessingCheckpointAsync(null, pendingId, null, "CP_TEST_MODE", "Success", "Test mode - mock response returned");
                    return Json(new
                    {
                        success = true,
                        message = $"[TEST] Resume #{request.PendingResumeId} ingested successfully!" + 
                                  (irfChanged ? $" (IRF changed to #{request.OverrideIRFId})" : ""),
                        candidateResumeId = 99000 + request.PendingResumeId,
                        irfChanged = irfChanged,
                        scheduleMessage = request.AlsoSchedule ? "[TEST] Interview scheduling queued." : null
                    });
                }

                // CP02: Load Pending Resume
                stepStopwatch.Restart();
                var pending = await _repository.GetPendingResumeByIdAsync(request.PendingResumeId);
                stepStopwatch.Stop();

                if (pending == null)
                {
                    await _repository.LogProcessingCheckpointAsync(null, pendingId, null, "CP02_LOAD_PENDING", "Failed", "Pending resume not found", null, processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                    return Json(new { success = false, error = "Pending resume not found" });
                }

                ticketId = pending.SupportTicketId;
                await _repository.LogProcessingCheckpointAsync(
                    ticketId, pendingId, null,
                    "CP02_LOAD_PENDING", "Success",
                    $"Loaded pending resume. IRF={pending.ResolvedIRFStringId}, ResumeUrl={pending.ResumeUrl}",
                    JsonConvert.SerializeObject(new { pending.ResolvedIRFId, pending.ResolvedIRFStringId, pending.CandidateName, pending.CandidateEmail, pending.Status }),
                    processedBy, (int)stepStopwatch.ElapsedMilliseconds);

                if (pending.Status != "Pending")
                {
                    await _repository.LogProcessingCheckpointAsync(ticketId, pendingId, null, "CP02_LOAD_PENDING", "Failed", $"Resume already processed (status: {pending.Status})");
                    return Json(new { success = false, error = $"Resume already processed (status: {pending.Status})" });
                }

                processedBy = request.ProcessedByEmail ?? HttpContext.Session.GetString("email")?.Trim('"') ?? "System";

                // CP03: IRF Override (if applicable)
                bool irfWasChanged = false;
                if (request.OverrideIRFId.HasValue && request.OverrideIRFId.Value != pending.ResolvedIRFId)
                {
                    stepStopwatch.Restart();
                    await _repository.LogProcessingCheckpointAsync(
                        ticketId, pendingId, null,
                        "CP03_IRF_OVERRIDE", "Started",
                        $"HR override IRF from {pending.ResolvedIRFId} to {request.OverrideIRFId}",
                        JsonConvert.SerializeObject(new { OriginalIRF = pending.ResolvedIRFId, NewIRF = request.OverrideIRFId, Reason = request.IRFChangeReason }),
                        processedBy);

                    var updated = await _repository.UpdatePendingResumeIRFAsync(
                        request.PendingResumeId,
                        request.OverrideIRFId.Value,
                        processedBy,
                        request.IRFChangeReason ?? "HR override during review");
                    stepStopwatch.Stop();
                    
                    if (!updated)
                    {
                        await _repository.LogProcessingCheckpointAsync(ticketId, pendingId, null, "CP03_IRF_OVERRIDE", "Failed", "Failed to update IRF in database", null, processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                        return Json(new { success = false, error = "Failed to update IRF. Please try again." });
                    }
                    
                    pending = await _repository.GetPendingResumeByIdAsync(request.PendingResumeId);
                    irfWasChanged = true;
                    await _repository.LogProcessingCheckpointAsync(ticketId, pendingId, null, "CP03_IRF_OVERRIDE", "Success", $"IRF updated to {pending.ResolvedIRFStringId}", null, processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                }

                if (!pending.ResolvedIRFId.HasValue)
                {
                    await _repository.LogProcessingCheckpointAsync(ticketId, pendingId, null, "CP04_VALIDATE_IRF", "Failed", "No IRF resolved for this resume");
                    return Json(new { success = false, error = "No IRF resolved for this resume. Please select an IRF." });
                }

                // CP04: Resolve HR User ID
                stepStopwatch.Restart();
                int hrId = await _repository.GetHRUserIdForIRFAsync(pending.ResolvedIRFId.Value);
                string hrSource = "IRF.RisebirdHr";
                if (hrId <= 0)
                {
                    hrId = await _repository.GetSystemUserIdAsync();
                    hrSource = "SystemFallback";
                    if (hrId <= 0)
                    {
                        stepStopwatch.Stop();
                        await _repository.LogProcessingCheckpointAsync(ticketId, pendingId, null, "CP04_RESOLVE_HR", "Failed", "No valid HR user found", null, processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                        return Json(new { success = false, error = "No valid HR user found to attribute this resume." });
                    }
                }
                stepStopwatch.Stop();
                await _repository.LogProcessingCheckpointAsync(
                    ticketId, pendingId, null,
                    "CP04_RESOLVE_HR", "Success",
                    $"HR resolved: UserId={hrId}, Source={hrSource}",
                    null, processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                
                int ingestedResumeId = 0;
                var scheduledDateTime = request.OverrideInterviewDateTime ?? pending.ScheduledInterviewDateTime;
                bool hasScheduleTime = request.AlsoSchedule && scheduledDateTime.HasValue;

                // CP05: Insert CandidateResume
                stepStopwatch.Restart();
                await _repository.LogProcessingCheckpointAsync(
                    ticketId, pendingId, null,
                    "CP05_INSERT_RESUME", "Started",
                    $"Inserting into CandidateResume. IRF={pending.ResolvedIRFId}, HasScheduleTime={hasScheduleTime}",
                    JsonConvert.SerializeObject(new { pending.ResolvedIRFId, pending.ResumeUrl, hrId, hasScheduleTime, scheduledDateTime }),
                    processedBy);

                if (hasScheduleTime)
                {
                    ingestedResumeId = await _repository.InsertCandidateResumeForAutoScheduleAsync(
                        pending.ResolvedIRFId.Value,
                        pending.ResumeUrl,
                        hrId,
                        processedBy,  
                        pending.SupportTicketId,
                        pending.CandidateName,
                        pending.CandidateEmail,
                        scheduledDateTime);
                }
                else
                {
                    // ClientHREmailId should be the SENDER (client HR who submitted), not the Risebird channel owner
                    ingestedResumeId = await _repository.InsertCandidateResumeAsync(
                        pending.ResolvedIRFId.Value,
                        pending.ResumeUrl,
                        hrId,
                        processedBy,  
                        pending.SupportTicketId);
                }
                stepStopwatch.Stop();
                candidateResumeId = ingestedResumeId > 0 ? ingestedResumeId : null;

                if (ingestedResumeId <= 0)
                {
                    await _repository.LogProcessingCheckpointAsync(ticketId, pendingId, null, "CP05_INSERT_RESUME", "Failed", "InsertCandidateResume returned 0 or negative ID", null, processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                    await _repository.UpdatePendingResumeStatusAsync(pending.Id, "Failed", null, processedBy);
                    return Json(new { success = false, error = "Failed to insert into CandidateResume" });
                }

                await _repository.LogProcessingCheckpointAsync(
                    ticketId, pendingId, candidateResumeId,
                    "CP05_INSERT_RESUME", "Success",
                    $"CandidateResume inserted with ID={ingestedResumeId}",
                    null, processedBy, (int)stepStopwatch.ElapsedMilliseconds);

                // CP06: Update Pending Status
                stepStopwatch.Restart();
                await _repository.UpdatePendingResumeStatusAsync(pending.Id, "Ingested", ingestedResumeId, processedBy);
                stepStopwatch.Stop();
                await _repository.LogProcessingCheckpointAsync(ticketId, pendingId, candidateResumeId, "CP06_UPDATE_STATUS", "Success", "Pending resume status updated to Ingested", null, processedBy, (int)stepStopwatch.ElapsedMilliseconds);

                // CP07: Parse Resume
                string parseMessage = null;
                string scheduleMessage = null;
                bool parsingSuccess = false;
                ResumeParseResult parseResult = null;

                if (_resumeParser != null)
                {
                    stepStopwatch.Restart();
                    await _repository.LogProcessingCheckpointAsync(ticketId, pendingId, candidateResumeId, "CP07_PARSE_RESUME", "Started", "Starting resume parsing");
                    
                    try
                    {
                        parseResult = await _resumeParser.ParseResumeAsync(ingestedResumeId);
                        stepStopwatch.Stop();
                        parsingSuccess = parseResult.Success;
                        
                        if (parseResult.Success)
                        {
                            parseMessage = $"Resume parsed successfully. Candidate: {parseResult.CandidateName}, Email: {parseResult.CandidateEmail}";
                            await _repository.LogProcessingCheckpointAsync(
                                ticketId, pendingId, candidateResumeId,
                                "CP07_PARSE_RESUME", "Success",
                                parseMessage,
                                JsonConvert.SerializeObject(new { parseResult.CandidateName, parseResult.CandidateEmail, parseResult.CandidatePhone, parseResult.CandidateId }),
                                processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                        }
                        else
                        {
                            parseMessage = $"Resume parsing failed: {parseResult.ErrorMessage}";
                            await _repository.LogProcessingCheckpointAsync(
                                ticketId, pendingId, candidateResumeId,
                                "CP07_PARSE_RESUME", "Failed",
                                parseMessage,
                                JsonConvert.SerializeObject(new { parseResult.ErrorMessage }),
                                processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                        }
                    }
                    catch (Exception parseEx)
                    {
                        stepStopwatch.Stop();
                        await _logger.LogMessage(LogLevelInfo.Exception, parseEx);
                        parseMessage = $"Resume parsing error: {parseEx.Message}";
                        await _repository.LogProcessingCheckpointAsync(
                            ticketId, pendingId, candidateResumeId,
                            "CP07_PARSE_RESUME", "Failed",
                            parseMessage,
                            JsonConvert.SerializeObject(new { Exception = parseEx.Message, StackTrace = parseEx.StackTrace }),
                            processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                    }
                }
                else
                {
                    parseMessage = "Resume added. Parsing will be handled by background job.";
                    await _repository.LogProcessingCheckpointAsync(ticketId, pendingId, candidateResumeId, "CP07_PARSE_RESUME", "Warning", parseMessage + " (ResumeParser service not available)");
                }

                // CP08: Schedule Interview (if requested)
                bool datetimeOverridden = request.OverrideInterviewDateTime.HasValue && 
                    request.OverrideInterviewDateTime != pending.ScheduledInterviewDateTime;
                
                if (request.AlsoSchedule && scheduledDateTime.HasValue)
                {
                    stepStopwatch.Restart();
                    await _repository.LogProcessingCheckpointAsync(
                        ticketId, pendingId, candidateResumeId,
                        "CP08_SCHEDULE", "Started",
                        $"Scheduling requested. DateTime={scheduledDateTime:yyyy-MM-dd HH:mm}, Overridden={datetimeOverridden}",
                        JsonConvert.SerializeObject(new { scheduledDateTime, datetimeOverridden, parsingSuccess, CandidateEmail = parseResult?.CandidateEmail }),
                        processedBy);

                    if (parsingSuccess && parseResult != null && !string.IsNullOrEmpty(parseResult.CandidateEmail))
                    {
                        try
                        {
                            const string apiUrl = "https://api.portal.risebird.io/api/Candidates/Schedule";
                            
                            var tenantId = await _repository.GetTenantIdForIRFAsync(pending.ResolvedIRFId.Value);

                            var dto = new ScheduleCandidatesRequest
                            {
                                skillChannelId = pending.ResolvedIRFStringId,
                                requestedBy    = pending.TicketSenderEmail,
                                requestType    = "schedule_supportTicket"
                            };
                            dto.CandidateList.Add(new ScheduleCandidateList
                            {
                                EmailId       = parseResult.CandidateEmail,
                                Interviewdate = scheduledDateTime.Value,
                                ResumeLink    = pending.ResumeUrl,
                                Phone         = parseResult.CandidatePhone,
                                Name          = parseResult.CandidateName ?? pending.CandidateName
                            });

                            await _repository.LogProcessingCheckpointAsync(
                                ticketId, pendingId, candidateResumeId,
                                "CP08_SCHEDULE_API_CALL", "Started",
                                $"Calling Schedule API. TenantId={tenantId}, SkillChannel={pending.ResolvedIRFStringId}",
                                JsonConvert.SerializeObject(dto),
                                processedBy);

                            var restClient = new RestClient(apiUrl);
                            var restRequest = new RestRequest(apiUrl, Method.Post);
                            restRequest.AddHeader("content-type", "application/json");
                            restRequest.AddHeader("x-functions-key", "SJJvpXEzy-1fOQN2mygBaTT6gXctL406VhpMwn0BqhKUAzFuBDDP5Q==");
                            restRequest.AddHeader("tenant-id", tenantId ?? string.Empty);
                            restRequest.AddParameter("application/json", JsonConvert.SerializeObject(dto), ParameterType.RequestBody);

                            var response = await restClient.ExecuteAsync(restRequest);
                            stepStopwatch.Stop();

                            // Parse response body to check actual scheduling status
                            // The API returns 200 OK even when scheduling fails (e.g., no slot available)
                            // We must check the onboardingStatus field to know the real outcome
                            string actualStatus = null;
                            string meetingLink = null;
                            bool schedulingActuallySucceeded = false;

                            if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                            {
                                try
                                {
                                    var scheduleResponse = JsonConvert.DeserializeObject<ScheduleCandidateResponse>(response.Content);
                                    if (scheduleResponse?.list != null && scheduleResponse.list.Count > 0)
                                    {
                                        var candidateStatus = scheduleResponse.list[0];
                                        actualStatus = candidateStatus.onboardingStatus;
                                        meetingLink = candidateStatus.meetingLink;
                                        
                                        // Only consider it successful if onboardingStatus is "successful"
                                        schedulingActuallySucceeded = string.Equals(actualStatus, "successful", StringComparison.OrdinalIgnoreCase);
                                    }
                                }
                                catch (Exception parseEx)
                                {
                                    await _logger.LogMessage(LogLevelInfo.Warning, parseEx);
                                    actualStatus = $"Parse error: {parseEx.Message}";
                                }
                            }

                            if (schedulingActuallySucceeded)
                            {
                                scheduleMessage = $"Interview scheduled for {scheduledDateTime.Value:dd MMM yyyy hh:mm tt} IST" +
                                    (datetimeOverridden ? " (time modified by HR)" : "") +
                                    (!string.IsNullOrEmpty(meetingLink) && meetingLink != "NA" ? $" | Meeting: {meetingLink}" : "");
                                await _repository.LogProcessingCheckpointAsync(
                                    ticketId, pendingId, candidateResumeId,
                                    "CP08_SCHEDULE", "Success",
                                    scheduleMessage,
                                    JsonConvert.SerializeObject(new { response.StatusCode, actualStatus, meetingLink, ResponseContent = response.Content }),
                                    processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                            }
                            else if (response.IsSuccessful)
                            {
                                // API returned 200 but scheduling didn't actually succeed
                                scheduleMessage = $"Scheduling not completed: {actualStatus ?? "Unknown status"}. Please check slot availability or try a different time.";
                                await _repository.LogProcessingCheckpointAsync(
                                    ticketId, pendingId, candidateResumeId,
                                    "CP08_SCHEDULE", "Warning",
                                    scheduleMessage,
                                    JsonConvert.SerializeObject(new { response.StatusCode, actualStatus, meetingLink, ResponseContent = response.Content }),
                                    processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                            }
                            else
                            {
                                // HTTP error
                                scheduleMessage = $"Scheduling failed (HTTP {(int)response.StatusCode}): {response.ErrorMessage ?? response.Content}";
                                await _repository.LogProcessingCheckpointAsync(
                                    ticketId, pendingId, candidateResumeId,
                                    "CP08_SCHEDULE", "Failed",
                                    scheduleMessage,
                                    JsonConvert.SerializeObject(new { response.StatusCode, response.ErrorMessage, ResponseContent = response.Content }),
                                    processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                            }
                        }
                        catch (Exception schedEx)
                        {
                            stepStopwatch.Stop();
                            await _logger.LogMessage(LogLevelInfo.Exception, schedEx);
                            scheduleMessage = $"Scheduling error: {schedEx.Message}";
                            await _repository.LogProcessingCheckpointAsync(
                                ticketId, pendingId, candidateResumeId,
                                "CP08_SCHEDULE", "Failed",
                                scheduleMessage,
                                JsonConvert.SerializeObject(new { Exception = schedEx.Message, StackTrace = schedEx.StackTrace }),
                                processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                        }
                    }
                    else
                    {
                        scheduleMessage = "Cannot schedule - resume parsing did not extract candidate email. Please schedule manually after parsing completes.";
                        stepStopwatch.Stop();
                        await _repository.LogProcessingCheckpointAsync(
                            ticketId, pendingId, candidateResumeId,
                            "CP08_SCHEDULE", "Warning",
                            scheduleMessage,
                            JsonConvert.SerializeObject(new { parsingSuccess, CandidateEmail = parseResult?.CandidateEmail }),
                            processedBy, (int)stepStopwatch.ElapsedMilliseconds);
                    }
                }

                // CP09: Refresh Ticket Flag
                stepStopwatch.Restart();
                await _repository.RefreshTicketPendingResumeFlagAsync(pending.SupportTicketId);
                stepStopwatch.Stop();
                await _repository.LogProcessingCheckpointAsync(ticketId, pendingId, candidateResumeId, "CP09_REFRESH_FLAG", "Success", "Ticket pending resume flag refreshed", null, processedBy, (int)stepStopwatch.ElapsedMilliseconds);

                // CP10: Complete
                overallStopwatch.Stop();
                await _repository.LogProcessingCheckpointAsync(
                    ticketId, pendingId, candidateResumeId,
                    "CP10_COMPLETE", "Success",
                    $"Processing complete. IRF={pending.ResolvedIRFStringId}, IRFChanged={irfWasChanged}, Parsed={parsingSuccess}, Scheduled={!string.IsNullOrEmpty(scheduleMessage) && !scheduleMessage.Contains("failed") && !scheduleMessage.Contains("Cannot")}",
                    null, processedBy, (int)overallStopwatch.ElapsedMilliseconds);

                return Json(new
                {
                    success = true,
                    message = $"Resume ingested successfully into IRF {pending.ResolvedIRFStringId}" +
                              (irfWasChanged ? " (IRF changed by HR)" : ""),
                    candidateResumeId = ingestedResumeId,
                    irfChanged = irfWasChanged,
                    parseMessage,
                    scheduleMessage,
                    parsingSuccess,
                    candidateName = parseResult?.CandidateName,
                    candidateEmail = parseResult?.CandidateEmail
                });
            }
            catch (Exception ex)
            {
                overallStopwatch.Stop();
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                await _repository.LogProcessingCheckpointAsync(
                    ticketId, pendingId, candidateResumeId,
                    "CP_EXCEPTION", "Failed",
                    $"Unhandled exception: {ex.Message}",
                    JsonConvert.SerializeObject(new { Exception = ex.Message, StackTrace = ex.StackTrace }),
                    processedBy, (int)overallStopwatch.ElapsedMilliseconds);
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

                // Parse response to check actual scheduling status
                // API returns 200 OK even when scheduling fails (e.g., no slot available)
                string actualStatus = null;
                string meetingLink = null;
                bool schedulingActuallySucceeded = false;

                if (response.IsSuccessful && !string.IsNullOrEmpty(response.Content))
                {
                    try
                    {
                        var scheduleResponse = JsonConvert.DeserializeObject<ScheduleCandidateResponse>(response.Content);
                        if (scheduleResponse?.list != null && scheduleResponse.list.Count > 0)
                        {
                            var candidateStatus = scheduleResponse.list[0];
                            actualStatus = candidateStatus.onboardingStatus;
                            meetingLink = candidateStatus.meetingLink;
                            
                            // Only consider successful if onboardingStatus is "successful"
                            schedulingActuallySucceeded = string.Equals(actualStatus, "successful", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    catch (Exception parseEx)
                    {
                        await _logger.LogMessage(LogLevelInfo.Warning, parseEx);
                        actualStatus = $"Response parse error: {parseEx.Message}";
                    }
                }

                if (schedulingActuallySucceeded)
                {
                    var successMsg = $"Interview scheduled for {candidateName} ({candidateEmail}) at {candidate.ScheduledInterviewDateTime:g}";
                    if (!string.IsNullOrEmpty(meetingLink) && meetingLink != "NA")
                        successMsg += $" | Meeting link: {meetingLink}";
                    
                    return Json(new
                    {
                        success = true,
                        message = successMsg,
                        onboardingStatus = actualStatus,
                        meetingLink = meetingLink,
                        apiResponse = response.Content
                    });
                }
                else if (response.IsSuccessful)
                {
                    // API returned 200 but scheduling didn't actually succeed
                    return Json(new
                    {
                        success = false,
                        error = $"Scheduling not completed: {actualStatus ?? "Unknown status"}. Please check slot availability or try a different time.",
                        onboardingStatus = actualStatus,
                        details = response.Content
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        error = $"Schedule API returned HTTP {(int)response.StatusCode}: {response.ErrorMessage}",
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
        /// Use pendingResumeId >= 1000 for test mode.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RejectPendingResume(int pendingResumeId)
        {
            try
            {
                // TEST MODE: Return mock success for test IDs (>= 1000)
                if (pendingResumeId >= 1000)
                {
                    await Task.Delay(300); // Simulate processing
                    return Json(new { success = true, message = $"[TEST] Resume #{pendingResumeId} rejected." });
                }

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

        /// <summary>
        /// Gets processing logs for debugging ingestion/scheduling flow.
        /// Optionally filter by ticket ID or pending resume ID.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProcessingLogs(int? ticketId = null, int? pendingResumeId = null, int limit = 100)
        {
            try
            {
                var logs = await _repository.GetProcessingLogsAsync(ticketId, pendingResumeId, limit);
                return Json(new { success = true, logs });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message, logs = new List<ProcessingLogVM>() });
            }
        }

        // ── Create Channel from Template ───────────────────────────────────────

        /// <summary>
        /// Gets templates whose assessment group names match the given primary specialization.
        /// Returns templates sorted by match score (best matches first).
        /// If no matches, returns all active templates.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTemplatesForSpecialization(string specialization)
        {
            try
            {
                var templates = await _repository.GetTemplatesForSpecializationAsync(specialization ?? "");
                return Json(new { success = true, templates });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message, templates = new List<TemplateMatchVM>() });
            }
        }

        /// <summary>
        /// Creates a new skill channel from the selected template.
        /// Uses the same flow as IRFController.CreateInterviewLinkV1.
        /// Accepts form data with optional JD file upload.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateChannelFromTemplate(
            int templateId,
            int clientId,
            string jdTitle,
            int billingOwnerId,
            bool isAIInterview,
            int aiInterviewType,
            int[] clientUserIds,
            IFormFile jdFile = null)
        {
            try
            {
                if (_irfService == null || _templateService == null)
                    return Json(new CreateChannelResponse { Success = false, Message = "Required services not available" });

                if (templateId <= 0)
                    return Json(new CreateChannelResponse { Success = false, Message = "Please select a template" });

                if (clientId <= 0)
                    return Json(new CreateChannelResponse { Success = false, Message = "Please select a client" });

                if (string.IsNullOrWhiteSpace(jdTitle))
                    return Json(new CreateChannelResponse { Success = false, Message = "Please enter a JD title" });

                var emailId = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "")
                           ?? "support-ticket-flow";

                // Get template data
                var templateData = await _templateService.GetTemplateDataForPopulatingSkillChannel(templateId);
                if (templateData == null)
                    return Json(new CreateChannelResponse { Success = false, Message = "Template not found" });

                // Generate unique skill channel name
                var clientName = await _clientService.GetClientName(clientId);
                var irfId = GenerateSkillChannelName(templateData.AssessmentGroupName, clientName);

                // Handle JD file upload if provided
                string jdLink = null;
                if (jdFile != null && jdFile.Length > 0 && _azureStorageHelper != null && _fileMimeTypeHelper != null)
                {
                    var filename = jdFile.FileName.Replace(" ", "_");
                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(jdFile);
                    
                    var allowedTypes = new[] {
                        "application/msword",
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        "application/pdf"
                    };
                    
                    if (allowedTypes.Contains(fileMimeType))
                    {
                        await _azureStorageHelper.InsertBlob(filename, "resumes", jdFile);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                        var blob = containerRef.GetBlockBlobReference(filename);
                        jdLink = blob.Uri.ToString();
                    }
                }

                // Build the IRF model
                var irfModel = new IRFVm
                {
                    CreatedFromTemplateId = templateId,
                    AssessmentGroupId     = templateData.AssessmentGroupId,
                    ClientId              = clientId,
                    JDTiltle              = jdTitle,
                    JDLink                = jdLink,
                    IRFId                 = irfId,
                    MeetingPlatform       = templateData.MeetingPlatform ?? "Zoho",
                    StartDate             = DateTime.Now,
                    EndDate               = DateTime.Now.AddMonths(3),
                    IsAIInterview         = isAIInterview,
                    AIInterviewType       = aiInterviewType,
                    BillingOwner          = billingOwnerId
                };

                // Generate scheduling link
                var schedulingLink = await GenerateSchedulingLinkAsync(irfModel);
                irfModel.CalendlyLink = schedulingLink;

                // Create the skill channel
                var result = await _irfService.CreateSkillChannelUsingTemplateByInterviewLinkPopUpV1(irfModel, emailId, "SupportTicket");

                if (result.Item1)
                {
                    // Get the created IRF details
                    var newIrf = await _irfService.GetSkillChannelDetailsByName(irfId);

                    return Json(new CreateChannelResponse
                    {
                        Success        = true,
                        NewIRFId       = newIrf?.id ?? 0,
                        NewIRFStringId = irfId,
                        JDTitle        = jdTitle,
                        SchedulingLink = schedulingLink,
                        Message        = $"Skill Channel '{irfId}' created successfully!"
                    });
                }
                else
                {
                    return Json(new CreateChannelResponse
                    {
                        Success = false,
                        Message = $"Failed to create skill channel: {result.Item2}"
                    });
                }
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new CreateChannelResponse { Success = false, Message = "Error creating channel: " + ex.Message });
            }
        }

        /// <summary>
        /// Generates a unique 10-character skill channel name.
        /// Format: 3 chars client + 2 chars assessment group + 5 chars date-based.
        /// </summary>
        private string GenerateSkillChannelName(string assessmentGroupName, string clientCompanyName)
        {
            bool flag = false;
            string skillChannelName = string.Empty;

            while (!flag)
            {
                if (!string.IsNullOrEmpty(clientCompanyName) && clientCompanyName.Length >= 3 &&
                    !string.IsNullOrEmpty(assessmentGroupName) && assessmentGroupName.Replace(" ", "").Length >= 2)
                {
                    skillChannelName = clientCompanyName.Substring(0, 3);
                    skillChannelName = skillChannelName + assessmentGroupName.Replace(" ", "").Substring(0, 2) + GenerateRandomString();
                }
                else
                {
                    skillChannelName = "SKC" + Guid.NewGuid().ToString("N").Substring(0, 7);
                }

                flag = _irfService?.CheckUniqueness(skillChannelName, false) ?? true;
            }

            skillChannelName = skillChannelName.Replace(".", "a").Replace(" ", "-").Replace("+", "s").Replace("&", "o");
            return skillChannelName;
        }

        private string GenerateRandomString()
        {
            var now = DateTime.Now;
            var day = now.Day.ToString("D2");
            var month = now.Month.ToString("D2");
            var sec = DigitalRoot(now.Second).ToString();
            return day + month + sec;
        }

        private int DigitalRoot(int n)
        {
            if (n == 0) return 0;
            return 1 + ((n - 1) % 9);
        }

        private async Task<string> GenerateSchedulingLinkAsync(IRFVm model)
        {
            try
            {
                var baseUrl = _configuration["SchedulerBaseUrl"] ?? "https://portal.risebird.io";
                var uniqueId = Guid.NewGuid().ToString();
                return $"{baseUrl}/InterviewScheduler/ScheduleEvent?id={uniqueId}";
            }
            catch
            {
                return $"https://portal.risebird.io/InterviewScheduler/ScheduleEvent?id={Guid.NewGuid()}";
            }
        }
    }
}
