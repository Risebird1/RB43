using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Business.Interface;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class NaukriOnboardingController : Controller
    {
        private readonly INaukriOnboardingRepository  _repository;
        private readonly IApplicationErrorLogger      _logger;
        private readonly IConfiguration               _configuration;
        private readonly IAssistantServiceHelper      _assistantServiceHelper;
        private readonly IPdfReaderHelper             _pdfReaderHelper;
        private readonly INotificationHelper          _notificationHelper;
        private readonly IInterviewerService          _interviewerService;
        private readonly IAzureStorageHelper           _azureStorageHelper;
        private readonly IFileMimeTypeHelper           _fileMimeTypeHelper;
        private readonly IHttpClientFactory            _httpClientFactory;
        private readonly IEncryptionHelper              _encryptionHelper;
        private readonly IPlivoCXService               _plivoCXService;
        private readonly IPlivoCXCallRepository         _cpRepo;

        public NaukriOnboardingController(
            INaukriOnboardingRepository  repository,
            IApplicationErrorLogger      logger,
            IConfiguration               configuration,
            IAssistantServiceHelper      assistantServiceHelper,
            IPdfReaderHelper             pdfReaderHelper,
            INotificationHelper          notificationHelper,
            IInterviewerService          interviewerService,
            IAzureStorageHelper           azureStorageHelper,
            IFileMimeTypeHelper           fileMimeTypeHelper,
            IHttpClientFactory            httpClientFactory,
            IEncryptionHelper             encryptionHelper,
            IPlivoCXService               plivoCXService,
            IPlivoCXCallRepository        cpRepo)
        {
            _repository             = repository;
            _logger                 = logger;
            _configuration          = configuration;
            _assistantServiceHelper = assistantServiceHelper;
            _pdfReaderHelper        = pdfReaderHelper;
            _notificationHelper     = notificationHelper;
            _interviewerService     = interviewerService;
            _azureStorageHelper     = azureStorageHelper;
            _fileMimeTypeHelper     = fileMimeTypeHelper;
            _httpClientFactory      = httpClientFactory;
            _encryptionHelper       = encryptionHelper;
            _plivoCXService         = plivoCXService;
            _cpRepo                 = cpRepo;
        }

        // ── Dashboard ───────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(
            string status            = null,
            string candidateName     = null,
            string magName           = null,
            string source            = null,
            string automationStatus  = null,
            string callStatus        = null,
            string disposition       = null,
            string funnelStage       = null,
            string onboardingType    = null,
            string assignedTo        = null,
            string dateFrom          = null,
            string dateTo            = null,
            int    page              = 1,
            int    pageSize          = 25)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 10) pageSize = 10;
                if (pageSize > 200) pageSize = 200;

                DateTime? dtFrom = null, dtTo = null;
                if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out var pFrom)) dtFrom = pFrom;
                if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out var pTo)) dtTo = pTo;

                var records = await _repository.GetListAsync(status, candidateName, magName, source, dtFrom, dtTo, page, pageSize, automationStatus, callStatus, disposition, funnelStage, onboardingType, assignedTo);

                var funnelCounts = await _repository.GetFunnelStageCountsAsync(status, candidateName, magName, source, dtFrom, dtTo, automationStatus, callStatus, disposition, funnelStage, onboardingType, assignedTo);

                var funnelReach = await _repository.GetFunnelReachCountsAsync(status, candidateName, magName, source, dtFrom, dtTo, automationStatus, callStatus, disposition, funnelStage, onboardingType, assignedTo);

                var assignees = await _repository.GetEligibleAssigneesAsync();

                var first = records.FirstOrDefault();
                int totalCount = first?.TotalCount ?? 0;
                int totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;

                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = totalPages;
                ViewBag.Status = status ?? string.Empty;
                ViewBag.CandidateName = candidateName ?? string.Empty;
                ViewBag.MagName = magName ?? string.Empty;
                ViewBag.Source = source ?? string.Empty;
                ViewBag.AutomationStatus = automationStatus ?? string.Empty;
                ViewBag.CallStatus = callStatus ?? string.Empty;
                ViewBag.Disposition = disposition ?? string.Empty;
                ViewBag.FunnelStage = funnelStage ?? string.Empty;
                ViewBag.OnboardingType = onboardingType ?? string.Empty;
                ViewBag.AssignedTo = assignedTo ?? string.Empty;
                ViewBag.Assignees = assignees;
                ViewBag.FunnelCounts = funnelCounts;
                ViewBag.FunnelReach = funnelReach;
                // Global status breakdown (Open + InProgress + Closed + Onboarded = Total).
                ViewBag.OpenCount = first?.OpenCount ?? 0;
                ViewBag.InProgressCount = first?.InProgressCount ?? 0;
                ViewBag.ClosedCount = first?.ClosedCount ?? 0;
                ViewBag.OnboardedCount = first?.OnboardedCount ?? 0;
                ViewBag.DateFrom = dateFrom ?? string.Empty;
                ViewBag.DateTo = dateTo ?? string.Empty;
                ViewBag.ProfileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") ?? "";

                return View(records);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.Page = 1; ViewBag.PageSize = pageSize; ViewBag.TotalCount = 0; ViewBag.TotalPages = 1;
                ViewBag.Status = status; ViewBag.CandidateName = candidateName; ViewBag.MagName = magName;
                ViewBag.Source = source ?? string.Empty;
                ViewBag.AutomationStatus = automationStatus ?? string.Empty;
                ViewBag.CallStatus = callStatus ?? string.Empty;
                ViewBag.Disposition = disposition ?? string.Empty;
                ViewBag.FunnelStage = funnelStage ?? string.Empty;
                ViewBag.OnboardingType = onboardingType ?? string.Empty;
                ViewBag.AssignedTo = assignedTo ?? string.Empty;
                ViewBag.Assignees = new List<Risebird.Entities.ViewModel.NaukriAssigneeVM>();
                ViewBag.FunnelCounts = new Dictionary<string, int>();
                ViewBag.FunnelReach = new Dictionary<string, int>();
                ViewBag.DateFrom = dateFrom; ViewBag.DateTo = dateTo;
                ViewBag.ProfileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") ?? "";
                return View(new List<NaukriProspectiveInterviewerVM>());
            }
        }

        // ── Prospect Progress Dashboard ──────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> ProspectProgress(
            string status        = null,
            string candidateName = null,
            string magName       = null,
            string source        = null,
            string dateFrom      = null,
            string dateTo        = null,
            int    page          = 1,
            int    pageSize      = 50)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 10) pageSize = 10;
                if (pageSize > 200) pageSize = 200;

                DateTime? dtFrom = null, dtTo = null;
                if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out var pFrom)) dtFrom = pFrom;
                if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out var pTo)) dtTo = pTo;

                var records = await _repository.GetListAsync(status, candidateName, magName, source, dtFrom, dtTo, page, pageSize);

                var first = records.FirstOrDefault();
                int totalCount = first?.TotalCount ?? 0;
                int totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;

                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = totalPages;
                ViewBag.Status = status ?? string.Empty;
                ViewBag.CandidateName = candidateName ?? string.Empty;
                ViewBag.MagName = magName ?? string.Empty;
                ViewBag.Source = source ?? string.Empty;
                ViewBag.DateFrom = dateFrom ?? string.Empty;
                ViewBag.DateTo = dateTo ?? string.Empty;

                return View(records);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.Page = 1; ViewBag.PageSize = pageSize; ViewBag.TotalCount = 0; ViewBag.TotalPages = 1;
                ViewBag.Status = status; ViewBag.CandidateName = candidateName; ViewBag.MagName = magName;
                ViewBag.Source = source ?? string.Empty;
                ViewBag.DateFrom = dateFrom; ViewBag.DateTo = dateTo;
                return View(new List<NaukriProspectiveInterviewerVM>());
            }
        }

        // ── Get record details (AJAX - for Manage modal) ────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetRecordDetails(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                return Json(new
                {
                    success = true,
                    record = new
                    {
                        record.Id, record.SenderEmail, record.Subject, record.MailContent,
                        receivedAt = record.ReceivedAt?.ToString("dd MMM yyyy, HH:mm"),
                        record.CandidateName, record.Location, record.PastExperience,
                        record.NoticePeriod, record.Education, record.KeySkills,
                        record.CurrentRole, record.ExperienceYears, record.Salary,
                        record.MagName, record.ResumeLink, record.HasAttachment,
                        record.ParsedEmail, record.ParsedPhone, record.ResumeParseCompleted,
                        record.IsExistingInterviewer, record.ExistingUserId, record.ExistingProfileId,
                        record.EmploymentVerified, record.EmploymentCompanyName,
                        record.EmploymentIsEmployed, record.EmploymentDetails,
                        record.LinkedInUrl, record.LinkedInMatch, record.LinkedInConfidence,
                        record.ResumeAIAnalysisCompleted, record.ResumeAIAnalysisResult,
                        record.AIVerdict, record.AIFitmentScore,
                        record.OnboardingCompleted, record.OnboardedUserId, record.OnboardedProfileId,
                        record.AssessmentGroupId, record.AcknowledgementEmailSent,
                        record.SelectedIRFId, record.SelectedIRFName, record.ApplicationLink,
                        record.InvitationEmailSent, invitationEmailSentAt = record.InvitationEmailSentAt?.ToString("dd MMM yyyy, HH:mm"),
                        record.ProspectResponse, prospectResponseAt = record.ProspectResponseAt?.ToString("dd MMM yyyy, HH:mm"),
                        record.SelfRegistrationCompleted,
                        record.PlivoCallStatus, record.PlivoConversationId,
                        plivoCallAt = record.PlivoCallAt?.ToString("dd MMM yyyy, HH:mm"),
                        record.PlivoCallDuration, record.PlivoProspectDisposition,
                        record.PlivoOnboardingPreference, record.PlivoKycAccepted, record.PlivoCalledBy,
                        record.AutomationStatus, record.PlivoCallAttemptsTotal, record.PlivoCallAttemptsToday,
                        record.Status, record.Source, record.ProcessedBy,
                        record.Intent, record.IntentSource, record.FunnelStage, record.TicketStatus, record.OnboardingType,
                        record.AssignedToUserId, record.AssignedToEmail, record.AssignedToName,
                        assignedAt = record.AssignedAt?.ToString("dd MMM yyyy, HH:mm"),
                        record.ResolvedAssessmentGroupId, record.AssessmentGroupName,
                        hasResumeLink = !string.IsNullOrEmpty(record.ResumeLink)
                                        && !string.Equals(record.ResumeLink, "None", StringComparison.OrdinalIgnoreCase)
                    }
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Status update (AJAX) ────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            // Operational ticket status only; pipeline outcomes are tracked via FunnelStage.
            var allowed = new[] { "Open", "InProgress", "Closed" };
            if (id <= 0 || !Array.Exists(allowed, s => s == status))
                return Json(new { success = false, error = "Invalid status." });

            var changedBy = HttpContext.Session.GetString("email").Trim('"');
            var existing = await _repository.GetByIdAsync(id);
            var ok = await _repository.UpdateStatusAsync(id, status, changedBy);
            if (ok)
                await _repository.InsertHistoryAsync(id, "StatusChange", existing?.TicketStatus ?? existing?.Status ?? "—", status, changedBy);
            return Json(new { success = ok, status });
        }

        // ── Ticket assignment ────────────────────────────────────────────────────────

        // Eligible operators (InterviewerManagement / SeniorInterviewerManagement) for the assignee
        // dropdowns. Returned as { userId, email, name, displayName }.
        [HttpGet]
        public async Task<IActionResult> GetEligibleAssignees()
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in.", assignees = new object[0] });

            try
            {
                var list = await _repository.GetEligibleAssigneesAsync();
                var assignees = list.Select(a => new
                {
                    userId = a.UserId,
                    email = a.Email,
                    name = a.Name,
                    displayName = a.DisplayName
                });
                return Json(new { success = true, assignees });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message, assignees = new object[0] });
            }
        }

        // (Re)assign a prospect (ticket) to an operator. Logs the change to NaukriOnboardingHistory as
        // an AssignmentChange so the assignee history is auditable like SupportTicket.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAssignment(int id, int assignedToUserId)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            if (id <= 0 || assignedToUserId <= 0)
                return Json(new { success = false, error = "Invalid request." });

            var changedBy = HttpContext.Session.GetString("email").Trim('"');
            var result = await _repository.UpdateAssignmentAsync(id, assignedToUserId);
            if (!result.Success)
                return Json(new { success = false, error = "Could not assign — the selected operator is not an active InterviewerManagement / SeniorInterviewerManagement user." });

            await _repository.InsertHistoryAsync(id, "AssignmentChange",
                string.IsNullOrWhiteSpace(result.FromAssignedTo) ? "Unassigned" : result.FromAssignedTo,
                result.ToEmail, changedBy);

            return Json(new
            {
                success = true,
                assignedToEmail = result.ToEmail,
                assignedToName = result.ToName
            });
        }

        // ── Bulk assignment by MAG ────────────────────────────────────────────────────
        // Distribute a whole MAG's tickets to one operator (the pre-system MAG-wise practice).

        [HttpGet]
        public async Task<IActionResult> GetMagAssignmentSummary()
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in.", mags = new object[0] });

            try
            {
                var mags = await _repository.GetMagAssignmentSummaryAsync();
                return Json(new { success = true, mags });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message, mags = new object[0] });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAssignByMag(List<string> magNames, int assignedToUserId, string scope)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            if (magNames == null || magNames.Count == 0 || assignedToUserId <= 0)
                return Json(new { success = false, error = "Select at least one MAG and an operator." });

            var changedBy = HttpContext.Session.GetString("email").Trim('"');
            int totalAffected = 0;
            string toEmail = null, toName = null;
            var failed = new List<string>();

            foreach (var mag in magNames.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).Distinct())
            {
                var result = await _repository.BulkAssignByMagAsync(mag, assignedToUserId, scope, changedBy);
                if (!result.Success)
                {
                    // A validation failure means the operator itself is invalid — abort the whole run.
                    return Json(new { success = false, error = "Could not assign — the selected operator is not an active InterviewerManagement / SeniorInterviewerManagement user." });
                }
                totalAffected += result.Affected;
                toEmail = result.ToEmail;
                toName = result.ToName;
            }

            return Json(new
            {
                success = true,
                affected = totalAffected,
                assignedToEmail = toEmail,
                assignedToName = toName,
                message = $"{totalAffected} ticket(s) assigned to {(string.IsNullOrWhiteSpace(toName) ? toEmail : toName)}."
            });
        }

        // ── Operator performance / supervision ────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Performance(string dateFrom = null, string dateTo = null,
            string operatorEmail = null, bool includeSystem = false)
        {
            if (HttpContext.Session.GetString("email") == null)
                return RedirectToAction("Index", "Home");

            // Default window: last 30 days (IST calendar).
            var todayIst = DateTime.UtcNow.AddMinutes(330).Date;
            DateTime from = todayIst.AddDays(-29), to = todayIst;
            if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out var pf)) from = pf.Date;
            if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out var pt)) to = pt.Date;
            if (to < from) { var tmp = from; from = to; to = tmp; }

            var vm = new NaukriPerformanceVM
            {
                DateFrom = from,
                DateTo = to,
                Operator = operatorEmail,
                IncludeSystem = includeSystem
            };

            try
            {
                var data = await _repository.GetOperatorPerformanceAsync(from, to, operatorEmail, includeSystem, 1000);

                // Column order for the event matrix: most frequent event types first.
                vm.EventTypes = data.Events
                    .GroupBy(e => e.EventType)
                    .OrderByDescending(g => g.Sum(x => x.Cnt))
                    .Select(g => g.Key)
                    .ToList();

                var eventsByOp = data.Events
                    .GroupBy(e => e.Operator, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                foreach (var s in data.Summary)
                {
                    var row = new NaukriPerformanceRow
                    {
                        Operator = s.Operator,
                        TotalActions = s.TotalActions,
                        TicketsTouched = s.TicketsTouched,
                        FirstActionAt = s.FirstActionAt,
                        LastActionAt = s.LastActionAt
                    };
                    if (eventsByOp.TryGetValue(s.Operator, out var evs))
                        foreach (var ev in evs) row.EventCounts[ev.EventType] = ev.Cnt;
                    vm.Operators.Add(row);
                }

                vm.Log = data.Log.Select(l => new NaukriPerformanceLogItem
                {
                    ChangedAt = l.ChangedAt,
                    Operator = l.Operator,
                    RecordId = l.RecordId,
                    CandidateName = l.CandidateName,
                    MagName = l.MagName,
                    EventType = l.EventType,
                    FromValue = l.FromValue,
                    ToValue = l.ToValue
                }).ToList();

                vm.TotalOperators = vm.Operators.Count;
                // Use the SP's grand totals (whole window) — NOT the capped log, which undercounts
                // tickets touched, and NOT a SUM of per-operator tickets, which would double-count
                // tickets handled by more than one operator. Fall back to the log-derived count only
                // if the totals row was unavailable.
                vm.TotalActions = data.GrandTotalActions > 0
                    ? data.GrandTotalActions
                    : vm.Operators.Sum(o => o.TotalActions);
                vm.TotalTicketsTouched = data.GrandTicketsTouched > 0
                    ? data.GrandTicketsTouched
                    : data.Log.Select(l => l.RecordId).Distinct().Count();

                // For the operator filter dropdown.
                ViewBag.Assignees = await _repository.GetEligibleAssigneesAsync();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.Assignees = new List<NaukriAssigneeVM>();
                TempData["Error"] = "Failed to load performance data.";
            }

            return View(vm);
        }

        // ── Inline field update (MAG, Email, Phone) ──────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateField(int id, string fieldName, string oldValue, string newValue)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            if (id <= 0 || string.IsNullOrWhiteSpace(fieldName))
                return Json(new { success = false, error = "Invalid request." });

            var changedBy = HttpContext.Session.GetString("email").Trim('"');

            try
            {
                bool ok = false;
                switch (fieldName)
                {
                    case "MagName":
                        ok = await _repository.UpdateMagNameAsync(id, newValue?.Trim());
                        break;
                    case "ParsedEmail":
                        ok = await _repository.UpdateParsedEmailAsync(id, newValue?.Trim());
                        break;
                    case "ParsedPhone":
                        // Relay often sends numbers with hyphens / spaces / country code — sanitise
                        // to the bare national number before storing.
                        newValue = Risebird.CommonHelper.PhoneNumberHelper.StripCountryCode(newValue?.Trim());
                        ok = await _repository.UpdateParsedPhoneAsync(id, newValue);
                        break;
                    default:
                        return Json(new { success = false, error = $"Field '{fieldName}' is not editable." });
                }

                if (ok)
                {
                    string eventType = fieldName switch
                    {
                        "MagName" => "MagChange",
                        "ParsedEmail" => "EmailChange",
                        "ParsedPhone" => "PhoneChange",
                        _ => "FieldChange"
                    };
                    await _repository.InsertHistoryAsync(id, eventType, oldValue ?? "—", newValue ?? "—", changedBy);
                }
                return Json(new { success = ok });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Delete Record (AJAX — Admin / SupportAdmin only) ────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRecord(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") ?? "";
            if (profileName != "Admin" && profileName != "SupportAdmin")
                return Json(new { success = false, error = "Not authorised." });

            var ok = await _repository.DeleteAsync(id);
            if (ok)
                await _repository.InsertHistoryAsync(id, "Deleted", "—", "Record deleted", profileName);
            return Json(new { success = ok });
        }

        // ── Step 1: Parse Resume — extract email & phone, check duplicate ───────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ParseResume(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                if (record.ResumeParseCompleted == true)
                    return Json(new
                    {
                        success = true,
                        alreadyParsed = true,
                        parsedEmail = record.ParsedEmail,
                        parsedPhone = record.ParsedPhone,
                        isExistingInterviewer = record.IsExistingInterviewer,
                        existingUserId = record.ExistingUserId,
                        existingProfileId = record.ExistingProfileId
                    });

                string resumeUrl = record.ResumeLink;
                if (string.IsNullOrWhiteSpace(resumeUrl))
                    return Json(new { success = false, error = "No resume link available. Upload a resume first.", needsUpload = true });

                // For old records that still have a Google Drive URL, re-upload to blob first
                if (resumeUrl.Contains("drive.google.com", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string directUrl = ConvertToDirectDownloadUrl(resumeUrl);
                        var httpClient = _httpClientFactory.CreateClient();
                        using var dlResp = await httpClient.GetAsync(directUrl);
                        if (dlResp.IsSuccessStatusCode)
                        {
                            var bytes = await dlResp.Content.ReadAsByteArrayAsync();
                            var ct = dlResp.Content.Headers.ContentType?.MediaType ?? "";
                            bool isHtml = ct.Contains("text/html")
                                || (bytes.Length > 5 && System.Text.Encoding.UTF8
                                    .GetString(bytes, 0, Math.Min(50, bytes.Length))
                                    .TrimStart().StartsWith("<", StringComparison.Ordinal));

                            if (bytes.Length > 0 && !isHtml)
                            {
                                string ext = ".pdf";
                                if (ct.Contains("msword") || resumeUrl.EndsWith(".doc", StringComparison.OrdinalIgnoreCase)) ext = ".doc";
                                else if (ct.Contains("wordprocessingml") || resumeUrl.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)) ext = ".docx";

                                string blobName = $"NK_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
                                using var ms = new MemoryStream(bytes);
                                var blobUrl = await _azureStorageHelper.InsertBlobCloudCompare(
                                    blobName, "resumes", blobFIleStream: ms, contentType: ct);
                                if (!string.IsNullOrWhiteSpace(blobUrl))
                                {
                                    resumeUrl = blobUrl;
                                    await _repository.UpdateResumeLinkAsync(id, blobUrl);
                                }
                            }
                        }
                    }
                    catch (Exception dlEx)
                    {
                        await _logger.LogMessage(LogLevelInfo.Exception, dlEx);
                    }
                }

                string resumeText = null;
                bool isDocFile = resumeUrl.EndsWith(".doc", StringComparison.OrdinalIgnoreCase)
                              || resumeUrl.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

                if (!isDocFile)
                {
                    try { resumeText = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeUrl); }
                    catch { /* not a valid PDF — will try DOC conversion next */ }
                }

                if (string.IsNullOrWhiteSpace(resumeText))
                {
                    try
                    {
                        string convertedUrl = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(resumeUrl);
                        if (!string.IsNullOrWhiteSpace(convertedUrl))
                            resumeText = await _pdfReaderHelper.ReadPdfFromUrlAsync(convertedUrl);
                    }
                    catch { /* conversion or converted-PDF read failed */ }
                }

                if (string.IsNullOrWhiteSpace(resumeText) && isDocFile)
                {
                    try { resumeText = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeUrl); }
                    catch { /* last-resort: maybe blob extension was wrong and it's actually a PDF */ }
                }

                if (string.IsNullOrWhiteSpace(resumeText))
                    return Json(new { success = false, error = "Could not read the resume. The file may be corrupted or in an unsupported format. Try uploading a valid PDF/DOC/DOCX.", needsUpload = true });

                string parsedEmail = ExtractEmail(resumeText);
                string parsedPhone = Risebird.CommonHelper.PhoneNumberHelper.StripCountryCode(ExtractPhone(resumeText));

                var (isExisting, existingUserId, existingProfileId) =
                    await _repository.UpdateResumeParseResultAsync(id, parsedEmail, parsedPhone);

                var user = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                await _repository.InsertHistoryAsync(id, "ResumeParse", "—",
                    $"Email: {parsedEmail ?? "N/A"}, Phone: {parsedPhone ?? "N/A"}" + (isExisting ? $" (Existing UserId: {existingUserId})" : " (New)"), user);

                // Funnel: EmailParsed -> PhoneParsed -> DuplicateChecked (or terminal Duplicate).
                await _repository.AdvanceFunnelStageAsync(id, "EmailParsed", "InProgress");
                if (!string.IsNullOrWhiteSpace(parsedPhone))
                    await _repository.AdvanceFunnelStageAsync(id, "PhoneParsed", "InProgress");
                if (isExisting)
                {
                    // Email already exists in the system — exits the funnel at the duplicate-check step.
                    await _repository.SetFunnelOutcomeAsync(id, "Duplicate", "InProgress");
                }
                else if (!string.IsNullOrWhiteSpace(parsedEmail))
                {
                    await _repository.AdvanceFunnelStageAsync(id, "DuplicateChecked", "InProgress");
                }

                return Json(new
                {
                    success = true,
                    parsedEmail,
                    parsedPhone,
                    isExistingInterviewer = isExisting,
                    existingUserId,
                    existingProfileId,
                    message = isExisting
                        ? $"This interviewer already exists in the system (UserId: {existingUserId})."
                        : "Resume parsed. Interviewer is new to the system."
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = "Parse failed. The resume file could not be processed — it may be corrupted or in an unsupported format. Try uploading a valid PDF/DOC/DOCX.", needsUpload = true });
            }
        }

        // ── Step 2: Decide EV path (LinkedIn-verified prospects skip Signzy/Surepass) ────
        // The client calls this BEFORE running the client-side Signzy call. When the prospect came
        // through the "LinkedIn Verified" Relay flow (all three LinkedIn fields populated), the LinkedIn
        // background check already established a genuine, currently-employed person, so we satisfy EV
        // from that verdict here and tell the client to skip Signzy. Otherwise we tell the client to run
        // Signzy/Surepass as usual (its result is then saved via SaveEmploymentResult).

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrepareEmploymentVerification(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                // Same business rule as the Signzy path: intent must be confirmed as 'Interested'.
                if (!string.Equals(record.Intent, "Interested", StringComparison.OrdinalIgnoreCase))
                {
                    string intentMsg = string.IsNullOrEmpty(record.Intent)
                        ? "Intent has not been confirmed yet. Collect the prospect's intent (email reply or AI Plivo call) before running Employment Verification."
                        : $"Intent is '{record.Intent}'. Employment Verification can only proceed once the prospect's intent is confirmed as 'Interested'.";
                    return Json(new { success = false, error = intentMsg, intentBlocked = true });
                }

                // No LinkedIn verdict → fall back to Signzy/Surepass on the client.
                if (!HasLinkedInVerdict(record))
                    return Json(new { success = true, runSignzy = true });

                // LinkedIn verdict present → satisfy EV from it and skip Signzy.
                bool verified   = string.Equals(record.LinkedInMatch?.Trim(), "YES", StringComparison.OrdinalIgnoreCase);
                string company  = ExtractCompanyFromRole(record.CurrentRole) ?? record.EmploymentCompanyName;
                bool isEmployed = verified;
                string details  = $"Satisfied by LinkedIn background verification (match={record.LinkedInMatch}, confidence={record.LinkedInConfidence}). Profile: {record.LinkedInUrl}";

                var ok = await _repository.UpdateEmploymentVerificationAsync(id, verified, company, isEmployed, details);
                if (ok)
                {
                    var user = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                    if (verified)
                    {
                        await _repository.InsertHistoryAsync(id, "EmploymentVerification", "—",
                            $"EV skipped — satisfied by LinkedIn verification (match={record.LinkedInMatch}, confidence={record.LinkedInConfidence}). Company: {company ?? "N/A"}. Profile: {record.LinkedInUrl}", user);
                        await _repository.AdvanceFunnelStageAsync(id, "EmploymentVerified", "InProgress");
                    }
                    else
                    {
                        await _repository.InsertHistoryAsync(id, "NotQualified", "—",
                            $"LinkedIn verification did not confirm the prospect (match={record.LinkedInMatch}). Funnel → Not Qualified.", user);
                        await _repository.SetFunnelOutcomeAsync(id, "NotQualified", "Closed");
                    }
                }

                return Json(new
                {
                    success = ok,
                    autoVerifiedViaLinkedIn = true,
                    verified,
                    isEmployed,
                    companyName = company,
                    details
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// True when the prospect carries a complete LinkedIn background-verification verdict (all three
        /// LinkedIn fields populated by the "LinkedIn Verified" Relay flow). When true, EV is satisfied
        /// from LinkedIn and Signzy/Surepass is skipped; when false, EV runs normally.
        /// </summary>
        private static bool HasLinkedInVerdict(NaukriProspectiveInterviewerVM rec)
            => rec != null
               && !string.IsNullOrWhiteSpace(rec.LinkedInUrl)
               && !string.IsNullOrWhiteSpace(rec.LinkedInMatch)
               && rec.LinkedInConfidence.HasValue;

        /// <summary>
        /// Best-effort extraction of the employer name from a "designation at company" style current-role
        /// string (e.g. "Development Team Lead at RSS Softech / gistec" → "RSS Softech / gistec"). Falls
        /// back to the whole role string when no " at " delimiter is present.
        /// </summary>
        private static string ExtractCompanyFromRole(string currentRole)
        {
            if (string.IsNullOrWhiteSpace(currentRole)) return null;
            int idx = currentRole.LastIndexOf(" at ", StringComparison.OrdinalIgnoreCase);
            string company = idx >= 0 ? currentRole.Substring(idx + 4) : currentRole;
            company = company?.Trim();
            return string.IsNullOrWhiteSpace(company) ? null : company;
        }

        // ── Step 2: Verify Employment (save result from client-side Signzy call) ────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveEmploymentResult(int id, bool verified, string companyName, bool isEmployed, string details)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                // Business rule: Intent must be CONFIRMED (and positive) before Employment Verification starts.
                var record = await _repository.GetByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                if (!string.Equals(record.Intent, "Interested", StringComparison.OrdinalIgnoreCase))
                {
                    string intentMsg = string.IsNullOrEmpty(record.Intent)
                        ? "Intent has not been confirmed yet. Collect the prospect's intent (email reply or AI Plivo call) before running Employment Verification."
                        : $"Intent is '{record.Intent}'. Employment Verification can only proceed once the prospect's intent is confirmed as 'Interested'.";
                    return Json(new { success = false, error = intentMsg, intentBlocked = true });
                }

                var ok = await _repository.UpdateEmploymentVerificationAsync(id, verified, companyName, isEmployed, details);
                if (ok)
                {
                    var user = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                    string evSummary = $"Company: {companyName ?? "N/A"}, Employed: {(isEmployed ? "Yes" : "No")}";

                    // EV qualifies ONLY when verification ran successfully AND the prospect is currently
                    // employed. A failed verification OR isEmployed = false → terminal 'Not Qualified'.
                    bool qualified = verified && isEmployed;
                    if (qualified)
                    {
                        await _repository.InsertHistoryAsync(id, "EmploymentVerification", "—", $"{evSummary} — PASSED", user);
                        await _repository.AdvanceFunnelStageAsync(id, "EmploymentVerified", "InProgress");
                    }
                    else
                    {
                        string reason = !verified ? "verification could not confirm a qualifying employer"
                                                  : "prospect is not currently employed";
                        await _repository.InsertHistoryAsync(id, "NotQualified", "—",
                            $"{evSummary} — Employment Verification FAILED ({reason}). Funnel → Not Qualified.", user);
                        await _repository.SetFunnelOutcomeAsync(id, "NotQualified", "Closed");
                    }
                }
                return Json(new { success = ok, verified, isEmployed });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Step 3: Analyze Resume (AI) ─────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnalyzeResume(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                if (record.ResumeAIAnalysisCompleted == true)
                    return Json(new { success = true, alreadyAnalyzed = true, analysisResult = record.ResumeAIAnalysisResult });

                string resumeUrl = record.ResumeLink;
                if (string.IsNullOrWhiteSpace(resumeUrl))
                    return Json(new { success = false, error = "No resume link.", needsUpload = true });

                string resumeText = null;
                try { resumeText = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeUrl); }
                catch { }

                if (string.IsNullOrWhiteSpace(resumeText))
                {
                    try
                    {
                        string convertedUrl = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(resumeUrl);
                        if (!string.IsNullOrWhiteSpace(convertedUrl))
                            resumeText = await _pdfReaderHelper.ReadPdfFromUrlAsync(convertedUrl);
                    }
                    catch { }
                }

                if (string.IsNullOrWhiteSpace(resumeText))
                    return Json(new { success = false, error = "Could not read resume. The file may be corrupted or unsupported. Try uploading a valid PDF.", needsUpload = true });

                string specialization = record.AssessmentGroupName ?? record.MagName ?? record.KeySkills ?? "General";

                var aiResponse = await _assistantServiceHelper.GenerateResumeAnalysis(resumeText, specialization);
                if (aiResponse == null)
                    return Json(new { success = false, error = "AI analysis returned no response." });

                var analysisJson = aiResponse.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value;
                if (string.IsNullOrWhiteSpace(analysisJson))
                    return Json(new { success = false, error = "AI analysis returned empty result." });

                var analysis = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(analysisJson);
                bool isPositive = analysis != null
                    && !string.IsNullOrEmpty(analysis.verdict)
                    && analysis.verdict.ToLower().Contains("yes");

                int? score = analysis?.fitmentScore;

                string newStatus = isPositive ? "Verified" : "InProgress";

                await _repository.UpdateAIAnalysisAsync(id, true, analysisJson, analysis?.verdict, score, newStatus);

                var aiUser = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                await _repository.InsertHistoryAsync(id, "ResumeAnalysis", "—",
                    $"Verdict: {analysis?.verdict ?? "N/A"}, Score: {score?.ToString() ?? "N/A"}", aiUser);

                await _repository.AdvanceFunnelStageAsync(id, "ResumeFitment", "InProgress");

                return Json(new
                {
                    success = true,
                    verdict = analysis?.verdict,
                    fitmentScore = analysis?.fitmentScore,
                    completeFitmentAnalysis = analysis?.completeFitmentAnalysis,
                    isPositiveVerdict = isPositive,
                    analysisResult = analysisJson
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = "Analysis failed: " + ex.Message });
            }
        }

        // ── Step 4: Onboard Interviewer ─────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnboardInterviewer(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                if (record.OnboardingCompleted == true)
                    return Json(new { success = true, alreadyOnboarded = true, message = "Already onboarded." });

                // NOTE: an "existing interviewer" is NOT blocked outright here anymore. If the existing
                // account is already ACTIVE we block in the branch below (a genuine live interviewer);
                // if it exists but is INACTIVE (e.g. a prior MAG-direct add that was never activated)
                // we ADOPT it so onboarding can complete instead of silently dead-ending the funnel.

                if (string.IsNullOrWhiteSpace(record.ParsedEmail))
                    return Json(new { success = false, error = "Email not parsed yet. Complete Step 1 first." });

                // Use the assessment group the operator explicitly CONFIRMED / locked (stored in
                // AssessmentGroupId by ConfirmMag / SelectSkillChannel — it is also the AG the MAG
                // application link was built from). Only fall back to the AG re-derived from the MAG
                // NAME text when no confirmed AG is present. Re-deriving from the name text here was a
                // bug: if the parsed MAG text resolved to a different AG than the one the operator
                // confirmed, the interviewer got mapped to the wrong role (e.g. onboarded for
                // "Salesforce Developer" but mapped under "Salesforce CRM Admin").
                int agId = (record.AssessmentGroupId.HasValue && record.AssessmentGroupId.Value > 0)
                    ? record.AssessmentGroupId.Value
                    : (record.ResolvedAssessmentGroupId ?? 0);

                if (agId <= 0)
                    return Json(new { success = false, error = $"No assessment group found for MAG: '{record.MagName}'. Confirm the MAG first." });

                string loggedInUser = HttpContext.Session.GetString("email").Trim('"');

                string candidateName = (record.CandidateName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(candidateName)) candidateName = "Interviewer";
                string[] nameParts = candidateName.Split(' ', 2);
                string firstName = nameParts[0];
                string lastName = nameParts.Length > 1 ? nameParts[1] : "";

                if (firstName.Length < 2) firstName = firstName.PadRight(2, 'x');

                decimal experience = 0;
                if (!string.IsNullOrWhiteSpace(record.ExperienceYears))
                {
                    var numMatch = Regex.Match(record.ExperienceYears, @"(\d+)");
                    if (numMatch.Success) decimal.TryParse(numMatch.Value, out experience);
                }

                string phone = record.ParsedPhone ?? "";
                if (phone.Length > 0 && phone.Length < 10)
                    phone = phone.PadRight(10, '0');

                int userId;
                int? profileId;
                string uniqueid = null;

                if (record.IsExistingInterviewer == true)
                {
                    // ── ADOPT an existing interviewer ──────────────────────────────────────────
                    // The prospect's email already maps to an interviewer account, so we must NOT
                    // re-create or overwrite that profile (doing so would clobber their real data via
                    // usp_AddOrEditInterviewerProfile's UPDATE path). Two sub-cases:
                    //   • already ACTIVE   → a genuine live interviewer; block the re-onboard.
                    //   • currently INACTIVE → an account that was never activated (e.g. a MAG-direct
                    //     add). Adopt it: map them to this MAG (channels available-only / inactive),
                    //     link the prospect, send the acknowledgement, and let acceptance activate them.
                    var existing = await _repository.FindInterviewerByEmailAnyStatusAsync(record.ParsedEmail);
                    if (!existing.UserId.HasValue || existing.UserId.Value <= 0)
                        return Json(new { success = false, error = $"Flagged as an existing interviewer, but no account was found for {record.ParsedEmail}." });

                    if (existing.IsActive == true)
                        return Json(new { success = false, error = "This interviewer is already ACTIVE in the system. Cannot re-onboard." });

                    userId = existing.UserId.Value;
                    profileId = existing.ProfileId ?? await _repository.GetInterviewerProfileIdByUserIdAsync(userId);
                    if (!profileId.HasValue || profileId.Value <= 0)
                        return Json(new { success = false, error = "Could not resolve the existing interviewer profile to onboard." });

                    // Map to this MAG WITHOUT touching profile fields: appends the AG id (so they
                    // surface in the MAG members list once active) and adds the AG's skill channels
                    // as available-only / inactive, pending manual operator activation.
                    await _repository.MapInterviewerToAssessmentGroupChannelsAsync(profileId.Value, userId, agId, loggedInUser);
                }
                else
                {
                    // ── CREATE a new interviewer account (INACTIVE) ────────────────────────────
                    var profileVM = new InterviewerProfileVM
                    {
                        EmailId = record.ParsedEmail.Trim().ToLower(),
                        FirstName = firstName,
                        LastName = lastName,
                        MobileNumber = phone,
                        Country = "26",
                        City = record.Location ?? "",
                        Experience = experience,
                        CurrentRole = record.CurrentRole ?? "",
                        ResumeLink = record.ResumeLink ?? "",
                        Gender = "Male",
                        AssessmentGroupId = agId.ToString(),
                        AssessmentGroupArray = new[] { agId.ToString() },
                        PreviousAssessmentGroupArray = new string[0],
                        AssessmentGroupDifference = agId.ToString(),
                        RemovedAssessmentGroups = "",
                        PrimarySpecializationForAcknowledgement = record.AssessmentGroupName ?? record.MagName ?? "",
                        // Create the interviewer account INACTIVE. Onboarding is gated on the prospect
                        // accepting the acknowledgement email: the "Yes/Accept" button hits
                        // InterviewerAcknowledgementResponse (status=1), which activates the UserList
                        // account (usp_UpdateInterviewerActiveStatus) and advances the funnel to 'Onboarded'.
                        // Setting IsActive = true here would make them a live, active interviewer the moment
                        // the profile is created — i.e. "onboarded without acknowledging" — which is the bug.
                        IsActive = false,
                        Isavailable = 1,
                        CompanyName = record.EmploymentCompanyName ?? "",
                        IsEmployed = record.EmploymentIsEmployed == true,
                        ResourceUrl = $"https://{Request.Host}/NaukriOnboarding/Index",
                        // This action sends its own richer acknowledgement email (both Accept/Drop
                        // buttons, correct host & AG names) below, so suppress the service's default
                        // one to avoid the prospect receiving two acknowledgement mails.
                        SuppressAcknowledgementEmail = true
                    };

                    bool addResult;
                    string assessmentGroupList = null;
                    int createdUserId = 0;
                    try
                    {
                        addResult = _interviewerService.AddInterviewerProfile(
                            profileVM, loggedInUser,
                            out assessmentGroupList, out createdUserId, out uniqueid);
                    }
                    catch (Exception svcEx)
                    {
                        await _logger.LogMessage(LogLevelInfo.Exception, svcEx);
                        return Json(new { success = false, error = $"Failed to create interviewer profile: {svcEx.Message}" });
                    }

                    if (!addResult || createdUserId <= 0)
                    {
                        string errMsg;
                        if (!string.IsNullOrEmpty(assessmentGroupList))
                        {
                            errMsg = $"AG capacity issue: {assessmentGroupList}";
                        }
                        else
                        {
                            var innerEx = _interviewerService.LastAddProfileException;
                            string detail = innerEx != null ? innerEx.Message : "Unknown reason";
                            errMsg = $"Failed to create interviewer profile. Reason: {detail} [Email: {profileVM.EmailId}, Phone: {profileVM.MobileNumber}, AG: {agId}]";
                        }
                        return Json(new { success = false, error = errMsg });
                    }

                    userId = createdUserId;
                    profileId = await _repository.GetInterviewerProfileIdByUserIdAsync(userId);
                }

                bool ackSent = false;
                try
                {
                    string yesButton = $"https://{Request.Host}/Interviewer/InterviewerAcknowledgementResponse?status=1&user={userId}";
                    string dropButton = $"https://{Request.Host}/Interviewer/InterviewerAcknowledgementResponse?status=4&user={userId}";
                    string agNames = record.AssessmentGroupName ?? record.MagName ?? "";

                    await _notificationHelper.SendAcknowledgementEmailToInterviewer(
                        record.ParsedEmail, record.CandidateName ?? firstName,
                        yesButton, dropButton, agNames,
                        record.AssessmentGroupName ?? record.MagName ?? "",
                        uniqueid ?? "");

                    ackSent = true;
                }
                catch (Exception ackEx)
                {
                    await _logger.LogMessage(LogLevelInfo.Exception, ackEx);
                }

                await _repository.UpdateOnboardingResultAsync(id, userId, profileId, agId, ackSent);

                // Onboarding type for reporting. This action is operator-initiated, so the canonical
                // type is 'ManuallyOnboarded' — even if automation had touched the record earlier
                // (the fully-automated path is tagged 'IntentAutoProcessed' by the automation job itself).
                // A self-registered prospect (rare here) is still attributed to 'SelfOnboarded'.
                string onboardingType = record.SelfRegistrationCompleted == true ? "SelfOnboarded"
                    : "ManuallyOnboarded";
                await _repository.SetOnboardingTypeAsync(id, onboardingType);

                // Business rule: the acknowledgement email is a MANDATORY stage, and onboarding is only
                // COMPLETE once the interviewer RESPONDS (accepts) to it. Sending the email advances the
                // funnel to 'AcknowledgementSent' and keeps the ticket open (awaiting the response);
                // the funnel reaches 'Onboarded' only in InterviewerAcknowledgementResponse (status=1).
                if (ackSent)
                {
                    await _repository.AdvanceFunnelStageAsync(id, "AcknowledgementSent", "InProgress");
                    await _repository.InsertHistoryAsync(id, "AcknowledgementSent", "—",
                        $"UserId: {userId}, AG: {agId}, Type: {onboardingType}. Acknowledgement email sent — awaiting interviewer's acceptance to complete onboarding.", loggedInUser);
                }
                else
                {
                    await _repository.AdvanceFunnelStageAsync(id, "ResumeFitment", "InProgress");
                    await _repository.InsertHistoryAsync(id, "AcknowledgementPending", "—",
                        $"UserId: {userId}, AG: {agId}, Type: {onboardingType}. Profile created but acknowledgement email FAILED — use Resend to send it.", loggedInUser);
                }

                return Json(new
                {
                    success = true,
                    userId,
                    uniqueid,
                    assessmentGroupId = agId,
                    acknowledgementEmailSent = ackSent,
                    onboardingComplete = false,
                    awaitingAcknowledgement = ackSent,
                    message = ackSent
                        ? $"{(record.IsExistingInterviewer == true ? "Existing interviewer" : "Profile created")} (UserId: {userId}) and mapped to AG + channels. Acknowledgement email sent — onboarding completes once the interviewer accepts it."
                        : $"{(record.IsExistingInterviewer == true ? "Existing interviewer" : "Profile created")} (UserId: {userId}) and mapped to AG + channels, but the acknowledgement email FAILED. Use the Resend button to send it."
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = "Onboarding failed: " + ex.Message });
            }
        }

        // ── Resend Acknowledgement Email ────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendAcknowledgement(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                if (record.OnboardingCompleted != true || !record.OnboardedUserId.HasValue)
                    return Json(new { success = false, error = "Interviewer not yet onboarded." });

                int userId = record.OnboardedUserId.Value;
                string yesButton = $"https://{Request.Host}/Interviewer/InterviewerAcknowledgementResponse?status=1&user={userId}";
                string dropButton = $"https://{Request.Host}/Interviewer/InterviewerAcknowledgementResponse?status=4&user={userId}";
                string agNames = record.AssessmentGroupName ?? record.MagName ?? "";

                await _notificationHelper.SendAcknowledgementEmailToInterviewer(
                    record.ParsedEmail ?? record.CandidateName,
                    record.CandidateName ?? "Interviewer",
                    yesButton, dropButton, agNames,
                    record.AssessmentGroupName ?? record.MagName ?? "",
                    "");

                await _repository.UpdateAckEmailSentAsync(id);

                var ackUser = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                await _repository.InsertHistoryAsync(id, "AckEmailResent", "—", "Acknowledgement email resent", ackUser);

                // The acknowledgement email is (re)sent — advance to 'AcknowledgementSent' and keep the
                // ticket open. Onboarding completes only when the interviewer accepts it (status=1),
                // which is handled in InterviewerAcknowledgementResponse.
                await _repository.AdvanceFunnelStageAsync(id, "AcknowledgementSent", "InProgress");
                if (string.IsNullOrWhiteSpace(record.OnboardingType))
                {
                    string onboardingType = record.SelfRegistrationCompleted == true ? "SelfOnboarded"
                        : "ManuallyOnboarded";
                    await _repository.SetOnboardingTypeAsync(id, onboardingType);
                }

                return Json(new { success = true, message = "Acknowledgement email sent successfully. Onboarding completes once the interviewer accepts it." });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = "Failed to send email: " + ex.Message });
            }
        }

        // ── Get all MAGs for searchable dropdown ──────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetAllMags()
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var mags = await _repository.GetAllMasterAssessmentGroupsAsync();
                return Json(new { success = true, data = mags });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Priority intent-collection MAGs ───────────────────────────────────────
        // Operators flag up to MaxPriorityMags MAGs per day. The parallel PriorityIntentCallJob
        // (Azure Function) then calls prospects mapped to these MAGs that are stuck at the
        // InterestCollection stage, bypassing the usual anti-spam caps. Flags reset daily (IST).

        private const int MaxPriorityMags = 2;

        [HttpGet]
        public async Task<IActionResult> GetPriorityMags()
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var mags = await _repository.GetPriorityMagsAsync();
                return Json(new { success = true, max = MaxPriorityMags, data = mags });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPriorityMag(int assessmentGroupId, string magName)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            if (assessmentGroupId <= 0 || string.IsNullOrWhiteSpace(magName))
                return Json(new { success = false, error = "A valid MAG is required." });

            try
            {
                var user = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                var result = await _repository.SetPriorityMagAsync(assessmentGroupId, magName.Trim(), user, MaxPriorityMags);

                if (string.Equals(result.Status, "LimitReached", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, status = result.Status, count = result.PriorityCount,
                        error = $"You can prioritise at most {MaxPriorityMags} MAGs at a time. Clear one first." });

                var mags = await _repository.GetPriorityMagsAsync();
                return Json(new { success = true, status = result.Status, count = result.PriorityCount, data = mags });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearPriorityMag(int assessmentGroupId)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                await _repository.ClearPriorityMagAsync(assessmentGroupId);
                var mags = await _repository.GetPriorityMagsAsync();
                return Json(new { success = true, data = mags });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Get active skill channels (IRFs) for a given assessment group ─────────

        [HttpGet]
        public async Task<IActionResult> GetActiveChannels(int assessmentGroupId)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });
            try
            {
                var channels = await _repository.GetActiveIRFsByAssessmentGroupIdAsync(assessmentGroupId);
                return Json(new { success = true, data = channels });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Select IRF / Skill Channel (Step 2 of new pipeline) ─────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectSkillChannel(int id, int irfId, string irfName, int assessmentGroupId)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });
            try
            {
                var encryptedId = await _encryptionHelper.EncryptStringValue(irfId.ToString());
                var appLink = $"{Request.Scheme}://{Request.Host}/Interviewer/ApplicationForm?id={System.Web.HttpUtility.UrlEncode(encryptedId)}";

                await _repository.UpdateSelectedIRFAsync(id, irfId, irfName, appLink, assessmentGroupId);

                var user = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                await _repository.InsertHistoryAsync(id, "SkillChannelSelected", "\u2014",
                    $"IRF: {irfName} (id:{irfId}), AG: {assessmentGroupId}", user);

                await _repository.AdvanceFunnelStageAsync(id, "MAGConfirmed", "InProgress");

                return Json(new { success = true, applicationLink = appLink, message = "Skill channel selected." });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Confirm MAG (funnel step 4) ─────────────────────────────────────────
        // Persists the operator's MAG choice, resolves & locks the assessment group +
        // MAG application link, and advances the funnel to 'MAGConfirmed'. The optional
        // assessmentGroupId lets the caller pass the exact AG picked in the dropdown; when
        // omitted (0) we fall back to the AG resolved from the MAG name on the record.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmMag(int id, string magName, int assessmentGroupId = 0)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            if (id <= 0 || string.IsNullOrWhiteSpace(magName))
                return Json(new { success = false, error = "MAG name is required." });

            try
            {
                var user = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";

                var record = await _repository.GetByIdAsync(id);
                if (record == null) return Json(new { success = false, error = "Record not found." });

                // Persist the chosen MAG name if it changed.
                if (!string.Equals(record.MagName ?? "", magName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    await _repository.UpdateMagNameAsync(id, magName.Trim());
                    await _repository.InsertHistoryAsync(id, "MagChange", record.MagName ?? "—", magName.Trim(), user);
                }

                // Resolve the assessment group: prefer the explicit dropdown value, else the one
                // resolved from the MAG name (recomputed after the name update).
                int agId = assessmentGroupId;
                if (agId <= 0)
                {
                    var refreshed = await _repository.GetByIdAsync(id);
                    agId = refreshed?.ResolvedAssessmentGroupId ?? 0;
                }

                if (agId <= 0)
                    return Json(new { success = false, error = "Could not resolve an assessment group for the selected MAG. Check the MAG name." });

                // Lock the MAG application link to the resolved AG.
                var encryptedAgId = await _encryptionHelper.EncryptStringValue(agId.ToString());
                var appLink = $"https://portal.risebird.io/Interviewer/MagApplicationForm?id={Uri.EscapeDataString(encryptedAgId)}";
                await _repository.UpdateSelectedIRFAsync(id, 0, null, appLink, agId);

                await _repository.InsertHistoryAsync(id, "MagConfirmed", "—",
                    $"MAG confirmed: {magName.Trim()} (AG: {agId}).", user);
                await _repository.AdvanceFunnelStageAsync(id, "MAGConfirmed", "InProgress");

                return Json(new
                {
                    success = true,
                    assessmentGroupId = agId,
                    applicationLink = appLink,
                    message = $"MAG confirmed: {magName.Trim()}."
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Invitation email delivery status (SendGrid) for the EmailParsed step ────
        [HttpGet]
        public async Task<IActionResult> GetInvitationEmailStatus(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetByIdAsync(id);
                if (record == null) return Json(new { success = false, error = "Record not found." });

                if (string.IsNullOrWhiteSpace(record.ParsedEmail))
                    return Json(new { success = true, hasEmail = false, sent = false });

                var st = await _repository.GetInvitationEmailStatusAsync(record.ParsedEmail);
                if (st == null || st.SentAt == null)
                    return Json(new { success = true, hasEmail = true, email = record.ParsedEmail, sent = false });

                return Json(new
                {
                    success = true,
                    hasEmail = true,
                    email = record.ParsedEmail,
                    sent = true,
                    sentAt = st.SentAt?.ToString("dd MMM yyyy, HH:mm"),
                    deliveryEvent = st.DeliveryEvent,
                    deliveryReason = st.DeliveryReason,
                    deliveryStatus = st.DeliveryStatus,
                    hasBounced = st.HasBounced,
                    opened = st.Opened,
                    clicked = st.Clicked
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── SendGrid events for BOTH the invitation and acknowledgement emails ──────
        [HttpGet]
        public async Task<IActionResult> GetProspectEmailEvents(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetByIdAsync(id);
                if (record == null) return Json(new { success = false, error = "Record not found." });

                if (string.IsNullOrWhiteSpace(record.ParsedEmail))
                    return Json(new { success = true, hasEmail = false });

                var data = await _repository.GetProspectEmailEventsAsync(record.ParsedEmail);

                Func<string, object> shape = type =>
                {
                    var s = data.Summaries?.FirstOrDefault(x =>
                        string.Equals(x.EmailType, type, StringComparison.OrdinalIgnoreCase));
                    if (s == null || s.SentAt == null) return new { sent = false };

                    var evs = (data.Events ?? new System.Collections.Generic.List<NaukriEmailEvent>())
                        .Where(e => string.Equals(e.EmailType, type, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(e => e.EventAt)
                        .Select(e => new
                        {
                            ev = e.Event,
                            reason = e.Reason,
                            status = e.Status,
                            response = e.Response,
                            url = e.Url,
                            at = e.EventAt?.ToString("dd MMM yyyy, HH:mm")
                        }).ToList();

                    return new
                    {
                        sent = true,
                        subject = s.MailSubject,
                        sentAt = s.SentAt?.ToString("dd MMM yyyy, HH:mm"),
                        deliveryEvent = s.DeliveryEvent,
                        deliveryReason = s.DeliveryReason,
                        deliveryStatus = s.DeliveryStatus,
                        hasBounced = s.HasBounced,
                        opened = s.Opened,
                        clicked = s.Clicked,
                        events = evs
                    };
                };

                return Json(new
                {
                    success = true,
                    hasEmail = true,
                    email = record.ParsedEmail,
                    invitation = shape("Invitation"),
                    acknowledgement = shape("Acknowledgement")
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Send Invitation Email (Step 3 of new pipeline) ──────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvitationEmail(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });
            try
            {
                var record = await _repository.GetByIdAsync(id);
                if (record == null) return Json(new { success = false, error = "Record not found." });

                string email = record.ParsedEmail;
                if (string.IsNullOrWhiteSpace(email))
                    return Json(new { success = false, error = "Prospect email not available. Parse the resume first." });

                if (!record.ResolvedAssessmentGroupId.HasValue || record.ResolvedAssessmentGroupId.Value <= 0)
                    return Json(new { success = false, error = "No MAG resolved for this prospect. Ensure the MAG name is set." });

                int agId = record.ResolvedAssessmentGroupId.Value;
                var encryptedAgId = await _encryptionHelper.EncryptStringValue(agId.ToString());
                var appLink = $"https://portal.risebird.io/Interviewer/MagApplicationForm?id={Uri.EscapeDataString(encryptedAgId)}";

                await _repository.UpdateSelectedIRFAsync(id, 0, null, appLink, agId);

                string candidateName = record.CandidateName ?? "there";
                string magName = record.MagName ?? record.AssessmentGroupName ?? "Interviewer";

                string responseBaseUrl = $"{Request.Scheme}://{Request.Host}/NaukriOnboarding/ProspectResponseAction";
                string needClarityLink = $"{responseBaseUrl}?id={id}&response=NeedClarity";
                string manualSupportLink = $"{responseBaseUrl}?id={id}&response=ManualSupport";
                string notInterestedLink = $"{responseBaseUrl}?id={id}&response=NotInterested";

                string htmlBody = BuildInvitationEmailHtml(candidateName, magName, appLink, needClarityLink, manualSupportLink, notInterestedLink);

                var mailModel = new Mail
                {
                    TO = email,
                    Subject = $"Risebird Interviewer Opportunity \u2014 {magName}",
                    Body = htmlBody
                };
                var result = await _notificationHelper.SendHelpsupportMail(mailModel);

                await _repository.UpdateInvitationEmailSentAsync(id);
                var user = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                await _repository.InsertHistoryAsync(id, "InvitationEmailSent", "\u2014",
                    $"Sent to: {email}, MAG: {magName} (AG: {agId}), Link: {appLink}", user);

                // Invitation sent → 'Interest Collection' (awaiting email reply or AI Plivo call).
                await _repository.AdvanceFunnelStageAsync(id, "InterestCollection", "InProgress");

                return Json(new { success = true, message = "Invitation email sent to " + email + ".", applicationLink = appLink });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = "Failed to send invitation email: " + ex.Message });
            }
        }

        // ── Prospect response action (public endpoint — clicked from email) ─────

        // Email-client / security scanners (Microsoft Safe Links, Mimecast, Proofpoint, Gmail, etc.)
        // auto-fetch every link in an email. The response buttons were plain GET endpoints with
        // side-effects, so a prefetch of the 'Not Interested' link would terminate & CLOSE the ticket
        // (and a prefetch of NeedClarity/ManualSupport would kick off auto-onboarding) without the
        // prospect ever clicking. The GET now only renders a confirmation page; the actual state change
        // happens on the POST below, which scanners do not issue.
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ProspectResponseAction(int id, string response)
        {
            var validResponses = new[] { "NeedClarity", "ManualSupport", "NotInterested" };
            if (id <= 0 || string.IsNullOrWhiteSpace(response) || !validResponses.Contains(response))
                return Content("Invalid request.", "text/html");

            string prompt = response switch
            {
                "NeedClarity"   => "You'd like more details about this opportunity. Confirm to let our team know.",
                "ManualSupport" => "You'd like our onboarding team to help you register. Confirm to let us know.",
                "NotInterested" => "You're letting us know you're not interested. Confirm your response.",
                _               => "Confirm your response."
            };
            string buttonLabel = response == "NotInterested" ? "Confirm" : "Confirm";
            string buttonColor = response == "NotInterested" ? "#c62828" : "#2e7d32";
            string actionUrl   = Url.Action("ProspectResponseAction", "NaukriOnboarding");

            string confirmHtml = $@"<!DOCTYPE html><html><head><meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1'>
                <meta name='robots' content='noindex,nofollow'><title>Risebird</title>
                <style>body{{font-family:Arial,sans-serif;display:flex;justify-content:center;align-items:center;min-height:100vh;background:#f5f7fa;margin:0;}}
                .card{{background:#fff;border-radius:12px;padding:40px;max-width:480px;text-align:center;box-shadow:0 4px 20px rgba(0,0,0,0.1);}}
                h2{{color:#2c3e50;margin-top:0;}} p{{color:#555;line-height:1.6;}}
                button{{background:{buttonColor};color:#fff;border:none;border-radius:8px;padding:14px 28px;font-size:16px;cursor:pointer;margin-top:16px;}}</style></head>
                <body><div class='card'><h2>One more step</h2><p>{prompt}</p>
                <form method='post' action='{actionUrl}'>
                    <input type='hidden' name='id' value='{id}' />
                    <input type='hidden' name='response' value='{System.Net.WebUtility.HtmlEncode(response)}' />
                    <button type='submit'>{buttonLabel}</button>
                </form></div></body></html>";
            return Content(confirmHtml, "text/html");
        }

        [AllowAnonymous]
        [HttpPost]
        [ActionName("ProspectResponseAction")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ProspectResponseActionConfirm(int id, string response)
        {
            try
            {
                if (id <= 0 || string.IsNullOrWhiteSpace(response))
                    return Content("Invalid request.");

                var validResponses = new[] { "NeedClarity", "ManualSupport", "NotInterested" };
                if (!validResponses.Contains(response))
                    return Content("Invalid response type.");

                await _repository.UpdateProspectResponseAsync(id, response);
                await _repository.InsertHistoryAsync(id, "ProspectResponse", "\u2014",
                    $"Prospect chose: {response}", "Prospect");

                // Unified Intent — email reply feeds the same single Intent as the AI Plivo call.
                string emailIntent = MapEmailResponseToIntent(response);
                if (!string.IsNullOrEmpty(emailIntent))
                {
                    await _repository.SetUnifiedIntentAsync(id, emailIntent, "Email", DateTime.UtcNow);
                    await ApplyIntentToFunnelAsync(id, emailIntent, "Email");
                }

                string message = response switch
                {
                    "NeedClarity" => "Thank you for your response! Our team will reach out to you shortly to provide more details about the opportunity.",
                    "ManualSupport" => "Thank you! Our onboarding team will contact you to assist with the registration process.",
                    "NotInterested" => "Thank you for letting us know. We respect your decision and hope to connect in the future.",
                    _ => "Thank you for your response."
                };

                return Content($@"
                    <html><head><title>Risebird</title>
                    <style>body{{font-family:Arial,sans-serif;display:flex;justify-content:center;align-items:center;min-height:100vh;background:#f5f7fa;margin:0;}}
                    .card{{background:#fff;border-radius:12px;padding:40px;max-width:500px;text-align:center;box-shadow:0 4px 20px rgba(0,0,0,0.1);}}
                    h2{{color:#2c3e50;}} p{{color:#555;line-height:1.6;}}</style></head>
                    <body><div class='card'><h2>Response Recorded</h2><p>{message}</p></div></body></html>", "text/html");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Content("Something went wrong. Please try again later.");
            }
        }

        // ── Check self-registration progress (polls UserList for the prospect's email) ──

        [HttpGet]
        public async Task<IActionResult> CheckSelfRegistration(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });
            try
            {
                var record = await _repository.GetByIdAsync(id);
                if (record == null) return Json(new { success = false, error = "Record not found." });

                if (record.SelfRegistrationCompleted == true)
                    return Json(new { success = true, completed = true, userId = record.OnboardedUserId, profileId = record.OnboardedProfileId });

                if (string.IsNullOrWhiteSpace(record.ParsedEmail))
                    return Json(new { success = true, completed = false, reason = "No email to check." });

                // Only a genuine self-registration counts: an active interviewer mapped to the CONFIRMED
                // assessment group. Matching the email against any active UserList row falsely flagged
                // pre-existing interviewers in other MAGs (and non-interviewer accounts) as self-registered,
                // linking the prospect to the wrong account and marking it Onboarded under the wrong role.
                int agId = (record.AssessmentGroupId.HasValue && record.AssessmentGroupId.Value > 0)
                    ? record.AssessmentGroupId.Value
                    : (record.ResolvedAssessmentGroupId ?? 0);
                if (agId <= 0)
                    return Json(new { success = true, completed = false, reason = "Confirm the MAG first, then check self-registration." });

                var (foundUserId, foundProfileId) = await _repository.FindSelfRegisteredInterviewerForMagAsync(record.ParsedEmail, agId);

                if (foundUserId.HasValue && foundUserId.Value > 0)
                {
                    await _repository.UpdateSelfRegistrationAsync(id, true, foundUserId.Value, foundProfileId);
                    await _repository.SetOnboardingTypeAsync(id, "SelfOnboarded");
                    await _repository.AdvanceFunnelStageAsync(id, "Onboarded", "Closed");
                    var sysUser = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                    await _repository.InsertHistoryAsync(id, "SelfRegistered", "\u2014",
                        $"Prospect self-registered via MAG link. UserId: {foundUserId.Value}, AG: {agId}", sysUser);
                    return Json(new { success = true, completed = true, userId = foundUserId.Value, profileId = foundProfileId });
                }

                return Json(new { success = true, completed = false, reason = "Prospect has not yet completed self-registration." });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = "Failed to check self-registration status. Please try again." });
            }
        }

        // ── Save manual override (MAG, email, phone) with audit ──────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveManualOverride(int id, string fieldName, string oldValue, string newValue)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            var allowedFields = new[] { "MagName", "ParsedEmail", "ParsedPhone" };
            if (string.IsNullOrWhiteSpace(fieldName) || !Array.Exists(allowedFields, f => f == fieldName))
                return Json(new { success = false, error = "Invalid field." });

            if (string.IsNullOrWhiteSpace(newValue))
                return Json(new { success = false, error = "New value cannot be empty." });

            // Sanitise phone numbers (strip hyphens / spaces / country code) to the bare national number.
            if (fieldName == "ParsedPhone")
                newValue = Risebird.CommonHelper.PhoneNumberHelper.StripCountryCode(newValue.Trim());

            try
            {
                var changedBy = HttpContext.Session.GetString("email").Trim('"');
                var result = await _repository.SaveManualOverrideAsync(id, fieldName, oldValue, newValue.Trim(), changedBy);
                if (result == null)
                    return Json(new { success = false, error = "Update failed." });

                string eventType = fieldName == "MagName" ? "MagChange"
                    : fieldName == "ParsedEmail" ? "EmailChange" : "PhoneChange";
                await _repository.InsertHistoryAsync(id, eventType, oldValue ?? "—", newValue.Trim(), changedBy);

                if (fieldName == "ParsedEmail")
                {
                    bool isEx = false;
                    int? exUserId = null, exProfileId = null;
                    try
                    {
                        isEx = (bool)(result.IsExisting ?? false);
                        exUserId = (int?)result.ExistingUserId;
                        exProfileId = (int?)result.ExistingProfileId;
                    }
                    catch { }

                    // Keep the funnel in sync with the corrected email so the prospect isn't stranded:
                    //  • a now-duplicate email exits at the terminal 'Duplicate' stage;
                    //  • correcting a previously-duplicate prospect to a new (non-duplicate) email
                    //    re-enters the pipeline at 'DuplicateChecked' (Duplicate is terminal/excluded
                    //    from automation, so without this it would stay stuck out of the funnel).
                    var rec = await _repository.GetByIdAsync(id);
                    if (isEx)
                    {
                        if (!string.Equals(rec?.FunnelStage, "Duplicate", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(rec?.FunnelStage, "Onboarded", StringComparison.OrdinalIgnoreCase))
                            await _repository.SetFunnelOutcomeAsync(id, "Duplicate", "InProgress");
                    }
                    else if (string.Equals(rec?.FunnelStage, "Duplicate", StringComparison.OrdinalIgnoreCase))
                    {
                        await _repository.SetFunnelOutcomeAsync(id, "DuplicateChecked", "InProgress");
                        await _repository.InsertHistoryAsync(id, "DuplicateCleared", "Duplicate",
                            "Email corrected to a non-duplicate address — prospect re-enters the pipeline at Duplicate Checked.", changedBy);
                    }

                    return Json(new
                    {
                        success = true,
                        isExistingInterviewer = isEx,
                        existingUserId = exUserId,
                        existingProfileId = exProfileId,
                        message = isEx
                            ? $"Email updated. This interviewer already exists (UserId: {exUserId})."
                            : "Email updated. Interviewer is new to the system."
                    });
                }

                return Json(new { success = true, message = $"{fieldName} updated successfully." });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Get history for a record ──────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetHistory(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var history = await _repository.GetHistoryAsync(id);
                return Json(history.Select(h => new
                {
                    h.EventType,
                    fromValue = h.FromValue ?? "—",
                    toValue = h.ToValue ?? "—",
                    changedBy = h.ChangedBy,
                    changedAt = h.ChangedAt.ToString("dd MMM yyyy, HH:mm")
                }));
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(Array.Empty<object>());
            }
        }

        // ── Upload Resume (AJAX) ────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadResume(int id, IFormFile resumeFile)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                if (resumeFile == null || resumeFile.Length == 0)
                    return Json(new { success = false, error = "No file selected." });

                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(resumeFile);
                var allowedTypes = new[] { "application/pdf", "application/msword",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

                if (!allowedTypes.Contains(fileMimeType))
                    return Json(new { success = false, error = "Only PDF, DOC, and DOCX files are allowed." });

                var fileName = $"NK_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}_{resumeFile.FileName.Replace(" ", "_")}";
                await _azureStorageHelper.InsertBlob(fileName, "resumes", resumeFile);
                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                var blob = containerRef.GetBlockBlobReference(fileName);
                string uploadedUrl = blob.Uri.ToString();

                if (string.IsNullOrEmpty(uploadedUrl))
                    return Json(new { success = false, error = "Upload failed." });

                await _repository.UpdateResumeLinkAsync(id, uploadedUrl);

                var uploadUser = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                await _repository.InsertHistoryAsync(id, "ResumeUpload", "—", resumeFile.FileName, uploadUser);

                return Json(new { success = true, resumeLink = uploadedUrl, message = "Resume uploaded." });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = "Upload failed: " + ex.Message });
            }
        }

        // ── Relay webhook (Relay.app → our server) ──────────────────────────────────

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            try
            {
                var expectedSecret = _configuration["RelayWebhook:NaukriOnboardingSecret"];
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

                var payload = JsonConvert.DeserializeObject<RelayNaukriPayload>(rawBody,
                    new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });

                if (payload == null || string.IsNullOrWhiteSpace(payload.SenderEmail))
                    return BadRequest(new { error = "Missing required field: sender_email." });

                DateTime? receivedAt = null;
                if (!string.IsNullOrWhiteSpace(payload.ReceivedAt) &&
                    DateTime.TryParse(payload.ReceivedAt, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                    receivedAt = parsed;

                string blobResumeUrl = null;
                string sourceResumeUrl = payload.ResumeLink?.Trim();

                if (!string.IsNullOrWhiteSpace(sourceResumeUrl)
                    && !sourceResumeUrl.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string downloadUrl = ConvertToDirectDownloadUrl(sourceResumeUrl);
                        var httpClient = _httpClientFactory.CreateClient();
                        using var response = await httpClient.GetAsync(downloadUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var contentBytes = await response.Content.ReadAsByteArrayAsync();
                            var ct = response.Content.Headers.ContentType?.MediaType ?? "";

                            bool isHtml = ct.Contains("text/html")
                                || (contentBytes.Length > 5
                                    && System.Text.Encoding.UTF8.GetString(contentBytes, 0, Math.Min(50, contentBytes.Length))
                                        .TrimStart().StartsWith("<", StringComparison.Ordinal));

                            if (contentBytes.Length > 0 && !isHtml)
                            {
                                string ext = ".pdf";
                                if (ct.Contains("msword") || sourceResumeUrl.EndsWith(".doc", StringComparison.OrdinalIgnoreCase)) ext = ".doc";
                                else if (ct.Contains("wordprocessingml") || sourceResumeUrl.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)) ext = ".docx";

                                string blobName = $"NK_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                                using var ms = new MemoryStream(contentBytes);
                                blobResumeUrl = await _azureStorageHelper.InsertBlobCloudCompare(
                                    blobName, "resumes", blobFIleStream: ms, contentType: ct);
                            }
                        }
                    }
                    catch (Exception dlEx)
                    {
                        await _logger.LogMessage(LogLevelInfo.Exception, dlEx);
                    }
                }

                string relayEmail = payload.CandidateEmail?.Trim();
                // Relay frequently sends phone numbers with hyphens / spaces / country code —
                // sanitise to the bare national number at ingestion.
                string relayPhone = Risebird.CommonHelper.PhoneNumberHelper.StripCountryCode(payload.CandidatePhone?.Trim());
                bool preParsed = !string.IsNullOrWhiteSpace(relayEmail)
                              || !string.IsNullOrWhiteSpace(relayPhone);

                var record = new NaukriProspectiveInterviewer
                {
                    SenderEmail     = payload.SenderEmail?.Trim(),
                    Subject         = payload.Subject?.Trim(),
                    MailContent     = payload.MailContent?.Trim(),
                    ReceivedAt      = receivedAt ?? DateTime.UtcNow,
                    CandidateName   = payload.CandidateName?.Trim(),
                    Location        = payload.Location?.Trim(),
                    PastExperience  = payload.PastExperience?.Trim(),
                    NoticePeriod    = payload.NoticePeriod?.Trim(),
                    Education       = payload.Education?.Trim(),
                    KeySkills       = payload.KeySkills?.Trim(),
                    CurrentRole     = payload.CurrentRole?.Trim(),
                    ExperienceYears = payload.ExperienceYears?.Trim(),
                    Salary          = payload.Salary?.Trim(),
                    MagName         = payload.MagName?.Trim(),
                    ParsedEmail     = relayEmail,
                    ParsedPhone     = relayPhone,
                    ResumeParseCompleted = preParsed ? true : null,
                    ResumeLink      = blobResumeUrl ?? sourceResumeUrl,
                    HasAttachment   = !string.IsNullOrWhiteSpace(blobResumeUrl) || payload.HasAttachment,
                    Source          = "Naukri",
                    LinkedInUrl        = payload.LinkedInUrl?.Trim(),
                    LinkedInMatch      = payload.LinkedInMatch?.Trim(),
                    LinkedInConfidence = payload.LinkedInConfidence
                };

                var newId = await _repository.InsertAsync(record);

                if (newId > 0)
                {
                    await _repository.InsertHistoryAsync(newId, "RecordCreated", "\u2014",
                        $"Candidate: {record.CandidateName}, MAG: {record.MagName}, Source: Naukri", "Relay Webhook");
                    if (preParsed)
                        await _repository.InsertHistoryAsync(newId, "RelayPreParsed", "\u2014",
                            $"Email: {relayEmail ?? "—"}, Phone: {relayPhone ?? "—"} (extracted by Relay agent)", "Relay Webhook");
                    if (!string.IsNullOrWhiteSpace(record.LinkedInMatch))
                        await _repository.InsertHistoryAsync(newId, "LinkedInVerified", "\u2014",
                            $"LinkedIn: {record.LinkedInMatch} (confidence {record.LinkedInConfidence?.ToString() ?? "—"}) — {record.LinkedInUrl ?? "no url"} (verified by Relay agent)", "Relay Webhook");
                }

                return Ok(new
                {
                    success = true,
                    id = newId,
                    message = $"Naukri prospect created (#{newId}) for {record.CandidateName}.",
                    resumeUploaded = !string.IsNullOrWhiteSpace(blobResumeUrl)
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ── LinkedIn Relay webhook ────────────────────────────────────────────────

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> LinkedInWebhook()
        {
            try
            {
                var expectedSecret = _configuration["RelayWebhook:LinkedInOnboardingSecret"];
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

                var payload = JsonConvert.DeserializeObject<RelayLinkedInPayload>(rawBody,
                    new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });

                if (payload == null || string.IsNullOrWhiteSpace(payload.SenderEmail))
                    return BadRequest(new { error = "Missing required field: sender_email." });

                DateTime? receivedAt = null;
                if (!string.IsNullOrWhiteSpace(payload.ReceivedAt) &&
                    DateTime.TryParse(payload.ReceivedAt, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                    receivedAt = parsed;

                string blobResumeUrl = null;
                string sourceResumeUrl = payload.ResumeLink?.Trim();

                if (!string.IsNullOrWhiteSpace(sourceResumeUrl)
                    && !sourceResumeUrl.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string downloadUrl = ConvertToDirectDownloadUrl(sourceResumeUrl);
                        var httpClient = _httpClientFactory.CreateClient();
                        using var response = await httpClient.GetAsync(downloadUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var contentBytes = await response.Content.ReadAsByteArrayAsync();
                            var ct = response.Content.Headers.ContentType?.MediaType ?? "";

                            bool isHtml = ct.Contains("text/html")
                                || (contentBytes.Length > 5
                                    && System.Text.Encoding.UTF8.GetString(contentBytes, 0, Math.Min(50, contentBytes.Length))
                                        .TrimStart().StartsWith("<", StringComparison.Ordinal));

                            if (contentBytes.Length > 0 && !isHtml)
                            {
                                string ext = ".pdf";
                                if (ct.Contains("msword") || sourceResumeUrl.EndsWith(".doc", StringComparison.OrdinalIgnoreCase)) ext = ".doc";
                                else if (ct.Contains("wordprocessingml") || sourceResumeUrl.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)) ext = ".docx";

                                string blobName = $"LI_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                                using var ms = new MemoryStream(contentBytes);
                                blobResumeUrl = await _azureStorageHelper.InsertBlobCloudCompare(
                                    blobName, "resumes", blobFIleStream: ms, contentType: ct);
                            }
                        }
                    }
                    catch (Exception dlEx)
                    {
                        await _logger.LogMessage(LogLevelInfo.Exception, dlEx);
                    }
                }

                string magName = payload.JobTitle?.Trim();
                string relayEmail = payload.CandidateEmail?.Trim();
                // Sanitise to the bare national number at ingestion (Relay may include formatting / country code).
                string relayPhone = Risebird.CommonHelper.PhoneNumberHelper.StripCountryCode(payload.CandidatePhone?.Trim());
                bool preParsed = !string.IsNullOrWhiteSpace(relayEmail)
                              || !string.IsNullOrWhiteSpace(relayPhone);

                var record = new NaukriProspectiveInterviewer
                {
                    SenderEmail     = payload.SenderEmail?.Trim(),
                    Subject         = payload.Subject?.Trim(),
                    MailContent     = payload.MailContent?.Trim(),
                    ReceivedAt      = receivedAt ?? DateTime.UtcNow,
                    CandidateName   = payload.CandidateName?.Trim(),
                    Location        = payload.Location?.Trim(),
                    PastExperience  = payload.PastExperience?.Trim(),
                    KeySkills       = payload.SkillsMatch?.Trim(),
                    CurrentRole     = payload.Headline?.Trim(),
                    MagName         = magName,
                    ParsedEmail     = relayEmail,
                    ParsedPhone     = relayPhone,
                    ResumeParseCompleted = preParsed ? true : null,
                    ResumeLink      = blobResumeUrl ?? sourceResumeUrl,
                    HasAttachment   = !string.IsNullOrWhiteSpace(blobResumeUrl) || payload.HasAttachment,
                    Source          = "LinkedIn"
                };

                var newId = await _repository.InsertAsync(record);

                if (newId > 0)
                {
                    await _repository.InsertHistoryAsync(newId, "RecordCreated", "—",
                        $"Candidate: {record.CandidateName}, MAG: {record.MagName}, Source: LinkedIn", "Relay Webhook");
                    if (preParsed)
                        await _repository.InsertHistoryAsync(newId, "RelayPreParsed", "—",
                            $"Email: {relayEmail ?? "—"}, Phone: {relayPhone ?? "—"} (extracted by Relay agent)", "Relay Webhook");
                }

                return Ok(new
                {
                    success = true,
                    id = newId,
                    message = $"LinkedIn prospect created (#{newId}) for {record.CandidateName}.",
                    resumeUploaded = !string.IsNullOrWhiteSpace(blobResumeUrl)
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ── Plivo CX Onboarding Call — Initiate (AJAX) ────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiatePlivoCall(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                string phone = record.ParsedPhone;
                if (string.IsNullOrWhiteSpace(phone))
                    return Json(new { success = false, error = "No phone number available. Parse the resume first." });

                if (phone.Length == 10 && !phone.StartsWith("+"))
                    phone = "+91" + phone;
                else if (phone.Length > 10 && !phone.StartsWith("+"))
                    phone = "+" + phone;

                var retryableStatuses = new[] { "trigger-failed", "no_answer", "busy_rejected", "failed", "no-answer", "busy" };
                if (!string.IsNullOrEmpty(record.PlivoCallStatus) &&
                    !Array.Exists(retryableStatuses, s => s.Equals(record.PlivoCallStatus, StringComparison.OrdinalIgnoreCase)))
                    return Json(new { success = false, error = $"A call has already been made (status: {record.PlivoCallStatus}). Use the view to check details.", alreadyCalled = true });

                var calledBy = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";

                await _repository.UpdatePlivoCallStatusAsync(id, "initiated", null,
                    DateTime.UtcNow, null, null, null, null, null, null, calledBy);

                var callerId = _configuration["PlivoCX:OnboardingCallerId"]
                            ?? _configuration["Plivo:FromNumber"]
                            ?? "+918031136547";

                var triggerRequest = new PlivoCXOnboardingTriggerRequest
                {
                    CallerId        = callerId,
                    PhoneNumber     = phone,
                    ProspectName    = record.CandidateName ?? "there",
                    ProspectEmail   = record.ParsedEmail,
                    Specialization  = record.MagName ?? record.KeySkills ?? "Technical",
                    ExperienceYears = record.ExperienceYears,
                    LogId           = id.ToString(),
                    TodayDate       = DateTime.Now.ToString("dddd, dd MMMM yyyy")
                };

                var response = await _plivoCXService.TriggerOnboardingCallAsync(triggerRequest);

                if (!string.IsNullOrEmpty(response?.Error))
                {
                    await _repository.UpdatePlivoCallStatusAsync(id, "trigger-failed", null,
                        null, null, null, response.Error, null, null, null, null);

                    return Json(new { success = false, error = $"Plivo CX trigger failed: {response.Error}" });
                }

                string conversationId = response?.ConversationId;
                await _repository.UpdatePlivoCallStatusAsync(id, "triggered", conversationId,
                    null, null, null, null, null, null, null, null);

                await _repository.InsertHistoryAsync(id, "PlivoCallInitiated", "—",
                    $"Call triggered to {phone}. ConversationId: {conversationId ?? "N/A"}", calledBy);

                // Call placed → still in the Interest Collection stage (awaiting call disposition).
                await _repository.AdvanceFunnelStageAsync(id, "InterestCollection", "InProgress");

                return Json(new
                {
                    success = true,
                    conversationId,
                    message = $"Call initiated to {record.CandidateName} ({phone})."
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = "Failed to initiate call: " + ex.Message });
            }
        }

        // ── Plivo CX Onboarding Call — Hangup Webhook (public endpoint) ─────────

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PlivoOnboardingHangup()
        {
            await SaveOnbCp("ob_hangup_received",
                $"PlivoOnboardingHangup hit — Content-Type: {Request.ContentType}");

            // ── Step 1: Read raw body ─────────────────────────────────────────────
            string rawBody = string.Empty;
            try
            {
                using (var reader = new StreamReader(Request.Body))
                    rawBody = await reader.ReadToEndAsync();

                await SaveOnbCp("ob_hangup_body_read",
                    $"Raw body length: {rawBody?.Length ?? 0}",
                    extra: rawBody?.Length > 800 ? rawBody.Substring(0, 800) + "…" : rawBody);
            }
            catch (Exception ex)
            {
                await SaveOnbCp("ob_hangup_body_read_failed", $"Body read FAILED: {ex.Message}");
                return BadRequest(new { error = "Could not read request body." });
            }

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                await SaveOnbCp("ob_hangup_empty_body", "Empty request body received.");
                return BadRequest(new { error = "Empty request body." });
            }

            // ── Step 2: Parse payload ─────────────────────────────────────────────
            PlivoCXOnboardingHangupPayload payload = null;
            int? recordId = null;
            string parseMethod = "none";

            // 2a: Try nested Plivo CX event envelope
            try
            {
                var nestedEvent = JsonConvert.DeserializeObject<PlivoCXHangupEvent>(rawBody);
                if (nestedEvent?.Data?.Object?.EventData != null)
                {
                    var ed = nestedEvent.Data.Object.EventData;
                    payload = new PlivoCXOnboardingHangupPayload
                    {
                        ConversationId        = nestedEvent.Data.Object.ConversationId,
                        CallUUID              = nestedEvent.Data.Object.CallUuid,
                        CallStatus            = GetEventValue(ed, "call_status"),
                        Duration              = GetEventValue(ed, "duration"),
                        Transcript            = GetEventValue(ed, "transcript"),
                        Summary               = GetEventValue(ed, "summary"),
                        LogId                 = GetEventValue(ed, "log_id"),
                        ProspectDisposition   = GetEventValue(ed, "prospect_disposition"),
                        OnboardingPreference  = GetEventValue(ed, "onboarding_preference"),
                        KycAccepted           = GetEventValue(ed, "kyc_accepted"),
                        HangupCause           = GetEventValue(ed, "hangup_cause")
                    };
                    parseMethod = "nested_event";
                    await SaveOnbCp("ob_hangup_nested_parsed",
                        $"Nested parse OK — log_id={payload.LogId}, disp={payload.ProspectDisposition}",
                        convId: payload.ConversationId);
                }
            }
            catch (Exception ex)
            {
                await SaveOnbCp("ob_hangup_nested_parse_failed", $"Nested parse failed: {ex.Message}");
            }

            // 2b: Try flat JSON (from AI conversation HTTP action or HTTP request nodes)
            if (payload == null || string.IsNullOrEmpty(payload.LogId))
            {
                try
                {
                    payload = JsonConvert.DeserializeObject<PlivoCXOnboardingHangupPayload>(rawBody,
                        new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
                    parseMethod = "flat_json";
                    await SaveOnbCp("ob_hangup_flat_parsed",
                        $"Flat JSON parse — log_id={payload?.LogId}, disp={payload?.ProspectDisposition}, phone={payload?.PhoneNumber}");
                }
                catch (Exception ex)
                {
                    await SaveOnbCp("ob_hangup_flat_parse_failed", $"Flat JSON parse failed: {ex.Message}");
                }
            }

            // 2c: Try extracting from Plivo CX HTTP node wrapper {"none":{},"json":{...},...}
            if (payload == null || string.IsNullOrEmpty(payload.LogId))
            {
                try
                {
                    var wrapper = JsonConvert.DeserializeObject<Dictionary<string, object>>(rawBody);
                    if (wrapper != null && wrapper.ContainsKey("json"))
                    {
                        var innerJson = wrapper["json"]?.ToString();
                        if (!string.IsNullOrEmpty(innerJson))
                        {
                            payload = JsonConvert.DeserializeObject<PlivoCXOnboardingHangupPayload>(innerJson,
                                new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
                            parseMethod = "wrapper_json";
                            await SaveOnbCp("ob_hangup_wrapper_parsed",
                                $"Wrapper JSON parse — log_id={payload?.LogId}, disp={payload?.ProspectDisposition}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await SaveOnbCp("ob_hangup_wrapper_parse_failed", $"Wrapper parse failed: {ex.Message}");
                }
            }

            if (payload == null)
            {
                await SaveOnbCp("ob_hangup_parse_failed", "All parse strategies failed — payload is null.");
                return BadRequest(new { error = "Could not parse hangup payload." });
            }

            // ── Step 3: Resolve record ID ─────────────────────────────────────────
            if (!string.IsNullOrEmpty(payload.LogId) && int.TryParse(payload.LogId, out int parsedId))
                recordId = parsedId;

            // Fallback: look up by phone number if log_id is missing/non-numeric
            if (!recordId.HasValue && !string.IsNullOrEmpty(payload.PhoneNumber))
            {
                await SaveOnbCp("ob_hangup_logid_missing",
                    $"log_id missing or non-numeric ('{payload.LogId}'). Trying phone lookup: {payload.PhoneNumber}");

                var foundId = await _repository.FindProspectIdByPhoneAndCallStatusAsync(
                    payload.PhoneNumber, "initiated");
                if (foundId.HasValue)
                {
                    recordId = foundId.Value;
                    await SaveOnbCp("ob_hangup_phone_lookup_ok",
                        $"Phone lookup matched record #{recordId}");
                }
                else
                {
                    foundId = await _repository.FindProspectIdByPhoneAndCallStatusAsync(
                        payload.PhoneNumber, "triggered");
                    if (foundId.HasValue)
                    {
                        recordId = foundId.Value;
                        await SaveOnbCp("ob_hangup_phone_lookup_ok",
                            $"Phone lookup (triggered) matched record #{recordId}");
                    }
                }
            }

            if (!recordId.HasValue)
            {
                await SaveOnbCp("ob_hangup_no_record",
                    $"Could not resolve record. log_id='{payload.LogId}', phone='{payload.PhoneNumber}', parseMethod={parseMethod}");
                return BadRequest(new { error = $"Missing log_id in hangup payload. log_id='{payload.LogId}', phone='{payload.PhoneNumber}'" });
            }

            // ── Step 4: Update database ───────────────────────────────────────────
            try
            {
                int? duration = null;
                if (!string.IsNullOrEmpty(payload.Duration) && int.TryParse(payload.Duration, out int dur))
                    duration = dur;

                bool? kycAccepted = null;
                if (!string.IsNullOrEmpty(payload.KycAccepted))
                    kycAccepted = payload.KycAccepted.Equals("true", StringComparison.OrdinalIgnoreCase)
                               || payload.KycAccepted == "1";

                string callStatus = !string.IsNullOrEmpty(payload.CallStatus)
                    ? payload.CallStatus.ToLower()
                    : "completed";

                await SaveOnbCp("ob_hangup_updating_db",
                    $"Updating record #{recordId} — status={callStatus}, disp={payload.ProspectDisposition}, pref={payload.OnboardingPreference}, kyc={payload.KycAccepted}, dur={duration}",
                    callLogId: recordId);

                var updated = await _repository.UpdatePlivoCallStatusAsync(
                    recordId.Value,
                    callStatus,
                    payload.ConversationId,
                    DateTime.UtcNow,
                    duration,
                    payload.Transcript,
                    payload.Summary,
                    payload.ProspectDisposition?.ToLower(),
                    payload.OnboardingPreference?.ToLower(),
                    kycAccepted,
                    null);

                await SaveOnbCp("ob_hangup_db_result",
                    $"UpdatePlivoCallStatusAsync returned {updated} for record #{recordId}",
                    callLogId: recordId);

                string historyDetail = $"Status: {callStatus}";
                if (!string.IsNullOrEmpty(payload.ProspectDisposition))
                    historyDetail += $", Disposition: {payload.ProspectDisposition}";
                if (!string.IsNullOrEmpty(payload.OnboardingPreference))
                    historyDetail += $", Preference: {payload.OnboardingPreference}";
                if (duration.HasValue)
                    historyDetail += $", Duration: {duration}s";

                await _repository.InsertHistoryAsync(recordId.Value, "PlivoCallCompleted", "—",
                    historyDetail, "PlivoCX Webhook");

                // Unified Intent — the AI Plivo call disposition feeds the same single Intent
                // as an email reply. no_answer / unknown leaves any existing intent untouched.
                string callIntent = MapCallDispositionToIntent(payload.ProspectDisposition);
                if (!string.IsNullOrEmpty(callIntent))
                {
                    await _repository.SetUnifiedIntentAsync(recordId.Value, callIntent, "Call", DateTime.UtcNow);
                    await ApplyIntentToFunnelAsync(recordId.Value, callIntent, "Call");
                    await SaveOnbCp("ob_hangup_intent_set",
                        $"Unified intent '{callIntent}' set for record #{recordId} from call", callLogId: recordId);
                }

                await SaveOnbCp("ob_hangup_complete",
                    $"Processing finished for record #{recordId}. parseMethod={parseMethod}",
                    callLogId: recordId);

                return Content($"OK. recordId={recordId.Value}", "text/html");
            }
            catch (Exception ex)
            {
                await SaveOnbCp("ob_hangup_db_exception",
                    $"DB update exception for record #{recordId}: {ex.Message}",
                    callLogId: recordId);
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Content($"Error: {ex.Message}", "text/html");
            }
        }

        // ── Get Plivo Call Details (AJAX) ─────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetPlivoCallDetails(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                return Json(new
                {
                    success = true,
                    plivoCallStatus = record.PlivoCallStatus,
                    plivoConversationId = record.PlivoConversationId,
                    plivoCallAt = record.PlivoCallAt?.ToString("dd MMM yyyy, HH:mm"),
                    plivoCallDuration = record.PlivoCallDuration,
                    plivoCallTranscript = record.PlivoCallTranscript,
                    plivoCallSummary = record.PlivoCallSummary,
                    plivoProspectDisposition = record.PlivoProspectDisposition,
                    plivoOnboardingPreference = record.PlivoOnboardingPreference,
                    plivoKycAccepted = record.PlivoKycAccepted,
                    plivoCalledBy = record.PlivoCalledBy
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Checkpoint helper (reuses PlivoCXWebhookCheckpoints table) ───────────

        private async Task SaveOnbCp(
            string name, string msg, string convId = null, int? callLogId = null, string extra = null)
        {
            try
            {
                await _cpRepo.SaveCheckpointAsync(new PlivoCXWebhookCheckpoint
                {
                    CheckpointName = name,
                    Message        = msg,
                    ConversationId = convId,
                    CallLogId      = callLogId,
                    AdditionalData = extra
                });
            }
            catch { }
        }

        // ── Prospect Onboarding redesign (Phase 2): unified-intent mapping helpers ──

        // Maps a confirmed unified intent onto the funnel:
        //   • Interested              → 'IntentConfirmed' (positive; pipeline continues, ticket InProgress)
        //   • NotInterested           → terminal 'NotInterested' (ticket Closed)
        //   • Unsubscribe/Drop profile→ terminal 'NotInterested' (ticket Closed) — folds into Not Interested
        // Terminal outcomes are set unconditionally (they are exits off the happy path).
        private async Task ApplyIntentToFunnelAsync(int id, string intent, string source)
        {
            switch ((intent ?? "").Trim())
            {
                case "Interested":
                    await _repository.AdvanceFunnelStageAsync(id, "IntentConfirmed", "InProgress");
                    await _repository.InsertHistoryAsync(id, "IntentConfirmed", "\u2014",
                        $"Unified Intent set to 'Interested' (source: {source}). Funnel \u2192 Intent Confirmed.", source == "Call" ? "PlivoCX Webhook" : "Prospect");
                    break;
                case "NotInterested":
                    await _repository.SetFunnelOutcomeAsync(id, "NotInterested", "Closed");
                    await _repository.InsertHistoryAsync(id, "IntentConfirmed", "\u2014",
                        $"Unified Intent set to 'NotInterested' (source: {source}). Funnel \u2192 Not Interested.", source == "Call" ? "PlivoCX Webhook" : "Prospect");
                    break;
                case "Unsubscribe":
                    // Drop/Unsubscribe folds into the 'Not Interested' funnel classification.
                    // (The unified Intent stays 'Unsubscribe' so the profile-deactivation logic still fires.)
                    await _repository.SetFunnelOutcomeAsync(id, "NotInterested", "Closed");
                    await _repository.InsertHistoryAsync(id, "IntentConfirmed", "\u2014",
                        $"Unified Intent set to 'Unsubscribe / Drop profile' (source: {source}). Funnel \u2192 Not Interested (Drop / Unsubscribe).", source == "Call" ? "PlivoCX Webhook" : "Prospect");
                    break;
            }
        }

        // Maps an email-button response to the canonical unified intent.
        private static string MapEmailResponseToIntent(string response)
        {
            switch ((response ?? "").Trim().ToLower())
            {
                case "needclarity":
                case "manualsupport":
                case "interested":
                case "yes":
                case "accept":
                case "accepted":
                    return "Interested";
                case "notinterested":
                case "no":
                case "decline":
                case "declined":
                    return "NotInterested";
                case "unsubscribe":
                    return "Unsubscribe";
                default:
                    return null;
            }
        }

        // Maps a Plivo call disposition to the canonical unified intent.
        // Returns null for no_answer / unknown so a previously-confirmed intent is not overwritten.
        private static string MapCallDispositionToIntent(string disposition)
        {
            switch ((disposition ?? "").Trim().ToLower())
            {
                case "interested":     return "Interested";
                case "not_interested": return "NotInterested";
                case "unsubscribe":    return "Unsubscribe";
                default:               return null;
            }
        }

        /// <summary>
        /// View recent onboarding-hangup checkpoints.
        /// Access: /NaukriOnboarding/OnboardingCheckpoints?count=100
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OnboardingCheckpoints(int count = 100)
        {
            try
            {
                var all = await _cpRepo.GetRecentCheckpointsAsync(count);
                var onb = all.Where(c => c.CheckpointName.StartsWith("ob_")).ToList();
                return Json(onb);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { error = ex.Message });
            }
        }

        private static string GetEventValue(Dictionary<string, string> eventData, string keySuffix)
        {
            if (eventData == null) return null;
            foreach (var kvp in eventData)
            {
                if (kvp.Key.EndsWith("." + keySuffix, StringComparison.OrdinalIgnoreCase)
                    || kvp.Key.Equals(keySuffix, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }
            return null;
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        private static string ConvertToDirectDownloadUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            // Google Drive: /file/d/{ID}/... → /uc?export=download&id={ID}
            var match = Regex.Match(url, @"drive\.google\.com/file/d/([^/]+)");
            if (match.Success)
                return $"https://drive.google.com/uc?export=download&id={match.Groups[1].Value}";

            // Google Drive: open?id={ID}
            match = Regex.Match(url, @"drive\.google\.com/open\?id=([^&]+)");
            if (match.Success)
                return $"https://drive.google.com/uc?export=download&id={match.Groups[1].Value}";

            return url;
        }

        private static string ExtractEmail(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var match = Regex.Match(text, @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}");
            return match.Success ? match.Value.ToLower() : null;
        }

        private static string ExtractPhone(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var matches = Regex.Matches(text, @"(?:\+?\d{1,3}[\s\-]?)?\(?\d{3,5}\)?[\s\-]?\d{3,5}[\s\-]?\d{3,5}");
            foreach (Match m in matches)
            {
                string digits = Regex.Replace(m.Value, @"[^\d]", "");
                if (digits.Length >= 10 && digits.Length <= 13) return digits;
            }
            return null;
        }

        // ── Batch Call — dedicated page ────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> BatchCall()
        {
            if (HttpContext.Session.GetString("email") == null)
                return RedirectToAction("Login", "Login");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProspectCallOutcomes(string magName, int page = 1, int pageSize = 25)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var (prospects, totalCount) = await _repository.GetProspectCallOutcomesForMAGAsync(magName, page, pageSize);
                return Json(new
                {
                    success = true,
                    totalCount,
                    page,
                    pageSize,
                    prospects = prospects.Select(p => new
                    {
                        p.Id, p.CandidateName, p.ParsedEmail, p.ParsedPhone,
                        p.ExperienceYears, p.CurrentRole, p.Location,
                        p.PlivoCallStatus, p.PlivoProspectDisposition,
                        p.PlivoOnboardingPreference, p.PlivoKycAccepted,
                        p.PlivoCallDuration, p.PlivoCalledBy, p.PlivoCallSummary,
                        plivoCallAt = p.PlivoCallAt?.ToString("dd MMM yyyy, HH:mm"),
                        p.PlivoCallAttemptsTotal, p.InvitationEmailSent, p.Status
                    })
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Batch Call — MAG list with callable counts ─────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetBatchCallMAGs()
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var mags = await _repository.GetMAGsWithCallableProspectCountsAsync();
                return Json(new { success = true, mags, count = mags.Count });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Batch Call — trigger call for a single prospect (reused by batch JS) ──

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchCallSingle(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                string phone = record.ParsedPhone;
                if (string.IsNullOrWhiteSpace(phone))
                    return Json(new { success = false, error = "No phone number." });

                if (phone.Length == 10 && !phone.StartsWith("+"))
                    phone = "+91" + phone;
                else if (phone.Length > 10 && !phone.StartsWith("+"))
                    phone = "+" + phone;

                var calledBy = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";

                await _repository.UpdatePlivoCallStatusAsync(id, "initiated", null,
                    DateTime.UtcNow, null, null, null, null, null, null, calledBy);

                var callerId = _configuration["PlivoCX:OnboardingCallerId"]
                            ?? _configuration["Plivo:FromNumber"]
                            ?? "+918031136547";

                var triggerRequest = new PlivoCXOnboardingTriggerRequest
                {
                    CallerId        = callerId,
                    PhoneNumber     = phone,
                    ProspectName    = record.CandidateName ?? "there",
                    ProspectEmail   = record.ParsedEmail,
                    Specialization  = record.MagName ?? record.KeySkills ?? "Technical",
                    ExperienceYears = record.ExperienceYears,
                    LogId           = id.ToString(),
                    TodayDate       = DateTime.Now.ToString("dddd, dd MMMM yyyy")
                };

                var response = await _plivoCXService.TriggerOnboardingCallAsync(triggerRequest);

                if (!string.IsNullOrEmpty(response?.Error))
                {
                    await _repository.UpdatePlivoCallStatusAsync(id, "trigger-failed", null,
                        null, null, null, response.Error, null, null, null, null);
                    return Json(new { success = false, error = $"Plivo trigger failed: {response.Error}" });
                }

                string conversationId = response?.ConversationId;
                await _repository.UpdatePlivoCallStatusAsync(id, "triggered", conversationId,
                    null, null, null, null, null, null, null, null);
                await _repository.InsertHistoryAsync(id, "BatchCallInitiated", "—",
                    $"Batch call triggered to {phone}. ConversationId: {conversationId ?? "N/A"}", calledBy);

                return Json(new { success = true, conversationId, candidateName = record.CandidateName, phone });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Batch Call — get callable prospect IDs for a MAG ──────────────────────

        [HttpGet]
        public async Task<IActionResult> GetCallableProspectsForMAG(string magName, bool onlyUncalled = true)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var prospects = await _repository.GetCallableProspectsForMAGAsync(magName, onlyUncalled);
                var items = prospects.Select(p => new
                {
                    p.Id, p.CandidateName, p.ParsedPhone, p.ParsedEmail,
                    p.ExperienceYears, p.PlivoCallAttemptsTotal, p.PlivoCallStatus
                });
                return Json(new { success = true, prospects = items });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Batch Call — logging and history ──────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartBatchCallLog(string magName, int totalProspects)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var initiatedBy = HttpContext.Session.GetString("email")?.Trim('"') ?? "Unknown";
                var logId = await _repository.InsertBatchCallLogAsync(magName, initiatedBy, totalProspects);
                return Json(new { success = logId > 0, logId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteBatchCallLog(int logId, int succeeded, int failed, int skipped, string status)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                await _repository.CompleteBatchCallLogAsync(logId, succeeded, failed, skipped, status ?? "Completed");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBatchCallHistory(string magName)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var history = await _repository.GetBatchCallHistoryAsync(magName);
                return Json(new
                {
                    success = true,
                    history = history.Select(h => new
                    {
                        h.Id, h.InitiatedBy,
                        initiatedAt = h.InitiatedAt.ToString("dd MMM yyyy, HH:mm"),
                        h.ProspectsAttempted, h.ProspectsSucceeded, h.ProspectsFailed, h.ProspectsSkipped,
                        completedAt = h.CompletedAt?.ToString("dd MMM yyyy, HH:mm"),
                        h.Status
                    })
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        private static string BuildInvitationEmailHtml(string candidateName, string magName, string appLink,
            string needClarityLink, string manualSupportLink, string notInterestedLink)
        {
            return $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
  <div style='text-align:center;padding:20px 0;'>
    <h2 style='color:#2c3e50;margin:0;'>Risebird Interview Panel</h2>
    <p style='color:#7f8c8d;margin:5px 0 0;'>Interviewer Opportunity</p>
  </div>
  <div style='background:#fff;border:1px solid #e0e0e0;border-radius:8px;padding:30px;'>
    <p style='font-size:15px;color:#333;'>Hi {System.Net.WebUtility.HtmlEncode(candidateName)},</p>
    <p style='font-size:14px;color:#555;line-height:1.7;'>
      We came across your profile and believe you would be an excellent fit for our interview panel as a
      <strong style='color:#2c3e50;'>{System.Net.WebUtility.HtmlEncode(magName)}</strong> specialist.
    </p>
    <p style='font-size:14px;color:#555;line-height:1.7;'>
      Risebird connects seasoned professionals with top companies to conduct technical interviews.
      It's 100% remote, fully flexible, and compensated transparently.
    </p>
    <div style='text-align:center;margin:25px 0;'>
      <a href='{appLink}' style='display:inline-block;background:#27ae60;color:#fff;padding:14px 32px;border-radius:6px;text-decoration:none;font-size:15px;font-weight:600;'>
        I'm Interested &mdash; Register Now
      </a>
    </div>
    <p style='font-size:13px;color:#888;text-align:center;'>Click the button above to complete a quick registration (takes ~2 minutes)</p>
    <hr style='border:none;border-top:1px solid #eee;margin:25px 0;'/>
    <p style='font-size:14px;color:#555;'>Not sure yet? Choose an option below:</p>
    <table style='width:100%;border-collapse:collapse;margin-top:10px;'>
      <tr>
        <td style='padding:8px 5px;text-align:center;'>
          <a href='{needClarityLink}' style='display:inline-block;background:#3498db;color:#fff;padding:10px 20px;border-radius:5px;text-decoration:none;font-size:13px;'>
            I Need More Details
          </a>
        </td>
        <td style='padding:8px 5px;text-align:center;'>
          <a href='{manualSupportLink}' style='display:inline-block;background:#e67e22;color:#fff;padding:10px 20px;border-radius:5px;text-decoration:none;font-size:13px;'>
            I Need Help Registering
          </a>
        </td>
        <td style='padding:8px 5px;text-align:center;'>
          <a href='{notInterestedLink}' style='display:inline-block;background:#95a5a6;color:#fff;padding:10px 20px;border-radius:5px;text-decoration:none;font-size:13px;'>
            Not Interested
          </a>
        </td>
      </tr>
    </table>
  </div>
  <div style='text-align:center;padding:20px 0;'>
    <p style='font-size:12px;color:#aaa;'>Risebird Technologies Pvt. Ltd. &bull; <a href='https://portal.risebird.io' style='color:#3498db;'>portal.risebird.io</a></p>
  </div>
</div>";
        }
    }
}
