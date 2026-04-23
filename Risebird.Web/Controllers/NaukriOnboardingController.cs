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
            IHttpClientFactory            httpClientFactory)
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
        }

        // ── Dashboard ───────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(
            string status        = null,
            string candidateName = null,
            string magName       = null,
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

                var records = await _repository.GetListAsync(status, candidateName, magName, dtFrom, dtTo, page, pageSize);

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
                ViewBag.DateFrom = dateFrom; ViewBag.DateTo = dateTo;
                ViewBag.ProfileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") ?? "";
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
                        record.Status, record.ProcessedBy,
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
            var ok = await _repository.UpdateStatusAsync(id, status, changedBy);
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
                    return Json(new { success = false, error = "Could not read the resume. The file may be corrupted or in an unsupported format. Try uploading a valid PDF.", needsUpload = true });

                string parsedEmail = ExtractEmail(resumeText);
                string parsedPhone = ExtractPhone(resumeText);

                var (isExisting, existingUserId, existingProfileId) =
                    await _repository.UpdateResumeParseResultAsync(id, parsedEmail, parsedPhone);

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
                return Json(new { success = false, error = "Parse failed: " + ex.Message });
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
                    HasAttachment   = !string.IsNullOrWhiteSpace(blobResumeUrl) || payload.HasAttachment
                };

                var newId = await _repository.InsertAsync(record);

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
    }
}
