using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Business.Interface;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class InterviewerAvailabilityController : Controller
    {
        private readonly IInterviewerEmailAvailabilityRepository _repository;
        private readonly IApplicationErrorLogger                 _logger;
        private readonly IConfiguration                          _configuration;
        private readonly IAssistantServiceHelper                 _assistantServiceHelper;
        private readonly IPdfReaderHelper                        _pdfReaderHelper;
        private readonly INotificationHelper                     _notificationHelper;
        private readonly IInterviewerService                     _interviewerService;
        private readonly IClientService                          _clientService;
        private readonly IIRFService                             _irfService;
        private readonly IWhatsAppHelper                         _whatsAppHelper;
        private readonly IAzureStorageHelper                     _azureStorageHelper;
        private readonly IFileMimeTypeHelper                     _fileMimeTypeHelper;

        public InterviewerAvailabilityController(
            IInterviewerEmailAvailabilityRepository repository,
            IApplicationErrorLogger                 logger,
            IConfiguration                          configuration,
            IAssistantServiceHelper                 assistantServiceHelper,
            IPdfReaderHelper                        pdfReaderHelper,
            INotificationHelper                     notificationHelper,
            IInterviewerService                     interviewerService,
            IClientService                          clientService,
            IIRFService                             irfService,
            IWhatsAppHelper                         whatsAppHelper,
            IAzureStorageHelper                     azureStorageHelper,
            IFileMimeTypeHelper                     fileMimeTypeHelper)
        {
            _repository             = repository;
            _logger                 = logger;
            _configuration          = configuration;
            _assistantServiceHelper = assistantServiceHelper;
            _pdfReaderHelper        = pdfReaderHelper;
            _notificationHelper     = notificationHelper;
            _interviewerService     = interviewerService;
            _clientService          = clientService;
            _irfService             = irfService;
            _whatsAppHelper         = whatsAppHelper;
            _azureStorageHelper     = azureStorageHelper;
            _fileMimeTypeHelper     = fileMimeTypeHelper;
        }

        // ── Dashboard ───────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(
            string status        = null,
            string senderEmail   = null,
            string resumeFitment = null,
            string dateFrom      = null,
            string dateTo        = null,
            int    page          = 1,
            int    pageSize      = 25)
        {
            try
            {
                if (page < 1)       page     = 1;
                if (pageSize < 10)  pageSize = 10;
                if (pageSize > 200) pageSize = 200;

                DateTime? dtFrom = null, dtTo = null;
                if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out var pFrom)) dtFrom = pFrom;
                if (!string.IsNullOrWhiteSpace(dateTo)   && DateTime.TryParse(dateTo,   out var pTo))   dtTo   = pTo;

                var records = await _repository.GetEmailAvailabilityAsync(
                    status, senderEmail, resumeFitment, dtFrom, dtTo, page, pageSize);

                var first      = records.FirstOrDefault();
                int totalCount = first?.TotalCount ?? 0;
                int totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;

                ViewBag.Page          = page;
                ViewBag.PageSize      = pageSize;
                ViewBag.TotalCount    = totalCount;
                ViewBag.TotalPages    = totalPages;
                ViewBag.OpenCount     = first?.OpenCount ?? 0;
                ViewBag.InProgressCount = first?.InProgressCount ?? 0;
                ViewBag.VerifiedCount = first?.VerifiedCount ?? 0;
                ViewBag.ClosedCount   = first?.ClosedCount ?? 0;
                ViewBag.Status        = status        ?? string.Empty;
                ViewBag.SenderEmail   = senderEmail   ?? string.Empty;
                ViewBag.ResumeFitment = resumeFitment  ?? string.Empty;
                ViewBag.DateFrom      = dateFrom      ?? string.Empty;
                ViewBag.DateTo        = dateTo        ?? string.Empty;
                ViewBag.ProfileName   = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") ?? "";

                return View(records);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.Page = 1; ViewBag.PageSize = pageSize; ViewBag.TotalCount = 0; ViewBag.TotalPages = 1;
                ViewBag.OpenCount = 0; ViewBag.InProgressCount = 0; ViewBag.VerifiedCount = 0; ViewBag.ClosedCount = 0;
                ViewBag.Status = status; ViewBag.SenderEmail = senderEmail; ViewBag.ResumeFitment = resumeFitment;
                ViewBag.DateFrom = dateFrom; ViewBag.DateTo = dateTo;
                ViewBag.ProfileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") ?? "";
                return View(new List<InterviewerEmailAvailabilityVM>());
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
                var record = await _repository.GetEmailAvailabilityByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                return Json(new
                {
                    success = true,
                    record = new
                    {
                        record.Id,
                        record.SenderEmail,
                        record.UserId,
                        record.InterviewerProfileId,
                        record.Subject,
                        record.MailContent,
                        receivedAt = record.ReceivedAt?.ToString("dd MMM yyyy, HH:mm"),
                        record.JobTitle,
                        record.InterviewerName,
                        record.Phone,
                        record.MagName,
                        record.IRFReference,
                        record.HasLoginCredentials,
                        record.SlotsPerDay,
                        record.TimeFrame,
                        record.AvailabilityIntent,
                        record.MailBodyIntent,
                        record.ConcernsOrNotInterested,
                        record.ResumeFitment,
                        record.ResumeLink,
                        record.HasAttachment,
                        record.Status,
                        record.EmploymentVerified,
                        record.ResumeAIAnalysisCompleted,
                        record.ResumeAIAnalysisResult,
                        record.AssessmentGroupId,
                        assessmentGroupAssignedAt = record.AssessmentGroupAssignedAt?.ToString("dd MMM yyyy, HH:mm"),
                        record.ProcessedBy,
                        record.InterviewerNickName,
                        record.CurrentRole,
                        record.Experience,
                        record.IsProfileVerified,
                        record.IsEmployed,
                        record.CompanyName,
                        record.ProfileResumeLink,
                        record.ExistingAssessmentGroupIds,
                        record.UserEmail,
                        record.MasterAssessmentGroupName,
                        record.ResolvedAssessmentGroupId,
                        record.AssessmentGroupName,
                        hasResumeLink = !string.IsNullOrEmpty(record.ResumeLink)
                                        && !string.Equals(record.ResumeLink, "None", StringComparison.OrdinalIgnoreCase),
                        hasProfileResumeLink = !string.IsNullOrEmpty(record.ProfileResumeLink)
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
        public async Task<IActionResult> UpdateStatusModal(int id, string status)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            var allowed = new[] { "Open", "InProgress", "Verified", "Closed" };
            if (id <= 0 || !Array.Exists(allowed, s => s == status))
                return Json(new { success = false, error = "Invalid status update request." });

            var changedBy = HttpContext.Session.GetString("email").Trim('"');
            var record = await _repository.GetEmailAvailabilityByIdAsync(id);
            string fromStatus = record?.Status ?? "—";
            var ok = await _repository.UpdateEmailAvailabilityStatusAsync(id, status, changedBy);
            if (ok) await _repository.InsertHistoryAsync(id, "StatusChange", fromStatus, status, changedBy);
            return Json(new { success = ok, status, error = ok ? null : "Could not update status." });
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
                return Json(new { success = false, error = "You are not authorised to delete records." });

            if (id <= 0)
                return Json(new { success = false, error = "Invalid record." });

            var ok = await _repository.DeleteEmailAvailabilityAsync(id);
            return Json(new { success = ok, error = ok ? null : "Could not delete record." });
        }

        // ── Drop Interviewer Profile (AJAX) ─────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DropProfile(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") ?? "";
            var dropAllowed = new[] { "Admin", "SupportAdmin", "HR", "SeniorHR", "InterviewerManagement", "SeniorInterviewerManagement" };
            if (!Array.Exists(dropAllowed, p => p == profileName))
                return Json(new { success = false, error = "You are not authorised to drop interviewer profiles." });

            try
            {
                var record = await _repository.GetEmailAvailabilityByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                if (!record.InterviewerProfileId.HasValue)
                    return Json(new { success = false, error = "Interviewer not found in system. Cannot drop profile." });

                var modifiedBy = HttpContext.Session.GetString("email").Trim('"');
                var ok = await _repository.DropInterviewerProfileAsync(record.InterviewerProfileId.Value, modifiedBy);

                if (ok)
                {
                    string fromStatus = record.Status ?? "Open";
                    await _repository.UpdateEmailAvailabilityStatusAsync(id, "Closed", modifiedBy);
                    await _repository.InsertHistoryAsync(id, "ProfileDropped", "Active", "Dropped (Isavailable=4)", modifiedBy);
                    await _repository.InsertHistoryAsync(id, "StatusChange", fromStatus, "Closed", modifiedBy);
                }

                return Json(new
                {
                    success = ok,
                    message = ok
                        ? "Interviewer profile dropped (Isavailable = 4). Record closed."
                        : "Failed to drop interviewer profile."
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
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
                var allowedTypes = new[]
                {
                    "application/pdf",
                    "application/msword",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                };

                if (!allowedTypes.Contains(fileMimeType))
                    return Json(new { success = false, error = "Only PDF, DOC, and DOCX files are allowed." });

                var record = await _repository.GetEmailAvailabilityByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                var fileName = $"EA_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}_{resumeFile.FileName.Replace(" ", "_")}";

                await _azureStorageHelper.InsertBlob(fileName, "resumes", resumeFile);
                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                var blob = containerRef.GetBlockBlobReference(fileName);
                string uploadedUrl = blob.Uri.ToString();

                if (string.IsNullOrEmpty(uploadedUrl))
                    return Json(new { success = false, error = "Upload failed — could not retrieve blob URL." });

                var ok = await _repository.UpdateResumeLinkAsync(id, uploadedUrl);

                if (ok)
                {
                    var uploader = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                    await _repository.InsertHistoryAsync(id, "ResumeUpload", null, resumeFile.FileName, uploader);
                }

                return Json(new
                {
                    success = ok,
                    resumeLink = uploadedUrl,
                    message = ok ? "Resume uploaded successfully." : "Upload succeeded but could not update record."
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = "Upload failed: " + ex.Message });
            }
        }

        // ── Analyze Resume (AJAX) ───────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnalyzeResume(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetEmailAvailabilityByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                if (record.ResumeAIAnalysisCompleted == true)
                    return Json(new
                    {
                        success = true,
                        alreadyAnalyzed = true,
                        message = "Resume already analyzed.",
                        analysisResult = record.ResumeAIAnalysisResult
                    });

                string resumeUrl = record.ResumeLink;
                if (string.IsNullOrWhiteSpace(resumeUrl) || string.Equals(resumeUrl, "None", StringComparison.OrdinalIgnoreCase))
                    resumeUrl = record.ProfileResumeLink;

                if (string.IsNullOrWhiteSpace(resumeUrl))
                    return Json(new { success = false, error = "No resume link available. Please upload a resume first using the upload button.", needsUpload = true });

                string jobTitle = record.AssessmentGroupName ?? record.MagName ?? record.JobTitle ?? "General";

                string resumeText = null;
                bool isDocFile = resumeUrl.EndsWith(".doc", StringComparison.OrdinalIgnoreCase)
                              || resumeUrl.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

                if (!isDocFile)
                {
                    try { resumeText = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeUrl); }
                    catch { }
                }

                if (string.IsNullOrWhiteSpace(resumeText))
                {
                    try
                    {
                        string convertedPdfUrl = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(resumeUrl);
                        if (!string.IsNullOrWhiteSpace(convertedPdfUrl))
                            resumeText = await _pdfReaderHelper.ReadPdfFromUrlAsync(convertedPdfUrl);
                    }
                    catch { }
                }

                if (string.IsNullOrWhiteSpace(resumeText) && isDocFile)
                {
                    try { resumeText = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeUrl); }
                    catch { }
                }

                if (string.IsNullOrWhiteSpace(resumeText))
                    return Json(new
                    {
                        success = false,
                        error = "Could not read the resume. The file may be corrupted, password-protected, or in an unsupported format. Please upload a valid PDF/DOC/DOCX resume using the upload button.",
                        needsUpload = true
                    });

                var aiResponse = await _assistantServiceHelper.GenerateResumeAnalysis(resumeText, jobTitle);
                if (aiResponse == null)
                    return Json(new { success = false, error = "AI analysis returned no response. Please try again." });

                var analysisJson = aiResponse.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value;
                if (string.IsNullOrWhiteSpace(analysisJson))
                    return Json(new { success = false, error = "AI analysis returned empty result. Please try again." });

                var analysis = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(analysisJson);
                bool isPositiveVerdict = analysis != null
                    && !string.IsNullOrEmpty(analysis.verdict)
                    && analysis.verdict.ToLower().Contains("yes");

                string newStatus = isPositiveVerdict ? "Verified" : "InProgress";

                var ok = await _repository.UpdateEmailAvailabilityVerificationAsync(
                    id, employmentVerified: null, resumeAIAnalysisCompleted: true,
                    resumeAIAnalysisResult: analysisJson, newStatus: newStatus);

                if (ok)
                {
                    var analyzer = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                    await _repository.InsertHistoryAsync(id, "ResumeAnalysis", null,
                        $"Verdict: {analysis?.verdict}, Score: {analysis?.fitmentScore}", analyzer);
                    if (record.Status != newStatus)
                        await _repository.InsertHistoryAsync(id, "StatusChange", record.Status ?? "Open", newStatus, analyzer);
                }

                return Json(new
                {
                    success = ok,
                    resumeAIAnalysisCompleted = true,
                    verdict = analysis?.verdict,
                    fitmentScore = analysis?.fitmentScore,
                    completeFitmentAnalysis = analysis?.completeFitmentAnalysis,
                    isPositiveVerdict,
                    analysisResult = analysisJson,
                    message = ok
                        ? $"Resume analysis complete. Verdict: {analysis?.verdict}, Score: {analysis?.fitmentScore}"
                        : "Failed to save analysis result."
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = "Analysis failed: " + ex.Message });
            }
        }

        // ── Assign Assessment Group (AJAX) ──────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAssessmentGroup(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetEmailAvailabilityByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                if (record.ResumeAIAnalysisCompleted != true)
                    return Json(new { success = false, error = "Resume analysis must be completed first." });

                if (!record.InterviewerProfileId.HasValue)
                    return Json(new { success = false, error = "Interviewer profile not found in system." });

                if (!record.ResolvedAssessmentGroupId.HasValue || record.ResolvedAssessmentGroupId.Value <= 0)
                    return Json(new { success = false, error = $"No assessment group found for MAG name '{record.MagName}'. Please check the MAG configuration." });

                if (record.AssessmentGroupId.HasValue && record.AssessmentGroupId.Value > 0)
                    return Json(new { success = true, alreadyAssigned = true, message = "Interviewer already assigned to the assessment group." });

                int agId = record.ResolvedAssessmentGroupId.Value;
                int profileId = record.InterviewerProfileId.Value;
                string assignedBy = HttpContext.Session.GetString("email").Trim('"');

                var ok = await _repository.AssignInterviewerToAssessmentGroupAsync(
                    id, profileId, agId, assignedBy);

                if (!ok)
                    return Json(new { success = false, error = "Failed to assign interviewer to assessment group." });

                // Send notification (email + WhatsApp) matching InterviewerCommonInterviewNotification pattern
                try
                {
                    string emailId = record.SenderEmail;
                    var upcomingInterviews = await _interviewerService.FetchAllInterviewerallUpcomingInterview("Verified", null, agId);
                    var interviewerItem = upcomingInterviews?.FirstOrDefault(x => x.EmailId == emailId);

                    if (interviewerItem != null && !string.IsNullOrEmpty(interviewerItem.EmailId))
                    {
                        string sendReportUrl = string.Empty;
                        if (string.IsNullOrEmpty(interviewerItem.uniqueid))
                        {
                            var uniqueId = Guid.NewGuid();
                            var savedId = await _clientService.PublicInterviewerUrlSave(interviewerItem.EmailId, uniqueId.ToString());
                            sendReportUrl = "https://" + Request.Host.ToString() +
                                "/Interview/PublicOpenInterviewInvite?id=" +
                                (string.IsNullOrEmpty(savedId) ? uniqueId.ToString() : savedId);
                            if (!string.IsNullOrEmpty(savedId))
                                interviewerItem.uniqueid = savedId;
                        }
                        else
                        {
                            sendReportUrl = "https://" + Request.Host.ToString() +
                                "/Interview/PublicOpenInterviewInvite?id=" + interviewerItem.uniqueid;
                        }

                        var mail = new Mail
                        {
                            TO = interviewerItem.EmailId,
                            CC = "talent@risebird.io",
                            Subject = "New interviews have come up! Please assign."
                        };

                        await _notificationHelper.SendInterviewerCommonInterviewNotification(
                            mail, sendReportUrl, interviewerItem.Total,
                            interviewerItem.uniqueid, agId: agId);

                        try
                        {
                            var whatsAppVM = new WhatsAppVM
                            {
                                Emailid = interviewerItem.EmailId,
                                MessageValue = interviewerItem.Total.ToString(),
                                buttonlink = !string.IsNullOrEmpty(sendReportUrl)
                                    ? sendReportUrl.Replace("https://" + Request.Host.ToString() + "/", "")
                                    : "",
                                Event = "Interviewer Common Interview",
                                irfId = interviewerItem.irfIntId.HasValue ? interviewerItem.irfIntId.Value : 0
                            };
                            await _whatsAppHelper.WhatsAppSendInterviewerCommonInterviewDetails(whatsAppVM);
                        }
                        catch (Exception waEx)
                        {
                            await _logger.LogMessage(LogLevelInfo.Exception, waEx);
                        }
                    }
                }
                catch (Exception notifEx)
                {
                    await _logger.LogMessage(LogLevelInfo.Exception, notifEx);
                }

                string agName = record.AssessmentGroupName ?? "Assessment Group #" + agId;
                await _repository.InsertHistoryAsync(id, "AGAssignment", null, agName, assignedBy);

                return Json(new
                {
                    success = true,
                    assessmentGroupId = agId,
                    assessmentGroupName = agName,
                    message = $"Interviewer assigned to {agName} and mapped to active skill channels. Notification sent."
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── MAG dropdown + override ───────────────────────────────────────────────

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMagOverride(int id, string oldValue, string newValue)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            if (string.IsNullOrWhiteSpace(newValue))
                return Json(new { success = false, error = "MAG name cannot be empty." });

            try
            {
                var changedBy = HttpContext.Session.GetString("email").Trim('"');
                var ok = await _repository.UpdateMagNameAsync(id, newValue.Trim());
                if (ok)
                    await _repository.InsertHistoryAsync(id, "MagChange", oldValue ?? "—", newValue.Trim(), changedBy);

                return Json(new { success = ok, error = ok ? null : "Failed to update MAG name." });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── History (JSON) ──────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetHistory(int id)
        {
            try
            {
                var history = await _repository.GetHistoryAsync(id);
                return Json(history.Select(h => new
                {
                    h.Id,
                    h.RecordId,
                    fromValue = h.FromValue ?? "—",
                    toValue = h.ToValue ?? "—",
                    changedBy = h.ChangedBy,
                    changedAt = h.ChangedAt.ToString("dd MMM yyyy, HH:mm") + " UTC",
                    eventType = h.EventType ?? "StatusChange"
                }));
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new object[0]);
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
                var expectedSecret = _configuration["RelayWebhook:InterviewerAvailabilitySecret"];
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

                var payload = JsonConvert.DeserializeObject<RelayInterviewerAvailabilityPayload>(rawBody,
                    new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });

                if (payload == null || string.IsNullOrWhiteSpace(payload.SenderEmail))
                    return BadRequest(new { error = "Missing required field: sender_email." });

                DateTime? receivedAt = null;
                if (!string.IsNullOrWhiteSpace(payload.ReceivedAt) &&
                    DateTime.TryParse(payload.ReceivedAt, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                    receivedAt = parsed;

                if (receivedAt.HasValue && receivedAt.Value < DateTime.UtcNow.AddHours(-46))
                    return Ok(new { success = false, skipped = true, reason = "Email older than 46 hours — skipped." });

                var systemSenders = new[] { "mailer-daemon@googlemail.com", "mailer-daemon@google.com", "workspace-noreply@google.com", "postmaster@google.com", "noreply@google.com" };
                if (systemSenders.Any(s => string.Equals(payload.SenderEmail?.Trim(), s, StringComparison.OrdinalIgnoreCase)))
                    return Ok(new { success = false, skipped = true, reason = "System email — skipped." });

                var senderLower = (payload.SenderEmail?.Trim() ?? "").ToLower();
                if (senderLower.StartsWith("mailer-daemon@") || senderLower.StartsWith("postmaster@"))
                    return Ok(new { success = false, skipped = true, reason = "Bounce/system sender — skipped." });

                var subjectLower = (payload.Subject?.Trim() ?? "").ToLower();
                var skipSubjects = new[] { "delivery status notification", "undeliverable", "delivery failure", "mail delivery failed", "returned mail" };
                if (skipSubjects.Any(s => subjectLower.Contains(s)))
                    return Ok(new { success = false, skipped = true, reason = "Delivery status/bounce notification — skipped." });

                var record = new InterviewerEmailAvailability
                {
                    SenderEmail             = payload.SenderEmail?.Trim(),
                    Subject                 = payload.Subject?.Trim(),
                    MailContent             = payload.MailContent?.Trim(),
                    ReceivedAt              = receivedAt ?? DateTime.UtcNow,
                    JobTitle                = payload.JobTitle?.Trim(),
                    InterviewerName         = payload.Name?.Trim(),
                    Phone                   = payload.Phone?.Trim(),
                    MagName                 = payload.MagName?.Trim(),
                    IRFReference            = payload.IRFId?.Trim(),
                    HasLoginCredentials     = payload.HasLoginCredentials?.Trim(),
                    SlotsPerDay             = payload.SlotsPerDay?.Trim(),
                    TimeFrame               = payload.TimeFrame?.Trim(),
                    AvailabilityIntent      = payload.AvailabilityIntent?.Trim(),
                    MailBodyIntent          = payload.MailBodyIntent?.Trim(),
                    ConcernsOrNotInterested = payload.ConcernsOrNotInterested?.Trim(),
                    ResumeFitment           = payload.ResumeFitment?.Trim(),
                    ResumeLink              = payload.ResumeLink?.Trim(),
                    HasAttachment           = payload.HasAttachment
                };

                var newId = await _repository.InsertEmailAvailabilityAsync(record);

                if (newId > 0)
                    await _repository.InsertHistoryAsync(newId, "RecordCreated", null, "Open", "Relay.app");

                return Ok(new
                {
                    success = true,
                    id      = newId,
                    message = $"Interviewer availability record created (#{newId}) for {record.SenderEmail}."
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
