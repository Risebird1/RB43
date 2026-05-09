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
            IPlivoCXService               plivoCXService)
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
        }

        // ── Dashboard ───────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(
            string status        = null,
            string candidateName = null,
            string magName       = null,
            string source        = null,
            string dateFrom      = null,
            string dateTo        = null,
            int    page          = 1,
            int    pageSize      = 25)
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
                ViewBag.ProfileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") ?? "";

                return View(records);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.Page = 1; ViewBag.PageSize = pageSize; ViewBag.TotalCount = 0; ViewBag.TotalPages = 1;
                ViewBag.Status = status; ViewBag.CandidateName = candidateName; ViewBag.MagName = magName;
                ViewBag.Source = source ?? string.Empty;
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
                        record.Status, record.Source, record.ProcessedBy,
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

            var allowed = new[] { "Open", "InProgress", "Verified", "Onboarded", "Rejected", "Closed" };
            if (id <= 0 || !Array.Exists(allowed, s => s == status))
                return Json(new { success = false, error = "Invalid status." });

            var changedBy = HttpContext.Session.GetString("email").Trim('"');
            var existing = await _repository.GetByIdAsync(id);
            var ok = await _repository.UpdateStatusAsync(id, status, changedBy);
            if (ok)
                await _repository.InsertHistoryAsync(id, "StatusChange", existing?.Status ?? "—", status, changedBy);
            return Json(new { success = ok, status });
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
                string parsedPhone = ExtractPhone(resumeText);

                var (isExisting, existingUserId, existingProfileId) =
                    await _repository.UpdateResumeParseResultAsync(id, parsedEmail, parsedPhone);

                var user = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                await _repository.InsertHistoryAsync(id, "ResumeParse", "—",
                    $"Email: {parsedEmail ?? "N/A"}, Phone: {parsedPhone ?? "N/A"}" + (isExisting ? $" (Existing UserId: {existingUserId})" : " (New)"), user);

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

        // ── Step 2: Verify Employment (save result from client-side Signzy call) ────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveEmploymentResult(int id, bool verified, string companyName, bool isEmployed, string details)
        {
            if (HttpContext.Session.GetString("email") == null)
                return Json(new { success = false, error = "Not signed in." });

            try
            {
                var ok = await _repository.UpdateEmploymentVerificationAsync(id, verified, companyName, isEmployed, details);
                if (ok)
                {
                    var user = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                    string evSummary = $"Company: {companyName ?? "N/A"}, Employed: {(isEmployed ? "Yes" : "No")}";
                    await _repository.InsertHistoryAsync(id, "EmploymentVerification", "—", evSummary, user);
                }
                return Json(new { success = ok });
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

                if (record.IsExistingInterviewer == true)
                    return Json(new { success = false, error = "This interviewer already exists in the system. Cannot re-onboard." });

                if (string.IsNullOrWhiteSpace(record.ParsedEmail))
                    return Json(new { success = false, error = "Email not parsed yet. Complete Step 1 first." });

                if (!record.ResolvedAssessmentGroupId.HasValue || record.ResolvedAssessmentGroupId.Value <= 0)
                    return Json(new { success = false, error = $"No assessment group found for MAG: '{record.MagName}'." });

                int agId = record.ResolvedAssessmentGroupId.Value;
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
                    IsActive = true,
                    Isavailable = 1,
                    CompanyName = record.EmploymentCompanyName ?? "",
                    IsEmployed = record.EmploymentIsEmployed == true,
                    ResourceUrl = $"https://{Request.Host}/NaukriOnboarding/Index"
                };

                bool addResult;
                string assessmentGroupList = null;
                int userId = 0;
                string uniqueid = null;
                try
                {
                    addResult = _interviewerService.AddInterviewerProfile(
                        profileVM, loggedInUser,
                        out assessmentGroupList, out userId, out uniqueid);
                }
                catch (Exception svcEx)
                {
                    await _logger.LogMessage(LogLevelInfo.Exception, svcEx);
                    return Json(new { success = false, error = $"Failed to create interviewer profile: {svcEx.Message}" });
                }

                if (!addResult || userId <= 0)
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

                int? profileId = await _repository.GetInterviewerProfileIdByUserIdAsync(userId);

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

                await _repository.InsertHistoryAsync(id, "Onboarded", "—",
                    $"UserId: {userId}, AG: {agId}, Ack email: {(ackSent ? "Sent" : "Failed")}", loggedInUser);

                return Json(new
                {
                    success = true,
                    userId,
                    uniqueid,
                    assessmentGroupId = agId,
                    acknowledgementEmailSent = ackSent,
                    message = ackSent
                        ? $"Interviewer onboarded (UserId: {userId}) and mapped to AG + channels. Acknowledgement email sent."
                        : $"Interviewer onboarded (UserId: {userId}) and mapped to AG + channels. Acknowledgement email FAILED — use Resend button."
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

                return Json(new { success = true, message = "Acknowledgement email sent successfully." });
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

                return Json(new { success = true, applicationLink = appLink, message = "Skill channel selected." });
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

                if (string.IsNullOrWhiteSpace(record.ApplicationLink))
                    return Json(new { success = false, error = "No application link generated. Select a skill channel first." });

                string candidateName = record.CandidateName ?? "there";
                string magName = record.MagName ?? record.SelectedIRFName ?? "Interviewer";
                string appLink = record.ApplicationLink;

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
                    $"Sent to: {email}, Channel: {record.SelectedIRFName ?? "\u2014"}", user);

                return Json(new { success = true, message = "Invitation email sent to " + email + "." });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, error = "Failed to send invitation email: " + ex.Message });
            }
        }

        // ── Prospect response action (public endpoint — clicked from email) ─────

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ProspectResponseAction(int id, string response)
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

                var (foundUserId, foundProfileId) = await _repository.FindUserByEmailAsync(record.ParsedEmail);

                if (foundUserId.HasValue && foundUserId.Value > 0)
                {
                    await _repository.UpdateSelfRegistrationAsync(id, true, foundUserId.Value, foundProfileId);
                    var sysUser = HttpContext.Session.GetString("email")?.Trim('"') ?? "System";
                    await _repository.InsertHistoryAsync(id, "SelfRegistered", "\u2014",
                        $"Prospect self-registered. UserId: {foundUserId.Value}", sysUser);
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
                    ResumeLink      = blobResumeUrl ?? sourceResumeUrl,
                    HasAttachment   = !string.IsNullOrWhiteSpace(blobResumeUrl) || payload.HasAttachment,
                    Source          = "Naukri"
                };

                var newId = await _repository.InsertAsync(record);

                if (newId > 0)
                    await _repository.InsertHistoryAsync(newId, "RecordCreated", "\u2014",
                        $"Candidate: {record.CandidateName}, MAG: {record.MagName}, Source: Naukri", "Relay Webhook");

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
                    ResumeLink      = blobResumeUrl ?? sourceResumeUrl,
                    HasAttachment   = !string.IsNullOrWhiteSpace(blobResumeUrl) || payload.HasAttachment,
                    Source          = "LinkedIn"
                };

                var newId = await _repository.InsertAsync(record);

                if (newId > 0)
                    await _repository.InsertHistoryAsync(newId, "RecordCreated", "—",
                        $"Candidate: {record.CandidateName}, MAG: {record.MagName}, Source: LinkedIn", "Relay Webhook");

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
            try
            {
                string rawBody;
                using (var reader = new StreamReader(Request.Body))
                    rawBody = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(rawBody))
                    return BadRequest(new { error = "Empty request body." });

                PlivoCXOnboardingHangupPayload payload = null;
                int? recordId = null;

                // Try parsing as nested Plivo CX event first
                try
                {
                    var nestedEvent = JsonConvert.DeserializeObject<PlivoCXHangupEvent>(rawBody);
                    if (nestedEvent?.Data?.Object?.EventData != null)
                    {
                        var ed = nestedEvent.Data.Object.EventData;
                        payload = new PlivoCXOnboardingHangupPayload
                        {
                            ConversationId     = nestedEvent.Data.Object.ConversationId,
                            CallUUID           = nestedEvent.Data.Object.CallUuid,
                            CallStatus         = GetEventValue(ed, "call_status"),
                            Duration           = GetEventValue(ed, "duration"),
                            Transcript         = GetEventValue(ed, "transcript"),
                            Summary            = GetEventValue(ed, "summary"),
                            LogId              = GetEventValue(ed, "log_id"),
                            ProspectDisposition   = GetEventValue(ed, "prospect_disposition"),
                            OnboardingPreference  = GetEventValue(ed, "onboarding_preference"),
                            KycAccepted           = GetEventValue(ed, "kyc_accepted"),
                            HangupCause        = GetEventValue(ed, "hangup_cause")
                        };
                    }
                }
                catch { }

                // Fallback: direct flat JSON
                if (payload == null || string.IsNullOrEmpty(payload.LogId))
                {
                    payload = JsonConvert.DeserializeObject<PlivoCXOnboardingHangupPayload>(rawBody,
                        new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore });
                }

                if (payload == null)
                    return BadRequest(new { error = "Could not parse hangup payload." });

                if (!string.IsNullOrEmpty(payload.LogId) && int.TryParse(payload.LogId, out int parsedId))
                    recordId = parsedId;

                if (!recordId.HasValue)
                    return BadRequest(new { error = "Missing log_id in hangup payload." });

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

                await _repository.UpdatePlivoCallStatusAsync(
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

                string historyDetail = $"Status: {callStatus}";
                if (!string.IsNullOrEmpty(payload.ProspectDisposition))
                    historyDetail += $", Disposition: {payload.ProspectDisposition}";
                if (!string.IsNullOrEmpty(payload.OnboardingPreference))
                    historyDetail += $", Preference: {payload.OnboardingPreference}";
                if (duration.HasValue)
                    historyDetail += $", Duration: {duration}s";

                await _repository.InsertHistoryAsync(recordId.Value, "PlivoCallCompleted", "—",
                    historyDetail, "PlivoCX Webhook");

                return Ok(new { success = true, recordId = recordId.Value });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return StatusCode(500, new { error = ex.Message });
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
