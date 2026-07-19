using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Enum;
using Risebird.Entities.Helpers;
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
        private readonly IPlivoCXService                         _plivoCXService;
        private readonly IPlivoCXCallRepository                  _cpRepo;
        private readonly IInterviewerListingSearchRepository     _interviewerListingSearchRepository;

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
            IFileMimeTypeHelper                     fileMimeTypeHelper,
            IPlivoCXService                         plivoCXService,
            IPlivoCXCallRepository                  cpRepo,
            IInterviewerListingSearchRepository     interviewerListingSearchRepository)
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
            _plivoCXService         = plivoCXService;
            _cpRepo                 = cpRepo;
            _interviewerListingSearchRepository = interviewerListingSearchRepository;
        }

        // ── Dashboard ───────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(
            string status        = null,
            string senderEmail   = null,
            string resumeFitment = null,
            string dateFrom      = null,
            string dateTo        = null,
            string source        = null,
            string autoClosed    = null,
            string activeStatus  = null,
            string recentlyAvailable = null,
            string magName       = null,
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

                bool? isAutoClosed = null;
                if (autoClosed == "yes") isAutoClosed = true;
                else if (autoClosed == "no") isAutoClosed = false;

                bool? isInterviewerActive = null;
                if (activeStatus == "active") isInterviewerActive = true;
                else if (activeStatus == "inactive") isInterviewerActive = false;

                bool? isRecentlyAvailable = null;
                if (recentlyAvailable == "yes") isRecentlyAvailable = true;
                else if (recentlyAvailable == "no") isRecentlyAvailable = false;

                var records = await _repository.GetEmailAvailabilityAsync(
                    status, senderEmail, resumeFitment, dtFrom, dtTo, page, pageSize, source, isAutoClosed, isInterviewerActive, isRecentlyAvailable, magName);

                var first      = records.FirstOrDefault();
                int totalCount = first?.TotalCount ?? 0;
                int totalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;

                ViewBag.Page          = page;
                ViewBag.PageSize      = pageSize;
                ViewBag.TotalCount    = totalCount;
                ViewBag.TotalPages    = totalPages;
                ViewBag.OpenCount     = first?.OpenCount ?? 0;
                ViewBag.InProgressCount = first?.InProgressCount ?? 0;
                ViewBag.ClosedCount   = first?.ClosedCount ?? 0;
                ViewBag.RecentlyAvailableCount = first?.RecentlyAvailableCount ?? 0;
                ViewBag.RecentlyAvailable = recentlyAvailable ?? string.Empty;
                ViewBag.Status        = status        ?? string.Empty;
                ViewBag.SenderEmail   = senderEmail   ?? string.Empty;
                ViewBag.ResumeFitment = resumeFitment  ?? string.Empty;
                ViewBag.DateFrom      = dateFrom      ?? string.Empty;
                ViewBag.DateTo        = dateTo        ?? string.Empty;
                ViewBag.Source        = source        ?? string.Empty;
                ViewBag.AutoClosed    = autoClosed    ?? string.Empty;
                ViewBag.ActiveStatus  = activeStatus  ?? string.Empty;
                ViewBag.MagName       = magName       ?? string.Empty;
                ViewBag.ProfileName   = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") ?? "";

                return View(records);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.Page = 1; ViewBag.PageSize = pageSize; ViewBag.TotalCount = 0; ViewBag.TotalPages = 1;
                ViewBag.OpenCount = 0; ViewBag.InProgressCount = 0; ViewBag.ClosedCount = 0;
                ViewBag.RecentlyAvailableCount = 0; ViewBag.RecentlyAvailable = recentlyAvailable;
                ViewBag.Status = status; ViewBag.SenderEmail = senderEmail; ViewBag.ResumeFitment = resumeFitment;
                ViewBag.DateFrom = dateFrom; ViewBag.DateTo = dateTo;
                ViewBag.Source = source; ViewBag.AutoClosed = autoClosed; ViewBag.ActiveStatus = activeStatus;
                ViewBag.MagName = magName ?? string.Empty;
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
                        record.Intent,
                        record.IntentSource,
                        intentConfirmedAt = record.IntentConfirmedAt?.ToString("dd MMM yyyy, HH:mm"),
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
                        hasProfileResumeLink = !string.IsNullOrEmpty(record.ProfileResumeLink),
                        record.PlivoCallStatus,
                        record.PlivoConversationId,
                        plivoCallAt = record.PlivoCallAt?.ToString("dd MMM yyyy, HH:mm"),
                        record.PlivoCallDuration,
                        record.PlivoCallSummary,
                        record.PlivoIntent,
                        record.PlivoAssignmentHelp,
                        record.PlivoEmailReceived,
                        record.PlivoCallbackRequested,
                        record.PlivoNotes,
                        record.PlivoCalledBy,
                        record.Source,
                        record.IsAutoClosed,
                        record.AutoClosedBy,
                        autoClosedAt = record.AutoClosedAt?.ToString("dd MMM yyyy, HH:mm"),
                        record.UserMobileNumber,
                        record.IsInterviewerActive,
                        record.InactivationType
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

            var allowed = new[] { "Open", "InProgress", "Closed" };
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

        // ── Inactivate Interviewer (from Availability page) ────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InactivateInterviewerFromEA(int eaId, string comment)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") ?? "";
            var allowed = new[] { "Admin", "SupportAdmin", "SeniorHR", "SeniorInterviewerManagement", "InterviewerManagement", "HR" };
            if (!Array.Exists(allowed, p => p == profileName))
                return Json(new { success = false, error = "Not authorised." });

            try
            {
                var record = await _repository.GetEmailAvailabilityByIdAsync(eaId);
                if (record == null || !record.UserId.HasValue)
                    return Json(new { success = false, error = "Record not found or no linked user." });

                var emailId = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "") ?? "";
                var result = await _interviewerService.InactivateInterviewer(
                    record.UserId.Value, emailId, string.IsNullOrWhiteSpace(comment) ? "Inactivated from Availability page" : comment, false);

                if (result)
                {
                    await _repository.InsertHistoryAsync(eaId, "InterviewerInactivated", "—",
                        $"Interviewer inactivated. Reason: {comment ?? "N/A"}", emailId);
                    return Json(new { success = true });
                }
                return Json(new { success = false, error = "Failed to inactivate." });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Reactivate Interviewer (from Availability page) ───────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateInterviewerFromEA(int eaId)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") ?? "";
            // Reactivating an inactivated interviewer is restricted to the Admin profile only.
            if (profileName != "Admin")
                return Json(new { success = false, error = "Not authorised. Only an Admin can reactivate an interviewer." });

            try
            {
                var record = await _repository.GetEmailAvailabilityByIdAsync(eaId);
                if (record == null || !record.UserId.HasValue)
                    return Json(new { success = false, error = "Record not found or no linked user." });

                var emailId = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "") ?? "";
                var result = await _interviewerService.ReactivateInterviewer(record.UserId.Value, emailId);

                if (result)
                {
                    await _repository.InsertHistoryAsync(eaId, "InterviewerReactivated", "—",
                        "Interviewer reactivated from Availability page", emailId);
                    return Json(new { success = true });
                }
                return Json(new { success = false, error = "Failed to reactivate." });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
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
                    await TrySyncInterviewerListingAsync(record.InterviewerProfileId.Value);
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

                // A positive AI verdict no longer becomes a distinct "Verified" status; the record
                // stays operationally "InProgress" (the verdict/score is preserved and returned below).
                string newStatus = "InProgress";

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

                bool verdictOk = false;
                if (!string.IsNullOrWhiteSpace(record.ResumeAIAnalysisResult))
                {
                    try
                    {
                        var parsed = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(record.ResumeAIAnalysisResult);
                        verdictOk = parsed != null
                            && !string.IsNullOrEmpty(parsed.verdict)
                            && parsed.verdict.IndexOf("yes", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    catch { }
                }
                if (!verdictOk)
                    return Json(new { success = false, error = "Resume fitment analysis verdict is not positive. Cannot assign to assessment group." });

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

        // ── Plivo CX Availability Call — Initiate (AJAX) ─────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiatePlivoCall(int id)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var record = await _repository.GetEmailAvailabilityByIdAsync(id);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                string phone = record.UserMobileNumber ?? record.Phone;
                if (string.IsNullOrWhiteSpace(phone))
                    return Json(new { success = false, error = "No phone number available for this interviewer." });

                phone = NormalizePhone(phone);

                var retryableStatuses = new[] { "trigger-failed", "no_answer", "busy_rejected", "failed", "no-answer", "busy" };
                if (!string.IsNullOrEmpty(record.PlivoCallStatus) &&
                    !Array.Exists(retryableStatuses, s => s.Equals(record.PlivoCallStatus, StringComparison.OrdinalIgnoreCase)))
                    return Json(new { success = false, error = $"A call has already been made (status: {record.PlivoCallStatus}).", alreadyCalled = true });

                var calledBy = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";

                await _repository.UpdatePlivoCallStatusAsync(id, "initiated", null,
                    DateTime.UtcNow, null, null, null, null, null, null, null, null, calledBy);

                var callerId = _configuration["PlivoCX:AvailabilityCallerId"]
                            ?? _configuration["PlivoCX:OnboardingCallerId"]
                            ?? _configuration["Plivo:FromNumber"]
                            ?? "+918031136547";

                var triggerRequest = new PlivoCXAvailabilityTriggerRequest
                {
                    CallerId        = callerId,
                    PhoneNumber     = phone,
                    InterviewerName = record.InterviewerName ?? record.InterviewerNickName ?? "there",
                    InterviewerEmail = record.SenderEmail,
                    MagName         = record.MagName ?? record.JobTitle ?? "Technical",
                    LogId           = id.ToString(),
                    TodayDate       = DateTime.Now.ToString("dddd, dd MMMM yyyy")
                };

                var response = await _plivoCXService.TriggerAvailabilityCallAsync(triggerRequest);

                if (!string.IsNullOrEmpty(response?.Error))
                {
                    await _repository.UpdatePlivoCallStatusAsync(id, "trigger-failed", null,
                        null, null, null, response.Error, null, null, null, null, null, null);
                    return Json(new { success = false, error = $"Plivo CX trigger failed: {response.Error}" });
                }

                string conversationId = response?.ConversationId;
                await _repository.UpdatePlivoCallStatusAsync(id, "triggered", conversationId,
                    null, null, null, null, null, null, null, null, null, null);

                await _repository.InsertHistoryAsync(id, "PlivoCallInitiated", "—",
                    $"Availability call triggered to {phone}. ConversationId: {conversationId ?? "N/A"}", calledBy);

                return Json(new { success = true, conversationId, message = $"Call initiated to {record.InterviewerName} ({phone})." });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = "Failed to initiate call: " + ex.Message });
            }
        }

        // ── Plivo CX Availability Call — Hangup Webhook (public endpoint) ────────

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PlivoAvailabilityHangup()
        {
            await SaveIaCp("ia_hangup_received",
                $"PlivoAvailabilityHangup hit — Content-Type: {Request.ContentType}");

            string rawBody;
            try
            {
                using (var reader = new StreamReader(Request.Body))
                    rawBody = await reader.ReadToEndAsync();
                await SaveIaCp("ia_hangup_body_read",
                    $"Raw body length: {rawBody?.Length ?? 0}",
                    extra: rawBody?.Length > 800 ? rawBody.Substring(0, 800) + "…" : rawBody);
            }
            catch (Exception ex)
            {
                await SaveIaCp("ia_hangup_body_read_failed", $"Body read FAILED: {ex.Message}");
                return Content("Error: Could not read request body.", "text/html");
            }

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                await SaveIaCp("ia_hangup_empty_body", "Empty request body received.");
                return Content("Error: Empty request body.", "text/html");
            }

            PlivoCXAvailabilityHangupPayload payload = null;
            int? recordId = null;
            string parseMethod = "none";

            // Strategy 1: Nested Plivo CX event envelope
            try
            {
                var nestedEvent = JsonConvert.DeserializeObject<PlivoCXHangupEvent>(rawBody);
                if (nestedEvent?.Data?.Object?.EventData != null)
                {
                    var ed = nestedEvent.Data.Object.EventData;
                    payload = new PlivoCXAvailabilityHangupPayload
                    {
                        ConversationId    = nestedEvent.Data.Object.ConversationId,
                        CallUUID          = nestedEvent.Data.Object.CallUuid,
                        CallStatus        = GetEventValue(ed, "call_status"),
                        Duration          = GetEventValue(ed, "duration"),
                        Transcript        = GetEventValue(ed, "transcript"),
                        Summary           = GetEventValue(ed, "summary"),
                        LogId             = GetEventValue(ed, "log_id"),
                        Intent            = GetEventValue(ed, "intent"),
                        AssignmentHelp    = GetEventValue(ed, "assignment_help"),
                        EmailReceived     = GetEventValue(ed, "email_received"),
                        CallbackRequested = GetEventValue(ed, "callback_requested"),
                        Notes             = GetEventValue(ed, "notes"),
                        HangupCause       = GetEventValue(ed, "hangup_cause")
                    };
                    parseMethod = "nested_event";
                    await SaveIaCp("ia_hangup_nested_parsed",
                        $"Nested parse OK — log_id={payload.LogId}, intent={payload.Intent}",
                        convId: payload.ConversationId);
                }
            }
            catch (Exception ex)
            {
                await SaveIaCp("ia_hangup_nested_parse_failed", $"Nested parse failed: {ex.Message}");
            }

            // Strategy 2: Flat JSON
            if (payload == null || string.IsNullOrEmpty(payload.LogId))
            {
                try
                {
                    payload = JsonConvert.DeserializeObject<PlivoCXAvailabilityHangupPayload>(rawBody,
                        new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
                    parseMethod = "flat_json";
                    await SaveIaCp("ia_hangup_flat_parsed",
                        $"Flat JSON parse — log_id={payload?.LogId}, intent={payload?.Intent}");
                }
                catch (Exception ex)
                {
                    await SaveIaCp("ia_hangup_flat_parse_failed", $"Flat parse failed: {ex.Message}");
                }
            }

            // Strategy 3: Wrapper {"none":{},"json":{...}}
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
                            payload = JsonConvert.DeserializeObject<PlivoCXAvailabilityHangupPayload>(innerJson,
                                new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
                            parseMethod = "wrapper_json";
                            await SaveIaCp("ia_hangup_wrapper_parsed",
                                $"Wrapper JSON parse — log_id={payload?.LogId}, intent={payload?.Intent}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await SaveIaCp("ia_hangup_wrapper_parse_failed", $"Wrapper parse failed: {ex.Message}");
                }
            }

            if (payload == null)
            {
                await SaveIaCp("ia_hangup_parse_failed", "All parse strategies failed — payload is null.");
                return Content("Error: Could not parse hangup payload.", "text/html");
            }

            // Resolve record ID.
            // A plain integer log_id points at an existing InterviewerEmailAvailability row.
            // A "PROF~…" token identifies a profile interviewer for whom NO row exists yet —
            // the row is created below only if the disposition confirms availability.
            bool   isDeferredProfileCall = false;
            int    deferredProfileId     = 0;
            string deferredMagName       = null;
            string deferredSource        = null;

            if (!string.IsNullOrEmpty(payload.LogId) && int.TryParse(payload.LogId, out int parsedId))
            {
                recordId = parsedId;
            }
            else if (AvailabilityCallToken.TryParse(payload.LogId, out deferredProfileId, out deferredMagName, out deferredSource))
            {
                isDeferredProfileCall = true;
                await SaveIaCp("ia_hangup_deferred_token",
                    $"Deferred profile call. profileId={deferredProfileId}, mag='{deferredMagName}', intent='{payload.Intent}'");
            }

            if (!recordId.HasValue && !isDeferredProfileCall && !string.IsNullOrEmpty(payload.PhoneNumber))
            {
                await SaveIaCp("ia_hangup_logid_missing",
                    $"log_id missing. Trying phone lookup: {payload.PhoneNumber}");
                var foundId = await _repository.FindByPhoneAndCallStatusAsync(payload.PhoneNumber, "initiated");
                if (!foundId.HasValue)
                    foundId = await _repository.FindByPhoneAndCallStatusAsync(payload.PhoneNumber, "triggered");
                if (foundId.HasValue)
                {
                    recordId = foundId.Value;
                    await SaveIaCp("ia_hangup_phone_lookup_ok", $"Phone lookup matched record #{recordId}");
                }
            }

            if (!recordId.HasValue && !isDeferredProfileCall)
            {
                await SaveIaCp("ia_hangup_no_record",
                    $"Could not resolve record. log_id='{payload.LogId}', phone='{payload.PhoneNumber}'");
                return Content($"Error: Missing log_id. log_id='{payload.LogId}', phone='{payload.PhoneNumber}'", "text/html");
            }

            try
            {
                string intentLower = payload.Intent?.ToLower();

                // For a deferred profile call, the interviewer is only added to
                // InterviewerEmailAvailability when they have CONFIRMED availability on the call.
                if (isDeferredProfileCall)
                {
                    if (!IsConfirmedAvailableIntent(intentLower))
                    {
                        // Unsubscribe on a deferred profile call: no availability record is created,
                        // but the interviewer must still be deactivated system-wide.
                        if (string.Equals(MapCallDispositionToIntent(intentLower), "Unsubscribe", StringComparison.OrdinalIgnoreCase))
                        {
                            await DeactivateProfileInterviewerOnUnsubscribeAsync(deferredProfileId, "voice call");
                            await SaveIaCp("ia_hangup_deferred_unsubscribe",
                                $"Unsubscribe disposition on deferred profile call for profile #{deferredProfileId} — interviewer deactivated system-wide.");
                            return Content("OK. Unsubscribe processed — interviewer deactivated.", "text/html");
                        }

                        await SaveIaCp("ia_hangup_deferred_skip",
                            $"No availability record created for profile #{deferredProfileId} — disposition '{payload.Intent ?? "none"}' is not a confirmed availability.");
                        return Content($"OK. No record created (intent='{payload.Intent ?? "none"}').", "text/html");
                    }

                    int createdId = await _repository.EnsureEARecordForProfileInterviewerAsync(
                        deferredProfileId, deferredMagName, deferredSource);
                    if (createdId <= 0)
                    {
                        await SaveIaCp("ia_hangup_deferred_create_failed",
                            $"Confirmed availability but could not create availability record for profile #{deferredProfileId} (inactive/deleted?).");
                        return Content("Error: Could not create availability record.", "text/html");
                    }

                    recordId = createdId;
                    await SaveIaCp("ia_hangup_deferred_created",
                        $"Created availability record #{recordId} for profile #{deferredProfileId} after confirmed availability.",
                        callLogId: recordId);
                }

                int? duration = null;
                if (!string.IsNullOrEmpty(payload.Duration) && int.TryParse(payload.Duration, out int dur))
                    duration = dur;

                bool? assignmentHelp = null;
                if (!string.IsNullOrEmpty(payload.AssignmentHelp))
                    assignmentHelp = payload.AssignmentHelp.Equals("true", StringComparison.OrdinalIgnoreCase);

                bool? emailReceived = null;
                if (!string.IsNullOrEmpty(payload.EmailReceived))
                    emailReceived = payload.EmailReceived.Equals("true", StringComparison.OrdinalIgnoreCase);

                bool? callbackRequested = null;
                if (!string.IsNullOrEmpty(payload.CallbackRequested))
                    callbackRequested = payload.CallbackRequested.Equals("true", StringComparison.OrdinalIgnoreCase);

                string callStatus = !string.IsNullOrEmpty(payload.CallStatus) ? payload.CallStatus.ToLower() : "completed";

                await _repository.UpdatePlivoCallStatusAsync(
                    recordId.Value, callStatus, payload.ConversationId,
                    DateTime.UtcNow, duration, payload.Transcript, payload.Summary,
                    payload.Intent?.ToLower(), assignmentHelp, emailReceived, callbackRequested,
                    payload.Notes, null);

                string historyDetail = $"Status: {callStatus}";
                if (!string.IsNullOrEmpty(payload.Intent))
                    historyDetail += $", Intent: {payload.Intent}";
                if (assignmentHelp == true)
                    historyDetail += ", Wants assignment help";
                if (duration.HasValue)
                    historyDetail += $", Duration: {duration}s";

                await _repository.InsertHistoryAsync(recordId.Value, "PlivoCallCompleted", "—",
                    historyDetail, "PlivoCX Webhook");

                // Unified Intent — the call disposition feeds the same single Intent as an
                // email reply. no_answer / unknown leaves any existing intent untouched.
                string callIntent = MapCallDispositionToIntent(payload.Intent);
                if (!string.IsNullOrEmpty(callIntent))
                {
                    await _repository.SetUnifiedIntentAsync(recordId.Value, callIntent, "Call", DateTime.UtcNow);
                    await _repository.InsertHistoryAsync(recordId.Value, "IntentSet", "—",
                        $"Unified Intent set to '{callIntent}' (source: Call)", "PlivoCX Webhook");
                    await SaveIaCp("ia_hangup_intent_set",
                        $"Unified intent '{callIntent}' set for record #{recordId} from call", callLogId: recordId);

                    // Unsubscribe on the call → deactivate the interviewer system-wide.
                    if (string.Equals(callIntent, "Unsubscribe", StringComparison.OrdinalIgnoreCase))
                    {
                        await DeactivateInterviewerOnUnsubscribeAsync(recordId.Value, "voice call");
                        await SaveIaCp("ia_hangup_unsubscribe",
                            $"Unsubscribe disposition on record #{recordId} — interviewer deactivated system-wide.", callLogId: recordId);
                    }
                }

                await SaveIaCp("ia_hangup_complete",
                    $"Processing finished for record #{recordId}. parseMethod={parseMethod}",
                    callLogId: recordId);

                // Auto-close if disposition is a confirmed availability
                if (IsConfirmedAvailableIntent(intentLower))
                {
                    _ = Task.Run(async () =>
                    {
                        try { await TryAutoCloseAsync(recordId.Value); }
                        catch { }
                    });
                }

                return Content($"OK. recordId={recordId.Value}", "text/html");
            }
            catch (Exception ex)
            {
                await SaveIaCp("ia_hangup_db_exception",
                    $"DB update exception for record #{recordId}: {ex.Message}", callLogId: recordId);
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
                var record = await _repository.GetEmailAvailabilityByIdAsync(id);
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
                    plivoIntent = record.PlivoIntent,
                    plivoAssignmentHelp = record.PlivoAssignmentHelp,
                    plivoEmailReceived = record.PlivoEmailReceived,
                    plivoCallbackRequested = record.PlivoCallbackRequested,
                    plivoNotes = record.PlivoNotes,
                    plivoCalledBy = record.PlivoCalledBy
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Call History — centralized page ──────────────────────────────────────────

        [HttpGet]
        public IActionResult CallHistory()
        {
            if (HttpContext.Session.GetString("email") == null)
                return RedirectToAction("Login", "Login");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCallDispositions(
            int page = 1, int pageSize = 50, string magName = null,
            string intent = null, string callStatus = null,
            string dateFrom = null, string dateTo = null)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });
            try
            {
                DateTime? from = null, to = null;
                if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out var df)) from = df;
                if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out var dt)) to = dt;

                var (items, totalCount) = await _repository.GetAllCallDispositionsAsync(
                    page, pageSize, magName, intent, callStatus, from, to);

                return Json(new
                {
                    success = true, totalCount, page, pageSize,
                    items = items.Select(d => new
                    {
                        d.Id, d.InterviewerProfileId, d.InterviewerName, d.SenderEmail,
                        phone = d.ResolvedPhone ?? d.Phone,
                        d.MagName, d.RecordStatus, d.Experience, d.CurrentRole,
                        d.PlivoCallStatus, d.PlivoConversationId, d.PlivoCallAt,
                        d.PlivoCallDuration, d.PlivoCallSummary, d.PlivoIntent,
                        d.PlivoAssignmentHelp, d.PlivoEmailReceived, d.PlivoCallbackRequested,
                        d.PlivoNotes, d.PlivoCalledBy
                    })
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBatchCallLogs(int page = 1, int pageSize = 50, string magName = null)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });
            try
            {
                var (items, totalCount) = await _repository.GetAllBatchCallLogsAsync(page, pageSize, magName);
                return Json(new
                {
                    success = true, totalCount, page, pageSize,
                    items = items.Select(h => new
                    {
                        h.Id, h.MagName, h.InitiatedBy, h.InitiatedAt,
                        h.InterviewersAttempted, h.InterviewersSucceeded,
                        h.InterviewersFailed, h.InterviewersSkipped,
                        h.CompletedAt, h.Status
                    })
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDistinctCalledMagNames()
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });
            try
            {
                var mags = await _repository.GetDistinctCalledMagNamesAsync();
                return Json(new { success = true, mags });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ── Batch Call — dedicated page ────────────────────────────────────────────

        [HttpGet]
        public IActionResult BatchCall()
        {
            if (HttpContext.Session.GetString("email") == null)
                return RedirectToAction("Login", "Login");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetBatchCallMAGs(int page = 1, int pageSize = 20, string search = null)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });
            try
            {
                var (mags, totalCount) = await _repository.GetMAGsWithCallableInterviewerCountsAsync(page, pageSize, search);
                return Json(new { success = true, mags, totalCount, page, pageSize });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInterviewerCallOutcomes(string magName, int page = 1, int pageSize = 25)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });
            try
            {
                var (interviewers, totalCount) = await _repository.GetInterviewerCallOutcomesForMAGAsync(magName, page, pageSize);
                return Json(new
                {
                    success = true,
                    totalCount,
                    page,
                    pageSize,
                    interviewers = interviewers.Select(p => new
                    {
                        p.Id, p.InterviewerProfileId, p.InterviewerName, p.SenderEmail, p.Phone,
                        p.Experience, p.CurrentRole,
                        p.PlivoCallStatus, p.PlivoIntent, p.PlivoAssignmentHelp,
                        p.PlivoEmailReceived, p.PlivoCallbackRequested,
                        p.PlivoCallDuration, p.PlivoCalledBy, p.PlivoCallSummary, p.PlivoNotes,
                        plivoCallAt = p.PlivoCallAt?.ToString("dd MMM yyyy, HH:mm"),
                        p.Status, p.Source
                    })
                });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchCallSingle(int id, string source = "EA", string magName = null)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var calledBy = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                var callerId = _configuration["PlivoCX:AvailabilityCallerId"]
                            ?? _configuration["PlivoCX:OnboardingCallerId"]
                            ?? _configuration["Plivo:FromNumber"]
                            ?? "+918031136547";

                // ── Profile interviewer (no availability record yet) ──────────────────
                // Do NOT create an InterviewerEmailAvailability row up-front. The row is
                // created by the hangup webhook only if the call confirms availability.
                if (source == "Profile")
                {
                    if (string.IsNullOrEmpty(magName))
                        return Json(new { success = false, error = "MAG name is required to call a profile interviewer." });

                    var contact = await _repository.GetProfileInterviewerContactAsync(id);
                    if (contact == null)
                        return Json(new { success = false, error = "Interviewer profile not found or inactive." });

                    string profilePhone = contact.UserMobileNumber ?? contact.Phone;
                    if (string.IsNullOrWhiteSpace(profilePhone))
                        return Json(new { success = false, error = "No phone number." });

                    profilePhone = NormalizePhone(profilePhone);

                    var profileTrigger = new PlivoCXAvailabilityTriggerRequest
                    {
                        CallerId         = callerId,
                        PhoneNumber      = profilePhone,
                        InterviewerName  = contact.InterviewerName ?? "there",
                        InterviewerEmail = contact.SenderEmail,
                        MagName          = magName,
                        LogId            = AvailabilityCallToken.Build(id, magName, "BatchCall"),
                        TodayDate        = DateTime.Now.ToString("dddd, dd MMMM yyyy")
                    };

                    var profileResponse = await _plivoCXService.TriggerAvailabilityCallAsync(profileTrigger);
                    if (!string.IsNullOrEmpty(profileResponse?.Error))
                        return Json(new { success = false, error = $"Plivo trigger failed: {profileResponse.Error}" });

                    return Json(new
                    {
                        success         = true,
                        conversationId  = profileResponse?.ConversationId,
                        interviewerName = contact.InterviewerName,
                        phone           = profilePhone,
                        eaId            = 0,        // no record yet — created only on confirmed availability
                        deferred        = true
                    });
                }

                // ── Existing availability record ──────────────────────────────────────
                int eaId = id;

                var record = await _repository.GetEmailAvailabilityByIdAsync(eaId);
                if (record == null)
                    return Json(new { success = false, error = "Record not found." });

                string phone = record.UserMobileNumber ?? record.Phone;
                if (string.IsNullOrWhiteSpace(phone))
                    return Json(new { success = false, error = "No phone number." });

                phone = NormalizePhone(phone);

                await _repository.UpdatePlivoCallStatusAsync(eaId, "initiated", null,
                    DateTime.UtcNow, null, null, null, null, null, null, null, null, calledBy);

                var triggerRequest = new PlivoCXAvailabilityTriggerRequest
                {
                    CallerId        = callerId,
                    PhoneNumber     = phone,
                    InterviewerName = record.InterviewerName ?? record.InterviewerNickName ?? "there",
                    InterviewerEmail = record.SenderEmail,
                    MagName         = record.MagName ?? record.JobTitle ?? "Technical",
                    LogId           = eaId.ToString(),
                    TodayDate       = DateTime.Now.ToString("dddd, dd MMMM yyyy")
                };

                var response = await _plivoCXService.TriggerAvailabilityCallAsync(triggerRequest);

                if (!string.IsNullOrEmpty(response?.Error))
                {
                    await _repository.UpdatePlivoCallStatusAsync(eaId, "trigger-failed", null,
                        null, null, null, response.Error, null, null, null, null, null, null);
                    return Json(new { success = false, error = $"Plivo trigger failed: {response.Error}" });
                }

                string conversationId = response?.ConversationId;
                await _repository.UpdatePlivoCallStatusAsync(eaId, "triggered", conversationId,
                    null, null, null, null, null, null, null, null, null, null);
                await _repository.InsertHistoryAsync(eaId, "BatchCallInitiated", "—",
                    $"Availability batch call triggered to {phone}. ConversationId: {conversationId ?? "N/A"}", calledBy);

                return Json(new { success = true, conversationId, interviewerName = record.InterviewerName, phone, eaId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCallableInterviewersForMAG(string magName, bool onlyUncalled = true)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });
            try
            {
                var interviewers = await _repository.GetCallableInterviewersForMAGAsync(magName, onlyUncalled);
                var items = interviewers.Select(p => new
                {
                    p.Id, p.InterviewerProfileId, p.InterviewerName, p.Phone, p.SenderEmail,
                    p.ExperienceYears, p.PlivoCallStatus, p.Source
                });
                return Json(new { success = true, interviewers = items });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartBatchCallLog(string magName, int totalInterviewers)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });
            try
            {
                var initiatedBy = HttpContext.Session.GetString("email")?.Trim('"') ?? "Unknown";
                var logId = await _repository.InsertBatchCallLogAsync(magName, initiatedBy, totalInterviewers);
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
                        h.InterviewersAttempted, h.InterviewersSucceeded, h.InterviewersFailed, h.InterviewersSkipped,
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

        // ── Checkpoint helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Runs the resume-fitment + assign-to-AG + close pipeline for an EA record
        /// whose disposition is favorable (e.g. Plivo "available" or Relay positive intent).
        /// Designed to be fire-and-forget so it doesn't block the calling webhook response.
        /// </summary>
        private async Task TryAutoCloseAsync(int eaId, string actor = "PlivoCX AutoClose")
        {

            try
            {
                await SaveIaCp("ia_autoclose_start", $"Starting auto-close for record #{eaId}", callLogId: eaId);

                var record = await _repository.GetEmailAvailabilityByIdAsync(eaId);
                if (record == null)
                {
                    await SaveIaCp("ia_autoclose_skip", $"Record #{eaId} not found", callLogId: eaId);
                    return;
                }

                if (string.Equals(record.Status, "Closed", StringComparison.OrdinalIgnoreCase))
                {
                    await SaveIaCp("ia_autoclose_skip", $"Record #{eaId} already closed", callLogId: eaId);
                    return;
                }

                // Step 1: Resume Fitment Analysis (if resume exists and not already analyzed)
                if (record.ResumeAIAnalysisCompleted != true)
                {
                    string resumeUrl = record.ResumeLink;
                    if (string.IsNullOrWhiteSpace(resumeUrl) || string.Equals(resumeUrl, "None", StringComparison.OrdinalIgnoreCase))
                        resumeUrl = record.ProfileResumeLink;

                    if (!string.IsNullOrWhiteSpace(resumeUrl))
                    {
                        try
                        {
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

                            if (!string.IsNullOrWhiteSpace(resumeText))
                            {
                                var aiResponse = await _assistantServiceHelper.GenerateResumeAnalysis(resumeText, jobTitle);
                                var analysisJson = aiResponse?.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value;
                                if (!string.IsNullOrWhiteSpace(analysisJson))
                                {
                                    var analysis = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(analysisJson);

                                    await _repository.UpdateEmailAvailabilityVerificationAsync(
                                        eaId, employmentVerified: null, resumeAIAnalysisCompleted: true,
                                        resumeAIAnalysisResult: analysisJson, newStatus: "InProgress");

                                    await _repository.InsertHistoryAsync(eaId, "ResumeAnalysis", null,
                                        $"AutoClose — Verdict: {analysis?.verdict}, Score: {analysis?.fitmentScore}", actor);

                                    await SaveIaCp("ia_autoclose_resume_done",
                                        $"Resume analysis done for #{eaId}. Verdict: {analysis?.verdict}", callLogId: eaId);
                                }
                            }
                            else
                            {
                                await SaveIaCp("ia_autoclose_resume_unreadable",
                                    $"Could not read resume for #{eaId}, skipping analysis", callLogId: eaId);
                            }
                        }
                        catch (Exception ex)
                        {
                            await SaveIaCp("ia_autoclose_resume_error",
                                $"Resume analysis error for #{eaId}: {ex.Message}", callLogId: eaId);
                        }
                    }
                    else
                    {
                        await SaveIaCp("ia_autoclose_no_resume",
                            $"No resume available for #{eaId}, skipping analysis", callLogId: eaId);
                    }
                }

                // Reload record after potential resume analysis update
                record = await _repository.GetEmailAvailabilityByIdAsync(eaId);

                // Gate: Only proceed to AG assignment if resume fitment verdict is positive
                bool verdictPositive = false;
                if (record.ResumeAIAnalysisCompleted == true && !string.IsNullOrWhiteSpace(record.ResumeAIAnalysisResult))
                {
                    try
                    {
                        var parsed = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(record.ResumeAIAnalysisResult);
                        verdictPositive = parsed != null
                            && !string.IsNullOrEmpty(parsed.verdict)
                            && parsed.verdict.IndexOf("yes", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    catch { }
                }

                if (!verdictPositive)
                {
                    string verdictReason = record.ResumeAIAnalysisCompleted != true
                        ? "Resume analysis not completed (no resume or unreadable)"
                        : "Resume fitment verdict is not positive";
                    await SaveIaCp("ia_autoclose_verdict_block",
                        $"Skipping AG assignment for #{eaId}: {verdictReason}", callLogId: eaId);

                    await _repository.UpdateEmailAvailabilityStatusAsync(eaId, "InProgress", actor);
                    await _repository.InsertHistoryAsync(eaId, "AutoCloseBlocked", record.Status ?? "Open",
                        $"InProgress — {verdictReason}", actor);
                    return;
                }

                // Step 2: Assign to Assessment Group (if not already assigned)
                // agMapped tracks whether the interviewer is actually mapped to an AG —
                // the record may only be auto-closed when this is true.
                bool agMapped = record.AssessmentGroupId.HasValue && record.AssessmentGroupId.Value > 0;

                if (record.InterviewerProfileId.HasValue
                    && !agMapped
                    && record.ResolvedAssessmentGroupId.HasValue && record.ResolvedAssessmentGroupId.Value > 0)
                {
                    try
                    {
                        int agId = record.ResolvedAssessmentGroupId.Value;
                        int profileId = record.InterviewerProfileId.Value;

                        var assigned = await _repository.AssignInterviewerToAssessmentGroupAsync(
                            eaId, profileId, agId, actor);

                        if (assigned)
                        {
                            agMapped = true;
                            await _repository.InsertHistoryAsync(eaId, "AssignedToAG", null,
                                $"AutoClose — Assigned to AG #{agId}", actor);
                            await SaveIaCp("ia_autoclose_ag_assigned",
                                $"Assigned #{eaId} to AG #{agId}", callLogId: eaId);

                            // Send notification email + WhatsApp
                            try
                            {
                                string emailId = record.SenderEmail;
                                var upcomingInterviews = await _interviewerService.FetchAllInterviewerallUpcomingInterview("Verified", null, agId);
                                var interviewerItem = upcomingInterviews?.FirstOrDefault(x => x.EmailId == emailId);

                                if (interviewerItem != null && !string.IsNullOrEmpty(interviewerItem.EmailId))
                                {
                                    string sendReportUrl;
                                    if (string.IsNullOrEmpty(interviewerItem.uniqueid))
                                    {
                                        var uniqueId = Guid.NewGuid();
                                        var savedId = await _clientService.PublicInterviewerUrlSave(interviewerItem.EmailId, uniqueId.ToString());
                                        sendReportUrl = "https://portal.risebird.io/Interview/PublicOpenInterviewInvite?id=" +
                                            (string.IsNullOrEmpty(savedId) ? uniqueId.ToString() : savedId);
                                        if (!string.IsNullOrEmpty(savedId))
                                            interviewerItem.uniqueid = savedId;
                                    }
                                    else
                                    {
                                        sendReportUrl = "https://portal.risebird.io/Interview/PublicOpenInterviewInvite?id=" + interviewerItem.uniqueid;
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
                                                ? sendReportUrl.Replace("https://portal.risebird.io/", "")
                                                : "",
                                            Event = "Interviewer Common Interview",
                                            irfId = interviewerItem.irfIntId.HasValue ? interviewerItem.irfIntId.Value : 0
                                        };
                                        await _whatsAppHelper.WhatsAppSendInterviewerCommonInterviewDetails(whatsAppVM);
                                    }
                                    catch { }

                                    await SaveIaCp("ia_autoclose_notification_sent",
                                        $"Notification sent to {emailId} for AG #{agId}", callLogId: eaId);
                                }
                            }
                            catch (Exception ex)
                            {
                                await SaveIaCp("ia_autoclose_notification_error",
                                    $"Notification error for #{eaId}: {ex.Message}", callLogId: eaId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await SaveIaCp("ia_autoclose_ag_error",
                            $"AG assignment error for #{eaId}: {ex.Message}", callLogId: eaId);
                    }
                }
                else
                {
                    string reason = !record.InterviewerProfileId.HasValue ? "No InterviewerProfileId"
                        : agMapped ? "Already assigned to AG"
                        : "No ResolvedAssessmentGroupId";
                    await SaveIaCp("ia_autoclose_ag_skip",
                        $"Skipping AG assignment for #{eaId}: {reason}", callLogId: eaId);
                }

                // Step 3: Only close when the interviewer is actually mapped to an AG.
                // Closing on the AI verdict alone — while the MAG is still unresolved or the
                // assignment failed — produces a contradictory "Closed but unresolved MAG" state.
                if (!agMapped)
                {
                    string blockReason = !record.InterviewerProfileId.HasValue
                            ? "No interviewer profile to map"
                        : (!record.ResolvedAssessmentGroupId.HasValue || record.ResolvedAssessmentGroupId.Value <= 0)
                            ? "MAG could not be resolved to an Assessment Group"
                            : "Assessment Group assignment did not complete";

                    await _repository.UpdateEmailAvailabilityStatusAsync(eaId, "InProgress", actor);
                    await _repository.InsertHistoryAsync(eaId, "AutoCloseBlocked", record.Status ?? "Open",
                        $"InProgress — Not closed: {blockReason}. Requires manual MAG/AG resolution.", actor);
                    await SaveIaCp("ia_autoclose_not_closed",
                        $"Record #{eaId} left InProgress (not closed): {blockReason}", callLogId: eaId);
                    return;
                }

                // Interviewer is mapped to an AG — safe to close and mark as auto-closed.
                await _repository.UpdateEmailAvailabilityStatusAsync(eaId, "Closed", actor);
                await _repository.MarkAutoClosedAsync(eaId, actor);
                await _repository.InsertHistoryAsync(eaId, "StatusChange", record.Status ?? "Open", "Closed", actor);
                await SaveIaCp("ia_autoclose_done", $"Auto-close completed for #{eaId}", callLogId: eaId);
            }
            catch (Exception ex)
            {
                await SaveIaCp("ia_autoclose_fatal",
                    $"Auto-close fatal error for #{eaId}: {ex.Message}", callLogId: eaId);
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        // A Plivo availability call only "confirms availability" when the AI-assessed
        // disposition is positive/interested. Anything else (not_available, no_answer,
        // unsubscribe, callback, empty) is NOT a confirmation.
        private static bool IsConfirmedAvailableIntent(string intent)
        {
            if (string.IsNullOrWhiteSpace(intent))
                return false;

            switch (intent.Trim().ToLowerInvariant())
            {
                case "available":
                case "interested":
                case "yes":
                case "confirmed":
                case "willing":
                    return true;
                default:
                    return false;
            }
        }

        // ── Unified availability intent ────────────────────────────────────────────
        // Both the email reply and the Plivo voice-agent call feed ONE canonical Intent
        // so the dashboard never shows a blank intent for call-driven records.
        // Canonical values: "Available" | "NotAvailable" | "Unsubscribe".

        // Maps a Plivo call disposition to the canonical intent.
        // Returns null for no_answer / unknown so a previously-set intent is not overwritten.
        private static string MapCallDispositionToIntent(string disposition)
        {
            switch ((disposition ?? "").Trim().ToLowerInvariant())
            {
                case "available":
                case "interested":
                case "yes":
                case "confirmed":
                case "willing":
                    return "Available";
                case "not_available":
                case "notavailable":
                case "not_interested":
                case "no":
                case "declined":
                    return "NotAvailable";
                case "unsubscribe":
                case "drop":
                case "drop_profile":
                case "dropprofile":
                case "drop_my_profile":
                case "drop my profile":
                case "remove":
                case "remove_me":
                case "remove_my_profile":
                case "remove my profile":
                    return "Unsubscribe";
                default:
                    return null;
            }
        }

        // Recognises the various ways a "stop contacting me / remove me" request can be phrased,
        // from either a call disposition or free-text email body. Any of these resolves to the
        // canonical 'Unsubscribe' intent (which triggers system-wide deactivation).
        private static bool IsUnsubscribeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var t = text.ToLowerInvariant();
            return t.Contains("unsubscribe")
                || t.Contains("drop my profile")
                || t.Contains("drop profile")
                || t.Contains("remove my profile")
                || t.Contains("remove my account")
                || t.Contains("delete my profile")
                || t.Contains("delete my account")
                || t.Contains("stop contacting")
                || t.Contains("stop emailing")
                || t.Contains("take me off");
        }

        // Maps the email-extracted availability fields to the canonical intent.
        // Returns null when the signal is ambiguous (so we don't assert an intent we can't infer).
        private static string MapEmailToIntent(string availabilityIntent, string mailBodyIntent, string concernsOrNotInterested)
        {
            var combined = $"{availabilityIntent} {mailBodyIntent} {concernsOrNotInterested}".ToLowerInvariant();
            if (IsUnsubscribeText(combined))
                return "Unsubscribe";

            if (IsFavorableAvailabilityIntent(availabilityIntent, mailBodyIntent, concernsOrNotInterested))
                return "Available";

            var concern = (concernsOrNotInterested ?? "").Trim().ToLowerInvariant();
            bool hasBlockingConcern = !string.IsNullOrEmpty(concern)
                && concern != "none" && concern != "no" && concern != "n/a" && concern != "na" && concern != "nil";

            var text = $"{availabilityIntent} {mailBodyIntent}".ToLowerInvariant();
            var negativeKeywords = new[] { "not available", "unavailable", "not interested", "declined", "decline" };
            if (hasBlockingConcern || negativeKeywords.Any(k => text.Contains(k)))
                return "NotAvailable";

            return null;
        }

        // ── Unsubscribe → system-wide deactivation ──────────────────────────────────
        // When an interviewer's unified intent resolves to 'Unsubscribe' (from EITHER a voice
        // call or an email reply) they must be pulled out of active circulation everywhere:
        //   • UserList.IsActive = 0  (with InactivationType = 'Unsubscribe')
        //   • every TransInterviewer (IRF) row → IsActive = 0, IsAvailable = 0
        // and the availability ticket is closed. Idempotent: a no-op if already inactive or if
        // the sender isn't a known interviewer in the system.
        private async Task DeactivateInterviewerOnUnsubscribeAsync(int eaId, string medium)
        {
            try
            {
                var rec = await _repository.GetEmailAvailabilityByIdAsync(eaId);
                if (rec == null) return;

                // Unsubscribe ends the conversation — close the ticket regardless.
                if (!string.Equals(rec.Status, "Closed", StringComparison.OrdinalIgnoreCase))
                {
                    await _repository.UpdateEmailAvailabilityStatusAsync(eaId, "Closed", "System (Unsubscribe)");
                    await _repository.InsertHistoryAsync(eaId, "StatusChange", rec.Status ?? "Open", "Closed", "System (Unsubscribe)");
                }

                // No linked system user → nothing active to deactivate.
                if (!rec.UserId.HasValue || rec.UserId.Value <= 0)
                {
                    await _repository.InsertHistoryAsync(eaId, "UnsubscribeNoUser", "—",
                        $"Unsubscribe received via {medium}, but no linked system user to deactivate.", "System (Unsubscribe)");
                    return;
                }

                // Already inactive — don't repeat the work.
                if (!rec.IsInterviewerActive)
                {
                    await _repository.InsertHistoryAsync(eaId, "UnsubscribeAlreadyInactive", "—",
                        $"Unsubscribe received via {medium}; interviewer (UserId {rec.UserId}) already inactive.", "System (Unsubscribe)");
                    return;
                }

                var reason = $"Auto-inactivated: interviewer requested unsubscribe / drop profile via {medium}.";
                var ok = await _interviewerService.InactivateInterviewer(
                    rec.UserId.Value, "System (Unsubscribe)", reason, false, "Unsubscribe");

                await _repository.InsertHistoryAsync(eaId,
                    ok ? "InterviewerInactivated" : "UnsubscribeInactivateFailed",
                    "Active",
                    ok ? $"Interviewer (UserId {rec.UserId}) inactivated system-wide (UserList + TransInterviewer) due to unsubscribe via {medium}."
                       : $"Failed to inactivate interviewer (UserId {rec.UserId}) on unsubscribe via {medium}.",
                    "System (Unsubscribe)");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        // Unsubscribe handling for a deferred profile call (IRF auto-call / batch / campaign to an
        // EXISTING profile interviewer) where no availability record is — or should be — created.
        // We still deactivate the interviewer system-wide. No-op if already inactive/dropped.
        private async Task DeactivateProfileInterviewerOnUnsubscribeAsync(int interviewerProfileId, string medium)
        {
            try
            {
                var contact = await _repository.GetProfileInterviewerContactAsync(interviewerProfileId);
                if (contact == null || !contact.UserId.HasValue || contact.UserId.Value <= 0)
                    return; // already inactive/dropped, or not resolvable — nothing to deactivate

                var reason = $"Auto-inactivated: profile interviewer requested unsubscribe / drop profile via {medium}.";
                await _interviewerService.InactivateInterviewer(
                    contact.UserId.Value, "System (Unsubscribe)", reason, false, "Unsubscribe");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        private async Task SaveIaCp(
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

        [HttpGet]
        public async Task<IActionResult> AvailabilityCheckpoints(int count = 100)
        {
            try
            {
                var all = await _cpRepo.GetRecentCheckpointsAsync(count);
                var ia = all.Where(c => c.CheckpointName.StartsWith("ia_")).ToList();
                return Json(ia);
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

        /// <summary>
        /// Determines whether the Relay-extracted availability signals indicate a favorable
        /// (auto-closeable) disposition — i.e. the interviewer is available/interested and
        /// has not expressed concerns or disinterest.
        /// </summary>
        private static bool IsFavorableAvailabilityIntent(
            string availabilityIntent, string mailBodyIntent, string concernsOrNotInterested)
        {
            var concern = (concernsOrNotInterested ?? "").Trim().ToLower();
            if (!string.IsNullOrEmpty(concern)
                && concern != "none" && concern != "no" && concern != "n/a" && concern != "na" && concern != "nil")
                return false;

            var positiveKeywords = new[] { "available", "yes", "interested", "confirmed", "willing", "agree", "positive" };

            var intent = (availabilityIntent ?? "").Trim().ToLower();
            if (positiveKeywords.Any(k => intent.Contains(k)))
                return true;

            var bodyIntent = (mailBodyIntent ?? "").Trim().ToLower();
            if (positiveKeywords.Any(k => bodyIntent.Contains(k)))
                return true;

            return false;
        }

        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return phone;
            phone = phone.Trim();
            if (phone.Length == 10 && !phone.StartsWith("+"))
                return "+91" + phone;
            if (phone.Length > 10 && !phone.StartsWith("+"))
                return "+" + phone;
            return phone;
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
                    HasAttachment           = payload.HasAttachment,
                    Source                  = "Relay"
                };

                var newId = await _repository.InsertEmailAvailabilityAsync(record);

                if (newId > 0)
                    await _repository.InsertHistoryAsync(newId, "RecordCreated", null, "Open", "Relay.app");

                // Unified Intent — the email reply feeds the same single Intent as the Plivo call.
                if (newId > 0)
                {
                    string emailIntent = MapEmailToIntent(
                        payload.AvailabilityIntent, payload.MailBodyIntent, payload.ConcernsOrNotInterested);
                    if (!string.IsNullOrEmpty(emailIntent))
                    {
                        await _repository.SetUnifiedIntentAsync(newId, emailIntent, "Email", record.ReceivedAt);
                        await _repository.InsertHistoryAsync(newId, "IntentSet", "—",
                            $"Unified Intent set to '{emailIntent}' (source: Email)", "Relay.app");

                        // Unsubscribe in the email reply → deactivate the interviewer system-wide.
                        if (string.Equals(emailIntent, "Unsubscribe", StringComparison.OrdinalIgnoreCase))
                            await DeactivateInterviewerOnUnsubscribeAsync(newId, "email reply");
                    }
                }

                // Auto-close if the Relay-extracted availability intent is favorable
                if (newId > 0 && IsFavorableAvailabilityIntent(
                        payload.AvailabilityIntent, payload.MailBodyIntent, payload.ConcernsOrNotInterested))
                {
                    _ = Task.Run(async () =>
                    {
                        try { await TryAutoCloseAsync(newId, "Relay AutoClose"); }
                        catch { }
                    });
                }

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

        private async Task TrySyncInterviewerListingAsync(int interviewerProfileId)
        {
            if (_interviewerListingSearchRepository == null
                || !_interviewerListingSearchRepository.IsEnabled
                || interviewerProfileId <= 0)
                return;

            try
            {
                var synced = await _interviewerListingSearchRepository.UpsertInterviewerAsync(interviewerProfileId);
                if (!synced)
                {
                    await _logger.LogMessage(
                        LogLevelInfo.Exception,
                        new InvalidOperationException(
                            $"Interviewer Azure Search upsert returned false for interviewerProfileId={interviewerProfileId}."));
                }
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
