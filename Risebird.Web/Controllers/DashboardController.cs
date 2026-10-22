using ExcelDataReader.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph.Me.RemoveAllDevicesFromManagement;
using Newtonsoft.Json;
using OfficeOpenXml;
using Risebird.Business;

//using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Constants;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository;
using Risebird.Entities.Data.AffIndaModels;
//using Risebird.Repository;
using Risebird.Repository.Interface;
using Risebird.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig.Logging;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using Azure.Core;
using Risebird.Entities.Data.VapiModels;
using Risebird.Entities.Data.PlivoModels;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Data.PlivoWhatsAppModels;
using NuGet.Protocol.Core.Types;
//using System.Web.WebPages;
//using UglyToad.PdfPig.Logging;
//using Risebird.Entities.Data.AffIndaModels;

namespace Risebird.Web.Controllers
{

    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IUserService _userService;
        private IConfiguration _configuration;
        private readonly IMeetingService _meetingService;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IUserRepository _userRepository;
        private readonly IInterviewerService _interviewerService;
        private readonly IMeetingRespository _meetingRespository;
        private readonly IZoomHelper _zoomHelper;
        private readonly ICandidateService _candidateService;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IIRFRepository _irfRepository;
        private readonly IIRFService _irfService;
        private readonly ITwilioHelper _twilioHelper;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IPdfReaderHelper _pdfReaderHelper;
        private readonly IAffIndaResumeParserHelper _affIndaResumeParserHelper;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IValidationHelper _validationHelper;
        private readonly IAWSRekognitionService _aWSRekognitionService;
        private readonly IZohoMeetingHelper _zoho;
        private readonly IResumeOptimizationHelper _resumeOptimizationHelper;
        private readonly IVapiService _vapiService;
        private readonly IVapiCallRepository _vapiCallRepository;
        private readonly IPhoneNumberHelper _phoneNumberHelper;
        private readonly IPlivoService _plivoService;
        private readonly IPlivoCallRepository _plivoCallRepository;
        private readonly IPlivoCXService _plivoCXService;
        private readonly IPlivoCXCallRepository _plivoCXCallRepository;
        private readonly IPlivoWhatsAppService _plivoWhatsAppService;
        private readonly IPlivoWhatsAppRepository _plivoWhatsAppRepository;
        private readonly ISupportTicketRepository _repository;
        private readonly ITranscriptOptimizationHelper _transcriptOptimizationHelper;
        private readonly IQcInterviewAIMergeJobService _qcInterviewAIMergeJobService;
        private readonly IAutoQcRemovalMonitorService _autoQcRemovalMonitorService;
        private readonly IDmIvAutoQcBackfillService _autoQcBackfillService;
        private readonly IDmIvAutoQcRemovalService _autoQcRemovalService;
        private readonly ITrialFlagReviewService _trialFlagReviewService;
        private readonly ITrialJudgmentService _trialJudgmentService;
        private readonly IClientRepository _clientRepository;
        private readonly IClientService _clientService;
        private readonly IClonableSkillChannelTemplateService _clonableSkillChannelTemplateService;

        //private readonly IIRFService _irfService;

        private readonly ICodeProjectSettingsProvider _codeProjectSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        #region Code project infrastructure setup

        /// <summary>
        /// Admin-only proxy to the functions app's one-click code project setup
        /// (CodeProjects/Setup in Risebird.AzureTimerFunctions). The target is CodeProjects:FunctionsBaseUrl,
        /// or the local functions host (http://localhost:7071/api) when running in Development.
        /// </summary>
        [HttpPost]
        public Task<IActionResult> CodeProjectSetupStart() => ProxyCodeProjectSetupAsync(HttpMethod.Post, "start");

        [HttpGet]
        public Task<IActionResult> CodeProjectSetupStatus() => ProxyCodeProjectSetupAsync(HttpMethod.Get, "status");

        private async Task<IActionResult> ProxyCodeProjectSetupAsync(HttpMethod method, string action)
        {
            var profile = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            var email = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            if (string.IsNullOrEmpty(email) || profile != "Admin")
                return Json(new { status = "error", message = "Only an Admin can run the code project setup." });

            var baseUrl = _codeProjectSettings.FunctionsBaseUrl();
            if (string.IsNullOrEmpty(baseUrl))
                return Json(new { status = "error", message = "Set CodeProjects:FunctionsBaseUrl (or run the web app in Development next to 'func start')." });

            try
            {
                var url = $"{baseUrl}/codeprojects/setup/{action}" + (action == "start" ? "?by=" + Uri.EscapeDataString(email) : "");
                using var client = _httpClientFactory.CreateClient();
                // Start runs the first steps (database, providers, …) before answering.
                client.Timeout = TimeSpan.FromMinutes(action == "start" ? 10 : 1);
                using var request = new HttpRequestMessage(method, url);
                var key = _codeProjectSettings.Get(CodeProjectSettingNames.FunctionsKey);
                if (!string.IsNullOrEmpty(key)) request.Headers.Add("x-functions-key", key);
                using var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                return Content(body, "application/json");
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return Json(new { status = "error", message = $"Could not reach the functions app at {baseUrl}: {ex.Message}. Is it running?" });
            }
        }

        #endregion

        public DashboardController(IDashboardService dashboardService, IApplicationErrorLogger applicationErrorLogger,
            IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper,
            IUserService userService, IConfiguration configuration, IMeetingService meetingService,
            IFileMimeTypeHelper fileMimeTypeHelper, IUserRepository userRepository, IInterviewerService interviewerService,
            IMeetingRespository meetingRespository, IZoomHelper zoomHelper, ICandidateService candidateService,
            IAssistantServiceHelper assistantServiceHelper, IIRFRepository irfRepository,
            ITwilioHelper twilioHelper, IEncryptionHelper encryptionHelper, IIRFService irfService,
            ICandidateRepository candidateRepository, IPdfReaderHelper pdfReaderHelper,
            IValidationHelper validationHelper,
            IAffIndaResumeParserHelper affIndaResumeParserHelper, IAWSRekognitionService aWSRekognitionService,
            IZohoMeetingHelper zoho, IResumeOptimizationHelper resumeOptimizationHelper,
            IVapiService vapiService, IVapiCallRepository vapiCallRepository,
            IPhoneNumberHelper phoneNumberHelper,
            IPlivoService plivoService, IPlivoCallRepository plivoCallRepository,
            IPlivoCXService plivoCXService, IPlivoCXCallRepository plivoCXCallRepository,
            IPlivoWhatsAppService plivoWhatsAppService, IPlivoWhatsAppRepository plivoWhatsAppRepository, ISupportTicketRepository repository, ITranscriptOptimizationHelper transcriptOptimizationHelper,
            IQcInterviewAIMergeJobService qcInterviewAIMergeJobService,
            IAutoQcRemovalMonitorService autoQcRemovalMonitorService,
            IDmIvAutoQcBackfillService autoQcBackfillService,
            IDmIvAutoQcRemovalService autoQcRemovalService,
            ITrialFlagReviewService trialFlagReviewService,
            ITrialJudgmentService trialJudgmentService,
            IClientRepository clientRepository,
            IClientService clientService,
            IClonableSkillChannelTemplateService clonableSkillChannelTemplateService,
            ICodeProjectSettingsProvider codeProjectSettings,
            IHttpClientFactory httpClientFactory)//, IAffIndaResumeParserHelper affIndaResumeParserHelper, ICandidateRepository candidateRepository, IValidationHelper validationHelper, IIRFService irfService
        {
            _codeProjectSettings = codeProjectSettings;
            _httpClientFactory = httpClientFactory;
            _dashboardService = dashboardService;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _azureStorageHelper = azureStorageHelper;
            _userService = userService;
            _configuration = configuration;
            _meetingService = meetingService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _userRepository = userRepository;
            _interviewerService = interviewerService;
            _meetingRespository = meetingRespository;
            _zoomHelper = zoomHelper;
            _candidateService = candidateService;
            _assistantServiceHelper = assistantServiceHelper;
            _irfRepository = irfRepository;
            _twilioHelper = twilioHelper;
            _encryptionHelper = encryptionHelper;
            _irfService = irfService;
            _candidateRepository = candidateRepository;
            _pdfReaderHelper = pdfReaderHelper;
            //_affIndaResumeParserHelper = affIndaResumeParserHelper;
            //_candidateRepository = candidateRepository;
            _validationHelper = validationHelper;
            _affIndaResumeParserHelper = affIndaResumeParserHelper;
            _aWSRekognitionService = aWSRekognitionService;
            _zoho = zoho;
            _resumeOptimizationHelper = resumeOptimizationHelper;
            _vapiService = vapiService;
            _vapiCallRepository = vapiCallRepository;
            _phoneNumberHelper = phoneNumberHelper;
            _plivoService = plivoService;
            _plivoCallRepository = plivoCallRepository;
            _plivoCXService = plivoCXService;
            _plivoCXCallRepository = plivoCXCallRepository;
            _plivoWhatsAppService = plivoWhatsAppService;
            _plivoWhatsAppRepository = plivoWhatsAppRepository;
            _repository = repository;
            _transcriptOptimizationHelper = transcriptOptimizationHelper;
            _qcInterviewAIMergeJobService = qcInterviewAIMergeJobService;
            _autoQcRemovalMonitorService = autoQcRemovalMonitorService;
            _autoQcBackfillService = autoQcBackfillService;
            _autoQcRemovalService = autoQcRemovalService;
            _trialFlagReviewService = trialFlagReviewService;
            _trialJudgmentService = trialJudgmentService;
            _clientRepository = clientRepository;
            _clientService = clientService;
            _clonableSkillChannelTemplateService = clonableSkillChannelTemplateService;
        }

        private static bool IsTrialFlagReviewerProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return false;
            return profileName == "Admin"
                || profileName == "HR"
                || profileName == "SeniorHR"
                || profileName == "Quality Controller"
                || profileName == "SupportAdmin"
                || profileName == "InterviewerManagement"
                || profileName == "SeniorInterviewerManagement"
                || profileName == "TechSupport"
                || profileName == "Finance";
        }

        private static bool IsAutoQcOpsProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return false;
            return profileName == "Admin"
                || profileName == "Quality Controller"
                || profileName == "SupportAdmin"
                || profileName == "SeniorHR"
                || profileName == "HR"
                || profileName == "InterviewerManagement"
                || profileName == "SeniorInterviewerManagement"
                || profileName == "TechSupport";
        }

        /// <summary>HR/SeniorHR can access IRF Audit but only for IRFs they own as Channel Owner (IRF.RisebirdHr).</summary>
        private static bool IsIrfAuditOwnerProfile(string profileName) =>
            profileName == "HR" || profileName == "SeniorHR";

        /// <summary>Ops profiles that can act on any IRF in IRF Audit without an ownership check.</summary>
        private static bool IsIrfAuditFullAccessProfile(string profileName) =>
            IsAutoQcOpsProfile(profileName) && !IsIrfAuditOwnerProfile(profileName);

        /// <summary>
        /// Gate for Keep/Remove actions on IRF Audit: ops profiles act on any IRF; HR/SeniorHR must own the IRF
        /// (IRF.RisebirdHr → UserList.EmailId matches the signed-in session email).
        /// </summary>
        private async Task<bool> CanActOnIrfAsync(int irfId, string email, string profileName)
        {
            if (IsIrfAuditFullAccessProfile(profileName))
                return true;
            if (!IsIrfAuditOwnerProfile(profileName))
                return false;
            if (irfId <= 0 || string.IsNullOrWhiteSpace(email))
                return false;

            var ownerEmail = await _autoQcBackfillService.GetIrfChannelOwnerEmailAsync(irfId);
            return !string.IsNullOrWhiteSpace(ownerEmail)
                && string.Equals(ownerEmail.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Date picker values are IST calendar dates; aligns with QCDoneOn storage (IST wall clock).</summary>
        private static readonly TimeSpan IstOffset = TimeSpan.FromHours(5.5);

        private static DateTime IstToday() => (DateTime.UtcNow + IstOffset).Date;

        private static (DateTime FromIst, DateTime ToIst) NormalizeBackfillIstDateRange(DateTime? from, DateTime? to)
        {
            var toDay = (to ?? IstToday()).Date;
            var fromDay = (from ?? toDay.AddDays(-90)).Date;
            if (fromDay > toDay)
                fromDay = toDay.AddDays(-90);
            return (fromDay, toDay);
        }

        /// <summary>IRF Audit Directory list: active skill channels with completed interviews in range (IST calendar dates, max 90-day span).</summary>
        private static (DateTime FromIst, DateTime ToIst, string Preset) NormalizeDirectoryIstDateRange(DateTime? from, DateTime? to, string preset)
        {
            var toDay = IstToday();
            var normalizedPreset = string.IsNullOrWhiteSpace(preset) ? null : preset.Trim().ToLowerInvariant();

            if (normalizedPreset == "1w")
            {
                return (toDay.AddDays(-7), toDay, "1w");
            }

            if (normalizedPreset == "2m")
            {
                return (toDay.AddDays(-60), toDay, "2m");
            }

            if (normalizedPreset == "1m")
            {
                return (toDay.AddDays(-30), toDay, "1m");
            }

            if (from.HasValue || to.HasValue)
            {
                toDay = (to ?? IstToday()).Date;
                var fromDay = (from ?? toDay.AddDays(-30)).Date;
                if (fromDay > toDay)
                    fromDay = toDay;
                if ((toDay - fromDay).TotalDays > 90)
                    fromDay = toDay.AddDays(-90);
                return (fromDay, toDay, "custom");
            }

            return (toDay.AddDays(-30), toDay, "1m");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index(bool testVapiCalls = false, bool testPlivoCalls = false, bool testPlivoRescheduleCalls = false,
            bool testPlivoWhatsAppSchedule = false, bool testPlivoWhatsAppReschedule = false,
            bool diagnoseWhatsApp = false, string waRunId = null, string waFlowId = null,
            bool testSurepassWorkflow = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                    ViewBag.ProfileName = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "TechSupport")
                    {
                        //var irfs = await MatchIRFAsync("snehal.choudhari@quadranttechnologies.com", "Java");
                        // ========================================
                        // PLIVO WHATSAPP DIAGNOSTICS
                        // Access via: /Dashboard/Index?diagnoseWhatsApp=true
                        // With run detail: /Dashboard/Index?diagnoseWhatsApp=true&waRunId=RUN_ID&waFlowId=FLOW_ID
                        // ========================================
                        if (diagnoseWhatsApp)
                        {
                            return await DiagnoseWhatsAppAsync(waRunId, waFlowId);
                        }

                        // ========================================
                        // SUREPASS WORKFLOW — TEST MODE
                        // Access via: /Dashboard/Index?testSurepassWorkflow=true
                        // ========================================
                        if (testSurepassWorkflow)
                        {
                            return await RunSurepassWorkflowTestAsync(Emailid);
                        }

                        // ========================================
                        // PLIVO WHATSAPP RESCHEDULE — TEST/TRIGGER MODE
                        // Access via: /Dashboard/Index?testPlivoWhatsAppReschedule=true
                        // ========================================
                        if (testPlivoWhatsAppReschedule)
                        {
                            return await RunPlivoWhatsAppRescheduleAsync(Emailid);
                        }

                        // ========================================
                        // PLIVO WHATSAPP SCHEDULE — TEST/TRIGGER MODE
                        // Access via: /Dashboard/Index?testPlivoWhatsAppSchedule=true
                        // ========================================
                        if (testPlivoWhatsAppSchedule)
                        {
                            return await RunPlivoWhatsAppScheduleAsync(Emailid);
                        }

                        // ========================================
                        // PLIVO CX RESCHEDULE CALLS — TEST/TRIGGER MODE
                        // Access via: /Dashboard/Index?testPlivoRescheduleCalls=true
                        // ========================================
                        if (testPlivoRescheduleCalls)
                        {
                            return await RunPlivoRescheduleCallsAsync(Emailid);
                        }
                        // ========================================
                        // END PLIVO CX RESCHEDULE TEST MODE
                        // ========================================

                        // ========================================
                        // PLIVO + OPENAI REALTIME AUTOMATED CALLS — TEST/TRIGGER MODE
                        // Access via: /Dashboard/Index?testPlivoCalls=true
                        // ========================================
                        if (testPlivoCalls)
                        {
                            return await RunPlivoCallsAsync(Emailid);
                        }
                        // ========================================
                        // END PLIVO TEST MODE
                        // ========================================

                        // ========================================
                        //VAPI AUTOMATED CALLS TEST MODE
                        //Access via: / Dashboard / Index ? testVapiCalls = true
                        //========================================
                        //if (testVapiCalls)
                        //{
                        //    var testResults = new List<string>();
                        //    var currentTime = DateTime.Now;
                        //    var currentHour = currentTime.Hour;

                        //    testResults.Add($"🕐 Test initiated at: {currentTime:yyyy-MM-dd HH:mm:ss}");

                        //    try
                        //    {
                        //         Check business hours (9 AM - 9 PM)
                        //        if (currentHour < 9 || currentHour >= 21)
                        //        {
                        //            testResults.Add($"⏰ SKIPPED - Current time {currentTime:HH:mm} is outside business hours (9 AM - 9 PM IST)");
                        //            ViewBag.VapiTestResults = testResults;
                        //            return View("VapiTestResults");
                        //        }

                        //        testResults.Add($"✅ Within business hours - Current time: {currentTime:HH:mm}");

                        //         Get eligible candidates
                        //        testResults.Add("📋 Fetching eligible candidates...");
                        //        var eligibleCandidates = await _vapiCallRepository.GetEligibleCandidatesForVapiCallsAsync(1);

                        //        if (eligibleCandidates == null || !eligibleCandidates.Any())
                        //        {
                        //            testResults.Add("❌ No eligible candidates found for calling");
                        //            ViewBag.VapiTestResults = testResults;
                        //            return View("VapiTestResults");
                        //        }

                        //        testResults.Add($"✅ Found {eligibleCandidates.Count} eligible candidates");
                        //        testResults.Add("");

                        //         Get configuration
                        //        var baseUrl = _configuration["HostDetails:baseUrl"];
                        //        var assistantId = _configuration["Vapi:InterviewBookingAssistantId"];

                        //        if (string.IsNullOrEmpty(assistantId))
                        //        {
                        //            testResults.Add("❌ ERROR: Vapi Assistant ID is not configured");
                        //            testResults.Add("   Please set Vapi:InterviewBookingAssistantId in configuration");
                        //            ViewBag.VapiTestResults = testResults;
                        //            return View("VapiTestResults");
                        //        }

                        //        testResults.Add($"✅ Using Assistant ID: {assistantId}");
                        //        testResults.Add($"✅ Webhook URL: {baseUrl}/VapiInterviewCall/Webhook");
                        //        testResults.Add("");

                        //         Process each candidate
                        //        int successCount = 0;
                        //        int failureCount = 0;

                        //        foreach (var candidate in eligibleCandidates) //.Where(x => x.CandidateEmail == "ashuseth2050@gmail.com").ToList()
                        //        {
                        //            try
                        //            {
                        //                testResults.Add($"📞 Processing: {candidate.CandidateName}");
                        //                testResults.Add($"   Client: {candidate.ClientName}");
                        //                testResults.Add($"   Email: {candidate.CandidateEmail}");
                        //                testResults.Add($"   Phone: {candidate.CandidatePhone}");
                        //                testResults.Add($"   IRF ID: {candidate.IRFId} ({candidate.IRFName})");
                        //                testResults.Add($"   Previous Attempts: {candidate.CallAttempts}");

                        //                 Validate phone
                        //                if (string.IsNullOrWhiteSpace(candidate.CandidatePhone))
                        //                {
                        //                    testResults.Add("   ⚠️ SKIPPED - No phone number");
                        //                    testResults.Add("");
                        //                    continue;
                        //                }

                        //                 Validate and sanitize phone number into E.164 format (+91XXXXXXXXXX)
                        //                if (!_phoneNumberHelper.TrySanitizeIndianPhoneNumber(candidate.CandidatePhone, out var phoneNumber, out var phoneError))
                        //                {
                        //                    testResults.Add($"Skipped {candidate.CandidateName}: invalid phone - {phoneError}");
                        //                    continue;
                        //                }
                        //                 Get system prompt with current date and dynamic client name
                        //                var systemPromptWithDate = GetSystemPromptWithCurrentDate(
                        //                    candidate.CandidateName,
                        //                    candidate.JobTitle ?? candidate.IRFName,
                        //                    candidate.IRFId.ToString(),
                        //                    candidate.CandidateEmail,
                        //                    "harshit_jajoo@risebird.io",
                        //                    candidate.ClientName);

                        //                 Create call request
                        //                var callRequest = new VapiCallRequest
                        //                {
                        //                    PhoneNumberId = _configuration["Vapi:PhoneNumberId"],
                        //                    Customer = new VapiCustomer
                        //                    {
                        //                        Number = phoneNumber,
                        //                        Name = candidate.CandidateName,
                        //                        Email = candidate.CandidateEmail
                        //                        Email = "harshit_jajoo@risebird.io"
                        //                    },
                        //                    AssistantId = assistantId,
                        //                    AssistantOverrides = new VapiAssistantOverrides
                        //                    {
                        //                        Model = new VapiModelOverride
                        //                        {
                        //                            Provider = "openai",
                        //                            Model = "gpt-4.1-mini",
                        //                            Messages = new List<VapiMessage>
                        //                            {
                        //                                new VapiMessage
                        //                                {
                        //                                    Role = "system",
                        //                                    Content = systemPromptWithDate
                        //                                }
                        //                            }
                        //                        }
                        //                    },
                        //                    VariableValues = new Dictionary<string, object>
                        //                    {
                        //                        { "irfId", candidate.IRFId.ToString() },
                        //                        { "candidateEmail", candidate.CandidateEmail }
                        //                    },
                        //                    CandidateId = candidate.CandidateId
                        //                };

                        //                 Initiate the call — VapiService increments VapiCallAttempts on success
                        //                var callResponse = await _vapiService.InitiateCallAsync(callRequest);

                        //                testResults.Add($"   ✅ Call initiated - CallId: {callResponse.Id}, Status: {callResponse.Status}");

                        //                 Save call log
                        //                var callLog = new VapiCallLog
                        //                {
                        //                    VapiCallId = callResponse.Id,
                        //                    InterviewId = null,
                        //                    IRFId = candidate.IRFId,
                        //                    RecipientId = candidate.CandidateId,
                        //                    RecipientPhone = phoneNumber,
                        //                    RecipientName = candidate.CandidateName,
                        //                    RecipientEmail = candidate.CandidateEmail,
                        //                    CallStatus = callResponse.Status,
                        //                    ScheduledDateTime = null,
                        //                    CallStartedAt = callResponse.StartedAt,
                        //                    AssistantId = assistantId,
                        //                    CreatedAt = DateTime.UtcNow,
                        //                    CreatedBy = $"Test-{Emailid}"
                        //                };

                        //                await _vapiCallRepository.SaveCallLogAsync(callLog);

                        //                testResults.Add($"   ✅ Call log saved to database");
                        //                successCount++;

                        //                 Add delay between calls
                        //                await Task.Delay(2000); // 2 seconds
                        //                testResults.Add("");
                        //            }
                        //            catch (Exception ex)
                        //            {
                        //                failureCount++;
                        //                testResults.Add($"   ❌ FAILED: {ex.Message}");
                        //                testResults.Add("");
                        //                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        //                    new Exception($"Test Vapi call failed for {candidate.CandidateEmail} (IRFId: {candidate.IRFId})", ex));
                        //            }
                        //        }

                        //        testResults.Add("═══════════════════════════════════");
                        //        testResults.Add($"📊 SUMMARY:");
                        //        testResults.Add($"   Total Candidates: {eligibleCandidates.Count}");
                        //        testResults.Add($"   Successful Calls: {successCount}");
                        //        testResults.Add($"   Failed Calls: {failureCount}");
                        //        testResults.Add("═══════════════════════════════════");

                        //        ViewBag.VapiTestResults = testResults;
                        //        return View("VapiTestResults");
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        testResults.Add($"❌ CRITICAL ERROR: {ex.Message}");
                        //        testResults.Add($"   Stack: {ex.StackTrace}");
                        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        //        ViewBag.VapiTestResults = testResults;
                        //        return View("VapiTestResults");
                        //    }
                        //}
                        // ========================================
                        // END VAPI TEST MODE
                        // ========================================

                        //await ParseresumeCopy2();
                        //var list = new List<string>();
                        //list.Add("harshitjajoo95@gmail.com");
                        //await _notificationHelper.SendIRFBasedInterviewerMail(list, DateTime.Now.ToString(), string.Empty, 100, 100);
                        //await _notificationHelper.EmergencyInterviewerMail("harshitjajoo95@gmail.com", DateTime.Now.ToString(), string.Empty, 100, 100);
                        //await GenerateTranscriptSummary();
                        //var videoUrl = "https://risebirdportal.blob.core.windows.net/botinterviewvideo/dbf97ddf-838d-439c-94e5-c079863e5f7f202510241703490521.mp4?sv=2024-11-04&spr=https&st=2025-10-24T11%3A33%3A49Z&se=2026-05-24T11%3A33%3A49Z&sr=c&sp=r&sig=iY%2BfFYOJ%2Fm0kVU0Llg0tsau9WUUInSAAlarLm7UkkYg%3D";
                        //var key = await _aWSRekognitionService.UploadVideoAsync(videoUrl);

                        ////key = "uploads/8207d478-a83f-4404-95b5-7eebbfcea380_dbf97ddf-838d-439c-94e5-c079863e5f7f202510241703490521.mp4";

                        //string key = "uploads/Azure_Blob_Video_encoded.mp4";
                        //var faceDetection = await _aWSRekognitionService.AnalyzeVideoAsync(key);
                        //for (int i = 0; i < 15; i++)
                        //{
                        //    //await ParseOpenAIResume();
                        //    await ParseOpenAIResumeTImerFunctionCopy();

                        //}
                        var model = new DashboardVM();
                        //await _assistantServiceHelper.DeleteFiles();
                        //daily interview tracking first chart
                        //model = GetDateWiseInterviews(DateTime.Now);
                        //daily interview tracking slots
                        //model.DailyInterviewSlots = await _dashboardService.GetDateWiseMeetingsSlots(DateTime.Now, Emailid);
                        model.InterviewerCountObj = await _dashboardService.GetInterviewerCount();
                        model.NotUpdatedMeetingsObj = await _dashboardService.GetNotUpdatedMeetingsCount(Emailid);
                        var progressDate = TempData.Peek("ProgressDate")?.ToString();
                        if (!string.IsNullOrEmpty(progressDate))
                        {
                            model.ProgressDate = Convert.ToDateTime(progressDate);
                            TempData.Keep("ProgressDate");
                        }
                        else
                        {
                            model.ProgressDate = DateTime.Now;
                        }
                        //model.IrfProgressList = (IList<IRF>)(await _dashboardService.GetIRFProgress(model.ProgressDate, Emailid));
                        //model.MeetingResultCountObj = await _dashboardService.GetMeetingResultCount(Emailid);
                        //CandidatesCountObj model.CandidatesCountObj = await _dashboardService.GetCandidatesCount(Emailid);
                        model.TotalCompletedMeetings = model.Meetings != null ? model.Meetings.Where(x => x.InterviewStatus.HasValue).Count() : 0;
                        model.ProgressOptions = new SelectList(await _dashboardService.GetProgressOptions(), "id", "Name");
                        var Assassination = from AssassinationStatus s in Enum.GetValues(typeof(AssassinationStatus))
                                            select new { ID = (int)s, Name = s.GetDescription() };
                        model.AssassinationList = new SelectList(Assassination, "ID", "Name");
                        //need to Update
                        //var channelrequest = await _dashboardService.FetchChannelRequests();
                        //model.ChannelRequest = channelrequest;
                        var interviewmanagement = (IList<InterviewManagementVM>)await _dashboardService.GetInterviewManagement(Emailid);
                        interviewmanagement = interviewmanagement?.OrderByDescending(x => x.interviewDate).ToList();
                        if (interviewmanagement != null)
                        {
                            foreach (var item in interviewmanagement)
                            {
                                item.interviewDate = item.interviewDate.Date;
                            }
                            model.InterviewManagement = interviewmanagement;
                        }
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        private async Task<IrfMatchVM> MatchIRFAsync(string senderEmail, string jobTitle)
        {
            var irfs = await _repository.GetIRFsForSenderAsync(senderEmail);
            if (irfs == null || irfs.Count == 0) return null;
            if (irfs.Count == 1) return irfs[0];

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

            var f = irfField.ToLowerInvariant();
            var jt = jobTitle.ToLowerInvariant();

            if (jt.Contains(f) || f.Contains(jt)) return 100;

            var sep = new[] { ' ', ',', '/', '+', '-', '.', '_', '(', ')' };
            var fWords = f.Split(sep, StringSplitOptions.RemoveEmptyEntries)
                           .Where(w => w.Length > 2).ToHashSet();
            var jtWords = jt.Split(sep, StringSplitOptions.RemoveEmptyEntries)
                            .Where(w => w.Length > 2).ToHashSet();

            if (fWords.Count == 0 || jtWords.Count == 0) return 0;

            int matches = fWords.Count(w => jtWords.Contains(w));
            if (matches == 0) return 0;

            return (int)((double)matches / Math.Max(fWords.Count, jtWords.Count) * 90);
        }

        // ─── Plivo call trigger ───────────────────────────────────────────────────

        /// <summary>
        /// Fetches one eligible candidate and fires a Plivo + OpenAI Realtime AI call.
        /// Access via GET /Dashboard/Index?testPlivoCalls=true
        /// Triggers a Plivo CX outbound AI call for one eligible candidate.
        /// Results are returned as the PlivoTestResults view (reuses VapiTestResults layout).
        /// </summary>
        private async Task<IActionResult> RunPlivoCallsAsync(string triggeredByEmail)
        {
            var testResults = new List<string>();
            var currentTime = DateTime.Now;
            var currentHour = currentTime.Hour;

            testResults.Add($"[Plivo CX] Test initiated at: {currentTime:yyyy-MM-dd HH:mm:ss}");

            var triggerUrl = _configuration["PlivoCX:TriggerEndpointUrl"];
            if (string.IsNullOrEmpty(triggerUrl))
            {
                testResults.Add("⚠ PlivoCX:TriggerEndpointUrl is not set in appsettings.json.");
                testResults.Add("  Steps:");
                testResults.Add("  1. Go to cx.plivo.com → AI Studio → your interview flow");
                testResults.Add("  2. Click the Trigger node → copy the API URL");
                testResults.Add("  3. Paste it into appsettings.json → PlivoCX:TriggerEndpointUrl");
                ViewBag.PlivoTestResults = testResults;
                return View("VapiTestResults");
            }

            try
            {
                if (currentHour < 9 || currentHour >= 21)
                {
                    testResults.Add($"SKIPPED — {currentTime:HH:mm} is outside business hours (9 AM – 9 PM IST)");
                    ViewBag.PlivoTestResults = testResults;
                    return View("VapiTestResults");
                }

                testResults.Add($"Within business hours — {currentTime:HH:mm}");
                testResults.Add("Fetching eligible candidates...");

                var eligibleCandidates = await _plivoCXCallRepository.GetEligibleCandidatesForCallsAsync(1);

                if (eligibleCandidates == null || !eligibleCandidates.Any())
                {
                    testResults.Add("No eligible candidates found.");
                    ViewBag.PlivoTestResults = testResults;
                    return View("VapiTestResults");
                }

                testResults.Add($"Found {eligibleCandidates.Count} eligible candidate(s)");
                testResults.Add("");

                int successCount = 0;
                int failureCount = 0;

                foreach (var candidate in eligibleCandidates)
                {
                    try
                    {
                        testResults.Add($"Processing: {candidate.CandidateName}");
                        testResults.Add($"   Client:   {candidate.ClientName}");
                        testResults.Add($"   Email:    {candidate.CandidateEmail}");
                        testResults.Add($"   Phone:    {candidate.CandidatePhone}");
                        testResults.Add($"   IRF ID:   {candidate.IRFId} ({candidate.IRFName})");
                        testResults.Add($"   Attempts: {candidate.CallAttempts}");

                        if (string.IsNullOrWhiteSpace(candidate.CandidatePhone))
                        {
                            testResults.Add("   SKIPPED — no phone number");
                            testResults.Add("");
                            continue;
                        }

                        if (!_phoneNumberHelper.TrySanitizeIndianPhoneNumber(candidate.CandidatePhone, out var phoneNumber, out var phoneError))
                        {
                            testResults.Add($"   SKIPPED — invalid phone: {phoneError}");
                            testResults.Add("");
                            continue;
                        }

                        //phoneNumber = "+918888571923";
                        //phoneNumber = "+918105232468";

                        testResults.Add($"   Sanitised phone: {phoneNumber}");

                        // 1. Save call log to get a DB Id
                        var callLog = new Risebird.Entities.Data.PlivoCXModels.PlivoCXCallLog
                        {
                            RecipientPhone = phoneNumber,
                            RecipientName = candidate.CandidateName,
                            RecipientEmail = candidate.CandidateEmail,
                            IRFId = candidate.IRFId,
                            CandidateId = candidate.CandidateId,
                            JobTitle = candidate.IRFName,
                            ClientName = candidate.ClientName,
                            CallStatus = "initiated",
                            InterviewBooked = false,
                            TriggeredBy = $"Dashboard-{triggeredByEmail}"
                        };
                        int logId = await _plivoCXCallRepository.SaveCallLogAsync(callLog);
                        testResults.Add($"   Call log saved — DB Id: {logId}");

                        // 2. Trigger Plivo CX outbound call
                        var triggerReq = new Risebird.Entities.Data.PlivoCXModels.PlivoCXTriggerRequest
                        {
                            PhoneNumber = phoneNumber,
                            CandidateName = candidate.CandidateName ?? "Candidate",
                            CandidateEmail = candidate.CandidateEmail,
                            IrfId = candidate.IRFId.ToString(),
                            JobTitle = candidate.IRFName,
                            ClientName = candidate.ClientName,
                            LogId = logId.ToString(),
                            TodayDate = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
                            TomorrowDate = DateTime.Now.AddDays(1).ToString("dddd, dd MMMM yyyy"),
                            DayAfterTomorrowDate = DateTime.Now.AddDays(2).ToString("dddd, dd MMMM yyyy")
                        };

                        var response = await _plivoCXService.TriggerOutboundCallAsync(triggerReq);

                        // Checkpoint — log trigger request + response
                        try
                        {
                            await _plivoCXCallRepository.SaveWebhookDebugLogAsync(
                                new Risebird.Entities.Data.PlivoCXModels.PlivoCXWebhookDebugLog
                                {
                                    LogType = "trigger-request",
                                    HttpStatusCode = response?.HttpStatusCode,
                                    PlivoCXId = response?.ApiId,
                                    CallLogId = logId,
                                    RawPayload = Newtonsoft.Json.JsonConvert.SerializeObject(triggerReq),
                                    ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                                    ErrorMessage = response?.Error
                                });
                            await _plivoCXCallRepository.SaveWebhookDebugLogAsync(
                                new Risebird.Entities.Data.PlivoCXModels.PlivoCXWebhookDebugLog
                                {
                                    LogType = "trigger-response",
                                    HttpStatusCode = response?.HttpStatusCode,
                                    PlivoCXId = response?.ApiId,
                                    CallLogId = logId,
                                    RawPayload = response?.RawResponseBody,
                                    ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                                    ErrorMessage = response?.Error
                                });
                        }
                        catch { }

                        if (!string.IsNullOrEmpty(response?.Error))
                        {
                            testResults.Add($"   TRIGGER FAILED: {response.Error}");
                            callLog.Id = logId;
                            callLog.CallStatus = "trigger-failed";
                            callLog.UpdatedAt = DateTime.UtcNow;
                            await _plivoCXCallRepository.UpdateCallLogAsync(callLog);
                            failureCount++;
                        }
                        else
                        {
                            testResults.Add($"   Triggered — ApiId: {response?.ApiId} | Http: {response?.HttpStatusCode}");
                            callLog.Id = logId;
                            callLog.PlivoCXConversationId = response?.ConversationId;
                            callLog.PlivoCXRequestId = response?.ApiId;
                            callLog.CallStatus = "triggered";
                            callLog.UpdatedAt = DateTime.UtcNow;
                            await _plivoCXCallRepository.UpdateCallLogAsync(callLog);

                            await _plivoCXCallRepository.IncrementCallAttemptsAsync(candidate.CandidateId);
                            successCount++;
                        }

                        await Task.Delay(2000);
                        testResults.Add("");
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        testResults.Add($"   FAILED: {ex.Message}");
                        testResults.Add("");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                            new Exception($"PlivoCX call failed for {candidate.CandidateEmail}", ex));
                    }
                }

                testResults.Add("═══════════════════════════════════");
                testResults.Add($"SUMMARY:");
                testResults.Add($"   Total Candidates: {eligibleCandidates.Count}");
                testResults.Add($"   Successful:       {successCount}");
                testResults.Add($"   Failed:           {failureCount}");
                testResults.Add("═══════════════════════════════════");
                testResults.Add("View call logs at: /PlivoCXInterviewCall/Index");

                ViewBag.PlivoTestResults = testResults;
                return View("VapiTestResults");
            }
            catch (Exception ex)
            {
                testResults.Add($"CRITICAL ERROR: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.PlivoTestResults = testResults;
                return View("VapiTestResults");
            }
        }

        private async Task<IActionResult> RunPlivoRescheduleCallsAsync(string triggeredByEmail)
        {
            var testResults = new List<string>();
            var currentTime = DateTime.Now;
            var currentHour = currentTime.Hour;

            testResults.Add($"[Plivo CX Reschedule] Test initiated at: {currentTime:yyyy-MM-dd HH:mm:ss}");

            var rescheduleUrl = _configuration["PlivoCX:RescheduleTriggerEndpointUrl"];
            if (string.IsNullOrEmpty(rescheduleUrl))
            {
                testResults.Add("⚠ PlivoCX:RescheduleTriggerEndpointUrl is not set in appsettings.json.");
                testResults.Add("  Steps:");
                testResults.Add("  1. Go to cx.plivo.com → AI Studio → your RESCHEDULE agent flow");
                testResults.Add("  2. Click the Trigger node → copy the API URL");
                testResults.Add("  3. Paste it into appsettings.json → PlivoCX:RescheduleTriggerEndpointUrl");
                ViewBag.PlivoTestResults = testResults;
                return View("VapiTestResults");
            }

            try
            {
                if (currentHour < 9 || currentHour >= 21)
                {
                    testResults.Add($"SKIPPED — {currentTime:HH:mm} is outside business hours (9 AM – 9 PM IST)");
                    ViewBag.PlivoTestResults = testResults;
                    return View("VapiTestResults");
                }

                testResults.Add($"Within business hours — {currentTime:HH:mm}");
                testResults.Add("Fetching eligible reschedule candidates...");

                var eligibleCandidates = await _plivoCXCallRepository.GetEligibleCandidatesForRescheduleAsync(1);

                if (eligibleCandidates == null || !eligibleCandidates.Any())
                {
                    testResults.Add("No eligible candidates found for rescheduling.");
                    ViewBag.PlivoTestResults = testResults;
                    return View("VapiTestResults");
                }

                testResults.Add($"Found {eligibleCandidates.Count} eligible candidate(s) for rescheduling");
                testResults.Add("");

                int successCount = 0;
                int failureCount = 0;

                foreach (var candidate in eligibleCandidates)
                {
                    try
                    {
                        testResults.Add($"Processing: {candidate.CandidateName}");
                        testResults.Add($"   Client:            {candidate.ClientName}");
                        testResults.Add($"   Email:             {candidate.CandidateEmail}");
                        testResults.Add($"   Phone:             {candidate.CandidatePhone}");
                        testResults.Add($"   IRF ID:            {candidate.IRFId} ({candidate.IRFName})");
                        testResults.Add($"   Original Date:     {candidate.OriginalInterviewDate ?? "(none)"}");
                        testResults.Add($"   Completed Calls:   {candidate.CompletedCallCount}");

                        if (string.IsNullOrWhiteSpace(candidate.CandidatePhone))
                        {
                            testResults.Add("   SKIPPED — no phone number");
                            testResults.Add("");
                            continue;
                        }

                        if (!_phoneNumberHelper.TrySanitizeIndianPhoneNumber(candidate.CandidatePhone, out var phoneNumber, out var phoneError))
                        {
                            testResults.Add($"   SKIPPED — invalid phone: {phoneError}");
                            testResults.Add("");
                            continue;
                        }

                        phoneNumber = "+918888571923";

                        testResults.Add($"   Sanitised phone: {phoneNumber}");

                        // 1. Save call log (CallPurpose = "reschedule")
                        var callLog = new Risebird.Entities.Data.PlivoCXModels.PlivoCXCallLog
                        {
                            RecipientPhone = phoneNumber,
                            RecipientName = candidate.CandidateName,
                            RecipientEmail = candidate.CandidateEmail,
                            IRFId = candidate.IRFId,
                            CandidateId = candidate.CandidateId,
                            JobTitle = candidate.JobTitle,
                            ClientName = candidate.ClientName,
                            CallStatus = "initiated",
                            InterviewBooked = false,
                            CallPurpose = "reschedule",
                            TriggeredBy = $"Dashboard-{triggeredByEmail}"
                        };
                        int logId = await _plivoCXCallRepository.SaveCallLogAsync(callLog);
                        testResults.Add($"   Call log saved — DB Id: {logId}");

                        // 2. Trigger the reschedule agent
                        var triggerReq = new Risebird.Entities.Data.PlivoCXModels.PlivoCXTriggerRequest
                        {
                            PhoneNumber = phoneNumber,
                            CandidateName = candidate.CandidateName ?? "Candidate",
                            CandidateEmail = candidate.CandidateEmail,
                            IrfId = candidate.IRFId.ToString(),
                            JobTitle = candidate.JobTitle,
                            ClientName = candidate.ClientName,
                            LogId = logId.ToString(),
                            CallPurpose = "reschedule",
                            OriginalInterviewDate = candidate.OriginalInterviewDate,
                            TodayDate = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
                            TomorrowDate = DateTime.Now.AddDays(1).ToString("dddd, dd MMMM yyyy"),
                            DayAfterTomorrowDate = DateTime.Now.AddDays(2).ToString("dddd, dd MMMM yyyy")
                        };

                        var response = await _plivoCXService.TriggerRescheduleCallAsync(triggerReq);

                        // Log trigger request + response
                        try
                        {
                            await _plivoCXCallRepository.SaveWebhookDebugLogAsync(
                                new Risebird.Entities.Data.PlivoCXModels.PlivoCXWebhookDebugLog
                                {
                                    LogType = "reschedule-trigger-request",
                                    HttpStatusCode = response?.HttpStatusCode,
                                    PlivoCXId = response?.ApiId,
                                    CallLogId = logId,
                                    RawPayload = Newtonsoft.Json.JsonConvert.SerializeObject(triggerReq),
                                    ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                                    ErrorMessage = response?.Error
                                });
                            await _plivoCXCallRepository.SaveWebhookDebugLogAsync(
                                new Risebird.Entities.Data.PlivoCXModels.PlivoCXWebhookDebugLog
                                {
                                    LogType = "reschedule-trigger-response",
                                    HttpStatusCode = response?.HttpStatusCode,
                                    PlivoCXId = response?.ApiId,
                                    CallLogId = logId,
                                    RawPayload = response?.RawResponseBody,
                                    ProcessingStatus = string.IsNullOrEmpty(response?.Error) ? "success" : "failed",
                                    ErrorMessage = response?.Error
                                });
                        }
                        catch { }

                        if (!string.IsNullOrEmpty(response?.Error))
                        {
                            testResults.Add($"   TRIGGER FAILED: {response.Error}");
                            callLog.Id = logId;
                            callLog.CallStatus = "trigger-failed";
                            callLog.UpdatedAt = DateTime.UtcNow;
                            await _plivoCXCallRepository.UpdateCallLogAsync(callLog);
                            failureCount++;
                        }
                        else
                        {
                            testResults.Add($"   Triggered — ApiId: {response?.ApiId} | Http: {response?.HttpStatusCode}");
                            callLog.Id = logId;
                            callLog.PlivoCXConversationId = response?.ConversationId;
                            callLog.PlivoCXRequestId = response?.ApiId;
                            callLog.CallStatus = "triggered";
                            callLog.UpdatedAt = DateTime.UtcNow;
                            await _plivoCXCallRepository.UpdateCallLogAsync(callLog);

                            await _plivoCXCallRepository.IncrementCallAttemptsAsync(candidate.CandidateId);
                            successCount++;
                        }

                        await Task.Delay(2000);
                        testResults.Add("");
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        testResults.Add($"   FAILED: {ex.Message}");
                        testResults.Add("");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                            new Exception($"PlivoCX reschedule call failed for {candidate.CandidateEmail}", ex));
                    }
                }

                testResults.Add("═══════════════════════════════════");
                testResults.Add($"SUMMARY:");
                testResults.Add($"   Total Candidates: {eligibleCandidates.Count}");
                testResults.Add($"   Successful:       {successCount}");
                testResults.Add($"   Failed:           {failureCount}");
                testResults.Add("═══════════════════════════════════");
                testResults.Add("View call logs at: /PlivoCXInterviewCall/Index");

                ViewBag.PlivoTestResults = testResults;
                return View("VapiTestResults");
            }
            catch (Exception ex)
            {
                testResults.Add($"CRITICAL ERROR: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.PlivoTestResults = testResults;
                return View("VapiTestResults");
            }
        }

        // ─── Plivo WhatsApp Schedule trigger ─────────────────────────────────────

        /// <summary>
        /// Fetches one eligible candidate and triggers a Plivo CX WhatsApp scheduling conversation.
        /// Access via GET /Dashboard/Index?testPlivoWhatsAppSchedule=true
        /// </summary>
        private async Task<IActionResult> RunPlivoWhatsAppScheduleAsync(string triggeredByEmail)
        {
            var testResults = new List<string>();
            var currentTime = DateTime.Now;

            testResults.Add($"[Plivo WA Schedule] Test initiated at: {currentTime:yyyy-MM-dd HH:mm:ss}");

            var triggerUrl = _configuration["PlivoWhatsApp:ScheduleTriggerEndpointUrl"];
            if (string.IsNullOrEmpty(triggerUrl))
            {
                testResults.Add("⚠ PlivoWhatsApp:ScheduleTriggerEndpointUrl is not set in appsettings.json.");
                testResults.Add("  Steps:");
                testResults.Add("  1. Publish your WhatsApp Schedule agent in cx.plivo.com");
                testResults.Add("  2. Click the API Request trigger node → copy the URL");
                testResults.Add("  3. Paste it into appsettings.json → PlivoWhatsApp:ScheduleTriggerEndpointUrl");
                ViewBag.PlivoTestResults = testResults;
                return View("VapiTestResults");
            }

            try
            {
                if (currentTime.Hour < 9 || currentTime.Hour >= 21)
                {
                    testResults.Add($"SKIPPED — {currentTime:HH:mm} is outside business hours (9 AM – 9 PM IST)");
                    ViewBag.PlivoTestResults = testResults;
                    return View("VapiTestResults");
                }

                testResults.Add($"Within business hours — {currentTime:HH:mm}");
                testResults.Add("Fetching eligible candidates...");

                var eligibleCandidates = await _plivoWhatsAppRepository.GetEligibleCandidatesForScheduleAsync(1);

                if (eligibleCandidates == null || !eligibleCandidates.Any())
                {
                    testResults.Add("No eligible candidates found.");
                    ViewBag.PlivoTestResults = testResults;
                    return View("VapiTestResults");
                }

                testResults.Add($"Found {eligibleCandidates.Count} eligible candidate(s)");
                testResults.Add("");

                int successCount = 0, failureCount = 0;

                foreach (var candidate in eligibleCandidates)
                {
                    try
                    {
                        testResults.Add($"Processing: {candidate.CandidateName}");
                        testResults.Add($"   Client:   {candidate.ClientName}");
                        testResults.Add($"   Email:    {candidate.CandidateEmail}");
                        testResults.Add($"   Phone:    {candidate.CandidatePhone}");
                        testResults.Add($"   IRF ID:   {candidate.IRFId} ({candidate.IRFName})");

                        if (string.IsNullOrWhiteSpace(candidate.CandidatePhone))
                        {
                            testResults.Add("   SKIPPED — no phone number");
                            testResults.Add("");
                            continue;
                        }
                        //candidate.CandidatePhone = "+918888571923";
                        if (!_phoneNumberHelper.TrySanitizeIndianPhoneNumber(candidate.CandidatePhone, out var phoneNumber, out var phoneError))
                        {
                            testResults.Add($"   SKIPPED — invalid phone: {phoneError}");
                            testResults.Add("");
                            continue;
                        }
                        phoneNumber = "+918888571923";
                        //phoneNumber = "+919830929409";
                        //phoneNumber = "+918077988602";
                        testResults.Add($"   Sanitised phone: {phoneNumber}");

                        var tomorrow = DateTime.Now.AddDays(1);
                        var dayAfter = DateTime.Now.AddDays(2);

                        // 1. Save log to get DB Id
                        var log = new PlivoWhatsAppLog
                        {
                            RecipientName = candidate.CandidateName,
                            RecipientPhone = phoneNumber,
                            RecipientEmail = candidate.CandidateEmail,
                            IRFId = candidate.IRFId,
                            CandidateId = candidate.CandidateId,
                            JobTitle = candidate.IRFName,
                            ClientName = candidate.ClientName,
                            MessagePurpose = "schedule",
                            Status = "initiated",
                            MessageStatus = "initiated",
                            TriggeredBy = $"Dashboard-{triggeredByEmail}"
                        };
                        int logId = await _plivoWhatsAppRepository.SaveLogAsync(log);
                        log.Id = logId;
                        testResults.Add($"   Log saved — DB Id: {logId}");

                        // 2. Build trigger context and send
                        var context = new PlivoWhatsAppTriggerContext
                        {
                            CandidateName = candidate.CandidateName,
                            CandidatePhone = phoneNumber,
                            CandidateEmail = candidate.CandidateEmail,
                            IrfId = candidate.IRFId,
                            CandidateId = candidate.CandidateId,
                            JobTitle = candidate.IRFName,
                            ClientName = candidate.ClientName,
                            MessagePurpose = "schedule",
                            LogId = logId.ToString(),
                            TodayDate = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
                            TomorrowDateLabel = tomorrow.ToString("dddd, dd MMMM yyyy"),
                            DayAfterDateLabel = dayAfter.ToString("dddd, dd MMMM yyyy"),
                            TriggeredBy = $"Dashboard-{triggeredByEmail}"
                        };

                        var response = await _plivoWhatsAppService.SendScheduleMessageAsync(context);

                        if (!string.IsNullOrEmpty(response?.Error))
                        {
                            testResults.Add($"   TRIGGER FAILED: {response.Error}");
                            log.Status = "trigger-failed";
                            log.MessageStatus = "trigger-failed";
                            log.BookingApiResponse = response.Error;
                            await _plivoWhatsAppRepository.UpdateLogAsync(log);
                            failureCount++;
                        }
                        else
                        {
                            testResults.Add($"   Triggered — ConversationId: {response?.ConversationId} | ApiId: {response?.ApiId}");
                            log.PlivoCXConversationId = response?.ConversationId;
                            log.PlivoCXRequestId = response?.ApiId;
                            log.MessageStatus = "triggered";
                            log.Status = "triggered";
                            await _plivoWhatsAppRepository.UpdateLogAsync(log);
                            await _plivoWhatsAppRepository.IncrementCallAttemptsAsync(candidate.CandidateId);
                            successCount++;
                        }

                        await Task.Delay(2000);
                        testResults.Add("");
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        testResults.Add($"   FAILED: {ex.Message}");
                        testResults.Add("");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                            new Exception($"WA schedule trigger failed for {candidate.CandidateEmail}", ex));
                    }
                }

                testResults.Add("═══════════════════════════════════");
                testResults.Add($"SUMMARY:");
                testResults.Add($"   Total Candidates: {eligibleCandidates.Count}");
                testResults.Add($"   Successful:       {successCount}");
                testResults.Add($"   Failed:           {failureCount}");
                testResults.Add("═══════════════════════════════════");
                testResults.Add("View WhatsApp logs at: /PlivoWhatsAppInterview/Index");

                ViewBag.PlivoTestResults = testResults;
                return View("VapiTestResults");
            }
            catch (Exception ex)
            {
                testResults.Add($"CRITICAL ERROR: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.PlivoTestResults = testResults;
                return View("VapiTestResults");
            }
        }

        // ─── Surepass Workflow test ─────────────────────────────────────────────────

        /// <summary>
        /// Calls the Surepass Superflow sandbox API to initialize a workflow.
        /// Access via GET /Dashboard/Index?testSurepassWorkflow=true
        /// </summary>
        private async Task<IActionResult> RunSurepassWorkflowTestAsync(string triggeredByEmail)
        {
            var testResults = new List<string>();
            var currentTime = DateTime.Now;

            testResults.Add($"[Surepass Workflow] Test initiated at: {currentTime:yyyy-MM-dd HH:mm:ss}");
            testResults.Add($"Triggered by: {triggeredByEmail}");
            testResults.Add("");

            var token = _configuration["Surepass:BearerToken"];
            if (string.IsNullOrEmpty(token))
            {
                testResults.Add("⚠ Surepass:BearerToken is not set in appsettings.json.");
                testResults.Add("  Add the following to appsettings.json:");
                testResults.Add("  \"Surepass\": { \"BearerToken\": \"<your-token>\" }");
                ViewBag.PlivoTestResults = testResults;
                return View("VapiTestResults");
            }

            try
            {
                var apiUrl = "https://superflow-sandbox.surepass.app/api/v1/workflows/sync/initialize-workflow";
                var workflowId = _configuration["Surepass:WorkflowId"] ?? "workflow_3CiBVnvlqqnMyJWASBaR7N5JNHW";
                var mobileNumber = _configuration["Surepass:TestMobileNumber"] ?? "8571967928";

                testResults.Add($"API URL:       {apiUrl}");
                testResults.Add($"Workflow ID:   {workflowId}");
                testResults.Add($"Mobile Number: {mobileNumber}");
                testResults.Add("");

                var requestBody = new
                {
                    workflow_id = workflowId,
                    interface_type = "detached",
                    block_input_data = true,
                    block_id_list = new[] { "mobile_to_pan", "pan_to_tan", "fetch_tan_details" },
                    advance_parameters = new
                    {
                        mobile_number = mobileNumber
                    },
                    timeout = 180
                };

                var jsonBody = JsonConvert.SerializeObject(requestBody, Formatting.Indented);
                testResults.Add("Request Body:");
                testResults.Add(jsonBody);
                testResults.Add("");

                // Surepass is disabled for the time being — we are not using Surepass for employment
                // verification. Signzy is the primary EV method, with LinkedIn as the fallback. The
                // Surepass API call below is intentionally commented out; re-enable if Surepass is
                // brought back into the EV flow.
                testResults.Add("Surepass API call is DISABLED (not used for employment verification at this time).");
                testResults.Add("═══════════════════════════════════");
                testResults.Add("RESULT: SKIPPED (Surepass disabled)");
                testResults.Add("═══════════════════════════════════");

                //using (var httpClient = new HttpClient())
                //{
                //    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                //    var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
                //    var response = await httpClient.PostAsync(apiUrl, content);
                //    var responseBody = await response.Content.ReadAsStringAsync();

                //    testResults.Add($"HTTP Status: {(int)response.StatusCode} {response.StatusCode}");
                //    testResults.Add("");

                //    try
                //    {
                //        var formatted = JToken.Parse(responseBody).ToString(Formatting.Indented);
                //        testResults.Add("Response Body:");
                //        testResults.Add(formatted);
                //    }
                //    catch
                //    {
                //        testResults.Add("Response Body (raw):");
                //        testResults.Add(responseBody);
                //    }

                //    testResults.Add("");
                //    testResults.Add("═══════════════════════════════════");
                //    testResults.Add(response.IsSuccessStatusCode ? "RESULT: SUCCESS" : "RESULT: FAILED");
                //    testResults.Add("═══════════════════════════════════");
                //}
            }
            catch (Exception ex)
            {
                testResults.Add($"CRITICAL ERROR: {ex.Message}");
                if (ex.InnerException != null)
                    testResults.Add($"Inner: {ex.InnerException.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            ViewBag.PlivoTestResults = testResults;
            return View("VapiTestResults");
        }

        // ─── Plivo WhatsApp Reschedule trigger ────────────────────────────────────

        /// <summary>
        /// Fetches one eligible reschedule candidate and triggers a Plivo CX WhatsApp rescheduling conversation.
        /// Access via GET /Dashboard/Index?testPlivoWhatsAppReschedule=true
        /// </summary>
        private async Task<IActionResult> RunPlivoWhatsAppRescheduleAsync(string triggeredByEmail)
        {
            var testResults = new List<string>();
            var currentTime = DateTime.Now;

            testResults.Add($"[Plivo WA Reschedule] Test initiated at: {currentTime:yyyy-MM-dd HH:mm:ss}");

            var triggerUrl = _configuration["PlivoWhatsApp:RescheduleTriggerEndpointUrl"];
            if (string.IsNullOrEmpty(triggerUrl))
            {
                testResults.Add("⚠ PlivoWhatsApp:RescheduleTriggerEndpointUrl is not set in appsettings.json.");
                testResults.Add("  Steps:");
                testResults.Add("  1. Publish your WhatsApp Reschedule agent in cx.plivo.com");
                testResults.Add("  2. Click the API Request trigger node → copy the URL");
                testResults.Add("  3. Paste it into appsettings.json → PlivoWhatsApp:RescheduleTriggerEndpointUrl");
                ViewBag.PlivoTestResults = testResults;
                return View("VapiTestResults");
            }

            try
            {
                if (currentTime.Hour < 9 || currentTime.Hour >= 21)
                {
                    testResults.Add($"SKIPPED — {currentTime:HH:mm} is outside business hours (9 AM – 9 PM IST)");
                    ViewBag.PlivoTestResults = testResults;
                    return View("VapiTestResults");
                }

                testResults.Add($"Within business hours — {currentTime:HH:mm}");
                testResults.Add("Fetching eligible reschedule candidates...");

                var eligibleCandidates = await _plivoWhatsAppRepository.GetEligibleCandidatesForRescheduleAsync(1);

                if (eligibleCandidates == null || !eligibleCandidates.Any())
                {
                    testResults.Add("No eligible reschedule candidates found.");
                    ViewBag.PlivoTestResults = testResults;
                    return View("VapiTestResults");
                }

                testResults.Add($"Found {eligibleCandidates.Count} eligible candidate(s)");
                testResults.Add("");

                int successCount = 0, failureCount = 0;

                foreach (var candidate in eligibleCandidates)
                {
                    try
                    {
                        testResults.Add($"Processing: {candidate.CandidateName}");
                        testResults.Add($"   Client:        {candidate.ClientName}");
                        testResults.Add($"   Email:         {candidate.CandidateEmail}");
                        testResults.Add($"   Phone:         {candidate.CandidatePhone}");
                        testResults.Add($"   IRF ID:        {candidate.IRFId} ({candidate.IRFName})");
                        testResults.Add($"   Original Date: {candidate.OriginalInterviewDate}");

                        if (string.IsNullOrWhiteSpace(candidate.CandidatePhone))
                        {
                            testResults.Add("   SKIPPED — no phone number");
                            testResults.Add("");
                            continue;
                        }

                        if (!_phoneNumberHelper.TrySanitizeIndianPhoneNumber(candidate.CandidatePhone, out var phoneNumber, out var phoneError))
                        {
                            testResults.Add($"   SKIPPED — invalid phone: {phoneError}");
                            testResults.Add("");
                            continue;
                        }

                        testResults.Add($"   Sanitised phone: {phoneNumber}");

                        var tomorrow = DateTime.Now.AddDays(1);
                        var dayAfter = DateTime.Now.AddDays(2);

                        // 1. Save log to get DB Id
                        var log = new PlivoWhatsAppLog
                        {
                            RecipientName = candidate.CandidateName,
                            RecipientPhone = phoneNumber,
                            RecipientEmail = candidate.CandidateEmail,
                            IRFId = candidate.IRFId,
                            CandidateId = candidate.CandidateId,
                            JobTitle = candidate.IRFName,
                            ClientName = candidate.ClientName,
                            MessagePurpose = "reschedule",
                            OriginalInterviewDate = candidate.OriginalInterviewDate,
                            Status = "initiated",
                            MessageStatus = "initiated",
                            TriggeredBy = $"Dashboard-{triggeredByEmail}"
                        };
                        int logId = await _plivoWhatsAppRepository.SaveLogAsync(log);
                        log.Id = logId;
                        testResults.Add($"   Log saved — DB Id: {logId}");

                        // 2. Build trigger context and send
                        var context = new PlivoWhatsAppTriggerContext
                        {
                            CandidateName = candidate.CandidateName,
                            CandidatePhone = phoneNumber,
                            CandidateEmail = candidate.CandidateEmail,
                            IrfId = candidate.IRFId,
                            CandidateId = candidate.CandidateId,
                            JobTitle = candidate.IRFName,
                            ClientName = candidate.ClientName,
                            MessagePurpose = "reschedule",
                            OriginalInterviewDate = candidate.OriginalInterviewDate,
                            LogId = logId.ToString(),
                            TodayDate = DateTime.Now.ToString("dddd, dd MMMM yyyy"),
                            TomorrowDateLabel = tomorrow.ToString("dddd, dd MMMM yyyy"),
                            DayAfterDateLabel = dayAfter.ToString("dddd, dd MMMM yyyy"),
                            TriggeredBy = $"Dashboard-{triggeredByEmail}"
                        };

                        var response = await _plivoWhatsAppService.SendRescheduleMessageAsync(context);

                        if (!string.IsNullOrEmpty(response?.Error))
                        {
                            testResults.Add($"   TRIGGER FAILED: {response.Error}");
                            log.Status = "trigger-failed";
                            log.MessageStatus = "trigger-failed";
                            log.BookingApiResponse = response.Error;
                            await _plivoWhatsAppRepository.UpdateLogAsync(log);
                            failureCount++;
                        }
                        else
                        {
                            testResults.Add($"   Triggered — ConversationId: {response?.ConversationId} | ApiId: {response?.ApiId}");
                            log.PlivoCXConversationId = response?.ConversationId;
                            log.PlivoCXRequestId = response?.ApiId;
                            log.MessageStatus = "triggered";
                            log.Status = "triggered";
                            await _plivoWhatsAppRepository.UpdateLogAsync(log);
                            await _plivoWhatsAppRepository.IncrementCallAttemptsAsync(candidate.CandidateId);
                            successCount++;
                        }

                        await Task.Delay(2000);
                        testResults.Add("");
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        testResults.Add($"   FAILED: {ex.Message}");
                        testResults.Add("");
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                            new Exception($"WA reschedule trigger failed for {candidate.CandidateEmail}", ex));
                    }
                }

                testResults.Add("═══════════════════════════════════");
                testResults.Add($"SUMMARY:");
                testResults.Add($"   Total Candidates: {eligibleCandidates.Count}");
                testResults.Add($"   Successful:       {successCount}");
                testResults.Add($"   Failed:           {failureCount}");
                testResults.Add("═══════════════════════════════════");
                testResults.Add("View WhatsApp logs at: /PlivoWhatsAppInterview/Index");

                ViewBag.PlivoTestResults = testResults;
                return View("VapiTestResults");
            }
            catch (Exception ex)
            {
                testResults.Add($"CRITICAL ERROR: {ex.Message}");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.PlivoTestResults = testResults;
                return View("VapiTestResults");
            }
        }

        // ─── Plivo WhatsApp Diagnostics ───────────────────────────────────────────

        /// <summary>
        /// Diagnostic tool for WhatsApp send failures.
        /// Queries two sources in parallel:
        ///   1. Plivo Messaging API — recent outbound WhatsApp messages from our number,
        ///      including per-message status and WhatsApp error codes
        ///   2. Plivo CX agentflow API — run-level detail for a specific agent run ID
        ///      (probes three probable endpoint patterns; returns whichever responds)
        ///
        /// Access via:
        ///   /Dashboard/Index?diagnoseWhatsApp=true
        ///   /Dashboard/Index?diagnoseWhatsApp=true&waRunId=RUN_ID&waFlowId=FLOW_ID
        ///
        /// Copy waRunId and waFlowId from the cx.plivo.com URL when viewing an agent run:
        ///   cx.plivo.com/agent/runs/{waFlowId}/{waRunId}
        /// </summary>
        private async Task<IActionResult> DiagnoseWhatsAppAsync(string runId, string flowId)
        {
            runId = "b49f5a19-3e45-4d37-8fde-58e8b2ceb734";
            flowId = "5d91b207-15d7-4e7a-a698-0683026f5a5f";

            var results = new List<string>();
            results.Add($"[WA Diagnose] Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            results.Add("");

            var authId = _configuration["PlivoWhatsApp:ApiAuthId"] ?? string.Empty;
            var authToken = _configuration["PlivoWhatsApp:ApiAuthToken"] ?? string.Empty;
            var fromNumber = (_configuration["PlivoWhatsApp:FromNumber"] ?? "+919667032088").TrimStart('+');

            if (string.IsNullOrEmpty(authId) || string.IsNullOrEmpty(authToken))
            {
                results.Add("ERROR: PlivoWhatsApp:ApiAuthId or ApiAuthToken not configured.");
                ViewBag.PlivoTestResults = results;
                return View("VapiTestResults");
            }

            var credentials = Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes($"{authId}:{authToken}"));

            using var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            http.Timeout = TimeSpan.FromSeconds(15);

            // ── 1. Plivo Messaging API — recent outbound WhatsApp messages ────────────
            results.Add("══════════════════════════════════════════════");
            results.Add("1. PLIVO MESSAGING API — Recent outbound messages");
            results.Add($"   From: +{fromNumber}  |  Type: whatsapp  |  Last 10");
            results.Add("══════════════════════════════════════════════");
            try
            {
                var msgUrl = $"https://api.plivo.com/v1/Account/{authId}/Message/" +
                             $"?message_type=whatsapp&src={fromNumber}&limit=10&sort=-created_on";
                results.Add($"   GET {msgUrl}");

                var msgResp = await http.GetAsync(msgUrl);
                var msgBody = await msgResp.Content.ReadAsStringAsync();

                results.Add($"   HTTP Status: {(int)msgResp.StatusCode} {msgResp.StatusCode}");
                results.Add("");

                if (msgResp.IsSuccessStatusCode)
                {
                    try
                    {
                        var parsed = JsonConvert.DeserializeObject<dynamic>(msgBody);
                        var objects = parsed?.objects;
                        if (objects != null)
                        {
                            int idx = 0;
                            foreach (var msg in objects)
                            {
                                idx++;
                                results.Add($"   [{idx}] MessageUUID : {msg?.message_uuid}");
                                results.Add($"        To           : {msg?.to_number ?? msg?.to}");
                                results.Add($"        Status       : {msg?.message_state ?? msg?.status}");
                                results.Add($"        Error Code   : {msg?.error_code ?? "(none)"}");
                                results.Add($"        Error Message: {msg?.error_message ?? "(none)"}");
                                results.Add($"        Created      : {msg?.creation_time ?? msg?.created_on}");
                                results.Add($"        Type         : {msg?.message_type}");
                                results.Add("");
                            }
                            if (idx == 0) results.Add("   No outbound WhatsApp messages found.");
                        }
                        else
                        {
                            results.Add($"   Raw response: {msgBody.Substring(0, Math.Min(1000, msgBody.Length))}");
                        }
                    }
                    catch
                    {
                        results.Add($"   Raw response: {msgBody.Substring(0, Math.Min(1000, msgBody.Length))}");
                    }
                }
                else
                {
                    results.Add($"   API Error: {msgBody.Substring(0, Math.Min(500, msgBody.Length))}");
                }
            }
            catch (Exception ex)
            {
                results.Add($"   EXCEPTION: {ex.Message}");
            }

            // ── 2. Plivo CX agentflow API — run detail ────────────────────────────────
            results.Add("══════════════════════════════════════════════");
            results.Add("2. PLIVO CX AGENTFLOW API — Run Detail");
            results.Add("══════════════════════════════════════════════");

            if (string.IsNullOrEmpty(runId))
            {
                results.Add("   No waRunId provided — skipping run detail lookup.");
                results.Add("   Pass ?diagnoseWhatsApp=true&waRunId=RUN_ID&waFlowId=FLOW_ID");
                results.Add("   (Copy both IDs from the cx.plivo.com URL when viewing an agent run)");
            }
            else
            {
                results.Add($"   Run ID  : {runId}");
                results.Add($"   Flow ID : {flowId ?? "(not provided)"}");
                results.Add("");

                // Pattern A — /flow/{flowId}/runs/{runId}
                if (!string.IsNullOrEmpty(flowId))
                {
                    await ProbeAgentflowUrl(http, results, "Pattern A (flow-scoped)",
                        $"https://agentflow.plivo.com/v1/account/{authId}/flow/{flowId}/runs/{runId}");
                }

                // Pattern B — /runs/{runId}
                await ProbeAgentflowUrl(http, results, "Pattern B (global runs)",
                    $"https://agentflow.plivo.com/v1/account/{authId}/runs/{runId}");

                // Pattern C — /flow/{flowId}/run/{runId} (singular)
                if (!string.IsNullOrEmpty(flowId))
                {
                    await ProbeAgentflowUrl(http, results, "Pattern C (singular /run/)",
                        $"https://agentflow.plivo.com/v1/account/{authId}/flow/{flowId}/run/{runId}");
                }

                // Pattern D — cx.plivo.com REST API
                await ProbeAgentflowUrl(http, results, "Pattern D (cx.plivo.com API)",
                    $"https://cx.plivo.com/api/v1/account/{authId}/runs/{runId}");
            }

            results.Add("");
            results.Add("══════════════════════════════════════════════");
            results.Add("Diagnose complete.");
            results.Add("The key field to look for is 'error_code' in section 1.");
            results.Add("Common codes:");
            results.Add("  131030 — Template not found or not approved for this WABA");
            results.Add("  131042 — Template message components mismatch");
            results.Add("  131026 — Message undeliverable (number not on WhatsApp)");
            results.Add("  131047 — Business account daily limit reached");
            results.Add("  132000 — Template param count mismatch");
            results.Add("══════════════════════════════════════════════");

            ViewBag.PlivoTestResults = results;
            return View("VapiTestResults");
        }

        private static async Task ProbeAgentflowUrl(HttpClient http, List<string> results, string label, string url)
        {
            try
            {
                results.Add($"   {label}");
                results.Add($"   GET {url}");
                var resp = await http.GetAsync(url);
                var body = await resp.Content.ReadAsStringAsync();
                results.Add($"   HTTP Status: {(int)resp.StatusCode} {resp.StatusCode}");
                results.Add($"   Response: {body.Substring(0, Math.Min(800, body.Length))}");
            }
            catch (Exception ex)
            {
                results.Add($"   EXCEPTION: {ex.Message}");
            }
            results.Add("");
        }

        // ─── Vapi prompt helper (existing) ───────────────────────────────────────

        private string GetSystemPromptWithCurrentDate(string candidateName, string jobTitle, string irfId, string candidateEmail, string clientName)
        {
            var dayOfWeek = DateTime.Now.ToString("dddd, MMMM dd, yyyy");

            return $@"You are an outbound interview scheduling assistant from Risebird Interviews calling on behalf of {clientName} for urgent hiring.

IDENTITY & PURPOSE:
- You represent Risebird Interviews on behalf of {clientName}
- This is URGENT and time-sensitive hiring - interviews MUST be scheduled within the next TWO days ONLY
- Your goal: Politely convey urgency and secure a confirmed interview slot within the next two days

CURRENT DATE INFORMATION (CRITICAL):
- Today is: {dayOfWeek}
- You can ONLY offer: TOMORROW or DAY AFTER TOMORROW
- When speaking to candidate, use the words tomorrow or day after tomorrow ONLY
- NEVER read out specific dates like numbers
- IRF ID for this interview: {irfId}
- Candidate email for confirmation: {candidateEmail}

CRITICAL RULES:
- Interviews MUST be within the next TWO days from today (tomorrow or day after tomorrow ONLY)
- Do NOT allow scheduling beyond 2 days under any circumstances
- When speaking dates: Always say tomorrow or day after tomorrow - NEVER read out specific date numbers
- If candidate requests later dates: ""I apologize, but due to the urgent nature of this hiring process, we can only accommodate slots within the next two days. Would either tomorrow or the day after work for you?""
- If candidate insists on dates beyond 2 days: Politely explain this is not possible and end call gracefully
- ALL times must be confirmed in IST (Indian Standard Time)

CALL FLOW:
1. Greet and confirm identity
   - ""Am I speaking with {candidateName}?""
   - Wait for confirmation (yes/no)
   - If wrong person: ""I apologize for the confusion. Could you please let {candidateName} know we called? Thank you."" Then end call.

2. State purpose with urgency
   - ""I'm calling on behalf of {clientName} regarding an interview opportunity for the {jobTitle} position.""
   - ""The scope of this call is limited to scheduling your interview slot. For any further details about the role or process, please check your email.""
   - ""This is time-sensitive and we're looking to schedule interviews within the next two days.""
   - ""Would you be available tomorrow or the day after tomorrow?""
   - Wait for response

3. Get specific time
   - ""What time would work best for you?"" 
   - If they say just ""morning"" or ""afternoon"", ask: ""What specific time would be most convenient? Please specify with AM or PM.""
   - Wait for specific time (e.g., ""10:00 AM"", ""02:30 PM"")

4. Confirm details explicitly
   - ""Thank you! Let me confirm the day and time - that's [tomorrow/day after tomorrow] at [TIME] IST. Is that correct?""
   - Wait for explicit confirmation (""yes"", ""correct"", ""that's right"")
   - IMPORTANT: When confirming, say tomorrow or day after tomorrow, do NOT read out date numbers

5. Submit booking (ONLY after explicit confirmation)
   - Call submitInterviewSlot function with:
     * candidateName: {candidateName}
     * candidateEmail: {candidateEmail}
     * interviewDate: Use tomorrow or day after tomorrow as the value
     * interviewTime: Time in HH:MM AM/PM format (e.g., ""10:00 AM"", ""02:30 PM"")
     * timezone: ""IST""
     * candidateConfirmed: true (ONLY if candidate said yes/confirmed/correct)
     * irfId: {irfId}

6. End professionally with gratitude and DISCONNECT
   - ""Perfect! Your interview is confirmed. You'll receive a confirmation email shortly. We appreciate your time and look forward to speaking with you. Good luck!""
   - After you say this sentence, END THE CALL IMMEDIATELY and hang up. Do not say anything else. Do not keep the line open. Do not continue the conversation.

OBJECTION HANDLING (BE POLITE BUT PERSISTENT):
- If busy now: ""I completely understand. This will only take 2 minutes to find a suitable time. May we proceed?""
- If asks to call back: ""I appreciate that, and since I have you on the line and this is time-sensitive, could we quickly lock in a time that works for you? It will just take a moment.""
- If uncertain about job: ""I understand. You had applied for this position, and the company is moving quickly with qualified candidates. Would you like to secure an interview slot?""
- If wants different date beyond 2 days: ""I appreciate your preference, but unfortunately, due to the urgent hiring timeline, we can only offer slots within the next two days - either tomorrow or the day after. Would either of those work for you?""
- If asks for role/process/salary/JD details: ""The scope of this call is limited to scheduling your interview slot. For all further details, please check your email. Now, shall we find a time that works for you?""

VALIDATION RULES:
- Date MUST be either tomorrow or day after tomorrow - NO EXCEPTIONS
- When SPEAKING to candidate: Use words tomorrow or day after tomorrow ONLY
- When SUBMITTING to tool: Use tomorrow or day after tomorrow as the interviewDate value
- Time MUST include AM or PM (e.g., ""10:00 AM"", not just ""10:00"")
- Candidate MUST explicitly confirm the final details with ""yes"", ""correct"", ""confirmed"", ""that's right"", or similar
- If candidate says ""maybe"", ""I think so"", ""probably"" - ask for explicit confirmation: ""I want to make sure we have this right. Can you confirm this time works for you?""
- Do NOT submit booking without explicit confirmation
- ALWAYS include the IRF ID and candidate email when submitting the slot

DISCONNECT RULES:
- Once you have submitted the booking and spoken the final confirmation sentence, you MUST end the call immediately.
- Do NOT wait silently on the line, do NOT ask additional questions, and do NOT engage in further small talk.

NEVER:
- Accept dates beyond 2 days from today (be firm but polite about this)
- Submit booking without explicit confirmation
- Use 24-hour time format (always use AM/PM)
- Forget to specify timezone as IST
- Read out specific date numbers when speaking
- Forget to include irfId and candidate email in the tool call";
        }
        private async Task GenerateTranscriptSummary()
        {
            var pendingVideoMeetings = await _meetingRespository.GetPendingVideoMeetings();

            //var pendingRBMeetings = pendingVideoMeetings.Where(x => Regex.IsMatch(x.ZoomMeetingId, "[a-zA-Z]")).ToList();
            var pendingRBMeetings = pendingVideoMeetings.Where(x => x.InterviewMeetingPlatForm == MeetingPlatforms.Zoho).ToList();

            var accesstoken = await _zoho.GetRecordingAccessTokenAsync();

            if (pendingRBMeetings.Count() > 0)
            {

                foreach (var meeting in pendingRBMeetings)
                {
                    try
                    {
                        //ZohoAzureuploadVideoandTranscript(meeting);
                        string videoSasUrl = string.Empty;
                        string uploadedVideoClientExternalLink = string.Empty;
                        string uploadedTranscriptClientExternalLink = string.Empty;
                        int? videoDuration = null;
                        string transcriptSasUrl = string.Empty;

                        try
                        {
                            var recordinginfo = await _zoho.GetRecordingAsync(meeting.ZoomMeetingId, accesstoken);
                            int maxduration = 0;
                            if (recordinginfo != null && recordinginfo.Any())
                                maxduration = recordinginfo.Max(r => r.duration);
                            var _recordingupload = recordinginfo?.Where(x => x.duration == maxduration).FirstOrDefault();

                            var transcriptSummaryCleanedJson = string.Empty;

                            if (string.IsNullOrEmpty(meeting.AudioTranscriptFileLink))
                            {
                                var transcriptionDownloadUrl = _recordingupload.transcriptionDownloadUrl;
                                if (transcriptionDownloadUrl != null)
                                {

                                    ///write code to generate transcript summary
                                    //var transcriptString = await DownloadTranscriptStringAsync(transcriptionDownloadUrl, accesstoken);
                                    int i = 0;
                                    string url = "https://risebirdportal.blob.core.windows.net/transcripts/5b770505f242f273bf0c359b23be0dd8d976a27545d58a75923c0c8b0a730941_Transcript_1764316062?sv=2024-11-04&spr=https&st=2025-11-28T07%3A47%3A42Z&se=2026-06-28T07%3A47%3A42Z&sr=c&sp=r&sig=1AkmnHsBGUTvHNFkz3ccLkDQ69byGw4N8VpBSFBYblQ%3D";

                                    var transcriptString = await DownloadTranscriptStringFromBlobUrl(url);

                                    if (!string.IsNullOrEmpty(transcriptString))
                                    {
                                        var runResult = await _assistantServiceHelper.GenerateIntervieweeResponseExtraction(transcriptString);
                                        transcriptSummaryCleanedJson = OpenAIResponsesHelper.CleanJsonResponseText(
                                            runResult?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value);
                                        if (OpenAIResponsesHelper.TryValidateJsonObject(transcriptSummaryCleanedJson, out _))
                                            await _meetingRespository.UpdateTranscriptSummary(539654, transcriptSummaryCleanedJson);
                                    }
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        return;
                    }
                }
            }

        }
        private async Task<string> DownloadTranscriptStringAsync(string url, string accessToken)
        {

            using var myWebClient = _httpClientFactory.CreateClient();

            myWebClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Zoho-oauthtoken", accessToken);

            using var myStream = await myWebClient.GetStreamAsync(url);
            using var reader = new StreamReader(myStream);

            string transcript = await reader.ReadToEndAsync();
            return transcript;


        }

        private async Task<string> DownloadTranscriptStringFromBlobUrl(string url)
        {

            using var client = _httpClientFactory.CreateClient();
            using var stream = await client.GetStreamAsync(url);
            using var reader = new StreamReader(stream);

            return await reader.ReadToEndAsync();
        }

        private async Task ParseOpenAIResume()
        {
            try
            {

                //get all the resumes that need to be parsed

                var resumeList = await _candidateRepository.GetCandidateResumesForParsing();
                var candidateList = new List<CandidateQuickAddModel>();
                var candidateResumeList = new List<CandidateResume>();

                foreach (var resume in resumeList)
                {

                    string resumeLinkToParse = string.Empty;
                    if (!resume.ResumeLink.Contains(".pdf"))
                    {
                        resumeLinkToParse = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(resume.ResumeLink);
                    }
                    else
                    {
                        resumeLinkToParse = resume.ResumeLink;
                    }
                    //convert resume into string
                    var resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeLinkToParse);
                    //var result = await _assistantServiceHelper.ParseResume(resumeStringContent);

                    if (!string.IsNullOrEmpty(resumeStringContent))
                    {
                        // OPTIMIZATION: Pre-screen resume for validity (saves API calls on corrupted resumes)
                        var (isValid, validationReason) = _resumeOptimizationHelper.ValidateResumeForParsing(resumeStringContent);
                        if (!isValid)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Info,
                                new Exception($"Resume {resume.id} validation failed: {validationReason}"));
                            candidateResumeList.Add(new CandidateResume
                            {
                                id = resume.id,
                                IsResumeParsed = false,
                                IsProcessed = false,
                                IsResumeParsingFailed = true,
                                ReasonBehindNotProcessed = validationReason
                            });
                            continue;
                        }

                        // OPTIMIZATION: Reduce token usage by removing boilerplate and excessive whitespace
                        var estimatedTokensBefore = _resumeOptimizationHelper.EstimateTokenCount(resumeStringContent);
                        resumeStringContent = _resumeOptimizationHelper.OptimizeResumeText(resumeStringContent);
                        var estimatedTokensAfter = _resumeOptimizationHelper.EstimateTokenCount(resumeStringContent);

                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Info,
                            new Exception($"Resume {resume.id} optimization: {estimatedTokensBefore} tokens -> {estimatedTokensAfter} tokens (saved {estimatedTokensBefore - estimatedTokensAfter} tokens)"));

                        var runResult = await _assistantServiceHelper.ParseResume(resumeStringContent);
                        var replyText = runResult?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;

                        CandidateResumeParsedDataOpenAI candidateParsedData = null;

                        try
                        {
                            candidateParsedData = JsonConvert.DeserializeObject<CandidateResumeParsedDataOpenAI>(replyText);

                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                            //continue;
                        }
                        //var candidateParsedData = JsonConvert.DeserializeObject<CandidateResumeParsedDataOpenAI>(replies?.FirstOrDefault());

                        try
                        {

                            if (candidateParsedData != null)
                            {
                                var candidate = new CandidateQuickAddModel();
                                candidate.Name = candidateParsedData.name;

                                if (!string.IsNullOrEmpty(candidateParsedData.emailId))
                                {
                                    candidate.EmailId = candidateParsedData.emailId.Trim();
                                    candidate.EmailId.Replace(":", "").Replace("-", "").Replace("%20", "").Replace("?:", "").Replace(":%20", "").Replace("-%20", "");

                                    if (candidate.EmailId.StartsWith(":") || candidate.EmailId.StartsWith("-"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(1); // Remove the first character (colon)
                                    }
                                    if (candidate.EmailId.StartsWith("%20"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                            //candidate.EmailId.Replace("%20", "");

                                    }
                                    if (candidate.EmailId.StartsWith("?:"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                            //candidate.EmailId.Replace("?:", "");

                                    }
                                    if (candidate.EmailId.StartsWith(":%20") || candidate.EmailId.StartsWith("-%20"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(4); // Remove the first four characters 
                                                                                            //candidate.EmailId.Replace(":%20", "").Replace("-%20", "");
                                    }
                                    if (!_validationHelper.ValidateEmailAddress(candidate.EmailId))
                                    {
                                        //mark it as cant be processed in the database
                                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = false });
                                        continue;
                                    }

                                }
                                else
                                {
                                    //mark it as cant be processed in the database
                                    candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = true });
                                    continue;
                                }
                                if (!string.IsNullOrEmpty(candidateParsedData.phoneNumber))
                                {
                                    candidate.Phone = candidateParsedData.phoneNumber.Replace(" ", string.Empty);
                                    if (candidate.Phone == "911" || candidate.Phone == "+911")
                                    {
                                        candidate.Phone = null;
                                    }
                                }
                                candidate.ResumeLink = resume.ResumeLink;
                                candidate.CandidateResumeId = resume.id;
                                candidate.IRFId = resume.IRFId;
                                candidate.AddedByUserId = resume.AddedBy;
                                candidate.ClientHREmail = resume.ClientHREmailId;
                                candidateList.Add(candidate);
                            }
                            else//parsing was tried, but failed due to some reason, in that case, we wont parse the resume again
                            {
                                candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true });

                            }
                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                            continue;
                        }
                    }
                    else
                    {
                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true });

                    }


                }

                if (candidateResumeList != null && candidateResumeList.Count() > 0)
                {
                    await _candidateRepository.UpdateCandidateResumeInBulk(candidateResumeList);
                }

                // add the candidate to database and update IsProcessed and IsResumeParsed
                if (candidateList != null && candidateList.Count() > 0)
                {
                    foreach (var irfId in candidateList.Select(x => x.IRFId).Distinct())
                    {
                        var candidateSubList = candidateList.Where(x => x.IRFId == irfId)?.ToList();
                        await _irfService.AddCandidateUsingResumeParserJob(candidateSubList, irfId);
                    }
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        private async Task ParseOpenAIResumeTImerFunctionCopy()//0 */5 * * * *
        {
            try
            {

                //get all the resumes that need to be parsed

                var resumeList = await _candidateRepository.GetCandidateResumesForParsing();
                var candidateList = new List<CandidateQuickAddModel>();
                var candidateResumeList = new List<CandidateResume>();

                foreach (var resume in resumeList)
                {

                    //string resumeLinkToParse = string.Empty;
                    //if (!resume.ResumeLink.Contains(".pdf"))
                    //{
                    //    resumeLinkToParse = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(resume.ResumeLink);
                    //}
                    //else
                    //{
                    //    resumeLinkToParse = resume.ResumeLink;
                    //}
                    ////convert resume into string
                    //var resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeLinkToParse);
                    if (!string.IsNullOrEmpty(resume.ResumeLink))
                    {
                        var runResult = await _assistantServiceHelper.ParseResume(
                            resume.ResumeLink,
                            usePublicUrlParser: true);
                        var replyText = runResult?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;

                        CandidateResumeParsedDataOpenAI candidateParsedData = null;

                        try
                        {
                            candidateParsedData = JsonConvert.DeserializeObject<CandidateResumeParsedDataOpenAI>(replyText);

                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                        }


                        //call the api for each of those resumes, return a candidate

                        try
                        {
                            if (candidateParsedData == null)
                            {
                                var candidateData = await _affIndaResumeParserHelper.UploadAndParseResume(resume.ResumeLink, resume.SkillChannelName, resume.IsAffindaIndexCreated);
                                if (!string.IsNullOrEmpty(candidateData.Item1))
                                {
                                    var documentData = JsonConvert.DeserializeObject<Document>(candidateData.Item1);
                                    var candidate = new CandidateQuickAddModel();
                                    candidate.Name = documentData.data?.name?.raw;
                                    if (!string.IsNullOrEmpty(candidateData.Item2))
                                    {
                                        candidate.AffindaResumeIdentifier = candidateData.Item2;
                                    }
                                    if (documentData.data.emails != null && documentData.data.emails.Count() > 0)
                                    {
                                        candidate.EmailId = documentData.data.emails[0].Trim();
                                        candidate.EmailId.Replace(":", "").Replace("-", "").Replace("%20", "").Replace("?:", "").Replace(":%20", "").Replace("-%20", "");

                                        if (candidate.EmailId.StartsWith(":") || candidate.EmailId.StartsWith("-"))
                                        {
                                            candidate.EmailId = candidate.EmailId.Substring(1); // Remove the first character (colon)
                                        }
                                        if (candidate.EmailId.StartsWith("%20"))
                                        {
                                            candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                                //candidate.EmailId.Replace("%20", "");

                                        }
                                        if (candidate.EmailId.StartsWith("?:"))
                                        {
                                            candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                                //candidate.EmailId.Replace("?:", "");

                                        }
                                        if (candidate.EmailId.StartsWith(":%20") || candidate.EmailId.StartsWith("-%20"))
                                        {
                                            candidate.EmailId = candidate.EmailId.Substring(4); // Remove the first four characters 
                                                                                                //candidate.EmailId.Replace(":%20", "").Replace("-%20", "");
                                        }
                                        if (!_validationHelper.ValidateEmailAddress(candidate.EmailId))
                                        {
                                            //mark it as cant be processed in the database
                                            candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = false });
                                            continue;
                                        }

                                    }
                                    else
                                    {
                                        //mark it as cant be processed in the database
                                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = true });
                                        continue;
                                    }
                                    if (documentData.data.phoneNumberDetails != null && documentData.data.phoneNumberDetails.Count() > 0)
                                    {
                                        //candidate.Phone = !string.IsNullOrEmpty(documentData.data.phoneNumberDetails[0].internationalCountryCode) ? documentData.data.phoneNumberDetails[0].rawText : null;
                                        candidate.Phone = documentData.data.phoneNumberDetails[0].rawText;
                                        if (candidate.Phone == "911" || candidate.Phone == "+911")
                                        {
                                            candidate.Phone = null;
                                        }
                                    }
                                    candidate.ResumeLink = resume.ResumeLink;
                                    candidate.CandidateResumeId = resume.id;
                                    candidate.IRFId = resume.IRFId;
                                    candidate.AddedByUserId = resume.AddedBy;
                                    candidate.ClientHREmail = resume.ClientHREmailId;
                                    candidateList.Add(candidate);
                                }
                                else//parsing was tried, but failed due to some reason, in that case, we wont parse the resume again
                                {
                                    candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true });

                                }
                            }

                            else if (candidateParsedData != null)
                            {
                                var candidate = new CandidateQuickAddModel();
                                candidate.Name = candidateParsedData.name;

                                if (!string.IsNullOrEmpty(candidateParsedData.emailId))
                                {
                                    candidate.EmailId = candidateParsedData.emailId.Trim();
                                    candidate.EmailId.Replace(":", "").Replace("-", "").Replace("%20", "").Replace("?:", "").Replace(":%20", "").Replace("-%20", "");

                                    if (candidate.EmailId.StartsWith(":") || candidate.EmailId.StartsWith("-"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(1); // Remove the first character (colon)
                                    }
                                    if (candidate.EmailId.StartsWith("%20"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                            //candidate.EmailId.Replace("%20", "");

                                    }
                                    if (candidate.EmailId.StartsWith("?:"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                            //candidate.EmailId.Replace("?:", "");

                                    }
                                    if (candidate.EmailId.StartsWith(":%20") || candidate.EmailId.StartsWith("-%20"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(4); // Remove the first four characters 
                                                                                            //candidate.EmailId.Replace(":%20", "").Replace("-%20", "");
                                    }
                                    if (!_validationHelper.ValidateEmailAddress(candidate.EmailId))
                                    {
                                        //mark it as cant be processed in the database
                                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = false });
                                        continue;
                                    }

                                }
                                else
                                {
                                    //mark it as cant be processed in the database
                                    candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = true });
                                    continue;
                                }
                                if (!string.IsNullOrEmpty(candidateParsedData.phoneNumber))
                                {
                                    candidate.Phone = candidateParsedData.phoneNumber.Replace(" ", string.Empty);
                                    if (candidate.Phone == "911" || candidate.Phone == "+911")
                                    {
                                        candidate.Phone = null;
                                    }
                                }
                                candidate.ResumeLink = resume.ResumeLink;
                                candidate.CandidateResumeId = resume.id;
                                candidate.IRFId = resume.IRFId;
                                candidate.AddedByUserId = resume.AddedBy;
                                candidate.ClientHREmail = resume.ClientHREmailId;
                                candidateList.Add(candidate);
                            }
                            else//parsing was tried, but failed due to some reason, in that case, we wont parse the resume again
                            {
                                candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true });

                            }
                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                            continue;
                        }
                    }
                    else
                    {
                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true });

                    }

                }

                if (candidateResumeList != null && candidateResumeList.Count() > 0)
                {
                    await _candidateRepository.UpdateCandidateResumeInBulk(candidateResumeList);
                }

                // add the candidate to database and update IsProcessed and IsResumeParsed
                if (candidateList != null && candidateList.Count() > 0)
                {
                    foreach (var irfId in candidateList.Select(x => x.IRFId).Distinct())
                    {
                        var candidateSubList = candidateList.Where(x => x.IRFId == irfId)?.ToList();
                        await _irfService.AddCandidateUsingResumeParserJob(candidateSubList, irfId);
                    }
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        private async Task ParseresumeCopy2()
        {
            try
            {

                //get all the resumes that need to be parsed

                var resumeList = await _candidateRepository.GetCandidateResumesForParsing();
                var candidateList = new List<CandidateQuickAddModel>();
                var candidateResumeList = new List<CandidateResume>();

                foreach (var resume in resumeList)
                {

                    if (!string.IsNullOrEmpty(resume.ResumeLink))
                    {
                        //undo the following comments that parsed the resume using open ai and initiated the variable candidateParsedData when you iron out the bugs/erratics of open ai and are ready to switch back to it as the first choice resume parser.


                        CandidateResumeParsedDataOpenAI candidateParsedData = null;

                        string resumeLinkToParse = string.Empty;
                        if (!resume.ResumeLink.Contains(".pdf"))
                        {
                            resumeLinkToParse = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(resume.ResumeLink);
                        }
                        else
                        {
                            resumeLinkToParse = resume.ResumeLink;
                        }
                        var resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeLinkToParse);

                        if (!string.IsNullOrEmpty(resumeStringContent))
                        {
                            // OPTIMIZATION: Pre-screen resume for validity (saves API calls on corrupted resumes)
                            var (isValid, validationReason) = _resumeOptimizationHelper.ValidateResumeForParsing(resumeStringContent);
                            if (!isValid)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Info,
                                    new Exception($"Resume {resume.id} validation failed: {validationReason}"));
                                candidateResumeList.Add(new CandidateResume
                                {
                                    id = resume.id,
                                    IsResumeParsed = false,
                                    IsProcessed = false,
                                    IsResumeParsingFailed = true,
                                    ReasonBehindNotProcessed = validationReason
                                });
                                continue;
                            }

                            // OPTIMIZATION: Reduce token usage by removing boilerplate and excessive whitespace
                            var estimatedTokensBefore = _resumeOptimizationHelper.EstimateTokenCount(resumeStringContent);
                            resumeStringContent = _resumeOptimizationHelper.OptimizeResumeText(resumeStringContent);
                            var estimatedTokensAfter = _resumeOptimizationHelper.EstimateTokenCount(resumeStringContent);

                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Info,
                                new Exception($"Resume {resume.id} optimization: {estimatedTokensBefore} tokens -> {estimatedTokensAfter} tokens (saved {estimatedTokensBefore - estimatedTokensAfter} tokens)"));

                            var runResult = await _assistantServiceHelper.ParseResume(resumeStringContent);
                            var replyText = runResult?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;

                            try
                            {
                                candidateParsedData = JsonConvert.DeserializeObject<CandidateResumeParsedDataOpenAI>(replyText);

                            }
                            catch (Exception ex)
                            {

                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                            }
                        }



                        //call the api for each of those resumes, return a candidate

                        try
                        {
                            if (candidateParsedData == null)
                            {
                                var candidateData = await _affIndaResumeParserHelper.UploadAndParseResume(resume.ResumeLink, resume.SkillChannelName, resume.IsAffindaIndexCreated);
                                if (!string.IsNullOrEmpty(candidateData.Item1))
                                {
                                    var documentData = JsonConvert.DeserializeObject<Document>(candidateData.Item1);
                                    var candidate = new CandidateQuickAddModel();
                                    candidate.Name = documentData.data?.name?.raw;
                                    if (!string.IsNullOrEmpty(candidateData.Item2))
                                    {
                                        candidate.AffindaResumeIdentifier = candidateData.Item2;
                                    }
                                    if (documentData.data.emails != null && documentData.data.emails.Count() > 0)
                                    {
                                        candidate.EmailId = documentData.data.emails[0].Trim();
                                        candidate.EmailId.Replace(":", "").Replace("-", "").Replace("%20", "").Replace("?:", "").Replace(":%20", "").Replace("-%20", "");

                                        if (candidate.EmailId.StartsWith(":") || candidate.EmailId.StartsWith("-"))
                                        {
                                            candidate.EmailId = candidate.EmailId.Substring(1); // Remove the first character (colon)
                                        }
                                        if (candidate.EmailId.StartsWith("%20"))
                                        {
                                            candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                                //candidate.EmailId.Replace("%20", "");

                                        }
                                        if (candidate.EmailId.StartsWith("?:"))
                                        {
                                            candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                                //candidate.EmailId.Replace("?:", "");

                                        }
                                        if (candidate.EmailId.StartsWith(":%20") || candidate.EmailId.StartsWith("-%20"))
                                        {
                                            candidate.EmailId = candidate.EmailId.Substring(4); // Remove the first four characters 
                                                                                                //candidate.EmailId.Replace(":%20", "").Replace("-%20", "");
                                        }
                                        if (!_validationHelper.ValidateEmailAddress(candidate.EmailId))
                                        {
                                            //mark it as cant be processed in the database
                                            candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = false });
                                            continue;
                                        }

                                    }
                                    else
                                    {
                                        //mark it as cant be processed in the database
                                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = true });
                                        continue;
                                    }
                                    if (documentData.data.phoneNumberDetails != null && documentData.data.phoneNumberDetails.Count() > 0)
                                    {
                                        //candidate.Phone = !string.IsNullOrEmpty(documentData.data.phoneNumberDetails[0].internationalCountryCode) ? documentData.data.phoneNumberDetails[0].rawText : null;
                                        candidate.Phone = documentData.data.phoneNumberDetails[0].rawText;
                                        if (candidate.Phone == "911" || candidate.Phone == "+911")
                                        {
                                            candidate.Phone = null;
                                        }
                                    }
                                    candidate.ResumeLink = resume.ResumeLink;
                                    candidate.CandidateResumeId = resume.id;
                                    candidate.IRFId = resume.IRFId;
                                    candidate.AddedByUserId = resume.AddedBy;
                                    candidate.ClientHREmail = resume.ClientHREmailId;
                                    candidateList.Add(candidate);
                                }
                                else//parsing was tried, but failed due to some reason, in that case, we wont parse the resume again
                                {
                                    candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true });

                                }
                            }

                            else if (candidateParsedData != null)
                            {
                                var candidate = new CandidateQuickAddModel();
                                candidate.Name = candidateParsedData.name;

                                if (!string.IsNullOrEmpty(candidateParsedData.emailId))
                                {
                                    candidate.EmailId = candidateParsedData.emailId.Trim();
                                    candidate.EmailId.Replace(":", "").Replace("-", "").Replace("%20", "").Replace("?:", "").Replace(":%20", "").Replace("-%20", "");

                                    if (candidate.EmailId.StartsWith(":") || candidate.EmailId.StartsWith("-"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(1); // Remove the first character (colon)
                                    }
                                    if (candidate.EmailId.StartsWith("%20"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                            //candidate.EmailId.Replace("%20", "");

                                    }
                                    if (candidate.EmailId.StartsWith("?:"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(3); // Remove the first three characters 
                                                                                            //candidate.EmailId.Replace("?:", "");

                                    }
                                    if (candidate.EmailId.StartsWith(":%20") || candidate.EmailId.StartsWith("-%20"))
                                    {
                                        candidate.EmailId = candidate.EmailId.Substring(4); // Remove the first four characters 
                                                                                            //candidate.EmailId.Replace(":%20", "").Replace("-%20", "");
                                    }
                                    if (!_validationHelper.ValidateEmailAddress(candidate.EmailId))
                                    {
                                        //mark it as cant be processed in the database
                                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = false });
                                        continue;
                                    }

                                }
                                else
                                {
                                    //mark it as cant be processed in the database
                                    candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = true, IsProcessed = false, IsResumeParsingFailed = false, IsEmailMissing = true });
                                    continue;
                                }
                                if (!string.IsNullOrEmpty(candidateParsedData.phoneNumber))
                                {
                                    candidate.Phone = candidateParsedData.phoneNumber.Replace(" ", string.Empty);
                                    if (candidate.Phone == "911" || candidate.Phone == "+911")
                                    {
                                        candidate.Phone = null;
                                    }
                                }
                                candidate.ResumeLink = resume.ResumeLink;
                                candidate.CandidateResumeId = resume.id;
                                candidate.IRFId = resume.IRFId;
                                candidate.AddedByUserId = resume.AddedBy;
                                candidate.ClientHREmail = resume.ClientHREmailId;
                                candidateList.Add(candidate);
                            }
                            else//parsing was tried, but failed due to some reason, in that case, we wont parse the resume again
                            {
                                candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true });

                            }
                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                            continue;
                        }
                    }
                    else
                    {
                        candidateResumeList.Add(new CandidateResume { id = resume.id, IsResumeParsed = false, IsProcessed = false, IsResumeParsingFailed = true });

                    }

                }

                if (candidateResumeList != null && candidateResumeList.Count() > 0)
                {
                    await _candidateRepository.UpdateCandidateResumeInBulk(candidateResumeList);
                }

                // add the candidate to database and update IsProcessed and IsResumeParsed
                if (candidateList != null && candidateList.Count() > 0)
                {
                    foreach (var irfId in candidateList.Select(x => x.IRFId).Distinct())
                    {
                        var candidateSubList = candidateList.Where(x => x.IRFId == irfId)?.ToList();
                        await _irfService.AddCandidateUsingResumeParserJob(candidateSubList, irfId);
                    }
                }

            }
            catch (Exception ex)
            {

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        //private async Task CandidateIVRCall()
        //{
        //    //var candidatemeetinglist = await _meetingService.FetchCandidateMeetingsForReminder();
        //    string timeZoneValue = "IST";
        //    //foreach (var item in candidatemeetinglist)
        //    //{
        //    //    try
        //    //    {
        //    //        timeZoneValue = "IST";

        //    //        var primaryskill = item.PrimarySkillInvite;
        //    //        string yesbutton = "https://portal.risebird.io/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=1&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;
        //    //        string nobutton = "https://portal.risebird.io/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=2&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;
        //    //        string rescheulebutton = "https://portal.risebird.io/Interview/CandidateRescheduleMeetingsStatus?meetingid=" + item.Interviewid + "&status=3&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;
        //    //        string needcleritybutton = "https://portal.risebird.io/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=4&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;

        //    //        if (!string.IsNullOrWhiteSpace(item.TimeZonesValue)
        //    //            && item.TimeZoneoffset.HasValue
        //    //            && !item.TimeZonesValue.Contains("Chennai"))
        //    //        {
        //    //            timeZoneValue = item.TimeZonesValue.Split(")").ElementAtOrDefault(1);

        //    //            item.interviewDate = item.interviewDate.AddMinutes(-(330 + item.TimeZoneoffset.Value));

        //    //        }

        //    //        await _twilioHelper.MakeIVRCallForCandidateMeetingReminder(item.MobileNumber, item.FirstName, item.uniqueid, item.Irfid, item.PrimarySkillInvite, item.Interviewid, item.candidateid, item.interviewDate, item.TimeZonesValue);

        //    //    }
        //    //    catch (Exception ex)
        //    //    {
        //    //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    //    }
        //    //}
        //    await _twilioHelper.MakeIVRCallForCandidateMeetingReminder("+918888571923", "Harshit", string.Empty, 16323, "expertise", 427827, 0, DateTime.Now, timeZoneValue);
        //}
        //public async Task<string> ReadPdfFromUrlAsync(string pdfUrl)
        //{

        //    try
        //    {
        //        using (HttpClient client = new HttpClient())
        //        {
        //            byte[] pdfBytes = await client.GetByteArrayAsync(pdfUrl);

        //            using (MemoryStream stream = new MemoryStream(pdfBytes))
        //            {
        //                using (var pdfDocument = UglyToad.PdfPig.PdfDocument.Open(stream))
        //                {
        //                    string text = "";
        //                    foreach (var page in pdfDocument.GetPages())
        //                    {
        //                        text += page.Text + "\n";
        //                    }
        //                    return text;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //var resultDe = _activityTrackerService.DebugLogger($"AI Resume Screener function error : {ex.Message}, {ex.StackTrace}", "aiScreener", string.Empty).Result;

        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

        //        return null;
        //    }
        //}

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMeetingResultCount()
        {
            try
            {
                var emailId = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                }
                var model = new MeetingResultCount();
                model = await _dashboardService.GetMeetingResultCount(emailId);
                return Json(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }

        [Authorize]
        [HttpGet]
        public async Task<DashboardVM> GetDateWiseInterviews(DateTime date)
        {
            try
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                var model = new DashboardVM();
                model.Meetings = await _dashboardService.GetDateWiseMeetings(date, Emailid);
                var meetingTimes = model.Meetings.Select(x => x.interviewDate).Distinct();
                //var irfList = model.Meetings.Select(x => x.IRFID).Distinct();
                var irfList = model.Meetings.Select(x => x.IRFnSkill).Distinct();
                List<object> iData = new List<object>();

                foreach (var item in meetingTimes)
                {
                    dynamic expando = new ExpandoObject();
                    expando.Time = item;
                    foreach (var irf in irfList)
                    {
                        AddProperty(expando, irf, model.Meetings.Where(x => x.IRFID == irf.Split("(")[0] && x.interviewDate == item).Count());
                    }
                    iData.Add(expando);
                }

                model.MeetingsData = JsonConvert.SerializeObject(iData);
                model.MeetingsDataLabels = JsonConvert.SerializeObject(irfList);
                return model;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetDateWiseInterviewStatus(DateTime date)
        {
            try
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                var model = new DashboardVM();
                model.Meetings = await _dashboardService.GetDateWiseMeetings(date, Emailid);
                model.TotalCompletedMeetings = model.Meetings != null ? model.Meetings.Where(x => x.InterviewStatus.HasValue).Count() : 0;
                return PartialView("InterviewStatusPartial", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetDateWiseMeetingsSlots(DateTime date)
        {
            try
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                var model = new DashboardVM();
                model.DailyInterviewSlots = await _dashboardService.GetDateWiseMeetingsSlots(date, Emailid);
                return PartialView("InterviewSlotsPartial", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateIRFProgress(DashboardVM model)
        {
            try
            {
                await _dashboardService.UpdateIRFProgress(model.IrfProgressList);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetDateWiseIRFProgress(DateTime date)
        {
            try
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                var model = new DashboardVM();
                model.IrfProgressList = (IList<IRF>)(await _dashboardService.GetIRFProgress(date, Emailid));
                model.ProgressOptions = new SelectList(await _dashboardService.GetProgressOptions(), "id", "Name");
                model.ProgressDate = date;
                TempData["ProgressDate"] = date;
                return PartialView("IRFProgressPartial", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ClientIndex()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var model = new ClientDashboardVM();
                        //var funnelData = await _dashboardService.GetClientFunnelData(emailid);
                        //var slotsDetails = await _dashboardService.GetDateWiseClientMeetingsSlots(DateTime.Now.Date, emailid);
                        //model = GetDateWiseClientInterviews(DateTime.Now.Date, emailid);
                        //model.CandidatesCountObj = await _dashboardService.GetClientCandidatesCurrentStatusCount(emailid);
                        model.EmailId = emailid;
                        //model.FunnelData = JsonConvert.SerializeObject(funnelData);
                        //model.DailyMeetingResultsSlots = slotsDetails;
                        var Whatsapp = _userService.GetWhatsappno(emailid);
                        if (Whatsapp == null)
                        {
                            ViewBag.Whatsappflag = true;
                        }
                        else
                        {
                            ViewBag.Whatsappflag = false;
                        }
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }

                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<string> GetClientFunnelData()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        var funnelData = await _dashboardService.GetClientFunnelData(emailid);
                        return JsonConvert.SerializeObject(funnelData);
                    }
                    else
                        return string.Empty;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<ClientDashboardVM> GetDateWiseClientInterviews(DateTime dateValue, string emailId)
        {
            try
            {
                var model = new ClientDashboardVM();
                model.Meetings = await _dashboardService.GetDateWiseClientMeetings(dateValue, emailId);
                var meetingTimes = model.Meetings.Select(x => x.interviewDate).Distinct();

                var irfList = model.Meetings.Select(x => x.IRFnSkill).Distinct();
                List<object> iData = new List<object>();

                foreach (var item in meetingTimes)
                {
                    dynamic expando = new ExpandoObject();
                    expando.Time = item;
                    foreach (var irf in irfList)
                    {
                        AddProperty(expando, irf, model.Meetings.Where(x => x.IRFID == irf.Split("(")[0] && x.interviewDate == item).Count());
                    }
                    iData.Add(expando);
                }

                model.MeetingsData = JsonConvert.SerializeObject(iData);
                model.MeetingsDataLabels = JsonConvert.SerializeObject(irfList);
                return model;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetDateWiseClientMeetingsSlots(DateTime dateValue, string emailId)
        {
            try
            {
                var model = new ClientDashboardVM();
                model.DailyMeetingResultsSlots = await _dashboardService.GetDateWiseClientMeetingsSlots(dateValue, emailId);
                return PartialView("ClientSlotsStatusPartial", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public JsonResult FetchActivityDetails(int id, int idprogess)
        {
            var result = _dashboardService.FetchActivityDetails(id, idprogess);
            return Json(result);
        }


        #region ChannelManagement
        [HttpGet]
        public async Task<IActionResult> RequirementManagement(string segment = "All")
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");

                if (profileName != "Admin" && profileName != "HR" && profileName != "SeniorHR"
                    && profileName != "SupportAdmin" && profileName != "Client")
                    return RedirectToAction("SignOut", "Session");

                var model = await _dashboardService.FetchChannelRequests() ?? new List<ChannelManagementVM>();
                ViewBag.Client = "false";
                if (profileName == "Client")
                {
                    var Emailaddress = emailid.ToUpper();
                    model = model.Where(x => (x.RequesterEmailid ?? "").ToUpper() == Emailaddress).ToList();
                    ViewBag.Client = "true";
                }

                ViewBag.SupportPersonCounts = model
                    .Where(x => !string.IsNullOrWhiteSpace(x.ChannelOwner))
                    .GroupBy(x => x.ChannelOwner.Trim())
                    .OrderByDescending(g => g.Count())
                    .ToDictionary(g => g.Key, g => g.Count());

                ViewBag.SegmentAllCount = model.Count;
                ViewBag.SegmentVerifiedCount = model.Count(x => string.Equals(x.RequesterType, "Verified", StringComparison.OrdinalIgnoreCase));
                ViewBag.SegmentExternalCount = model.Count(x => string.Equals(x.RequesterType, "External", StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(x.RequesterType));
                ViewBag.ProcessedCount = model.Count(x => x.IsProcessed);

                segment = string.IsNullOrWhiteSpace(segment) ? "All" : segment.Trim();
                ViewBag.ActiveSegment = segment;
                if (string.Equals(segment, "Verified", StringComparison.OrdinalIgnoreCase))
                    model = model.Where(x => string.Equals(x.RequesterType, "Verified", StringComparison.OrdinalIgnoreCase)).ToList();
                else if (string.Equals(segment, "External", StringComparison.OrdinalIgnoreCase))
                    model = model.Where(x => string.Equals(x.RequesterType, "External", StringComparison.OrdinalIgnoreCase)
                        || string.IsNullOrWhiteSpace(x.RequesterType)).ToList();

                var magList = _interviewerService.FetchAssessmentGroups()
                    ?.Where(x => x != null && x.id > 0 && !string.IsNullOrWhiteSpace(x.AssessmentGroup))
                    .GroupBy(x => x.id)
                    .Select(g => g.First())
                    .ToDictionary(x => x.id, x => x.AssessmentGroup)
                    ?? new Dictionary<int, string>();
                ViewBag.MAGList = magList;

                var templates = await _clonableSkillChannelTemplateService.GetAllClonableSkillChannels()
                    ?? Enumerable.Empty<ClonableSkillChannelVM>();
                var templateList = templates
                    .Where(x => x != null && x.id > 0)
                    .GroupBy(x => x.id)
                    .Select(g => g.First())
                    .ToDictionary(
                        x => x.id,
                        x => !string.IsNullOrWhiteSpace(x.Name) ? x.Name
                            : (!string.IsNullOrWhiteSpace(x.PrimarySkills) ? x.PrimarySkills : ("Template #" + x.id)));
                ViewBag.TemplateList = templateList;

                try
                {
                    var clients = await _clientService.GetAllClients() ?? Enumerable.Empty<Client>();
                    ViewBag.ClientOptions = new SelectList(clients.Where(c => c != null && c.id > 0).OrderBy(c => c.Name), "id", "Name");
                    var roles = await _clientService.GetClientRoles() ?? new List<ClientRole>();
                    ViewBag.ClientRoleOptions = new SelectList(roles.Where(r => r != null && r.id > 0).OrderBy(r => r.Name), "id", "Name");
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    ViewBag.ClientOptions = new SelectList(Enumerable.Empty<SelectListItem>());
                    ViewBag.ClientRoleOptions = new SelectList(Enumerable.Empty<SelectListItem>());
                }

                if (TempData["SuccessErrorMessageInterview"] != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                    TempData.Remove("SuccessErrorMessageInterview");
                }
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> GetIRFsByMAG(int magId)
        {
            try
            {
                if (magId <= 0)
                    return Json(new { success = true, data = Array.Empty<object>() });

                var irfs = await _irfRepository.GetAssessmentGroupIRFList(magId)
                    ?? Enumerable.Empty<AssessmentIRFListVM>();
                var data = irfs
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.IRFId))
                    .Select(x => new
                    {
                        id = x.id,
                        irfId = x.IRFId,
                        label = string.IsNullOrWhiteSpace(x.PrimarySkillInvite)
                            ? x.IRFId
                            : (x.IRFId + " — " + x.PrimarySkillInvite)
                    })
                    .ToList();
                return Json(new { success = true, data });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, data = Array.Empty<object>(), message = "Failed to load IRFs." });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> SetRequirementDetails([FromBody] SetRequirementDetailsVM model)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "");
                if (profileName != "Admin" && profileName != "HR" && profileName != "SeniorHR" && profileName != "SupportAdmin")
                    return Json(new { success = false, message = "Unauthorized." });

                if (model == null || model.Id <= 0)
                    return Json(new { success = false, message = "Invalid requirement." });

                var hasMapping = model.MAGId.GetValueOrDefault() > 0 || model.TemplateId.GetValueOrDefault() > 0;
                if (!hasMapping || string.IsNullOrWhiteSpace(model.IRFIDs))
                    return Json(new { success = false, message = "Select a MAG or Template and at least one IRF." });

                var result = await _dashboardService.SetRequirementMapping(model);
                if (result == "Success")
                    return Json(new { success = true, message = "Requirement mapped successfully." });

                return Json(new { success = false, message = "Failed to save mapping." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Failed to save mapping." });
            }
        }

        // --- Public mini Add Requirement (Session 1 / intake hand-off) ---
        [AllowAnonymous]
        [HttpGet("/Requirement/Externaluser")]
        public IActionResult PublicAddRequirement()
        {
            return View(new PublicChannelVM());
        }

        [AllowAnonymous]
        [HttpGet]
        [EnableRateLimiting("PublicIntake")]
        public async Task<JsonResult> VerifyRequesterEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return Json(new { ok = false, clientId = 0, clientName = (string)null });

                int clientId = await _clientService.GetClientByEmailId(email.Trim());
                // Anonymous (public form) callers only learn yes/no, so the endpoint can't be used to map
                // emails to client ids or company names. Staff sessions keep the full response.
                var sessionProfile = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                var isStaff = HttpContext.Session.GetString("email") != null
                    && Risebird.Web.Helpers.DeliveryHeadInstructionViewMode.IsOperationalProfile(sessionProfile);
                if (clientId > 0 && !isStaff)
                    return Json(new { ok = true, clientId = 0, clientName = (string)null });
                if (clientId > 0)
                {
                    var company = await _clientService.GetClientRoleAndCompanyNameByEmailId(email.Trim());
                    // Tuple: ClientId, Name, LogoLink — use Item2 (Name) per frozen contract
                    var clientName = company?.Item2;
                    if (string.IsNullOrWhiteSpace(clientName))
                        clientName = await _clientService.GetClientName(clientId);
                    return Json(new { ok = true, clientId, clientName });
                }
                return Json(new { ok = false, clientId = 0, clientName = (string)null });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, clientId = 0, clientName = (string)null });
            }
        }

        [AllowAnonymous]
        [HttpPost("/Requirement/Externaluser")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("PublicIntake")]
        public async Task<IActionResult> PublicAddRequirement(PublicChannelVM m)
        {
            try
            {
                var hasJd = (m.JdFile != null && m.JdFile.Length > 0) || !string.IsNullOrWhiteSpace(m.JdLink);
                if (!hasJd)
                    ModelState.AddModelError("JdLink", "Please upload a JD file or paste a JD link.");

                if (m.JdFile != null && m.JdFile.Length > 0)
                {
                    var ext = Path.GetExtension(m.JdFile.FileName)?.ToLowerInvariant();
                    var allowed = new[] { ".pdf", ".doc", ".docx" };
                    if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext))
                        ModelState.AddModelError("JdFile", "JD file must be PDF, DOC, or DOCX.");
                }

                if (!ModelState.IsValid)
                    return View(m);

                if (m.JdFile != null && m.JdFile.Length > 0)
                {
                    var safeName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(m.JdFile.FileName)}";
                    await _azureStorageHelper.InsertBlob(safeName, "resumes", m.JdFile);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                    var blob = containerRef.GetBlockBlobReference(safeName);
                    m.JdLink = blob.Uri.ToString();
                }

                // Re-verify server-side — never trust the badge
                int clientId = await _clientService.GetClientByEmailId(m.RequesterEmailid?.Trim());
                string clientName = null;
                if (clientId > 0)
                {
                    var company = await _clientService.GetClientRoleAndCompanyNameByEmailId(m.RequesterEmailid.Trim());
                    clientName = company?.Item2;
                    if (string.IsNullOrWhiteSpace(clientName))
                        clientName = await _clientService.GetClientName(clientId);
                }

                var channel = new ChannelVM
                {
                    Client = clientId > 0 ? clientId.ToString() : "",
                    ClientId = clientId > 0 ? clientId : 0,
                    RequesterType = clientId > 0 ? "Verified" : "External",
                    Status = "Pending",
                    Origin = "Public",
                    Requester = clientId > 0 ? (clientName ?? "") : "",
                    RequesterMobile = "",
                    RequesterEmailid = m.RequesterEmailid?.Trim(),
                    Title = m.Title?.Trim(),
                    PrimarySkill = m.PrimarySkill?.Trim() ?? "",
                    JdLink = m.JdLink?.Trim(),
                    EndClient = "",
                    Location = "",
                    SeniorityLevel = "",
                    Comments = "",
                    TurnAroundTimeindays = "0",
                    NumberofInterviews = 0,
                    MAGId = null,
                    TemplateId = null,
                    IRFIDs = null,
                    CreatedBy = "PublicForm",
                    isEditable = false
                };

                var result = await _dashboardService.AddChannelRequest(channel);
                if (string.Equals(result, "Success", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["PublicReqTitle"] = m.Title;
                    TempData["PublicReqEmail"] = m.RequesterEmailid;
                    return RedirectToAction(nameof(PublicRequirementThankYou));
                }

                ModelState.AddModelError("", "Could not submit your requirement. Please try again.");
                return View(m);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ModelState.AddModelError("", "Could not submit your requirement. Please try again.");
                return View(m);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult PublicRequirementThankYou()
        {
            var vm = new PublicChannelVM
            {
                Title = TempData["PublicReqTitle"] as string,
                RequesterEmailid = TempData["PublicReqEmail"] as string
            };
            return View("PublicAddRequirementThanks", vm);
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> AddClientUserFromRequirement([FromBody] AddClientUserFromRequirementVM model)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "");
                if (profileName != "Admin" && profileName != "HR" && profileName != "SeniorHR" && profileName != "SupportAdmin")
                    return Json(new { ok = false, message = "Unauthorized." });

                if (model == null || model.RequirementId <= 0 || string.IsNullOrWhiteSpace(model.EmailId))
                    return Json(new { ok = false, message = "Invalid request." });

                int clientId = model.SelectedClient.GetValueOrDefault();
                if (clientId <= 0 && !string.IsNullOrWhiteSpace(model.NewClientName))
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? "";
                    var newClient = new Client
                    {
                        Name = model.NewClientName.Trim(),
                        Description = model.NewClientName.Trim(),
                        Createdby = emailid
                    };
                    var created = await _clientService.AddClient(newClient);
                    if (!created)
                        return Json(new { ok = false, message = "Could not create client." });

                    var all = await _clientService.GetAllClients() ?? Enumerable.Empty<Client>();
                    clientId = all
                        .Where(c => c != null && string.Equals(c.Name, model.NewClientName.Trim(), StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(c => c.id)
                        .Select(c => c.id)
                        .FirstOrDefault();
                    if (clientId <= 0)
                        return Json(new { ok = false, message = "Client created but could not resolve id." });
                }

                if (clientId <= 0 || model.SelectedClientRole.GetValueOrDefault() <= 0)
                    return Json(new { ok = false, message = "Select a client and client role." });

                var user = new ClientUserVM
                {
                    FirstName = model.FirstName?.Trim(),
                    LastName = model.LastName?.Trim(),
                    EmailId = model.EmailId.Trim(),
                    MobileNumber = model.MobileNumber?.Trim(),
                    WhatsappNumber = model.WhatsappNumber?.Trim(),
                    SelectedClient = clientId,
                    SelectedClientRole = model.SelectedClientRole
                };

                if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName))
                    return Json(new { ok = false, message = "First name and last name are required." });

                bool ok = await _userService.AddClientUser(user);
                if (ok)
                {
                    var clientName = await _clientService.GetClientName(clientId);
                    await _dashboardService.LinkRequirementToClient(model.RequirementId, clientId, clientName);
                    return Json(new { ok = true, clientId, clientName, requesterType = "Verified" });
                }
                return Json(new { ok = false, message = "Failed to add client user." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Failed to add client user." });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> GetWeekendDriveSpotlight(string bucket = "This")
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "");
                if (profileName != "Admin" && profileName != "HR" && profileName != "SeniorHR" && profileName != "SupportAdmin")
                    return Json(new { success = false, data = Array.Empty<object>() });

                var rows = await _dashboardService.GetWeekendDriveSpotlight(bucket) ?? Enumerable.Empty<WeekendDriveSpotlightVM>();
                return Json(new { success = true, data = rows });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, data = Array.Empty<object>() });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> UpsertWeekendDriveSpotlight(int irfId, string weekendBucket, string actionsTaken = null, bool remove = false)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "");
                if (profileName != "Admin" && profileName != "HR" && profileName != "SeniorHR" && profileName != "SupportAdmin")
                    return Json(new { ok = false, message = "Unauthorized." });

                var email = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? "";
                var ok = await _dashboardService.UpsertWeekendDriveSpotlight(irfId, weekendBucket, actionsTaken, remove, email);
                if (!ok)
                {
                    return Json(new
                    {
                        ok = false,
                        message = "Spotlight save failed. Check DB permissions to create/write WeekendDriveSpotlight."
                    });
                }
                return Json(new { ok = true });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> SyncWeekendDriveSpotlight(string weekendBucket, string irfIds)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "");
                if (profileName != "Admin" && profileName != "HR" && profileName != "SeniorHR" && profileName != "SupportAdmin")
                    return Json(new { ok = false, message = "Unauthorized." });

                var email = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? "";
                var ids = (irfIds ?? "")
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
                    .Where(n => n > 0)
                    .Distinct()
                    .ToList();

                var ok = await _dashboardService.SyncWeekendDriveSpotlight(
                    string.IsNullOrWhiteSpace(weekendBucket) ? "This" : weekendBucket.Trim(),
                    ids,
                    email);
                if (!ok)
                {
                    return Json(new
                    {
                        ok = false,
                        count = ids.Count,
                        message = "Spotlight save failed. Check DB permissions to create/write WeekendDriveSpotlight."
                    });
                }
                return Json(new { ok = true, count = ids.Count });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false });
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult WeekendDrivePlanner()
        {
            var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "");
            if (profileName != "Admin" && profileName != "HR" && profileName != "SeniorHR" && profileName != "SupportAdmin")
                return RedirectToAction("SignOut", "Session");

            // Reuse the existing Weekend Drive planner (InterviewDemand) — spotlight APIs above feed curation.
            return RedirectToAction("Index", "InterviewDemand");
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> AddRequirement()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                ViewBag.type = "back";
            }
            else
            {
                ViewBag.type = "no";
                var menuData = await _userService.GetMenuBasedOnUserEmailld("Admin");
                var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName == "Support").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
            }
            ViewBag.Client = "false";
            var model = new ChannelVM();
            model.isEditable = false;
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid?.Replace("\"", "");
                if (!string.IsNullOrWhiteSpace(emailid))
                    model.CreatedBy = emailid;
            }
            await PopulateChannelRequirementDropDowns(model);
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> AddRequirement(ChannelVM model)
        {
            if (string.IsNullOrWhiteSpace(model.CreatedBy)
                && HttpContext.Session.GetString("email") != null
                && HttpContext.Session.GetString("profileName") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid?.Replace("\"", "");
                if (!string.IsNullOrWhiteSpace(emailid))
                    model.CreatedBy = emailid;
            }
            await ResolveChannelClientFromSelection(model);
            if (!string.IsNullOrWhiteSpace(model.Client))
                ModelState.Remove("Client");

            if (!ModelState.IsValid)
            {
                SetAddRequirementViewBag();
                await PopulateChannelRequirementDropDowns(model);
                if (model.isEditable)
                    PopulateChannelEditDropDowns(model);
                return View(model);
            }
            if (model.Jdfile == null && string.IsNullOrWhiteSpace(model.JdLink))
            {
                ModelState.AddModelError(string.Empty, "Please upload a Jd.");
                SetAddRequirementViewBag();
                await PopulateChannelRequirementDropDowns(model);
                if (model.isEditable)
                    PopulateChannelEditDropDowns(model);
                return View(model);
            }
            if (model.Jdfile != null)
            {
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.Jdfile);

                if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                || fileMimeType == "application/pdf")
                {
                    await _azureStorageHelper.InsertBlob(model.Jdfile.FileName, "resumes", model.Jdfile);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                    var blob = containerRef.GetBlockBlobReference(model.Jdfile.FileName);
                    model.JdLink = blob.Uri.ToString();
                }

            }

            if (model.isEditable)
            {
                var result = await _dashboardService.AddChannelRequest(model);
                if (result == "Success")
                {
                    TempData["SuccessErrorMessageInterview"] = "Channel Request has been updated Successfully!";
                }
                else
                {
                    TempData["SuccessErrorMessageInterview"] = "Failed to update channel request!";
                }
                return RedirectToAction("RequirementManagement");
            }
            else
            {
                // Classify from ClientUser (never trust the hidden form field alone)
                if (!string.IsNullOrWhiteSpace(model.RequesterEmailid))
                {
                    var knownClientId = await _clientService.GetClientByEmailId(model.RequesterEmailid.Trim());
                    model.RequesterType = knownClientId > 0 ? "Verified" : "External";
                }
                else if (string.IsNullOrWhiteSpace(model.RequesterType))
                {
                    model.RequesterType = "External";
                }

                var result = await _dashboardService.AddChannelRequest(model);
                if (result == "Success")
                {
                    var Emailds = await _dashboardService.FetchAdminandHRList();
                    string Emailaddress = string.Join(";", Emailds);
                    Mail _mail = new Mail
                    {
                        TO = Emailaddress,
                        Subject = $"Risebird: a new Channel request is created for " + model.Client
                    };
                    var resultset = await _notificationHelper.ChannelRequestNotification(_mail, model.Client, model.PrimarySkill, model.RequesterEmailid);
                    TempData["SuccessErrorMessageInterview"] = "Channel Request has been added Successfully!";
                    ViewBag.Message = "Channel Request has been added Successfully!";
                }
                else
                {
                    TempData["SuccessErrorMessageInterview"] = "Failed to add channel request!";
                    ViewBag.Message = "Failed to add channel request.";
                }
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                {
                    var menuData = await _userService.GetMenuBasedOnUserEmailld("Admin");
                    var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                    if (menus != null)
                    {
                        menus = menus.Where(x => x.functionName == "Support").ToList();
                        HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                    }
                    SetAddRequirementViewBag();
                    await PopulateChannelRequirementDropDowns(model);
                    return View(model);
                }
                return RedirectToAction("RequirementManagement");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditRequirement(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    ViewBag.type = "back";
                    ViewBag.Client = "false";
                    if (profileName == "Client")
                    {
                        ViewBag.Client = "true";
                    }
                    var model = await _dashboardService.FetchChannelRequestsByid(id);
                    model.isEditable = true;
                    if (model.ClientId <= 0 && !string.IsNullOrWhiteSpace(model.Client))
                    {
                        var clients = await _clientRepository.GetClientsForDropdown();
                        var matchedClient = clients?.FirstOrDefault(c =>
                            string.Equals(c.Name, model.Client, StringComparison.OrdinalIgnoreCase));
                        if (matchedClient != null)
                            model.ClientId = matchedClient.id;
                    }
                    await PopulateChannelRequirementDropDowns(model);
                    PopulateChannelEditDropDowns(model);
                    return View("AddRequirement", model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> GetRequesterEmails(int clientId)
        {
            var emails = await _clientRepository.GetClientUsersByClientId(clientId);
            return Json(new { success = true, data = emails });
        }

        [Authorize]
        [HttpGet]
        public async Task<JsonResult> GetClientDetails(int clientId)
        {
            var client = await _clientRepository.GetClient(clientId);
            return Json(new { success = client != null, planType = client?.PlanType, clientType = client?.ClientType });
        }

        private async Task PopulateChannelRequirementDropDowns(ChannelVM model)
        {
            var clients = await _clientRepository.GetClientsForDropdown();
            model.ClientList = new SelectList(clients ?? Enumerable.Empty<Client>(), "id", "Name", model.ClientId > 0 ? model.ClientId : null);

            model.CreatedByList = new SelectList(_irfRepository.GetRisebirdUsersForDropdown() ?? Enumerable.Empty<Users>(), "EmailId", "EmailId", model.CreatedBy);

            var planTypes = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                            select new { ID = (int)s, Name = s.GetDescription() };
            model.PlanTypeList = new SelectList(planTypes, "ID", "Name", model.PlanType > 0 ? model.PlanType : null);

            var clientTypes = from CompanyType s in Enum.GetValues(typeof(CompanyType))
                              select new { ID = (int)s, Name = s.GetDescription() };
            model.ClientTypeList = new SelectList(clientTypes, "ID", "Name", model.ClientType > 0 ? model.ClientType : null);
        }

        private static void PopulateChannelEditDropDowns(ChannelVM model)
        {
            var priority = from Prioritylevels s in Enum.GetValues(typeof(Prioritylevels))
                           select new { ID = (int)s, Name = s.GetDescription() };
            model.Prioritylist = new SelectList(priority, "ID", "Name", model.Priority);

            var origin = from Originlevel s in Enum.GetValues(typeof(Originlevel))
                         select new { ID = (int)s, Name = s.GetDescription() };
            model.Originlist = new SelectList(origin, "ID", "Name", model.Origin);

            var engagementType = from Engagementtypelevel s in Enum.GetValues(typeof(Engagementtypelevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
            model.EngagementTypelist = new SelectList(engagementType, "ID", "Name", model.EngagementType);

            var stage = from stagelevel s in Enum.GetValues(typeof(stagelevel))
                        select new { ID = (int)s, Name = s.GetDescription() };
            model.stagelist = new SelectList(stage, "ID", "Name", model.Stage);

            var paymentStatus = from paymentstatuslevel s in Enum.GetValues(typeof(paymentstatuslevel))
                                select new { ID = (int)s, Name = s.GetDescription() };
            model.PaymentStatuslist = new SelectList(paymentStatus, "ID", "Name", model.PaymentStatus);

            var confirmationStatus = from ConfirmationStatuslevel s in Enum.GetValues(typeof(ConfirmationStatuslevel))
                                     select new { ID = (int)s, Name = s.GetDescription() };
            model.ConfirmationStatuslist = new SelectList(confirmationStatus, "ID", "Name", model.ConfirmationStatus);
        }

        private async Task ResolveChannelClientFromSelection(ChannelVM model)
        {
            if (model.ClientId <= 0)
                return;

            model.Client = await _clientRepository.GetClientName(model.ClientId);
        }

        private void SetAddRequirementViewBag()
        {
            ViewBag.Client = "false";
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                ViewBag.type = "back";
            else
                ViewBag.type = "no";
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Interviewer(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                {
                    var model = await _dashboardService.FetchInterviewerBychannelid(id);
                    ViewBag.channelid = id;
                    if (TempData["SuccessErrorMessageInterview"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                        TempData.Remove("SuccessErrorMessageInterview");
                    }
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public ActionResult AddInterviewer(int id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                {
                    var model = new ChannelInterviewerVM();
                    model.Channelid = id;
                    var Priority = from Prioritylevels s in Enum.GetValues(typeof(Prioritylevels))
                                   select new { ID = (int)s, Name = s.GetDescription() };
                    model.Prioritylist = new SelectList(Priority, "ID", "Name");
                    model.isEditable = false;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddInterviewer(ChannelInterviewerVM model)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                model.CreatedBy = emailid;
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                {
                    if (!ModelState.IsValid)
                    {
                        return View(model);
                    }
                    if (model.CV == null && (model.CVLink == null))
                    {
                        ModelState.AddModelError(string.Empty, "Please upload a CV.");
                        var Priority = from Prioritylevels s in Enum.GetValues(typeof(Prioritylevels))
                                       select new { ID = (int)s, Name = s.GetDescription() };
                        model.Prioritylist = new SelectList(Priority, "ID", "Name");
                        return View(model);
                    }
                    if (model.CV != null)
                    {
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.CV);

                        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                        || fileMimeType == "application/pdf")
                        {
                            await _azureStorageHelper.InsertBlob(model.CV.FileName, "resumes", model.CV);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(model.CV.FileName);
                            model.CVLink = blob.Uri.ToString();
                        }

                    }
                    if (model.CodedCV != null)
                    {
                        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.CodedCV);

                        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                        || fileMimeType == "application/pdf")
                        {
                            await _azureStorageHelper.InsertBlob(model.CodedCV.FileName, "resumes", model.CodedCV);
                            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                            var blob = containerRef.GetBlockBlobReference(model.CodedCV.FileName);
                            model.CodedCVLink = blob.Uri.ToString();
                        }

                    }
                    var result = await _dashboardService.AddChannelInterviewer(model);
                    if (result == "Success")
                    {
                        if (model.isEditable)
                        {
                            TempData["SuccessErrorMessageInterview"] = "Interviewer has been updated Successfully!";
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterview"] = "Interviewer has been added Successfully!";
                        }
                    }
                    else
                    {
                        if (model.isEditable)
                        {
                            TempData["SuccessErrorMessageInterview"] = "Failed to update interviewer!";
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterview"] = "Failed to add interviewer!";
                        }
                    }
                    return RedirectToAction("Interviewer", "DashBoard", new { id = model.Channelid });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> DeleteInterviewer(int id, int channelid)
        {
            var result = await _dashboardService.DeleteChannelInterviewerByid(id, channelid);
            if (result == "Success")
            {
                return Json("Success");
            }
            else
            {
                return Json("fail");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult> EditInterviewer(int id, int channelid)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var model = await _dashboardService.FecthInterviewerById(id);
                model.Channelid = channelid;
                var Priority = from Prioritylevels s in Enum.GetValues(typeof(Prioritylevels))
                               select new { ID = (int)s, Name = s.GetDescription() };
                model.Prioritylist = new SelectList(Priority, "ID", "Name");
                model.isEditable = true;
                return View("AddInterviewer", model);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public async Task<JsonResult> FetchHR(string term)
        {
            var model = await _dashboardService.FetchHRList(term);
            return Json(model);
        }

        [HttpGet]
        public IActionResult AddInterviewerList(int id, string channelname)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (profileName == "Client")
                        {
                            ViewBag.IsClient = "true";
                        }
                        ViewBag.ChannelName = channelname;
                        ViewBag.ISCId = id;
                        ViewBag.profile = profileName;
                        var interviewerList = new List<ChannelApprovedInterviewer>();
                        interviewerList = _dashboardService.GetChannelInterviewers(id)?.ToList();


                        if (TempData["SuccessErrorMessageISCInterviewerList"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageISCInterviewerList"].ToString();
                            TempData.Remove("SuccessErrorMessageISCInterviewerList");
                        }
                        return View(interviewerList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public IActionResult SelectChannelInterviewers(int cmId, string cmTitle)
        {

            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
                {
                    var model = new AddChannelInterviewersVM();
                    model.ChannelInterviewersCollection = (IList<ChannelApprovedInterviewer>)_dashboardService.GetChannelApprovedInterviewer(cmId);
                    model.ChannelId = cmId;
                    model.ChannelTitle = cmTitle;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SelectChannelInterviewers(AddChannelInterviewersVM model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
                    {
                        if (_dashboardService.AddChannelApprovedInterviewer(model.ChannelInterviewersCollection, model.ChannelId))
                        {
                            TempData["SuccessErrorMessageISCInterviewerList"] = "Interviewer has been added Successfully!";
                            return RedirectToAction("AddInterviewerList", new { id = model.ChannelId, channelname = model.ChannelTitle });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageISCInterviewerList"] = "Interviewer could not be added.";
                            return RedirectToAction("AddInterviewerList", new { id = model.ChannelId, channelname = model.ChannelTitle });
                        }
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }

                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageISCInterviewerList"] = "Interviewer could not be added.";
                return RedirectToAction("AddInterviewerList", new { id = model.ChannelId, channelname = model.ChannelTitle });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteChannelInterviewer(int id, int iscId, string iscName)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
                    {
                        if (await _dashboardService.DeleteChannelInterviewer(id))
                        {
                            //TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer removed Successfully.";
                            return RedirectToAction("AddInterviewerList", new { id = iscId, channelname = iscName });
                        }
                        else
                        {
                            //TempData["SuccessErrorMessageISCInterviewer"] = "Could not remove Interviewer.";
                            return RedirectToAction("AddInterviewerList", new { id = iscId, channelname = iscName });
                        }
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }

                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageISCInterviewer"] = "Could not remove Interviewer.";
                return RedirectToAction("GetISCInterviewers", new { id = iscId, InterviewerSkillidName = iscName });
            }

        }

        [HttpGet]
        public async Task<IActionResult> ChannelInterviewerAction(int id, string channelName, int channelid, string status)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client")
                    {
                        if (await _dashboardService.ChannelInterviewerAction(id, status))
                        {
                            if (status == "Approved")
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer has been approved Successfully!";
                            }
                            else
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Interviewer has been unapproved Successfully!";
                            }

                            return RedirectToAction("AddInterviewerList", new { id = channelid, channelname = channelName });
                        }
                        else
                        {
                            if (status == "Approved")
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Could not approved Interviewer.";
                            }
                            else
                            {
                                TempData["SuccessErrorMessageISCInterviewer"] = "Could not unapproved Interviewer.";
                            }
                            return RedirectToAction("AddInterviewerList", new { id = channelid, channelname = channelName });
                        }
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }

                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageISCInterviewer"] = "Could not remove Interviewer.";
                return RedirectToAction("GetISCInterviewers", new { id = channelid, channelname = channelName });
            }
        }

        #endregion

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UnassignedInterviews(string actionstatus, int actiontype, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.emailid = emailid;
                    ViewBag.PageFrom = "Completed";
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Interviewer" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "TechSupport" || profileName == "Finance")
                    {
                        if (HttpContext.Session.GetString("UnassignedInterviewsFilters") != null)
                            HttpContext.Session.Remove("UnassignedInterviewsFilters");
                        if (actionstatus == RibbonActionType.DeletedZoomMeetings)
                        {
                            return RedirectToAction("DeletedZoomMeetings", new { /*actionStatus = actionstatus, */actionType = actiontype, returnBack = returnBack });
                        }
                        var modelList = new List<MeetingVM>();
                        if (actiontype == 1 && actionstatus == RibbonActionType.UnAssignedCoordianter)
                        {
                            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration, _httpClientFactory);
                            var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");

                            var myInterview = _meetingService.GetHRAssignedInterviews(emailid);
                            var WeekDate = DateTime.Now.AddDays(-30);
                            var interviewerids = myInterview.Where(x => x.interviewDate >= WeekDate).Select(x => x.id).Distinct().ToList();
                            if (interviewerids != null)
                            {
                                foreach (var item in myInterview)
                                {
                                    if (result.Where(x => x.interviewid == item.id).Count() == 0)
                                    {
                                        modelList.Add(new MeetingVM
                                        {
                                            IRFId = item.IRFID,
                                            interviewDate = item.interviewDate,
                                            candidateName = item.candidateName,
                                            candidateEmail = item.candidateEmail,
                                            imid = item.id,
                                            interviewer = item.interviewer,
                                            confirmationStatus = "1",
                                            HRCoordinatorEmailId = emailid,
                                            MeetingStatus = item.MeetingStatus,
                                            InterviewStatus = item.InterviewStatus,
                                        });
                                    }
                                }
                            }
                        }
                        else if (actionstatus == RibbonActionType.PropableRedo || actionstatus == RibbonActionType.QCUnderReview || actionstatus == RibbonActionType.QCAIInterview)
                        {
                            modelList = await _dashboardService.FetchPropableRedoDetails(emailid, profileName, actionstatus);
                        }
                        else
                        {
                            modelList = await _dashboardService.FetchRibbonDetailsValues(actionstatus, emailid, actiontype, profileName);
                        }
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        ViewBag.PageHeader = "";
                        ViewBag.Actiontype = actiontype;
                        ViewBag.actionstatus = actionstatus;
                        ViewBag.ProfileName = profileName;
                        if (actionstatus == RibbonActionType.InterviewCount)
                        {
                            ViewBag.PageHeader = "Unassigned Interviews";
                        }
                        else if (actionstatus == RibbonActionType.MeetingStatus)
                        {
                            ViewBag.PageHeader = "Status Pending";
                        }
                        else if (actionstatus == RibbonActionType.AIMeetingStatus)
                        {
                            ViewBag.PageHeader = "AI Status Pending";
                        }
                        else if (actionstatus == RibbonActionType.QcPending)
                        {
                            ViewBag.PageHeader = "Qc Pending";
                        }
                        else if (actionstatus == RibbonActionType.UnAssignedCoordianter && actiontype == 0)
                        {
                            ViewBag.PageHeader = "Auto Cancelled Interview";
                        }
                        else if (actionstatus == RibbonActionType.UnAssignedCoordianter && actiontype == 1)
                        {
                            ViewBag.PageHeader = "Auto Cancelled Interview";
                        }
                        else if (actionstatus == RibbonActionType.ZoomLink)
                        {
                            ViewBag.PageHeader = "Missing Zoom Link";
                        }
                        else if (actionstatus == RibbonActionType.UnAttendedZoomLink)
                        {
                            ViewBag.PageHeader = "Participant Waiting";
                        }
                        else if (actionstatus == RibbonActionType.VideoLink)
                        {
                            ViewBag.PageHeader = "Missing Video Link";
                        }
                        else if (actionstatus == RibbonActionType.QCPendingReport || actionstatus == RibbonActionType.QCReport)
                        {
                            ViewBag.PageHeader = "QC Report";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskByClient)
                        {
                            ViewBag.PageHeader = "Potential Risk By Client";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskInterviewCount)
                        {
                            ViewBag.PageHeader = "Potential Risk By Candidate";
                        }
                        else if (actionstatus == RibbonActionType.NeedClarity)
                        {
                            ViewBag.PageHeader = "Need Clarity";
                        }
                        else if (actionstatus == RibbonActionType.ReScheduleCount)
                        {
                            ViewBag.PageHeader = "Reschedule Interview";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskInterviewCount)
                        {
                            ViewBag.PageHeader = "Potential Risk By Candidate Interview";
                        }
                        else if (actionstatus == RibbonActionType.SuspeciousInterviewerCount)
                        {
                            ViewBag.PageHeader = "Suspicious Interviewer";
                        }

                        else if (actionstatus == RibbonActionType.CancelledInterview)
                        {
                            ViewBag.PageHeader = "Cancelled Interviwes";
                        }

                        else if (actionstatus == RibbonActionType.RedoInterview)
                        {
                            ViewBag.PageHeader = "Redo Interviwes";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskByClient)
                        {
                            ViewBag.PageHeader = "Potential Risk By Client Interviwes";
                        }
                        else if (actionstatus == RibbonActionType.InterviewerChurn)
                        {
                            ViewBag.PageHeader = "Interviewer Churn Data";
                        }
                        else if (actionstatus == RibbonActionType.PropableRedo)
                        {
                            ViewBag.PageHeader = "Propable Redo Interviews";
                        }
                        else if (actionstatus == RibbonActionType.QCUnderReview)
                        {
                            ViewBag.PageHeader = "Under Review Interviews";
                        }
                        else if (actionstatus == RibbonActionType.QCAIInterview)
                        {
                            ViewBag.PageHeader = "AI Interviews";
                        }
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty; return x; }).ToList();
                        modelList = modelList.OrderBy(x => x.interviewDate).ToList();

                        return View(modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UnassignedInterviews(UnassignedInterviewsFilter filter, string actionstatus, int actiontype, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.emailid = emailid;
                    ViewBag.ProfileName = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer" || profileName == "TechSupport")
                    {
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("UnassignedInterviewsFilters", JsonConvert.SerializeObject(filter));
                        }
                        ViewBag.PageHeader = "";
                        ViewBag.Actiontype = actiontype;
                        ViewBag.actionstatus = actionstatus;
                        if (actionstatus == RibbonActionType.InterviewCount)
                        {
                            ViewBag.PageHeader = "Unassigned Interviews";
                        }
                        else if (actionstatus == RibbonActionType.MeetingStatus)
                        {
                            ViewBag.PageHeader = "Status Pending";
                        }
                        else if (actionstatus == RibbonActionType.QcPending)
                        {
                            ViewBag.PageHeader = "Qc Pending";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskByClient)
                        {
                            ViewBag.PageHeader = "Potential Risk By Client";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskInterviewCount)
                        {
                            ViewBag.PageHeader = "Potential Risk By Candidate";
                        }
                        else if (actionstatus == RibbonActionType.UnAssignedCoordianter && actiontype == 0)
                        {
                            ViewBag.PageHeader = "Unassigned RB Coordinator";
                        }
                        else if (actionstatus == RibbonActionType.UnAssignedCoordianter && actiontype == 1)
                        {
                            ViewBag.PageHeader = "PENDING CANDIDATE FEEDBACK";
                        }
                        else if (actionstatus == RibbonActionType.ZoomLink)
                        {
                            ViewBag.PageHeader = "Missing Zoom Link";
                        }
                        else if (actionstatus == RibbonActionType.UnAttendedZoomLink)
                        {
                            ViewBag.PageHeader = "Participant Waiting";
                        }
                        else if (actionstatus == RibbonActionType.VideoLink)
                        {
                            ViewBag.PageHeader = "Missing Video Link";
                        }
                        else if (actionstatus == RibbonActionType.QCPendingReport || actionstatus == RibbonActionType.QCReport)
                        {
                            ViewBag.PageHeader = "QC Report";
                        }
                        else if (actionstatus == RibbonActionType.ReScheduleCount)
                        {
                            ViewBag.PageHeader = "Reschedule Interview";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskInterviewCount)
                        {
                            ViewBag.PageHeader = "Potential Risk By Candidate Interview";
                        }
                        else if (actionstatus == RibbonActionType.SuspeciousInterviewerCount)
                        {
                            ViewBag.PageHeader = "Suspicious Interviewer";
                        }

                        else if (actionstatus == RibbonActionType.CancelledInterview)
                        {
                            ViewBag.PageHeader = "Cancelled Interviwes";
                        }

                        else if (actionstatus == RibbonActionType.RedoInterview)
                        {
                            ViewBag.PageHeader = "Redo Interviwes";
                        }
                        else if (actionstatus == RibbonActionType.PotentialRiskByClient)
                        {
                            ViewBag.PageHeader = "Potential Risk By Client Interviwes";
                        }
                        else if (actionstatus == RibbonActionType.InterviewerChurn)
                        {
                            ViewBag.PageHeader = "Interviewer Churn Data";
                        }

                        var modelList = new List<MeetingVM>();
                        if (actiontype == 1 && actionstatus == RibbonActionType.UnAssignedCoordianter)
                        {
                            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration, _httpClientFactory);
                            var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                            string email = string.Empty;
                            if (HttpContext.Session.GetString("email") != null)
                            {
                                email = HttpContext.Session.GetString("email")?.Trim();
                                email = email.Replace("\"", "");
                            }
                            var myInterview = _meetingService.GetHRAssignedInterviews(email);
                            var interviewerids = myInterview.Select(x => x.id).Distinct().ToList();
                            if (interviewerids != null)
                            {
                                foreach (var item in myInterview)
                                {
                                    if (result.Where(x => x.interviewid == item.id).Count() == 0)
                                    {
                                        modelList.Add(new MeetingVM
                                        {
                                            IRFId = item.IRFID,
                                            interviewDate = item.interviewDate,
                                            candidateName = item.candidateName,
                                            candidateEmail = item.candidateEmail,
                                            imid = item.id,
                                            interviewer = item.interviewer,
                                            confirmationStatus = "1",
                                            HRCoordinatorEmailId = email,
                                            MeetingStatus = item.MeetingStatus,
                                            InterviewStatus = item.InterviewStatus,
                                        });
                                    }
                                }
                            }
                        }
                        else if (actionstatus == RibbonActionType.PropableRedo || actionstatus == RibbonActionType.QCUnderReview || actionstatus == RibbonActionType.QCAIInterview)
                        {
                            modelList = await _dashboardService.FetchPropableRedoDetails(emailid, profileName, actionstatus);
                        }
                        else
                        {
                            modelList = await _dashboardService.FetchRibbonDetailsValuesbydate(actionstatus, emailid, actiontype, profileName, filter);
                        }
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        ViewBag.actionstatus = actionstatus;
                        ViewBag.Actiontype = actiontype;
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            if (actionstatus == RibbonActionType.UnAttendedZoomLink)
                            {
                                filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                            }
                            modelList = modelList.Where(x => x.interviewDate >= filter.StartDate && x.interviewDate <= filter.EndDate).ToList();
                        }
                        filter.Client = filter.Client == null ? filter.Client = modelList.Select(x => x.Name).ToArray() : filter.Client = filter.Client;
                        filter.ClientHREmail = filter.ClientHREmail == null ? filter.ClientHREmail = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHREmail = filter.ClientHREmail;
                        modelList = modelList.Where(c => (filter.Client.Count() > 0 ? filter.Client.Contains(c.Name) : c.Name == null)
                        && (filter.ClientHREmail.Count() > 0 ? filter.ClientHREmail.Contains(c.ClientHrContact) : c.ClientHrContact == null)).ToList();

                        modelList = modelList.Select(x =>
                        {
                            x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                            return x;
                        }).ToList();
                        modelList = modelList.Select(x =>
                        {
                            x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription();
                            return x;
                        }).ToList();
                        modelList = modelList.Select(x =>
                        {
                            x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                            return x;
                        }).ToList();
                        if (actionstatus == RibbonActionType.InterviewCount)
                        {
                            modelList = modelList.OrderBy(x => x.IRFId).ToList();
                        }
                        else
                        {
                            modelList = modelList.OrderBy(x => x.interviewDate).ToList();
                        }
                        //modelList = modelList.OrderBy(x => x.interviewDate).ToList();
                        return View(modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SensitiveData(string actionstatus, int actiontype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.emailid = emailid;
                    ViewBag.PageFrom = "Completed";
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Interviewer" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "TechSupport" || profileName == "Finance")
                    {
                        if (HttpContext.Session.GetString("SensitiveDataFilters") != null)
                            HttpContext.Session.Remove("SensitiveDataFilters");
                        var modelList = new List<MeetingVM>();
                        if (actionstatus == RibbonActionType.PropableRedo || actionstatus == RibbonActionType.QCUnderReview || actionstatus == RibbonActionType.QCAIInterview || actionstatus == RibbonActionType.QCSelect || actionstatus == RibbonActionType.QCPendingReport)
                        {
                            modelList = await _dashboardService.FetchPropableRedoDetails(emailid, profileName, actionstatus);
                        }
                        else if (actionstatus == RibbonActionType.FakeReport)
                        {
                            modelList = await _dashboardService.FetchMissingParticipantFakeReporting(emailid, profileName, actionstatus, null);
                        }
                        //else
                        //{
                        //    modelList = await _dashboardService.FetchRibbonDetailsValues(actionstatus, emailid, actiontype, profileName);
                        //}
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        ViewBag.PageHeader = "";
                        ViewBag.Actiontype = actiontype;
                        ViewBag.actionstatus = actionstatus;
                        ViewBag.ProfileName = profileName;
                        if (actionstatus == RibbonActionType.FakeReport)
                        {
                            ViewBag.PageHeader = "Possible Fake Reporting";
                        }
                        else if (actionstatus == RibbonActionType.QCPendingReport || actionstatus == RibbonActionType.QCReport)
                        {
                            //ViewBag.PageHeader = "QC Report";
                            ViewBag.PageHeader = "Reject Pending";
                        }
                        else if (actionstatus == RibbonActionType.PropableRedo)
                        {
                            ViewBag.PageHeader = "Propable Redo Interviews";
                        }
                        else if (actionstatus == RibbonActionType.QCUnderReview)
                        {
                            ViewBag.PageHeader = "Under Review Interviews";
                        }
                        else if (actionstatus == RibbonActionType.QCAIInterview)
                        {
                            ViewBag.PageHeader = "AI Interviews";
                        }
                        else if (actionstatus == RibbonActionType.QCSelect)
                        {
                            ViewBag.PageHeader = "Select Pending";
                        }
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty; x.PriorityLevelDesc = x.PriorityLevel > 0 ? ((PriorityLevel)x.PriorityLevel).GetDescription() : string.Empty; return x; }).ToList();
                        modelList = modelList.OrderBy(x => x.interviewDate).ToList();

                        return View(modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SensitiveData(UnassignedInterviewsFilter filter, string actionstatus, int actiontype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.emailid = emailid;
                    ViewBag.ProfileName = profileName;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer" || profileName == "TechSupport")
                    {
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("SensitiveDataFilters", JsonConvert.SerializeObject(filter));
                        }
                        ViewBag.PageHeader = "";
                        ViewBag.Actiontype = actiontype;
                        ViewBag.actionstatus = actionstatus;
                        if (actionstatus == RibbonActionType.FakeReport)
                        {
                            ViewBag.PageHeader = "Possible Fake Reporting";
                        }
                        else if (actionstatus == RibbonActionType.QCPendingReport || actionstatus == RibbonActionType.QCReport)
                        {
                            ViewBag.PageHeader = "QC Report";
                        }
                        else if (actionstatus == RibbonActionType.PropableRedo)
                        {
                            ViewBag.PageHeader = "Propable Redo Interviews";
                        }
                        else if (actionstatus == RibbonActionType.QCUnderReview)
                        {
                            ViewBag.PageHeader = "Under Review Interviews";
                        }
                        else if (actionstatus == RibbonActionType.QCAIInterview)
                        {
                            ViewBag.PageHeader = "AI Interviews";
                        }
                        else if (actionstatus == RibbonActionType.QCSelect)
                        {
                            ViewBag.PageHeader = "Select Pending";
                        }
                        var modelList = new List<MeetingVM>();
                        if (actionstatus == RibbonActionType.PropableRedo || actionstatus == RibbonActionType.QCUnderReview || actionstatus == RibbonActionType.QCAIInterview || actionstatus == RibbonActionType.QCSelect)
                        {
                            modelList = await _dashboardService.FetchPropableRedoDetails(emailid, profileName, actionstatus);
                        }
                        else if (actionstatus == RibbonActionType.FakeReport)
                        {
                            modelList = await _dashboardService.FetchMissingParticipantFakeReporting(emailid, profileName, actionstatus, filter);
                        }
                        else
                        {
                            modelList = await _dashboardService.FetchRibbonDetailsValuesbydate(actionstatus, emailid, actiontype, profileName, filter);
                        }
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        ViewBag.actionstatus = actionstatus;
                        ViewBag.Actiontype = actiontype;
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            if (actionstatus == RibbonActionType.UnAttendedZoomLink)
                            {
                                filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                            }
                            modelList = modelList.Where(x => x.interviewDate >= filter.StartDate && x.interviewDate <= filter.EndDate).ToList();
                        }
                        filter.Client = filter.Client == null ? filter.Client = modelList.Select(x => x.Name).ToArray() : filter.Client = filter.Client;
                        filter.ClientHREmail = filter.ClientHREmail == null ? filter.ClientHREmail = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHREmail = filter.ClientHREmail;
                        modelList = modelList.Where(c => (filter.Client.Count() > 0 ? filter.Client.Contains(c.Name) : c.Name == null)
                        && (filter.ClientHREmail.Count() > 0 ? filter.ClientHREmail.Contains(c.ClientHrContact) : c.ClientHrContact == null)).ToList();

                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty; x.PriorityLevelDesc = x.PriorityLevel > 0 ? ((PriorityLevel)x.PriorityLevel).GetDescription() : string.Empty; return x; }).ToList();
                        modelList = modelList.OrderBy(x => x.interviewDate).ToList();
                        return View(modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// List of all unassigned interviews clubbed together by Skill Channel, with action to send mail notification
        /// </summary>
        /// <param name="actiontype"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SendOpenInterviewsNotification(int actiontype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement")
                    {
                        var modelList = new List<MeetingVM>();
                        modelList = await _dashboardService.FetchRibbonDetailsValues(RibbonActionType.InterviewCount, emailid, actiontype, profileName);
                        return View(modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("UnassignedInterviews");
            }

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetUnassignedInterviewsFilter(string actionstatus, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("UnassignedInterviewsFilters");
                    return RedirectToAction("UnassignedInterviews", new { actionstatus = actionstatus, returnBack = returnBack });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("UnassignedInterviews");
            }

        }

        public async Task<IActionResult> ResetSensitiveDataFilter(string actionstatus, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("SensitiveDataFilters");
                    return RedirectToAction("SensitiveData", new { actionstatus = actionstatus, returnBack = returnBack });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SensitiveData");
            }

        }

        [HttpGet]
        public async Task<JsonResult> FetchRibbonValues(int section, bool getDeletedZoomMeetingsCount = false)
        {
            if (section == 1)
            {
                var modelData = new List<SurveyVM>();
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration, _httpClientFactory);
                var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                string email = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    email = HttpContext.Session.GetString("email")?.Trim();
                    email = email.Replace("\"", "");
                }
                var Ribbonmodel = await _dashboardService.GetNotUpdatedMeetingsCountbyHr(email, getDeletedZoomMeetingsCount);
                var modelList = await _dashboardService.FetchParticipantwaitingDetails(email);
                Ribbonmodel.CandidatewaitingCount = modelList.Where(x => x.PerformedOn != null).Count();
                var myInterview = _meetingService.GetHRAssignedInterviews(email);
                var WeekDate = DateTime.Now.AddDays(-7);
                var interviewerids = myInterview.Where(x => x.interviewDate >= WeekDate).Select(x => x.id).Distinct().ToList();
                int countvalue = 0;
                if (interviewerids != null)
                {
                    foreach (var item in interviewerids)
                    {
                        if (result.Where(x => x.interviewid == item).Count() == 0)
                        {
                            countvalue++;
                        }
                    }
                }
                Ribbonmodel.UnfilledCommentsCount = countvalue;
                return Json(Ribbonmodel);
            }
            else
            {
                var Emailid = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                }
                var Ribbonmodel = await _dashboardService.GetNotUpdatedMeetingsCount(Emailid, getDeletedZoomMeetingsCount);
                var modelList = await _dashboardService.FetchParticipantwaitingDetails(Emailid);
                Ribbonmodel.CandidatewaitingCount = modelList.Where(x => x.PerformedOn != null).Count();
                return Json(Ribbonmodel);
            }
        }

        [HttpPost]
        public JsonResult SendNotoificationForParticipantWaiting()
        {
            //Send mail only one time no duplicate mail
            return Json("Success");
        }

        [HttpPost]
        public async Task<JsonResult> FetchZoomMeetingHistory(string meetingid)
        {
            var modelList = await _dashboardService.FetchZoomMeetingHistory(meetingid);
            return Json(modelList);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeletedZoomMeetings(int actionType, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    var model = await _dashboardService.GetUpcomingMeetingsDeletedFromZoom(emailId);
                    ViewBag.Actiontype = actionType;
                    ViewBag.PageHeader = "";
                    ViewBag.PageHeader = "Deleted Zoom Meetings";
                    ViewBag.ReturnBack = returnBack;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }
        #region Interviewer Dashboard
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerIndex()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var model = new InterviewerDashboardVM();
                        model = await _dashboardService.GetInterviewDetailsCount(emailid);
                        //var codedname = await _meetingService.FetchInterviewerCodedName(emailid);
                        if (HttpContext.Session.GetString("InterviewerCodedName") != null)
                        {
                            model.CodedName = HttpContext.Session.GetString("InterviewerCodedName")?.Trim().Replace("\"", "");
                        }
                        //var isCodedResumePresent = _meetingService.CheckInterviewerCodedResume(emailid);
                        ViewBag.isCodedResumePresent = false;
                        ViewBag.TimeExpire = _configuration["InterviewerReport:ExpireTime"];
                        ViewBag.isFirstTimeLogin = model.FirstTimePopupDisplayed;
                        ViewBag.PaymentSLAConfirmation = model.PaymentSLAConfirmation;
                        ViewBag.InterviewerRedMarkerReport = model.RedmarkerAcknowledge;
                        ViewBag.VideoKYC = model.VideoKYC;
                        if (model.VideoConfirmation == 0)
                        {
                            ViewBag.VideoConfirmationPopup = true;
                        }
                        else
                        {
                            ViewBag.VideoConfirmationPopup = false;
                        }
                        if (model.AcknowledgementStatus.ToUpper() != "YES" && model.AcknowledgementStatus.ToUpper() != "NO")
                        {
                            return RedirectToAction("InterviewersOnboarding", "Interviewer");
                        }
                        //Boradcastmessage
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> InterviewerCandidateRatingDetails()
        {
            var candidaterating = 0.0;
            if (HttpContext.Session.GetString("email") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                int interviewerid = 0;
                var Candidatemodel = new List<InterviewerRating>();
                Candidatemodel = await _interviewerService.fetchClientFeedbackbyInterviewer(emailid, "Candidate");
                if (Candidatemodel.Count() != 0)
                {
                    candidaterating = Math.Round(Candidatemodel.Select(x => x.Question2).Average(), 2);
                }
                return Json(new { candidateRating = candidaterating });
            }
            else
            {
                return Json(new { candidateRating = candidaterating });
            }
        }
        [Authorize]
        [HttpPost]
        public async Task<JsonResult> InterviewerClientRatingDetails()
        {
            var clientrating = 0.0;
            if (HttpContext.Session.GetString("email") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var Clientmodel = new List<InterviewerRating>();
                var results = await _interviewerService.fetchClientFeedbackbyInterviewer(emailid, "Client");
                if (results != null)
                    Clientmodel = results;

                if (Clientmodel.Count() != 0)
                {
                    clientrating = Math.Round(Clientmodel.Select(x => x.Rating).Average(), 2);
                }
                return Json(new { clientRating = clientrating });
            }
            else
            {
                return Json(new { clientRating = clientrating });
            }
        }
        [Authorize]
        [HttpPost]
        public async Task<JsonResult> InterviewerPendingPaymentDetails()
        {
            var Payment = 0;
            var PaidPayment = 0;
            if (HttpContext.Session.GetString("email") != null)
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var model = await _meetingService.RBDetails(emailid);
                model = model.Select(x => { x.InterviewStatusDetails = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.MeetingStatusDetails = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; }).ToList();
                Payment = model.Where(x => x.Status == "Submitted" || x.Status == "Pending").Sum(x => x.Points) - model.Where(x => x.Status == "Advance").Sum(x => x.Points);
                PaidPayment = model.Where(x => x.Status == "Paid").Sum(x => x.Points);
                return Json(new { payment = Payment, paidpayment = PaidPayment });
            }
            else
            {
                return Json(new { payment = Payment, paidpayment = PaidPayment });
            }
        }
        #endregion
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UnProcessedResume(int actiontype, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer" || profileName == "TechSupport")
                    {
                        var modelList = new List<MeetingVM>();
                        modelList = await _dashboardService.FetchRibbonDetailsValues("UnProcessedResume", emailid, actiontype, profileName);
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        ViewBag.PageHeader = "";
                        ViewBag.Actiontype = actiontype;
                        ViewBag.PageHeader = "Unprocessed Resume";
                        //modelList = modelList.OrderBy(x => x.interviewDate).ToList();
                        return View(modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> UnassignedInterviewsExportToExcel(string actionstatus, int actiontype)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("QC Report Details");
            string fileName = "Risebird_" + DateTime.Now.ToString("dd/MM/yyyy") + "_QCReport" + ".xlsx";
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        var modelList = new List<MeetingVM>();
                        var filter = new UnassignedInterviewsFilter();
                        if (HttpContext.Session.GetString("UnassignedInterviewsFilters") != null)
                        {
                            filter = JsonConvert.DeserializeObject<UnassignedInterviewsFilter>(HttpContext.Session.GetString("UnassignedInterviewsFilters"));
                            modelList = await _dashboardService.FetchRibbonDetailsValuesbydate(actionstatus, emailid, actiontype, profileName, filter);
                        }
                        else
                        {
                            modelList = await _dashboardService.FetchRibbonDetailsValues(actionstatus, emailid, actiontype, profileName);
                        }
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        if (filter != null)
                        {
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                if (actionstatus == "UnAttendedZoomLink")
                                {
                                    filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                                }
                                modelList = modelList.Where(x => x.interviewDate >= filter.StartDate && x.interviewDate <= filter.EndDate).ToList();
                            }
                            filter.Client = filter.Client == null ? filter.Client = modelList.Select(x => x.Name).ToArray() : filter.Client = filter.Client;
                            filter.ClientHREmail = filter.ClientHREmail == null ? filter.ClientHREmail = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHREmail = filter.ClientHREmail;
                            modelList = modelList.Where(c => (filter.Client.Count() > 0 ? filter.Client.Contains(c.Name) : c.Name == null)
                            && (filter.ClientHREmail.Count() > 0 ? filter.ClientHREmail.Contains(c.ClientHrContact) : c.ClientHrContact == null)).ToList();
                        }
                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty; return x; }).ToList();
                        modelList = modelList.OrderBy(x => x.interviewDate).ToList();
                        if (modelList != null && modelList.Count() > 0)
                        {
                            Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                            Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                            Sheet.Cells["A1:AI1"].Style.WrapText = true;
                            int startColumn = 1; int endColumn = 12;
                            for (int a = startColumn; a <= endColumn; a++)
                            {
                                Sheet.Column(a).Width = 40;
                            }
                            Sheet.Cells[1, 1].Value = "Skill Channel";
                            Sheet.Cells[1, 2].Value = "Interview Date";
                            Sheet.Cells[1, 3].Value = "Candidate Name";
                            Sheet.Cells[1, 4].Value = "Candidate Email";
                            Sheet.Cells[1, 5].Value = "Interviewer";
                            Sheet.Cells[1, 6].Value = "Meeting Status";
                            Sheet.Cells[1, 7].Value = "Interview Status";
                            Sheet.Cells[1, 8].Value = "Client";
                            if (actionstatus == "QCReport")
                            {
                                Sheet.Cells[1, 9].Value = "Red Flags";
                            }
                            int i = 2;
                            foreach (var item in modelList)
                            {
                                Sheet.Cells[i, 1].Value = item.IRFId;
                                Sheet.Cells[i, 2].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm");
                                Sheet.Cells[i, 3].Value = item.candidateName;
                                Sheet.Cells[i, 4].Value = item.candidateEmail;
                                Sheet.Cells[i, 5].Value = item.interviewer;
                                Sheet.Cells[i, 6].Value = item.MeetingStatusStatusDesc;
                                Sheet.Cells[i, 7].Value = item.intervieweStatusStatDesc;
                                Sheet.Cells[i, 8].Value = item.Name;
                                if (actionstatus == "QCReport")
                                {
                                    Sheet.Cells[i, 9].Value = item.Redflags;
                                }
                                i++;
                            }
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                        else
                        {
                            ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                            Rng.Value = "No data available in table !!!";
                            return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                        }
                    }
                    else
                    {
                        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                        Rng.Value = "No data available in table !!!";
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "No data available in table !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
        [Authorize]
        [HttpPost]
        public async Task<JsonResult> FetchAlertMessage()
        {
            var modelList = await _dashboardService.FetchAlertMessage();
            for (int i = 0; i < modelList.Count; i++)
            {
                modelList[i] = System.Web.HttpUtility.HtmlDecode(modelList[i]);
            }
            return Json(modelList);
        }

        [HttpPost]
        [Authorize]
        public async Task<JsonResult> SaveWhatsappNumber(string whatsappnumber)
        {

            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {

                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");

                //var result = _userService.GetWhatsappno(model.EmailId);
                _ = await _userService.UpdateWhatsappno(emailid, whatsappnumber);
                return Json("Whatsapp added Successfully.");
            }
            else
            {

                return Json("Failed to update Whatsapp number!");
            }

        }
        [Authorize]
        [HttpPost]
        public async Task<JsonResult> FetchClientIRFFeedbackList()
        {
            var results = await _interviewerService.FetchClientIRFFeedbackList();
            if (results == null)
            {
                var Clientmodel = new List<ClientIRFsFeedbackVM>();
                return Json(Clientmodel);
            }
            return Json(results);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Dashboard911()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "TechSupport")
                    {
                        var model = new DashboardVM();
                        model.NotUpdatedMeetingsObj = new NotUpdatedMeetings();
                        //model.InterviewerCountObj = await _dashboardService.GetInterviewerCount();
                        model.NotUpdatedMeetingsObj = await _dashboardService.GetNotUpdatedMeetingsCount(Emailid);
                        var modelList = await _dashboardService.FetchParticipantwaitingDetails(Emailid);
                        model.NotUpdatedMeetingsObj.CandidatewaitingCount = modelList.Where(x => x.PerformedOn != null).Count();
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> BlackListCompany(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GetBlackkListComapany();

                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public async Task<JsonResult> UndoBlackListCompany(string CompanyName)
        {
            try
            {

                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                BlackListCompanyVM model = new BlackListCompanyVM();
                model.CompanyName = CompanyName;
                model.ModifyDate = DateTime.Now;
                model.ModifyBy = emailId;
                var data = await _dashboardService.UndoBlackListCompanyService(model);
                if (data == true)
                {
                    return Json("Undo blacklisted company marked Successfully");
                }
                else
                {
                    return Json("Failed to mark whether undo blacklisted company.");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<JsonResult> ApprovedBlackListCompany(string CompanyName)
        {
            try
            {

                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                BlackListCompanyVM model = new BlackListCompanyVM();
                model.CompanyName = CompanyName;
                model.ModifyDate = DateTime.Now;
                model.ModifyBy = emailId;
                var data = await _dashboardService.ApprovedBlackListCompanyService(model);
                if (data == true)
                {
                    return Json("Approved blacklisted company has been marked successfully");
                }
                else
                {
                    return Json("Failed to mark whether approved blacklisted company.");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRedFlagsQCReport(int Interviewid)
        {
            try
            {
                var data = await _dashboardService.GetRedFlagsQCReportService(Interviewid);
                return PartialView("_GetRedFlagsQCReportPartial", data);
            }
            catch (Exception ex)
            {
                var data = new QCFormVM();
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return PartialView("_GetRedFlagsQCReportPartial", data);
            }
        }

        [HttpPost]
        public async Task<JsonResult> UndoSuspiciousInterviewer(int UserId)
        {
            try
            {

                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                // int interviewerProfileid = InterviewerProfileId;
                if (UserId <= 0)
                    return Json("Failed to mark whether undo suspicious interviewer.");
                var data = await _dashboardService.UndoSuspiciousInterviewerService(UserId, emailId);
                if (data == true)
                {
                    return Json("Undo suspicious interviewer marked Successfully");
                }
                else
                {
                    return Json("Failed to mark whether undo suspicious interviewer.");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpPost]
        public async Task<JsonResult> ApprovedRedoInterviews(int InterviewId)
        {
            try
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                var modifyDate = DateTime.Now;
                var modifyBy = emailId;
                var data = await _dashboardService.ApprovedRedoInterviewsService(InterviewId, modifyBy, modifyDate);
                if (data == true)
                {
                    return Json("Approved Redo Interview has been marked successfully");
                }
                else
                {
                    return Json("Failed to mark whether approved Redo Interview.");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RedoRequest(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GetRedoRequestService(emailid);

                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> HighRiskInterviewers(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GetHighRiskInterviewersService();

                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> MoveToUnassign(int meetingId)
        {
            try
            {
                var data = await _dashboardService.MoveToUnassignService(meetingId);
                if (data == true)
                {
                    return Json("Move to Unassign Successfully");
                }

                else
                {
                    return Json("Fail to marked Move To Unassign, Try again after sometime.");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<JsonResult> AddRedoInterviews(int InterviewId)
        {
            try
            {
                string Type = "Candidate";
                var data = await _dashboardService.AddRedoInterviewsService(InterviewId, Type);
                if (data == true)
                {
                    return Json(" Redo Interview has been marked successfully");
                }
                else
                {
                    return Json("Failed to mark whether  Redo Interview.");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PotentialRiskByCandidate(int returnBack, string tab = PotentialRiskTabs.All)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GetPotentialRiskByCandidateService(emailid) ?? Enumerable.Empty<MeetingVM>();
                        foreach (var item in model)
                        {
                            if (item.InterviewStatus.HasValue)
                            {
                                item.intervieweStatusStatDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                        }
                        // Status tabs (All · Pending · Under Review · Reviewed · Removed): counts over the whole list,
                        // then the list narrowed to the tab. An unknown tab falls back to All.
                        var all = model.ToList();
                        var activeTab = PotentialRiskTabs.Normalize(tab);
                        ViewBag.PrTab = activeTab;
                        ViewBag.PrTabCounts = PotentialRiskTabs.Counts(all);
                        return View(PotentialRiskTabs.Filter(all, activeTab).ToList());
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> ExportPotentialRiskByCandidate(string tab = PotentialRiskTabs.All)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Potential Risk By Candidate");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Potential Risk By Candidate\r\n" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                // Same tab as the page (All when missing or unknown).
                var modelList = PotentialRiskTabs.Filter(
                    await _dashboardService.GetPotentialRiskByCandidateService(emailid), tab).ToList();
                foreach (var item in modelList)
                {

                    if (item.InterviewStatus.HasValue)
                    {
                        item.intervieweStatusStatDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                    }
                }
                Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                Sheet.Cells["A1:AI1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 12;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 30;
                }
                Sheet.Cells[1, 1].Value = "Skill Channel ID";
                Sheet.Cells[1, 2].Value = "Interview Date";
                Sheet.Cells[1, 3].Value = "Interviewer";
                Sheet.Cells[1, 4].Value = "Candidate Name";
                Sheet.Cells[1, 5].Value = "Candidate Email";
                Sheet.Cells[1, 6].Value = "Candidate Contact";
                Sheet.Cells[1, 7].Value = "Interview Status";
                Sheet.Cells[1, 8].Value = "Client Name";
                Sheet.Cells[1, 9].Value = "Channel Owner";
                Sheet.Cells[1, 10].Value = "Red Marker";
                Sheet.Cells[1, 11].Value = "Rating";
                Sheet.Cells[1, 12].Value = "Rated On";
                Sheet.Cells[1, 13].Value = "Payment SLA Status";
                Sheet.Cells[1, 14].Value = "QC status";
                Sheet.Column(13).Width = 30;
                Sheet.Column(14).Width = 30;

                int i = 2;
                if (modelList.Count() > 0)
                {
                    foreach (var item in modelList)
                    {
                        Sheet.Cells[i, 1].Value = item.IRFId;
                        Sheet.Cells[i, 2].Value = item.interviewDate.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 3].Value = item.interviewer;
                        Sheet.Cells[i, 4].Value = item.candidateName;
                        Sheet.Cells[i, 5].Value = item.candidateEmail;

                        Sheet.Cells[i, 6].Value = item.MobileNumber;
                        Sheet.Cells[i, 7].Value = item.intervieweStatusStatDesc;
                        Sheet.Cells[i, 8].Value = item.Name;
                        Sheet.Cells[i, 9].Value = item.ChannelOwner;
                        Sheet.Cells[i, 10].Value = item.RedMarker;
                        Sheet.Cells[i, 11].Value = item.Rating;
                        Sheet.Cells[i, 12].Value = item.RatingAddedOn;
                        if (item.IsApproved == 1)
                        {
                            Sheet.Cells[i, 13].Value = "Approved";
                        }
                        else if (item.IsApproved == 2)
                        {
                            Sheet.Cells[i, 13].Value = "Not Approved";
                        }
                        // QC status of the interviewer (Pending / Under Review / Reviewed / Removed), plus who and when.
                        Sheet.Cells[i, 14].Value = PotentialRiskTabs.StatusText(item)
                            + (item.PotentialRiskStatus.HasValue && !string.IsNullOrWhiteSpace(item.PotentialRiskStatusBy)
                                ? " - " + item.PotentialRiskStatusBy + (item.PotentialRiskStatusOn.HasValue ? " on " + item.PotentialRiskStatusOn.Value.ToString("dd MMM yyyy HH:mm") : "")
                                : "");
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Participantwaiting(string type, int returnBack)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                ViewBag.ReturnBack = returnBack;
                var modelList = await _dashboardService.FetchParticipantwaitingDetails(emailid);
                if (type == "Interviewer")
                {
                    ViewBag.Type = "Interviewer";
                    modelList = modelList.Where(x => x.Joinedon != null).ToList();
                }
                else
                {
                    ViewBag.Type = "Candidate";
                    modelList = modelList.Where(x => x.PerformedOn != null).ToList();
                }
                return View(modelList);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerVideoKYC(int returnBack, string email = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    ViewBag.FilterEmail = email;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GeInterviewerVideoKYC();

                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public async Task<JsonResult> ApprovedInterviewerVideoKYC(int id, string type)
        {
            try
            {

                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                var data = await _dashboardService.UpdateInterviewerVideoKYC(id, type);
                if (type == "Approve")
                {
                    if (data == true)
                    {
                        return Json("Approved Video KYC has been marked successfully");
                    }
                    else
                    {
                        return Json("Failed to mark whether approved Video KYC.");
                    }
                }
                else
                {
                    if (data == true)
                    {
                        return Json("Re-Record has been marked successfully");
                    }
                    else
                    {
                        return Json("Failed to mark whether Re-Record.");
                    }
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ProxyReport(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GetProxyReport();
                        model = model.Select(x =>
                        {
                            x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                            return x;
                        }).ToList();
                        foreach (var item in model)
                        {

                            if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                            {
                                if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                                {
                                    item.MeetingStatusStatusDesc = "Status Pending";
                                }
                            }
                            else
                            {
                                item.MeetingStatusStatusDesc = string.Empty;

                            }
                        }

                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetProxyReportCountDetails(int id)
        {
            try
            {
                var model = await _dashboardService.GetProxyReportCountDetails(id);

                return PartialView(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IActionResult> ProctoringReport(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GetProctoringSessions();
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpGet]
        public async Task<IActionResult> ProctoringSessionReport(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = new ProctoringSessionReportDetailVM
                        {
                            Session = await _dashboardService.GetProctoringSessionById(id),
                            Logs = await _dashboardService.GetProctoringSessionLogs(id),
                            Screenshots = await _dashboardService.GetProctoringSessionScreenshots(id)
                        };
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        #region Missed Candidate
        [HttpGet]
        public async Task<IActionResult> InterviewManagementDetails(string type, string pagetype, int returnBack)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var Emailid = HttpContext.Session.GetString("email")?.Trim();
                Emailid = Emailid.Replace("\"", "");
                ViewBag.ReturnBack = returnBack;
                DateTime startdate;
                DateTime enddate;
                startdate = DateTime.Now.Date.AddDays(-7);
                enddate = DateTime.Now.Date.AddDays(1);
                if (HttpContext.Session.GetString("InterviewManagementDetailsfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<MyInterviewFilter>(HttpContext.Session.GetString("InterviewManagementDetailsfilters"));
                    startdate = Convert.ToDateTime(filter.StartDate);
                    enddate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                    ViewBag.ReturnBack = filter.HdnReturnBack;
                }
                var interviewmanagement = await _dashboardService.GetInterviewManagementDetails(startdate, enddate, Emailid);
                interviewmanagement = interviewmanagement.Where(x => (x.IsRescheduleNotificationSent == 1 && x.MeetingStatus != 3 && x.MeetingStatus != 4 && x.MeetingStatus != 6 && x.MeetingStatus != 7 && x.MeetingStatus != 8) || (x.PerformedOn != null && (x.MeetingStatus == 1 || x.MeetingStatus == 5 || x.MeetingStatus == 9))).ToList();
                interviewmanagement = interviewmanagement.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); ; x.LastMeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.LastMeetingStatus)).GetDescription(); return x; }).ToList();
                return View(interviewmanagement);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public async Task<IActionResult> InterviewManagementDetails(MyInterviewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("InterviewManagementDetailsfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("InterviewManagementDetails");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpGet]
        public async Task<IActionResult> ResetInterviewManagementDetailsfilters(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("InterviewManagementDetailsfilters");
                    return RedirectToAction("InterviewManagementDetails", new { returnBack = returnBack });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("InterviewManagementDetails", new { returnBack = returnBack });
            }
        }
        #endregion
        #region Interview Management
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> EmergencyInterviewDetails(string interviewdate, string type, string pagetype)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                ViewBag.Pagetype = pagetype;
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var Emailid = HttpContext.Session.GetString("email")?.Trim();
                Emailid = Emailid.Replace("\"", "");
                DateTime startdate;
                DateTime enddate;
                if (string.IsNullOrEmpty(interviewdate))
                {
                    startdate = DateTime.Now.Date;
                }
                else
                {
                    startdate = Convert.ToDateTime(interviewdate);
                }
                enddate = startdate.AddDays(1);
                ViewBag.interviewdate = interviewdate;
                if (HttpContext.Session.GetString("EmergencyInterviewDetailsfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<MyInterviewFilter>(HttpContext.Session.GetString("EmergencyInterviewDetailsfilters"));
                    startdate = Convert.ToDateTime(filter.StartDate);
                    enddate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                    //filter.ClientName = filter.ClientName == null ? filter.ClientName = modelList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                    //interviewmanagement = interviewmanagement.Where(c => filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null).ToList();
                }
                var interviewmanagement = await _dashboardService.GetInterviewManagementDetails(startdate, enddate, Emailid);
                if (type == "InterviewM")
                {
                    ViewBag.Type = "InterviewM";
                    interviewmanagement = interviewmanagement.Where(x => x.IsMeetingNotificationSent == 1 && x.PerformedOn != null).ToList();
                }
                else if (type == "OnTimeIM")
                {
                    ViewBag.Type = "OnTimeIM";
                    interviewmanagement = interviewmanagement.Where(x => x.IsMeetingNotificationSent == 0 && x.PerformedOn != null && x.Joinedon != null).ToList();
                }
                else
                {
                    ViewBag.Type = "RescheduleIM";
                    interviewmanagement = interviewmanagement.Where(x => (x.IsRescheduleNotificationSent == 1 && x.MeetingStatus != 3 && x.MeetingStatus != 4 && x.MeetingStatus != 6 && x.MeetingStatus != 7 && x.MeetingStatus != 8) || (x.PerformedOn != null && (x.MeetingStatus == 1 || x.MeetingStatus == 5 || x.MeetingStatus == 9))).ToList();
                    interviewmanagement = interviewmanagement.Select(x => { x.LastMeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.LastMeetingStatus)).GetDescription(); return x; }).ToList();

                }
                interviewmanagement = interviewmanagement.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; })?.OrderByDescending(x => x.interviewDate).ToList();
                return View(interviewmanagement);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> EmergencyInterviewDetails(MyInterviewFilter filter, string type, string pagetype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("EmergencyInterviewDetailsfilters", JsonConvert.SerializeObject(filter));
                    }
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    return RedirectToAction("EmergencyInterviewDetails", new { interviewdate = DateTime.Now.Date.ToString(), type = type, pagetype = pagetype });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ResetEmergencyInterviewDetailsfilters(string pagetype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("EmergencyInterviewDetailsfilters");
                    return RedirectToAction("EmergencyInterviewDetails", new { interviewdate = DateTime.Now.Date.ToString(), pagetype = pagetype });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("EmergencyInterviewDetails", new { interviewdate = DateTime.Now.Date.ToString() });
            }
        }
        #endregion
        #region Bounce & Dropped Mail
        // Who may open the page and use its row actions. "Unreachable" is not offered to Client users.
        private static readonly string[] BounceDroppedProfiles = { "Admin", "HR", "SeniorHR", "SupportAdmin", "Client" };
        private static readonly string[] BounceDroppedRisebirdProfiles = { "Admin", "HR", "SeniorHR", "SupportAdmin" };
        private const string BounceNoAccessMessage = "Candidate not found or you do not have access.";
        private const string BounceDomainMessage = "Email domain is not allowed for a candidate.";
        private const string BounceUpdateFailedMessage = "Could not update email. Please try again.";
        private static readonly TimeSpan BounceResendCooldown = TimeSpan.FromMinutes(10);

        /// <summary>Session email + profile, or nulls when not signed in.</summary>
        private (string Email, string Profile) GetBounceDroppedSessionUser()
        {
            var email = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            var profile = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(profile))
            {
                return (null, null);
            }
            return (email, profile);
        }

        private MailActivityFilter GetBounceDroppedSessionFilter()
        {
            var json = HttpContext.Session.GetString("MailActivityFilters");
            return string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<MailActivityFilter>(json);
        }

        private static string[] NullIfEmpty(string[] values)
        {
            var cleaned = values?.Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
            return cleaned == null || cleaned.Length == 0 ? null : cleaned;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetEmailRecipientsBounceAndDropped(string recipientType, int returnBack, string pagetype)
        {
            try
            {
                var (emailid, profileName) = GetBounceDroppedSessionUser();
                if (emailid == null || !BounceDroppedProfiles.Contains(profileName))
                {
                    return RedirectToAction("SignOut", "Session");
                }
                ViewBag.Profile = profileName;
                ViewBag.UserProfile = profileName;
                ViewBag.ReturnBack = returnBack;
                ViewBag.pagetype = pagetype;

                var filter = GetBounceDroppedSessionFilter();
                if (filter != null && string.IsNullOrEmpty(recipientType))
                {
                    recipientType = filter.RecipientType;
                    ViewBag.ReturnBack = filter.returnBack;
                }
                else
                {
                    // Opening a tab (recipientType in the query) starts from the default view.
                    HttpContext.Session.Remove("MailActivityFilters");
                    filter = new MailActivityFilter { RangeDays = BounceDroppedDateRange.DefaultDays };
                }
                if (string.IsNullOrEmpty(recipientType))
                {
                    recipientType = "Candidate";
                }

                var today = DateTime.Today;
                var range = BounceDroppedDateRange.Resolve(filter.RangeDays, filter.StartDate, filter.EndDate, today);
                var settings = new BounceDroppedPageSettingsVM
                {
                    RecipientType = recipientType,
                    StartDate = range.Start,
                    EndDate = range.End,
                    RangeDays = range.RangeDays,
                    MinDate = BounceDroppedDateRange.EarliestStart(today),
                    MaxDate = today,
                    RangeLabel = BounceDroppedDateRange.Label(range.Start, range.End),
                    Notice = range.Notice,
                    IncludeResolved = filter.IncludeResolved,
                    SelectedSkillChannels = filter.SkillChannel ?? new string[0],
                    SelectedSentBy = filter.SentBy ?? new string[0],
                    SelectedRecipients = filter.Recipient ?? new string[0],
                    SelectedActivities = filter.Activity ?? new string[0],
                    SelectedBounceReasons = filter.BounceReason ?? new string[0],
                };

                // Filter options come from the whole date range, not from the rows on screen.
                var options = await _dashboardService.GetBounceDroppedFilterOptions(recipientType, range.Start, range.End, emailid, filter.IncludeResolved)
                              ?? Enumerable.Empty<BounceDroppedFilterOption>();
                List<string> OptionsFor(string name) => options.Where(o => o.FilterName == name).Select(o => o.FilterValue).Distinct().ToList();
                settings.SkillChannelOptions = OptionsFor("SkillChannel");
                settings.SentByOptions = OptionsFor("SentBy");
                settings.RecipientOptions = OptionsFor("Recipient");
                settings.ActivityOptions = OptionsFor("Activity");
                settings.BounceReasonOptions = OptionsFor("BounceReason");

                ViewBag.recipientType = recipientType;
                return View(settings);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GetEmailRecipientsBounceAndDropped(MailActivityFilter filter, string recipientType, int returnBack, string pagetype)
        {
            try
            {
                if (filter != null)
                {
                    filter.RecipientType = recipientType;
                    filter.returnBack = returnBack;
                    // Store the resolved window so a preset keeps meaning "ending today" and custom dates are clamped.
                    var range = BounceDroppedDateRange.Resolve(filter.RangeDays, filter.StartDate, filter.EndDate, DateTime.Today);
                    filter.RangeDays = range.RangeDays;
                    if (range.RangeDays == null)
                    {
                        filter.StartDate = range.Start;
                        filter.EndDate = range.End;
                        if (range.Notice != null)
                        {
                            TempData["BounceDroppedNotice"] = range.Notice;
                        }
                    }
                    else
                    {
                        filter.StartDate = null;
                        filter.EndDate = null;
                    }
                    filter.SkillChannel = NullIfEmpty(filter.SkillChannel);
                    filter.SentBy = NullIfEmpty(filter.SentBy);
                    filter.Recipient = NullIfEmpty(filter.Recipient);
                    filter.Activity = NullIfEmpty(filter.Activity);
                    filter.BounceReason = NullIfEmpty(filter.BounceReason);
                    HttpContext.Session.SetString("MailActivityFilters", JsonConvert.SerializeObject(filter));
                }
                return RedirectToAction("GetEmailRecipientsBounceAndDropped", new { pagetype = pagetype });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetRecipientActivityDetails(int returnBack, string pagetype, string recipientType)
        {
            try
            {
                HttpContext.Session.Remove("MailActivityFilters");
                return RedirectToAction("GetEmailRecipientsBounceAndDropped", new { recipientType = string.IsNullOrEmpty(recipientType) ? "Candidate" : recipientType, returnBack = returnBack, pagetype = pagetype });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        /// <summary>Server-side paging endpoint of the Bounce &amp; Dropped Mail DataTable.</summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetEmailRecipientsBounceAndDroppedData(string recipientType)
        {
            int.TryParse(Request.Form["draw"], out var draw);
            try
            {
                var (emailid, profileName) = GetBounceDroppedSessionUser();
                if (emailid == null || !BounceDroppedProfiles.Contains(profileName))
                {
                    return Unauthorized();
                }
                if (recipientType != "Interviewer") recipientType = "Candidate";

                int.TryParse(Request.Form["start"], out var offset);
                int.TryParse(Request.Form["length"], out var pageSize);
                if (pageSize <= 0) pageSize = 10;
                if (pageSize > 100) pageSize = 100;
                if (offset < 0) offset = 0;
                string sortColumn = null;
                if (int.TryParse(Request.Form["order[0][column]"], out var sortIndex))
                {
                    sortColumn = Request.Form[$"columns[{sortIndex}][name]"];
                }
                string sortDir = Request.Form["order[0][dir]"];

                var filter = GetBounceDroppedSessionFilter();
                if (filter != null && filter.RecipientType != recipientType)
                {
                    filter = null;
                }
                filter ??= new MailActivityFilter { RangeDays = BounceDroppedDateRange.DefaultDays };
                var range = BounceDroppedDateRange.Resolve(filter.RangeDays, filter.StartDate, filter.EndDate, DateTime.Today);

                var page = await _dashboardService.GetBounceDroppedPage(new BounceDroppedQuery
                {
                    RecipientType = recipientType,
                    StartDate = range.Start,
                    EndDate = range.End,
                    EmailId = emailid,
                    IncludeResolved = filter.IncludeResolved,
                    SkillChannels = filter.SkillChannel,
                    SentBy = filter.SentBy,
                    Recipients = filter.Recipient,
                    Activities = filter.Activity,
                    BounceReasons = filter.BounceReason,
                    Search = Request.Form["search[value]"],
                    SortColumn = sortColumn,
                    SortDir = sortDir,
                    Offset = offset,
                    PageSize = pageSize,
                }) ?? new BounceDroppedPageVM();

                var data = page.Rows.Select(r => new
                {
                    sendgridEventId = r.SendgridEventId,
                    candidateId = r.CandidateId,
                    interviewerId = r.InterviewerId,
                    email = r.email,
                    candidateName = r.candidateName,
                    candidateEmail = r.candidateEmail,
                    mailSubject = r.MailSubject,
                    sentOn = r.SentOn.ToString("dd MMM yyyy hh:mm tt"),
                    sentBy = r.SentBy,
                    latestEvent = r.latestEvent,
                    bounceReason = r.BounceReason,
                    bounceStatus = r.status,
                    bounceCount = r.BounceCount,
                    activityDate = r.ActivityDate.ToString("dd MMM yyyy hh:mm tt"),
                    skillChannelName = r.SkillChannelName,
                    irfId = r.IRFId,
                    interviewId = r.InterviewId,
                    interviewDate = r.interviewDate?.ToString("dd MMM yyyy hh:mm tt"),
                    channelOwner = r.ChannelOwner,
                    mobileNumber = r.MobileNumber,
                    resumeLink = r.ResumeLink,
                    resolutionStatus = r.ResolutionStatus,
                    clientDomain = r.ClientDomain,
                    bounceKind = r.BounceKind,
                    emailCorrected = r.EmailCorrected,
                    isResolved = r.IsResolved,
                    recipientActivityUrl = Url.Action("GetRecipientMailActivitiesByMessageId", "Mail", new { recipientEmail = r.email, messageId = r.sg_message_id }),
                    scheduleUrl = Url.Action("CandidateScheduleEvent", "InterviewScheduler", new { id = r.SchedulerUniqueid, CandidateEmail = r.candidateEmail }),
                    sendInviteUrl = string.IsNullOrEmpty(r.candidateEmail) ? null : Url.Action("SendInvite", "Candidate", new { IRFName = r.IRFId, irfId = r.InterviewId, emailId = r.candidateEmail, endDate = r.EndDate }),
                    openCandidateUrl = r.CandidateId.HasValue ? Url.Action("Edit", "Candidate", new { id = r.CandidateId, irfName = r.SkillChannelName, irfId = r.IRFId }) : null,
                    editInterviewerUrl = r.InterviewerId.HasValue ? Url.Action("EditInterviewer", "User", new { id = r.InterviewerId }) : null,
                });

                return Json(new { draw, recordsTotal = page.RecordsTotal, recordsFiltered = page.RecordsFiltered, data });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { draw, recordsTotal = 0, recordsFiltered = 0, data = new object[0], error = "Could not load the list. Please try again." });
            }
        }

        /// <summary>Mail body of a bounce row, loaded on click instead of being embedded in the page.</summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetBounceMailContent(int sendgridEventId, int? candidateId)
        {
            try
            {
                var (emailid, profileName) = GetBounceDroppedSessionUser();
                if (emailid == null || !BounceDroppedProfiles.Contains(profileName))
                {
                    return Unauthorized();
                }
                var row = await _dashboardService.GetBounceDroppedRowForAction(sendgridEventId, candidateId, emailid);
                if (row == null)
                {
                    return Json(new { success = false, message = "Mail not found or you do not have access." });
                }
                var mail = await _dashboardService.GetSentMailForBounce(sendgridEventId);
                if (mail == null)
                {
                    return Json(new { success = false, message = "Mail not found or you do not have access." });
                }
                return Json(new { success = true, subject = mail.MailSubject, body = mail.MailBody ?? string.Empty });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not load the mail. Please try again." });
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBouncedCandidateEmail(int candidateId, int sendgridEventId, string newEmail, bool resend)
        {
            try
            {
                var (emailid, profileName) = GetBounceDroppedSessionUser();
                if (emailid == null || !BounceDroppedProfiles.Contains(profileName))
                {
                    return Json(new { success = false, message = BounceNoAccessMessage });
                }

                newEmail = (newEmail ?? string.Empty).Trim().ToLowerInvariant();
                if (newEmail.Length == 0 || newEmail.Length > 150 || !IsValidBounceEmail(newEmail))
                {
                    return Json(new { success = false, message = "Please enter a valid email address." });
                }

                // Never trust ids from the browser: the pair must be a row this user can see.
                var row = await _dashboardService.GetBounceDroppedRowForAction(sendgridEventId, candidateId, emailid);
                if (row == null || !row.CandidateId.HasValue)
                {
                    return Json(new { success = false, message = BounceNoAccessMessage });
                }
                if (IsClosedBounceStatus(row.ResolutionStatus))
                {
                    return Json(new { success = false, message = "This row is already closed." });
                }
                if (string.Equals(row.CandidateCurrentEmail?.Trim(), newEmail, StringComparison.OrdinalIgnoreCase))
                {
                    return Json(SameAddressResponse(row));
                }

                var billingOwner = row.IRFId.HasValue ? await _candidateService.FetchBillingownerById(row.IRFId.Value) : null;
                if (CandidateEmailDomainRule.IsBlocked(newEmail, CandidateEmailDomainRule.GetClientDomain(billingOwner)))
                {
                    return Json(new { success = false, message = BounceDomainMessage });
                }

                var result = await _dashboardService.UpdateBouncedCandidateEmail(row.CandidateId.Value, sendgridEventId, newEmail, emailid);
                // The address is refused only when it is already a candidate in THIS channel (DUPLICATE). If it belongs to
                // a candidate in other channels, this channel's candidate is linked to that person (RELINKED).
                var updatedMessage = "Email updated";
                switch (result?.Result)
                {
                    case "OK":
                        break;
                    case "RELINKED":
                        updatedMessage = "Email updated (linked to the existing candidate profile of " + newEmail + ")";
                        break;
                    case "DUPLICATE":
                        return Json(new { success = false, message = DuplicateEmailMessage(result, profileName) });
                    case "NOT_FOUND":
                        return Json(new { success = false, message = BounceNoAccessMessage });
                    case "SAME":
                        return Json(SameAddressResponse(row));
                    default:
                        return Json(new { success = false, message = BounceUpdateFailedMessage });
                }

                if (!resend)
                {
                    return Json(new { success = true, message = updatedMessage, resent = false });
                }

                var resendResult = await ResendBouncedMailCore(sendgridEventId, row.CandidateId, emailid);
                return Json(new
                {
                    success = true,
                    message = resendResult.Success ? updatedMessage + " and mail resent"
                        : resendResult.Code == BounceInterviewOverCode ? updatedMessage + ". Mail not resent: interview has passed."
                        : updatedMessage,
                    resent = resendResult.Success,
                    resendMessage = resendResult.Message,
                    code = resendResult.Code
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = BounceUpdateFailedMessage });
            }
        }

        /// <summary>
        /// Fix Email with the address the row already has (QA round 1, Q1): a temporary bounce points to Retry same
        /// address; a permanent one asks for a different email.
        /// </summary>
        private static object SameAddressResponse(BounceDroppedRowContext row)
        {
            var reason = string.IsNullOrWhiteSpace(row.BounceReason) ? "no reason given" : row.BounceReason.Trim();
            string message;
            switch (row.BounceKind)
            {
                case "Temporary":
                    message = $"This address bounced temporarily ({reason}). Use Retry same address instead.";
                    break;
                case "Blocked":
                    // The address is valid: the receiving server refused the message because of how it was sent
                    // (e.g. DMARC / From-header alignment), not because the mailbox is wrong.
                    message = $"This address is valid, but the candidate's mail server blocked the message ({reason}). "
                              + "Use Retry same address. If it is blocked again, the sending setup (SPF / DKIM / DMARC) must be fixed first.";
                    break;
                default:
                    message = $"This address does not exist ({reason}). Enter a different email or call the candidate.";
                    break;
            }
            return new
            {
                success = false,
                message,
                bounceKind = row.BounceKind,
                canRetry = CanRetrySameAddress(row.BounceKind)
            };
        }

        /// <summary>
        /// Email addresses are login ids, unique across all users (QA round 1, Q2). Name the owner for Risebird staff;
        /// Client users only see the owner's profile.
        /// </summary>
        private static string DuplicateEmailMessage(BounceEmailUpdateResult result, string profileName)
        {
            // DUPLICATE now means: that person is already a candidate in this channel (other channels are fine).
            var profile = string.IsNullOrWhiteSpace(result.OwnerProfile) ? "candidate" : result.OwnerProfile.Trim();
            if (!BounceDroppedRisebirdProfiles.Contains(profileName) || string.IsNullOrWhiteSpace(result.OwnerName))
            {
                return $"This email already belongs to another {profile} in this channel. Use a different email.";
            }
            return $"This email already belongs to {result.OwnerName.Trim()} ({profile}), who is already in this channel. Use a different email.";
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendBouncedMail(int sendgridEventId, int? candidateId)
        {
            try
            {
                var (emailid, profileName) = GetBounceDroppedSessionUser();
                if (emailid == null || !BounceDroppedProfiles.Contains(profileName))
                {
                    return Json(new { success = false, message = BounceNoAccessMessage });
                }
                var result = await ResendBouncedMailCore(sendgridEventId, candidateId, emailid);
                return Json(new { success = result.Success, message = result.Message, code = result.Code });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not resend the mail. Please try again." });
            }
        }

        /// <summary>
        /// Temporary bounce (4.x.x, mailbox full, quota, try again): take the address off SendGrid's bounce list and send
        /// the mail again to the same address (QA round 1, Q1). The interview rules of Resend still apply.
        /// </summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetrySameAddress(int sendgridEventId, int? candidateId)
        {
            try
            {
                var (emailid, profileName) = GetBounceDroppedSessionUser();
                if (emailid == null || !BounceDroppedProfiles.Contains(profileName))
                {
                    return Json(new { success = false, message = BounceNoAccessMessage });
                }
                var row = await _dashboardService.GetBounceDroppedRowForAction(sendgridEventId, candidateId, emailid);
                if (row == null || !row.CandidateId.HasValue)
                {
                    return Json(new { success = false, message = BounceNoAccessMessage });
                }
                if (IsClosedBounceStatus(row.ResolutionStatus))
                {
                    return Json(new { success = false, message = "This row is already closed." });
                }
                if (!CanRetrySameAddress(row.BounceKind))
                {
                    return Json(new { success = false, message = "This address does not exist. Enter a different email." });
                }
                if (row.LastResentAt.HasValue && DateTime.Now - row.LastResentAt.Value < BounceResendCooldown)
                {
                    return Json(new { success = false, message = "This mail was resent less than 10 minutes ago. Please wait before resending." });
                }

                // Without this SendGrid drops the retry, because the address is on its bounce list.
                await _notificationHelper.RemoveBounceSuppression(row.BouncedEmail);
                var result = await ResendBouncedMailCore(sendgridEventId, row.CandidateId, emailid,
                    allowSameAddress: true, resolutionStatus: "RetriedSameAddress");
                return Json(new { success = result.Success, message = result.Message, code = result.Code });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not resend the mail. Please try again." });
            }
        }

        private const string BounceInterviewOverCode = "InterviewOver";

        /// <summary>
        /// Resends the mail behind a bounce row to the candidate's current address and records the resolution.
        /// The body never comes from the client.
        ///   - Interview passed, cancelled or missing: nothing is sent (QA round 1, Q3).
        ///   - Upcoming interview: the stored body with the old address replaced, plus a fresh calendar invite; the
        ///     original CC list is told about the change (Q4).
        ///   - No interview: the stored body with the old address replaced (Q4).
        /// A row already Resent may be resent again (after the cooldown): the calendar then updates the same event.
        /// </summary>
        private async Task<(bool Success, string Message, string Code)> ResendBouncedMailCore(int sendgridEventId, int? candidateId, string actionBy,
            bool allowSameAddress = false, string resolutionStatus = "Resent")
        {
            var row = await _dashboardService.GetBounceDroppedRowForAction(sendgridEventId, candidateId, actionBy);
            if (row == null || !row.CandidateId.HasValue || string.IsNullOrWhiteSpace(row.CandidateCurrentEmail))
            {
                return (false, BounceNoAccessMessage, null);
            }
            if (IsClosedBounceStatus(row.ResolutionStatus) && row.ResolutionStatus != "Resent")
            {
                return (false, "This row is already closed.", null);
            }
            var sameAddress = string.Equals(row.CandidateCurrentEmail.Trim(), row.BouncedEmail?.Trim(), StringComparison.OrdinalIgnoreCase);
            if (sameAddress && !allowSameAddress)
            {
                return (false, "Fix the email first.", null);
            }
            if (!sameAddress && allowSameAddress)
            {
                return (false, "The email has already been changed. Use Resend Mail instead.", null);
            }
            if (row.LastResentAt.HasValue && DateTime.Now - row.LastResentAt.Value < BounceResendCooldown)
            {
                return (false, "This mail was resent less than 10 minutes ago. Please wait before resending.", null);
            }

            var mail = await _dashboardService.GetSentMailForBounce(sendgridEventId);
            if (mail == null || string.IsNullOrWhiteSpace(mail.MailBody))
            {
                return (false, "The original mail could not be found.", null);
            }

            var newEmail = row.CandidateCurrentEmail.Trim();
            var body = ReplaceEmailInBody(mail.MailBody, row.BouncedEmail, newEmail);
            string notice = null;
            switch (mail.InterviewState)
            {
                case "Past":
                case "Cancelled":
                case "Missing":
                    return (false, "This interview has passed or was cancelled. Schedule a new interview instead.", BounceInterviewOverCode);
                case "Upcoming":
                    var invite = await ResendBouncedInterviewInvite(mail, row.BouncedEmail, newEmail, body);
                    if (!invite.Success)
                    {
                        return (false, invite.Message, null);
                    }
                    notice = invite.Message;
                    break;
                default: // "None": not an interview mail
                    var sendResult = await _notificationHelper.ResendStoredMail(newEmail, mail.MailSubject, body, mail.IRFId, mail.InterviewId);
                    if (sendResult != "Mail sent successfully.")
                    {
                        return (false, "Could not resend the mail. Please try again.", null);
                    }
                    break;
            }

            var verb = resolutionStatus == "RetriedSameAddress" ? "Retried " : "Resent to ";
            await _dashboardService.ResolveMailBounce(sendgridEventId, row.CandidateId, resolutionStatus, verb + newEmail, actionBy);
            return (true, "Mail resent to " + newEmail + (notice == null ? string.Empty : ". " + notice), null);
        }

        /// <summary>
        /// Upcoming interview (QA round 1, Q4): the stored invite has no calendar file and shows the old address, so send
        /// the patched body with a fresh .ics built from InterviewMeetings (same UID as every invite of this interview,
        /// next SEQUENCE). Then withdraw the old invite for the original CC list. No new Zoom meeting, no meeting change.
        /// </summary>
        private async Task<(bool Success, string Message)> ResendBouncedInterviewInvite(BounceMailContent mail, string oldEmail, string newEmail, string body)
        {
            if (!mail.InterviewId.HasValue || !mail.InterviewStartUtc.HasValue)
            {
                return (false, "The interview details could not be found.");
            }
            var interviewId = mail.InterviewId.Value;
            var startUtc = DateTime.SpecifyKind(mail.InterviewStartUtc.Value, DateTimeKind.Utc);
            var duration = mail.InterviewDurationMinutes > 0 ? mail.InterviewDurationMinutes : 60;

            var nextSequence = await _notificationHelper.NextInterviewInviteSequence(interviewId);
            var sequence = nextSequence ?? 0;
            // Sequence 1 = nothing was sent with the stable UID before: the original invite (sent before this release)
            // has a random UID that no calendar can match, so its recipients also get a plain notice.
            var legacyInvite = !nextSequence.HasValue || nextSequence.Value <= 1;

            var sendResult = await _notificationHelper.ResendInterviewInvite(newEmail, mail.MailSubject, body, interviewId, sequence,
                startUtc, duration, mail.ZoomLink, mail.IRFId);
            if (sendResult != "Mail sent successfully.")
            {
                return (false, "Could not resend the mail. Please try again.");
            }

            if (string.Equals(oldEmail?.Trim(), newEmail, StringComparison.OrdinalIgnoreCase))
            {
                return (true, null); // same-address retry: nothing changed for the other recipients
            }

            var cc = (mail.MailCc ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0
                            && !string.Equals(x, oldEmail?.Trim(), StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(x, newEmail, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (cc.Count == 0)
            {
                return (true, null);
            }

            var dateText = mail.InterviewDate.HasValue ? mail.InterviewDate.Value.ToString("dd MMM yyyy hh:mm tt") : "the scheduled date";
            string noticeResult;
            if (legacyInvite)
            {
                await _notificationHelper.SendInterviewInviteCancel(cc, interviewId, sequence, startUtc, duration, mail.MailSubject, mail.IRFId);
                noticeResult = await _notificationHelper.SendCandidateEmailCorrectedNotice(cc, oldEmail, newEmail, dateText, interviewId, mail.IRFId);
            }
            else
            {
                // Their event already has the stable UID: update it in place rather than cancel it, so it stays on
                // their calendars with the corrected candidate address.
                var ics = _notificationHelper.BuildInterviewInviteIcs(interviewId, sequence, startUtc, duration, mail.ZoomLink, mail.MailSubject,
                    new[] { newEmail }.Concat(cc));
                noticeResult = await _notificationHelper.SendCandidateEmailCorrectedNotice(cc, oldEmail, newEmail, dateText, interviewId, mail.IRFId, ics);
            }
            return (true, noticeResult == "Mail sent successfully."
                ? "The original recipients were told about the new address."
                : "Could not notify the original recipients about the new address.");
        }

        /// <summary>Replaces the bounced address in a stored mail body (plain, HTML-escaped and URL-encoded forms).</summary>
        private static string ReplaceEmailInBody(string body, string oldEmail, string newEmail)
        {
            if (string.IsNullOrEmpty(body) || string.IsNullOrWhiteSpace(oldEmail) || string.IsNullOrWhiteSpace(newEmail)
                || string.Equals(oldEmail.Trim(), newEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return body;
            }
            oldEmail = oldEmail.Trim();
            newEmail = newEmail.Trim();
            return body
                .Replace(oldEmail, newEmail, StringComparison.OrdinalIgnoreCase)
                .Replace(oldEmail.Replace("@", "&#64;"), newEmail.Replace("@", "&#64;"), StringComparison.OrdinalIgnoreCase)
                .Replace(oldEmail.Replace("@", "%40"), newEmail.Replace("@", "%40"), StringComparison.OrdinalIgnoreCase);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveMailBounce(int sendgridEventId, int? candidateId, string status, string note)
        {
            try
            {
                var (emailid, profileName) = GetBounceDroppedSessionUser();
                if (emailid == null || !BounceDroppedProfiles.Contains(profileName))
                {
                    return Json(new { success = false, message = BounceNoAccessMessage });
                }
                if (status != "Resolved" && status != "Unreachable")
                {
                    return Json(new { success = false, message = "Invalid status." });
                }
                if (status == "Unreachable" && !BounceDroppedRisebirdProfiles.Contains(profileName))
                {
                    return Json(new { success = false, message = BounceNoAccessMessage });
                }
                note = note?.Trim();
                if (status == "Unreachable" && string.IsNullOrEmpty(note))
                {
                    return Json(new { success = false, message = "Please add a note." });
                }
                if (note != null && note.Length > 500)
                {
                    return Json(new { success = false, message = "Note must be 500 characters or less." });
                }

                var row = await _dashboardService.GetBounceDroppedRowForAction(sendgridEventId, candidateId, emailid);
                if (row == null)
                {
                    return Json(new { success = false, message = BounceNoAccessMessage });
                }
                if (IsClosedBounceStatus(row.ResolutionStatus))
                {
                    return Json(new { success = false, message = "This row is already closed." });
                }

                var ok = await _dashboardService.ResolveMailBounce(sendgridEventId, row.CandidateId, status, note, emailid);
                return Json(ok
                    ? new { success = true, message = status == "Resolved" ? "Marked as resolved" : "Marked as unreachable" }
                    : new { success = false, message = "Could not save. Please try again." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not save. Please try again." });
            }
        }

        /// <summary>
        /// The same address can be tried again when the mailbox exists: a temporary bounce (mailbox full, 4.x.x) or a
        /// Blocked one (refused because of the sender, e.g. DMARC alignment). Not for Permanent (address does not exist).
        /// </summary>
        private static bool CanRetrySameAddress(string bounceKind) =>
            bounceKind == "Temporary" || bounceKind == "Blocked";

        private static bool IsClosedBounceStatus(string status) =>
            status == "Resolved" || status == "Unreachable" || status == "Resent" || status == "RetriedSameAddress";

        private static bool IsValidBounceEmail(string email)
        {
            if (email.Contains(" ") || email.Count(c => c == '@') != 1)
            {
                return false;
            }
            try
            {
                var address = new System.Net.Mail.MailAddress(email);
                var domain = address.Host;
                return address.Address == email && domain.Contains('.') && !domain.StartsWith('.') && !domain.EndsWith('.');
            }
            catch (FormatException)
            {
                return false;
            }
        }
        #endregion
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetDMApproveDetails(int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _dashboardService.GetDMApproveDetails(emailid);


                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetOpenClickCampaignMailDetails(int campaignId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement")
                    {
                        IEnumerable<MailRecipientActivityDetailsVM> recipients = null;


                        recipients = await _dashboardService.GetOpenClickCampaignMailDetails(campaignId: campaignId);

                        // ViewBag.campaignName = campaignName;

                        ViewBag.UserProfile = profileName;

                        return View(recipients);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetColdCandidatesList(int ColdCandidateValue, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var data = await _dashboardService.GetColdCandidatesList(ColdCandidateValue, emailid);
                        //data = data.Select(x =>
                        //{
                        //    x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                        //    return x;
                        //}).ToList();
                        ViewBag.ColdCandidateValue = ColdCandidateValue;
                        return View(data);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public async Task<JsonResult> MarkedColdCandidate(int CandidateId, string IRFId, string CandidateEmail, int InterviewId, int irfIntId)
        {
            try
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
                //  var result = await _dashboardService.GetFutureDateInterviewSchedule(InterviewId);
                var irfOwnersAndRestrictedIrfOwner = await _candidateService.GetCandidateStaffingAgencyAndClientHRByInterviewId(InterviewId);

                var data = await _dashboardService.MarkedColdCandidate(CandidateId, emailId, InterviewId, 1);

                if (data != null)
                {

                    foreach (var item in data)
                    {
                        if (item.interviewDate > DateTime.Now)
                        {
                            List<string> interviewers = new List<string>();
                            if (!string.IsNullOrEmpty(item.interviewer))
                            {
                                interviewers.Add(item.interviewer);
                            }
                            var cancellationsource = string.Empty;
                            cancellationsource = await _meetingService.GetInterviewCancellationSource(InterviewId);

                            var emailresult = await _notificationHelper.SendInterviewCancelledMail(interviewers, item.interviewDate.ToString(), irfOwnersAndRestrictedIrfOwner?.ToList(), irfIntId, InterviewId);
                            //var emailresult1 = await _notificationHelper.SendInterviewCancelledMailClient(item.interviewDate.ToString(), item.CandidateName, irfOwnersAndRestrictedIrfOwner?.ToList(), irfIntId, InterviewId, cancellationsource);
                            await _notificationHelper.SendInterviewCancelledMailCandidate(item.candidateEmail, item.interviewDate.ToString(), item.CandidateName, irfIntId, InterviewId);
                        }
                    }
                    return Json("Candidate " + "(" + CandidateEmail + ")" + " for " + IRFId + " has been marked as cold successfully");
                }
                else
                {
                    return Json("Failed to mark candidate as Cold.");
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpPost]
        public async Task<JsonResult> UndoMarkedColdCandidate(int CandidateId, string IRFId, string CandidateEmail, int irfIntId)
        {
            try
            {
                var emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");


                var data = await _dashboardService.MarkedColdCandidate(CandidateId, emailId, 0, 2);

                if (data == null || data.Count == 0)
                {
                    return Json("Candidate " + "(" + CandidateEmail + ")" + " for " + IRFId + " has been undo as cold successfully");
                }
                else
                {
                    return Json("Failed to undo cold candidate .");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        #region PQI
        public async Task<IActionResult> InterviewerIRFDetails(string type, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        if (type == "Rejection")
                        {
                            ViewBag.Type = "Rejection";
                        }
                        else if (type == "NegativeCandidate")
                        {
                            type = "NegativeCandidate";
                            ViewBag.Type = "NegativeCandidate";
                        }
                        else
                        {
                            type = "Selection";
                            ViewBag.Type = "Selection";
                        }
                        var model = await _dashboardService.GetInterviewerIRFDetails(type, null, emailid);
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        public async Task<IActionResult> InterviewerIRFLogisticDetails(string type, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        if (type == "Shortselectcomment")
                        {
                            type = "Shortselectcomment";
                            ViewBag.Type = "Shortselectcomment";
                        }
                        else if (type == "Shortrejectcomment")
                        {
                            type = "Shortrejectcomment";
                            ViewBag.Type = "Shortrejectcomment";
                        }
                        else if (type == "Lessquestion")
                        {
                            type = "Lessquestion";
                            ViewBag.Type = "Lessquestion";
                        }
                        else if (type == "Lessduration")
                        {
                            type = "Lessduration";
                            ViewBag.Type = "Lessduration";
                        }
                        else
                        {
                            type = "Latejoin";
                            ViewBag.Type = "Latejoin";
                        }
                        var model = await _dashboardService.InterviewerIRFLogisticDetails(type, emailid);
                        // PLI decision per row (Under review / Reviewed / Removed; QC Tracking Delta 5).
                        if (model != null && model.Count > 0)
                        {
                            var pliStatuses = await _dashboardService.GetQcPageStatus("PLI", model.Select(m => m.UserId));
                            QcPageStatusVM.ApplyPli(model, pliStatuses);
                        }
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        public async Task<IActionResult> InterviewerRiskIRFDetails(string type, string uniqueid)
        {
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid?.Replace("\"", "");
            if (string.IsNullOrEmpty(emailid))
            {
                return RedirectToAction("PublicAllMeetings", "Candidate", new { id = uniqueid });
            }
            ViewBag.Profile = "Client";
            ViewBag.Emailid = emailid;
            ViewBag.PageStatus = "Public";
            ViewBag.uniqueid = uniqueid;
            if (type == "Rejection")
            {
                ViewBag.Type = "Rejection";
            }
            else if (type == "NegativeCandidate")
            {
                type = "NegativeCandidate";
                ViewBag.Type = "NegativeCandidate";
            }
            else
            {
                type = "Selection";
                ViewBag.Type = "Selection";
            }
            var model = await _dashboardService.GetInterviewerIRFDetails(type, uniqueid);
            return View("InterviewerIRFDetails", model);
        }
        public async Task<IActionResult> TotalInterviewerRiskIRFDetails(string uniqueid, string type)
        {
            var emailid = HttpContext.Session.GetString("email")?.Trim();
            emailid = emailid?.Replace("\"", "");
            if (string.IsNullOrEmpty(emailid))
            {
                return RedirectToAction("PublicAllMeetings", "Candidate", new { id = uniqueid });
            }
            ViewBag.Profile = "Client";
            ViewBag.Emailid = emailid;
            ViewBag.PageStatus = "Public";
            ViewBag.uniqueid = uniqueid;
            ViewBag.InterviewerStatus = "TotalInterviewer";
            var model = await _dashboardService.GetTotalInterviewerRiskIRFDetails(uniqueid);
            if (model == null)
            {
                model = new List<PQIDetails>();
            }
            if (type == "ShortList")
            {
                model = model.Where(x => x.IsActive == true).ToList();
                model = model.Where(x => x.ShortListstatus == "Shortlisted").ToList();
            }
            else if (type == "Removed")
            {
                model = model.Where(x => x.ShortListstatus == "Rejected").ToList();
            }
            else
            {
                model = model.Where(x => x.IsActive == true).ToList();
                model = model.Where(x => string.IsNullOrWhiteSpace(x.ShortListstatus)).ToList();
            }
            ViewBag.Type = type;
            return View("InterviewerIRFDetails", model);
        }
        [HttpPost]
        public async Task<JsonResult> UpdateRedMarkeredInterviewerstatus(int interviewid, int IRFid, int status, string Comment)
        {
            string emailId = string.Empty;
            if (HttpContext.Session.GetString("email") != null)
            {
                emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
            }
            var result = await _dashboardService.UpdateRedMarkeredInterviewerstatus(interviewid, IRFid, status, Comment, emailId, "IRF", "RedMarker");
            if (result)
            {
                return Json("Interviewer Moved Successfully.");
            }
            return Json("Fail to Move the Interviewer, Please try again.");
        }

        [HttpPost]
        public async Task<JsonResult> FetchRedMarkerCommentDetails(int interviewid, int IRFid, string type)
        {
            var model = await _dashboardService.FetchRedMarkerCommentDetails(interviewid, IRFid, type);
            return Json(model);
        }

        public async Task<IActionResult> TotalRedMarkeredInterviewers(string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "TechSupport" || profileName == "Interviewer" || profileName == "Finance")
                    {
                        if (string.IsNullOrWhiteSpace(type))
                        {
                            type = "Pending";
                        }
                        var model = await _dashboardService.GetTotalRedMarkeredInterviewers();
                        if (type == "UnderReview")
                        {
                            model = model.Where(x => x.RedMarkerStatus == 1 && x.IsActive == true).ToList();
                        }
                        else if (type == "Reviewed")
                        {
                            model = model.Where(x => x.RedMarkerStatus == 2 && x.IsActive == true).ToList();
                        }
                        else if (type == "Removed")
                        {
                            model = model.Where(x => x.IsActive != true).ToList();
                        }
                        else if (type == "Pending")
                        {
                            model = model.Where(x => x.IsActive == true && x.RedMarkerStatus != 1 && x.RedMarkerStatus != 2).ToList();
                        }
                        ViewBag.Type = type;
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        #endregion
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> QCOwnerDashboard()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement" || profileName == "Finance")
                    {
                        return View();
                    }
                    return RedirectToAction("SignOut", "Session");
                }
                return RedirectToAction("SignOut", "Session");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetQCOwnerDashboardData(DateTime? dateFrom, DateTime? dateTo)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(null);
                var response = await _dashboardService.GetQCOwnerDashboardMetrics(dateFrom, dateTo);
                foreach (var item in response.Rows)
                {
                    if (item.PenalizedReports > 0)
                        item.TotalPenaltyPercent = Math.Round(item.TotalPenaltyPercent / item.PenalizedReports, 2);
                }
                return Json(response);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(null);
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetQCOwnerFlagDetails(string qcOwner, DateTime? dateFrom, DateTime? dateTo)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(null);
                var response = await _dashboardService.GetQCOwnerFlagDetails(qcOwner, dateFrom, dateTo);
                return Json(response);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(null);
            }
        }

        public async Task<IActionResult> TechQCDetails(string type, int ReturnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = ReturnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var modelList = await _dashboardService.FetchTechQCDetails(emailid);
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        modelList = modelList.Select(x => { x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; return x; }).ToList();
                        if (type == "Completed")
                        {
                            ViewBag.Type = "Completed";
                            modelList = modelList.Where(x => x.IsQC == true).ToList();
                        }
                        else
                        {
                            ViewBag.Type = "Pending";
                            modelList = modelList.Where(x => x.IsQC == false).ToList();
                        }
                        return View(modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        public async Task<IActionResult> PotentialPausedDetails(int type, int returnBack)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    ViewBag.ReturnBack = returnBack;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement")
                    {
                        var modelList = await _dashboardService.PotentialPausedDetails(emailid);
                        if (modelList == null)
                        {
                            modelList = new List<MeetingVM>();
                        }
                        if (type == 0)
                        {
                            modelList = modelList.Where(x => !x.IsPaused).ToList();
                            ViewBag.ColdCandidateValue = 0;
                        }
                        else
                        {
                            modelList = modelList.Where(x => x.IsPaused).ToList();
                            ViewBag.ColdCandidateValue = 1;
                        }
                        // modelList = modelList.Select(x => { x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; return x; }).ToList();
                        return View(modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetPotentialColdCandidatesByClient(string Email)
        {
            try
            {
                var data = await _dashboardService.GetColdCandidatesByClient(Email);
                ViewBag.ClientEmail = Email;
                return View("_GetPotentialColdCandidatesByClient", data.ToList());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EmergencyDashboard()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                    ViewBag.ProfileName = profileName;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer")
                    {
                        var model = new DashboardVM();
                        model.NotUpdatedMeetingsObj = new NotUpdatedMeetings();
                        model.NotUpdatedMeetingsObj = await _dashboardService.GetNotUpdatedMeetingsCount(Emailid);
                        var modelList = await _dashboardService.FetchParticipantwaitingDetails(Emailid);
                        model.NotUpdatedMeetingsObj.CandidatewaitingCount = modelList.Where(x => x.PerformedOn != null).Count();
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> NotScheduledMeetingsCO(int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profile = profileName;
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    int ClientId = 0;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer")
                    {
                        if (TempData["SuccessErrorMessageCandidate"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageCandidate"].ToString();
                            TempData.Remove("SuccessErrorMessageCandidate");
                        }
                        if (profileName == "Client")
                        {
                            var client = await _userRepository.GetClientUserDetails(emailid);
                            ClientId = client.ClientId;
                            ViewBag.ClientRole = client?.RoleId != 0 ? ((ClientRoleEnum)Convert.ToInt32(client.RoleId)).GetDescription() : string.Empty;
                        }
                        List<MeetingVM> meetings = new List<MeetingVM>();
                        if (profileName == "SeniorHR")
                        {
                            meetings = await _dashboardService.GetNotSecheduleSeniorHRList(emailid);
                        }
                        else
                        {
                            meetings = await _meetingService.GetClientNotScheduledMeetingList(ClientId, emailid);
                        }
                        if (HttpContext.Session.GetString("NotScheduledMeetingsfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("NotScheduledMeetingsfilters"));

                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                                meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate && t.dtCreatedOn <= filter.EndDate).ToList();
                            }
                            else if (filter.StartDate != null && filter.EndDate == null)
                            {
                                meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate).ToList();
                            }
                            else if (filter.StartDate == null && filter.EndDate != null)
                            {
                                filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                                meetings = meetings.Where(t => t.dtCreatedOn <= filter.EndDate).ToList();
                            }

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                            filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;

                            meetings = meetings.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                                                    && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                                                    && ((filter.RFID != null && filter.RFID.Count() > 0) ? filter.RFID.Contains(c.IRFId) : c.IRFId == null)
                                                    && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();
                        }
                        else
                        {
                            DateTime StartDate = DateTime.Now.AddMonths(-1);
                            DateTime EndDate = DateTime.Now.AddDays(1);
                            meetings = meetings.Where(t => t.dtCreatedOn >= StartDate && t.dtCreatedOn <= EndDate).ToList();
                        }
                        return View(meetings);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> NotScheduledMeetingsCO(InterviewFilter filter, int? Page, string searchValue)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("NotScheduledMeetingsfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("NotScheduledMeetingsCO", new { Page = Page, searchValue = searchValue });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetNotScheduleMeetingsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("NotScheduledMeetingsfilters");
                    return RedirectToAction("NotScheduledMeetingsCO");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<FileContentResult> NotScheduledMeetingstoExcel()
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Interviews");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_Interviews" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                int ClientId = 0;
                if (profileName == "Client")
                {
                    var client = await _userRepository.GetClientUserDetails(emailid);
                    ClientId = client.ClientId;
                    ViewBag.ClientRole = client?.RoleId != 0 ? ((ClientRoleEnum)Convert.ToInt32(client?.RoleId)).GetDescription() : string.Empty;
                }
                List<MeetingVM> meetings = new List<MeetingVM>();
                if (profileName == "SeniorHR")
                {
                    meetings = await _dashboardService.GetNotSecheduleSeniorHRList(emailid);
                }
                else
                {
                    meetings = await _meetingService.GetClientNotScheduledMeetingList(ClientId, emailid);
                }
                if (HttpContext.Session.GetString("NotScheduledMeetingsfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("NotScheduledMeetingsfilters"));

                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                        meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate && t.dtCreatedOn <= filter.EndDate).ToList();
                    }
                    else if (filter.StartDate != null && filter.EndDate == null)
                    {
                        meetings = meetings.Where(t => t.dtCreatedOn >= filter.StartDate).ToList();
                    }
                    else if (filter.StartDate == null && filter.EndDate != null)
                    {
                        filter.EndDate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                        meetings = meetings.Where(t => t.dtCreatedOn <= filter.EndDate).ToList();
                    }

                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = meetings.Select(x => x.JDTiltle).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = meetings.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                    filter.RFID = filter.RFID == null ? filter.RFID = meetings.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                    filter.ClientName = filter.ClientName == null ? filter.ClientName = meetings.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;

                    meetings = meetings.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                                            && ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                                            && ((filter.RFID != null && filter.RFID.Count() > 0) ? filter.RFID.Contains(c.IRFId) : c.IRFId == null)
                                            && ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)).ToList();
                }
                else
                {
                    DateTime StartDate = DateTime.Now.AddMonths(-1);
                    DateTime EndDate = DateTime.Now.AddDays(1);
                    meetings = meetings.Where(t => t.dtCreatedOn >= StartDate && t.dtCreatedOn <= EndDate).ToList();
                }

                Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                Sheet.Cells["A1:AI1"].Style.WrapText = true;
                int startColumn = 1; int endColumn = 10;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "Added Date";
                Sheet.Cells[1, 2].Value = "Client Name";
                Sheet.Cells[1, 3].Value = "JD Title";
                Sheet.Cells[1, 4].Value = "Candidate Name";
                Sheet.Cells[1, 5].Value = "Candidate Email";
                Sheet.Cells[1, 6].Value = "Candidate Mobile Number";
                Sheet.Cells[1, 7].Value = "IRF ID";
                Sheet.Cells[1, 8].Value = "Resume";
                Sheet.Cells[1, 9].Value = "Client HR";
                Sheet.Cells[1, 10].Value = "Channel Owner";
                Sheet.Cells[1, 11].Value = "Latest Status";
                int i = 2;
                if (meetings.Count() > 0)
                {
                    foreach (var item in meetings)
                    {
                        Sheet.Cells[i, 1].Value = item.dtCreatedOn.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 2].Value = item.ClientName;
                        Sheet.Cells[i, 3].Value = item.JDTiltle;
                        Sheet.Cells[i, 4].Value = item.candidateName;
                        Sheet.Cells[i, 5].Value = item.candidateEmail;
                        Sheet.Cells[i, 6].Value = item.MobileNumber;
                        Sheet.Cells[i, 7].Value = item.IRFId;
                        if (!string.IsNullOrEmpty(item.resumeLink))
                        {
                            try
                            {
                                Sheet.Cells[i, 8].Hyperlink = new Uri(item.resumeLink, UriKind.Absolute);
                                Sheet.Cells[i, 8].Value = "Resume";
                                Sheet.Cells[i, 8].Style.Font.UnderLine = true;
                                Sheet.Cells[i, 8].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                            }
                            catch (Exception)
                            {
                                Sheet.Cells[i, 8].Value = "No Resume";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 8].Value = "No Resume";
                        }
                        Sheet.Cells[i, 9].Value = item.ClientHrContact;
                        Sheet.Cells[i, 10].Value = item.ChannelOwner;
                        Sheet.Cells[i, 11].Value = item.StatusValue;
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> AllEmergencyInterviewDetails()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                    DateTime startdate = DateTime.Now.AddMonths(-1);
                    DateTime enddate = DateTime.Now;

                    if (HttpContext.Session.GetString("EmergencyInterviewDetailsfilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<MyInterviewFilter>(HttpContext.Session.GetString("EmergencyInterviewDetailsfilters"));
                        startdate = Convert.ToDateTime(filter.StartDate);
                        enddate = Convert.ToDateTime(filter.EndDate).AddDays(1);
                    }
                    var interviewmanagement = await _dashboardService.GetAllInterviewManagementDetails(startdate, enddate);

                    interviewmanagement = interviewmanagement.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; }).ToList();
                    return View(interviewmanagement);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> AllEmergencyInterviewDetails(MyInterviewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("EmergencyInterviewDetailsfilters", JsonConvert.SerializeObject(filter));
                    }
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    return RedirectToAction("AllEmergencyInterviewDetails");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> AllResetEmergencyInterviewDetailsfilters(string pagetype)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("EmergencyInterviewDetailsfilters");
                    return RedirectToAction("AllEmergencyInterviewDetails");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("AllEmergencyInterviewDetails");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetSurchargesDetails(int actionType)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    ViewBag.Type = actionType;
                    DateTime? StartDate;
                    DateTime? EndDate;
                    if (HttpContext.Session.GetString("SurchargeFilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("SurchargeFilters"));
                        StartDate = filter.StartDate;
                        EndDate = filter.EndDate;
                    }
                    else
                    {
                        StartDate = DateTime.Now.AddMonths(-1);
                        EndDate = DateTime.Now;
                    }
                    var surchaes = await _dashboardService.GetSurcharesDetails(actionType, StartDate, EndDate);
                    surchaes = surchaes.Select(x =>
                    {
                        x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                        return x;
                    }).ToList();
                    return View(surchaes);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GetSurchargesDetails(SurchargeFilter filter, int actionType)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("SurchargeFilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("GetSurchargesDetails", "Dashboard", new { actionType });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetGetSurchargesDetailsFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("SurchargeFilters");
                    return RedirectToAction("GetSurchargesDetails");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<JsonResult> ApproveSurcharge(int InterviewId, int Surcharge, string SurchargeComment, string Type, int IRFId, int actionType, int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    var Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                    var data = await _dashboardService.ApproveSurcharge(InterviewId, Surcharge, SurchargeComment, Type, Emailid, IRFId, actionType, UserId);

                    if (data)
                    {
                        return Json("Successfully");

                    }
                    else
                    {
                        return Json("UnSuccessfully");
                    }

                }
                else
                {
                    return Json(new { success = false, redirectUrl = Url.Action("SignOut", "Session") });
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateSurchargeDetails(int InterviewId, int IRFId, string Type, int UserId, string SurchargeComment, int Surchargeamt)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    var model = await _dashboardService.UpdateSurchargeDetails(InterviewId, IRFId, Type, UserId, SurchargeComment, Surchargeamt, emailId);
                    if (model)
                    {
                        return Json("Surcharge details are updated successfully.");
                    }
                    else
                    {
                        return Json("Failed to update Surcharge details, please try again.");
                    }
                }
                else
                {
                    return Json("Failed to update Surcharge details, please try again.");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetSurchargesActivityTracker(int InterviewId, int IRFId, string Type, int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var surchaes = await _dashboardService.GetSurcharesActivityTraker(InterviewId, IRFId, Type, UserId);
                    return PartialView("_GetSurchargesActivityTracker", surchaes);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        //Interviewer rate and Plan type
        [HttpPost]
        public async Task<IActionResult> GetPerSuccessRateDetails(int InterviewId, int IRFId, string Type, int UserId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var surchaes = await _dashboardService.GetPerSuccessRateDetails(InterviewId, IRFId, Type, UserId);
                    foreach (var item in surchaes)
                    {
                        return Json(new { Heuristicsscore = item.Heuristicsscore, PerSuccessRate = item.PerSuccessRate });
                    }
                    return View();
                }

                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }


        /// <summary>
        /// Gets active skills channel Interviewers whose AI verdict is negative i.e, not suitable.
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> InterviewersWithNegativeAIVerdict(string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");

                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer")
                    {
                        ViewBag.Profile = profileName;
                        if (string.IsNullOrWhiteSpace(type))
                        {
                            type = "Pending";
                        }
                        var model = await _dashboardService.GetTransInterviewersWithNegativeAIVerdict();
                        if (type == "UnderReview")
                        {
                            model = model.Where(x => x.NegativeAIVerdictStatus == 1 && x.IsActive == true).ToList();
                        }
                        else if (type == "Reviewed")
                        {
                            model = model.Where(x => x.NegativeAIVerdictStatus == 2 && x.IsActive == true).ToList();
                        }
                        else if (type == "Removed")
                        {
                            model = model.Where(x => x.IsActive != true).ToList();
                        }
                        else if (type == "Pending")
                        {
                            model = model.Where(x => x.IsActive == true && x.NegativeAIVerdictStatus != 1 && x.NegativeAIVerdictStatus != 2).ToList();
                        }
                        ViewBag.Type = type;
                        return View("InterviewersWithNegativeAIVerdict", model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpGet]
        public async Task<IActionResult> AIQCNegative(int? interviewId = null)
        {
            try
            {
                if (!CompletedInterviewsViewMode.TryAuthorizeEvidenceEndpoints(HttpContext, out var profileName))
                {
                    return RedirectToAction("SignOut", "Session");
                }

                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                ViewBag.Profile = profileName;
                ViewBag.InterviewIdFilter = interviewId;
                var model = await _dashboardService.GetAIQCNegativeVerdictReports();
                if (interviewId.HasValue)
                {
                    model = model.Where(x => x.InterviewId == interviewId.Value).ToList();
                }
                model = model.Select(x =>
                {
                    x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                    x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                    x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                    return x;
                }).ToList();
                foreach (var item in model)
                {

                    if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                    {
                        if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                        {
                            item.MeetingStatusStatusDesc = "Status Pending";
                        }
                    }
                    else
                    {
                        item.MeetingStatusStatusDesc = string.Empty;

                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UnconfirmedCandidates()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer")
                    {
                        var model = await _dashboardService.GetCandidatesWhoHaventConfirmed();
                        model = model.Select(x =>
                        {
                            x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            //x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                            return x;
                        }).ToList();

                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SkillChannelInterviewerBilling()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "Finance")
                    {
                        var model = await _dashboardService.GetAllSkillChannelsList();
                        if (model == null)
                        {
                            model = new List<IRFDetailsList>();
                        }
                        model = model.Select(x =>
                        {
                            x.Heuristicsscore = !string.IsNullOrWhiteSpace(x.Heuristicsscore) ? ((Heuristicsscorelevel)Convert.ToInt32(x.Heuristicsscore)).GetDescription() : string.Empty;
                            x.CompanyType = !string.IsNullOrWhiteSpace(x.CompanyType) ? ((CompanyType)Convert.ToInt32(x.CompanyType)).GetDescription() : string.Empty;
                            x.JobCountry = x.JobCountry == "2" ? "Onsite" : "Offshore";
                            return x;
                        }).ToList();
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        #region Skill Channel Billing Rate
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SkillChannelBillingRate()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "Finance")
                    {
                        var model = await _dashboardService.GetSkillChannelBillingRates();
                        if (model == null) model = new List<SkillChannelBillingRateVM>();
                        model = model.Select(x =>
                        {
                            x.Heuristicsscore = !string.IsNullOrWhiteSpace(x.Heuristicsscore)
                                ? ((Heuristicsscorelevel)Convert.ToInt32(x.Heuristicsscore)).GetDescription()
                                : string.Empty;
                            x.CompanyType = !string.IsNullOrWhiteSpace(x.CompanyType)
                                ? ((CompanyType)Convert.ToInt32(x.CompanyType)).GetDescription()
                                : string.Empty;
                            x.JobCountry = x.JobCountry == "2" ? "Onsite" : "Offshore";
                            return x;
                        }).ToList();
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> SaveSkillChannelBillingRate(int irfId, int billingRate)
        {
            try
            {
                var createdBy = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
                var result = await _dashboardService.SaveSkillChannelBillingRate(irfId, billingRate, createdBy);
                return Json(result ? "Success" : "Failed");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Error");
            }
        }
        #endregion

        #region Client Action Data
        public async Task<IActionResult> ClientRemovedInterviewers()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "SeniorHr")
                    {
                        var model = await _dashboardService.GetClientRemovedInterviewers();
                        if (model == null)
                        {
                            model = new List<PQIDetails>();
                        }
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        #endregion

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AITranscriptVerdicts()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.Profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "SeniorInterviewerManagement" || profileName == "Interviewer")
                    {
                        var model = await _dashboardService.GetAITranscriptReportsWithVerdict(DateTime.Now.AddDays(-30), DateTime.Now);
                        model = model.Select(x =>
                        {
                            x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                            return x;
                        }).ToList();
                        foreach (var item in model)
                        {

                            if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                            {
                                if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                                {
                                    item.MeetingStatusStatusDesc = "Status Pending";
                                }
                            }
                            else
                            {
                                item.MeetingStatusStatusDesc = string.Empty;

                            }
                        }
                        ViewBag.Pagetype = "";
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpGet]
        public async Task<IActionResult> AITranscriptVerdictsInterviewer(string interviewer, string Irfid)
        {
            try
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName?.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid?.Replace("\"", "");
                ViewBag.Profile = profileName;
                var model = await _dashboardService.GetAITranscriptReportsWithVerdictInterviewer(interviewer, Irfid);
                model = model.Select(x =>
                {
                    x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                    x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                    x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                    return x;
                }).ToList();
                foreach (var item in model)
                {
                    if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                    {
                        if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                        {
                            item.MeetingStatusStatusDesc = "Status Pending";
                        }
                    }
                    else
                    {
                        item.MeetingStatusStatusDesc = string.Empty;
                    }
                }
                ViewBag.Pagetype = "Public";
                return View("AITranscriptVerdicts", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TotalInterviews(string type)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");

                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "Quality Controller" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SupportAdmin" || profileName == "HR" || profileName == "Interviewer")
                    {
                        ViewBag.Profile = profileName;
                        DateTime startdate = DateTime.Now.AddMonths(-1);
                        DateTime enddate = DateTime.Now.AddDays(1);
                        var model = await _dashboardService.GetTotalInterviews(startdate, enddate);
                        if (string.IsNullOrWhiteSpace(type))
                        {
                            type = "Pending";
                        }
                        if (type == "UnderReview")
                        {
                            model = model.Where(x => x.RedMarkerStatus == 1).ToList();
                        }
                        else if (type == "Reviewed")
                        {
                            model = model.Where(x => x.RedMarkerStatus == 2).ToList();
                        }
                        else if (type == "Removed")
                        {
                            model = model.Where(x => x.RedMarkerStatus == 3 && !x.Isactive).ToList();
                        }
                        else if (type == "Pending")
                        {
                            var removedmodel = model.Where(x => x.RedMarkerStatus == 3 && !x.Isactive).ToList();
                            model = model.Where(x => x.RedMarkerStatus != 1 && x.RedMarkerStatus != 2).ToList();
                            model = model.Except(removedmodel);
                        }
                        ViewBag.Type = type;
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RedoInterviewsDetails()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    ViewBag.emailid = emailId;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Finance" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller")
                    {
                        ViewBag.profile = profileName;
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        List<MeetingVM> modelList = new List<MeetingVM>();
                        if (HttpContext.Session.GetString("Interviewfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("Interviewfilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                modelList = await _meetingService.GetAllMeetingList(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), emailId);
                            }
                            modelList = modelList.Select(x =>
                            {
                                x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                                //x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                                x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                                x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription();
                                //x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty;
                                return x;
                            }).ToList();

                            foreach (var item in modelList)
                            {
                                item.NextLevelApprovalStat = item.IsApproved.HasValue ? ((NextLevelApprovalStatus)item.IsApproved).GetDescription() : string.Empty;

                                if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                                {
                                    if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                                    {
                                        item.MeetingStatusStatusDesc = "Status Pending";

                                    }
                                    //else if (item.MeetingStatus.HasValue && (item.MeetingStatus.Value == (int)MeetingStatus.DroppedSelected || item.MeetingStatus.Value == (int)MeetingStatus.DroppedRejected || item.MeetingStatus.Value == (int)MeetingStatus.DroppedBorderLine))
                                    //{
                                    //    item.MeetingStatusStatusDesc = "Completed";
                                    //}
                                }
                                else
                                {
                                    item.MeetingStatusStatusDesc = string.Empty;

                                }

                                //interview status
                                if (item.InterviewStatus.HasValue)
                                {
                                    item.intervieweStatusStatDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                                }
                            }
                            //modelList.ForEach(x => x.NextLevelApprovalStat = x.IsApproved.HasValue ? ((NextLevelApprovalStatus)x.IsApproved).GetDescription() : string.Empty);

                            //filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.RFID = filter.RFID == null ? filter.RFID = modelList.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = modelList.Select(x => x.CandidateClientHR).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                            filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                            filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                            //filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;
                            //filter.ClientName = filter.ClientName == null ? filter.ClientName = modelList.Select(x => x.ClientName).ToArray() : filter.ClientName = filter.ClientName;
                            //filter.Client = filter.Client == null ? filter.Client = modelList.Select(x => x.Client).ToArray() : filter.Client = filter.Client;
                            //filter.IsCandidateResponsed = filter.IsCandidateResponsed == null ? filter.IsCandidateResponsed = modelList.Select(x => x.IsCandidateResponsed).ToArray() : filter.IsCandidateResponsed = filter.IsCandidateResponsed;
                            //filter.TemplateName = filter.TemplateName == null ? filter.TemplateName = modelList.Select(x => x.TemplateName).ToArray() : filter.TemplateName;
                            //filter.AadhaarVerified = filter.AadhaarVerified == null ? filter.AadhaarVerified = modelList.Select(x => x.AadhaarVerified).ToArray() : filter.AadhaarVerified = filter.AadhaarVerified;
                            //filter.ChannelOwner = filter.ChannelOwner == null ? filter.ChannelOwner = modelList.Select(x => x.ChannelOwner).ToArray() : filter.ChannelOwner;

                            modelList = modelList.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                            && filter.RFID.Contains(c.IRFId)
                            //&& ((filter.ClientHR != null && filter.ClientHR.Count() > 0) ? filter.ClientHR.Contains(c.CandidateClientHR) : c.CandidateClientHR == null)
                            //&& ((filter.ConfirmationStatus != null && filter.ConfirmationStatus.Count() > 0) ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                            && ((filter.MeetingStatus != null && filter.MeetingStatus.Count() > 0) ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                            && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                            //&& ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)
                            //&& ((filter.ClientName != null && filter.ClientName.Count() > 0) ? filter.ClientName.Contains(c.ClientName) : c.ClientName == null)
                            //&& ((filter.Client != null && filter.Client.Count() > 0) ? filter.Client.Contains(c.Client) : c.Client == null)
                            //&& ((filter.IsCandidateResponsed != null && filter.IsCandidateResponsed.Count() > 0) ? filter.IsCandidateResponsed.Contains(c.IsCandidateResponsed) : c.IsCandidateResponsed == false)
                            //&& ((filter.TemplateName != null && filter.TemplateName.Count() > 0) ? filter.TemplateName.Contains(c.TemplateName) : c.TemplateName == null)
                            //&& ((filter.AadhaarVerified != null && filter.AadhaarVerified.Count() > 0) ? filter.AadhaarVerified.Contains(c.AadhaarVerified) : c.AadhaarVerified == false)
                            && ((filter.ChannelOwner != null && filter.ChannelOwner.Count() > 0) ? filter.ChannelOwner.Contains(c.ChannelOwner) : c.ChannelOwner == null)).ToList();

                        }
                        else
                        {
                            var startDate = DateTime.Now.AddDays(-30);
                            var endDate = DateTime.Now.Date.AddDays(1);


                            modelList = await _meetingService.GetAllMeetingList(startDate, endDate, emailId);

                            modelList = modelList.Select(x =>
                            {
                                x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                                //x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; 
                                x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription();
                                //x.QCStatusStatusDesc = x.QCStatus.HasValue ? ((QCStatus)x.QCStatus).GetDescription() : string.Empty; 

                                return x;
                            }).ToList();
                            foreach (var item in modelList)
                            {
                                item.NextLevelApprovalStat = item.IsApproved.HasValue ? ((NextLevelApprovalStatus)item.IsApproved).GetDescription() : string.Empty;

                                if (item.confirmationStatus.Trim() != ReceiveMeetingStatus.Cancelled.GetDescription())
                                {
                                    if (item.interviewDate < DateTime.Now && item.MeetingStatusStatusDesc == Risebird.Entities.Enum.MeetingStatus.Scheduled.GetDescription())
                                    {
                                        item.MeetingStatusStatusDesc = "Status Pending";

                                    }
                                }
                                else
                                {
                                    item.MeetingStatusStatusDesc = string.Empty;

                                }

                                if (item.InterviewStatus.HasValue)
                                {
                                    item.intervieweStatusStatDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                                }
                            }

                            //modelList.ForEach(x => x.NextLevelApprovalStat = x.IsApproved.HasValue ? ((NextLevelApprovalStatus)x.IsApproved).GetDescription() : string.Empty);
                        }
                        ViewBag.Type = "RedoInterviews";
                        modelList = modelList.Where(x => x.RedoInterview == true).ToList();



                        return View("RedoInterviewsDetails", modelList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public async Task<JsonResult> UpdateNegativeAIVerdictStatusChange(int interviewid, int IRFid, int status, string Comment)
        {
            string emailId = string.Empty;
            if (HttpContext.Session.GetString("email") != null)
            {
                emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
            }
            var result = await _dashboardService.UpdateRedMarkeredInterviewerstatus(interviewid, IRFid, status, Comment, emailId, "NegativeAIVerdict", "NegativeAIVerdict");
            if (result)
            {
                return Json("Interviewer Moved Successfully.");
            }
            return Json("Fail to Move the Interviewer, Please try again.");
        }
        [HttpPost]
        public async Task<JsonResult> UpdateTotalInterviewInterviewerstatus(int interviewid, int IRFid, int status, string Comment)
        {
            string emailId = string.Empty;
            if (HttpContext.Session.GetString("email") != null)
            {
                emailId = HttpContext.Session.GetString("email")?.Trim();
                emailId = emailId.Replace("\"", "");
            }
            var result = await _dashboardService.UpdateRedMarkeredInterviewerstatus(interviewid, IRFid, status, Comment, emailId, "TotalInterview", "TotalInterview");
            if (result)
            {
                return Json("Interviewer Moved Successfully.");
            }
            return Json("Fail to Move the Interviewer, Please try again.");
        }
        /// <summary>
        /// QC page of a status decision: "PotentialRisk" (Potential Risk by Candidate), "HighRisk" (High-Risk Interviewers)
        /// or "PLI" (Process Lapse Indicators); case-insensitive. Anything else returns null (not a QC status page).
        /// </summary>
        internal static string NormalizeQcStatusSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) return null;
            var s = source.Trim();
            if (string.Equals(s, "PotentialRisk", StringComparison.OrdinalIgnoreCase)) return "PotentialRisk";
            if (string.Equals(s, "HighRisk", StringComparison.OrdinalIgnoreCase)) return "HighRisk";
            if (string.Equals(s, "PLI", StringComparison.OrdinalIgnoreCase)) return "PLI";
            return null;
        }

        /// <summary>
        /// Under review (1) / Reviewed (2) on an interviewer from Potential Risk by Candidate, High-Risk Interviewers or
        /// PLI. Stored as a RedMarkerStatusDetails row (Type = source) with the comment and the session user, counted on
        /// QC Activity under that page. Remove (3) goes through InactivateInterviewer with the same source.
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<JsonResult> UpdateQcInterviewerStatus(int interviewerUserId, int irfId, int status, string comment, string source)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { success = false, message = "Your session has expired. Please sign in again." });
                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsQcStatusProfile(profileName))
                    return Json(new { success = false, message = "Not authorized." });

                var normalizedSource = NormalizeQcStatusSource(source);
                if (normalizedSource == null)
                    return Json(new { success = false, message = "Unknown page." });
                if (interviewerUserId <= 0)
                    return Json(new { success = false, message = "Invalid interviewer." });
                if (status != 1 && status != 2)
                    return Json(new { success = false, message = "Invalid status." });
                if (string.IsNullOrWhiteSpace(comment))
                    return Json(new { success = false, message = "Please enter a comment." });

                var emailId = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? "";
                var trimmed = comment.Trim();
                if (trimmed.Length > 300) trimmed = trimmed.Substring(0, 300);
                var result = await _dashboardService.UpdateRedMarkeredInterviewerstatus(
                    interviewerUserId, Math.Max(0, irfId), status, trimmed, emailId, normalizedSource, normalizedSource);
                if (result)
                    return Json(new { success = true, message = status == 1 ? "Moved to Under Review." : "Marked as Reviewed." });
                return Json(new { success = false, message = "Fail to Move the Interviewer, Please try again." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "An error occurred while saving the status." });
            }
        }

        /// <summary>Profiles that may move interviewers on the QC status pages (Potential Risk, High-Risk, PLI).</summary>
        private static bool IsQcStatusProfile(string profileName) =>
            IsAutoQcOpsProfile(profileName);

        [HttpPost]
        public async Task<JsonResult> InactivateInterviewer(int interviewerId, string comment, bool blockinterviewer, string source = null)
        {
            try
            {
                string emailId = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                }

                // source (PotentialRisk | HighRisk | PLI): the removal is recorded for that page on QC Activity.
                var result = await _interviewerService.InactivateInterviewer(interviewerId, emailId, comment, blockinterviewer, null, NormalizeQcStatusSource(source));
                if (result)
                {
                    return Json(new { success = true, message = "Interviewer inactivated successfully." });
                }
                return Json(new { success = false, message = "Failed to inactivate interviewer. Please try again." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "An error occurred while inactivating the interviewer." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> ReactivateInterviewer(int userId)
        {
            try
            {
                string emailId = string.Empty;
                if (HttpContext.Session.GetString("email") != null)
                {
                    emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                }

                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "") ?? "";
                // Reactivating an inactivated interviewer is restricted to the Admin profile only.
                if (profileName != "Admin")
                    return Json(new { success = false, message = "You do not have permission to reactivate interviewers. Only an Admin can do this." });

                var result = await _interviewerService.ReactivateInterviewer(userId, emailId);
                if (result)
                    return Json(new { success = true, message = "Interviewer reactivated successfully." });

                return Json(new { success = false, message = "Failed to reactivate interviewer. Please try again." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "An error occurred while reactivating the interviewer." });
            }
        }

        /// <summary>
        /// Gets a list of all the mags that have unassigned interviews upcoming
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> UnassignedMags()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "Finance" || profileName == "SeniorHR" || profileName == "SeniorInterviewerManagement" || profileName == "InterviewerManagement" || profileName == "HR")
                    {
                        var magList = await _irfService.GetUnassignedInterviewsMAG();
                        return View(magList);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        /// <summary>
        /// AI Recruiter Copilot page
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> AIRecruiterCopilot()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View("Error"); // Return error view instead of redirecting to signout
            }
        }
        [HttpPost]
        public async Task<JsonResult> SubmitRecruiterCopilot(string Name, string Emailid, string Designation, string Phone, string Company, string Type)
        {
            try
            {
                var result = await _dashboardService.SubmitRecruiterCopilot(Name, Emailid, Designation, Phone, Company, Type);
                if (result)
                {
                    return Json("Thank you! Your request is registered successfully, We’ll contact you shortly.");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return Json("");
        }

        [HttpGet]
        public async Task<IActionResult> AITwinInterviewer(Guid? seId = null)
        {
            try
            {
                // Pass session ID via ViewBag if provided
                if (seId.HasValue)
                {
                    ViewBag.SessionId = seId.Value.ToString();
                }
                else
                {
                    // Optionally check session storage for session ID
                    var sessionIdFromSession = HttpContext.Session.GetString("AISessionId");
                    if (!string.IsNullOrEmpty(sessionIdFromSession))
                    {
                        ViewBag.SessionId = sessionIdFromSession;
                    }
                }

                return View();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View("Error"); // Return error view instead of redirecting to signout
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Landing()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");

                    var model = new DashboardVM();
                    model.NotUpdatedMeetingsObj = await _dashboardService.GetNotUpdatedMeetingsCount(emailid);

                    ViewBag.ProfileName = profileName;
                    ViewBag.IsClientUser = false;

                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Error retrieving data! Please try again.");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> HrScreenEvaluator()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    return View();
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        /// <summary>
        /// Ops monitor for automated QC verdict removals — forward trigger + 60-day backfill.
        /// Trigger: QcInterviewAIMergeJobService.ProcessRowAsync after usp_SaveQcInterviewAIMergedDetails.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AutoRemovalMonitor(DateTime? from = null, DateTime? to = null, string preset = null, int? expandIrfId = null, string view = null, int needsCall = 0)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return RedirectToAction("SignOut", "Session");

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsAutoQcOpsProfile(profileName))
                    return RedirectToAction("SignOut", "Session");

                var (fromIst, toIst, activePreset) = NormalizeDirectoryIstDateRange(from, to, preset);
                var sessionEmail = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? "";

                IReadOnlyList<IrfAuditDirectoryRowVm> directory;
                try
                {
                    // HR/SeniorHR are scoped server-side to IRFs they own (Channel Owner); other ops profiles see all.
                    var ownerEmailFilter = IsIrfAuditOwnerProfile(profileName) ? sessionEmail : null;
                    directory = await _autoQcBackfillService.GetIrfAuditDirectoryAsync(fromIst, toIst, ownerEmailFilter, profileName);
                    ViewBag.DirectoryError = null;
                }
                catch (Exception loadEx)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, loadEx);
                    directory = Array.Empty<IrfAuditDirectoryRowVm>();
                    ViewBag.DirectoryError = loadEx.GetBaseException()?.Message ?? loadEx.Message;
                }

                ViewBag.Profile = profileName;
                ViewBag.DirectoryFromIst = fromIst.ToString("yyyy-MM-dd");
                ViewBag.DirectoryToIst = toIst.ToString("yyyy-MM-dd");
                ViewBag.DirectoryPreset = activePreset;
                ViewBag.ExpandIrfId = expandIrfId.GetValueOrDefault() > 0 ? expandIrfId : null;
                // Three levels (QC Tracking Delta 4): Channels (IRF), High-Risk Interviewers (overall), Reports (report).
                ViewBag.ActiveAuditView = string.Equals(view, "interviewers", StringComparison.OrdinalIgnoreCase)
                    ? "interviewers"
                    : string.Equals(view, "reports", StringComparison.OrdinalIgnoreCase)
                        ? "reports"
                        : "channels";
                // "Needs a call" filter on the Interviewers tab (Delta 6; the Interviewer QC View menu link lands here).
                ViewBag.NeedsCall = needsCall == 1;
                return View(directory);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        /// <summary>
        /// Central trial-flag review queue — SMEs/ops triage first-interview trial outcomes per (interviewer, IRF).
        /// Consumer for rows written to dbo.TrialFlags by the trial judgment producer.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TrialFlagReview(string tab = null, bool refresh = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return RedirectToAction("SignOut", "Session");

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsTrialFlagReviewerProfile(profileName))
                    return RedirectToAction("SignOut", "Session");

                TrialFlagReviewPageVm page;
                try
                {
                    // Shell only — grid loads via TrialFlagReviewList (lazy two-tier read).
                    page = await _trialFlagReviewService.GetPageShellAsync(tab);
                    ViewBag.LoadError = null;
                }
                catch (Exception loadEx)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, loadEx);
                    page = new TrialFlagReviewPageVm { ActiveTab = TrialFlagConstants.ReviewPending };
                    ViewBag.LoadError = loadEx.GetBaseException()?.Message ?? loadEx.Message;
                }

                ViewBag.Profile = profileName;
                return View(page);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        /// <summary>
        /// AJAX list — light List proc only (no JSON / PaymentSLA / QC-merge).
        /// Request/response uses DataTables server-side fields (draw/start/length/records*).
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TrialFlagReviewList(
            string tab = null,
            int draw = 1,
            int start = 0,
            int length = 25,
            string search = null,
            string severity = null,
            string channelOwner = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { draw, recordsTotal = 0, recordsFiltered = 0, data = Array.Empty<object>(), error = "Unauthorized" });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsTrialFlagReviewerProfile(profileName))
                    return Json(new { draw, recordsTotal = 0, recordsFiltered = 0, data = Array.Empty<object>(), error = "Not authorized." });

                // DataTables nested search[value]
                if (string.IsNullOrWhiteSpace(search))
                    search = Request.Query["search[value]"].FirstOrDefault();

                var result = await _trialFlagReviewService.GetListAsync(new TrialFlagReviewListRequest
                {
                    Tab = tab,
                    Offset = start,
                    PageSize = length <= 0 ? 25 : length,
                    Search = search,
                    Severity = severity,
                    ChannelOwner = channelOwner,
                    Draw = draw
                });

                return Json(new
                {
                    draw = result.Draw,
                    recordsTotal = result.RecordsTotal,
                    recordsFiltered = result.RecordsFiltered,
                    data = result.Data,
                    pendingCount = result.PendingCount,
                    keptCount = result.KeptCount,
                    removedCount = result.RemovedCount,
                    ownerSummaries = result.OwnerSummaries,
                    error = result.Error
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new
                {
                    draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = Array.Empty<object>(),
                    error = ex.GetBaseException()?.Message ?? "List failed. Deploy usp_GetTrialFlagsForReview_List."
                });
            }
        }

        /// <summary>On-demand heavy row detail — usp_GetTrialFlagDetail.</summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TrialFlagReviewDetail(int trialFlagId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Unauthorized" });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsTrialFlagReviewerProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                if (trialFlagId <= 0)
                    return Json(new { ok = false, message = "Invalid trial flag." });

                var detail = await _trialFlagReviewService.GetDetailAsync(trialFlagId);
                if (detail == null)
                    return Json(new { ok = false, message = "Trial flag not found." });

                return Json(new { ok = true, detail });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Detail failed. Deploy usp_GetTrialFlagDetail." });
            }
        }

        /// <summary>
        /// AI evaluation summary for a trial triggering interview (Report / Transcript / Contradiction / Merge).
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewAiRiskSummary(int interviewId, int? irfId = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Unauthorized" });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsTrialFlagReviewerProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                if (interviewId <= 0)
                    return Json(new { ok = false, message = "Invalid interview." });

                var rows = await _trialFlagReviewService.GetInterviewAiRiskSummaryAsync(interviewId);
                return Json(new
                {
                    ok = true,
                    interviewId,
                    irfId,
                    rows
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Failed to load AI risk summary. Deploy usp_GetInterviewAiRiskSummary." });
            }
        }

        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> KeepTrialFlagReviewer(int trialFlagId, string comment = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Session expired." });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsTrialFlagReviewerProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                var email = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? "";
                var result = await _trialFlagReviewService.KeepOnIrfAsync(trialFlagId, email, comment);
                return Json(new { ok = result.Ok, message = result.Message });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Keep action failed." });
            }
        }

        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RemoveTrialFlagReviewer(int trialFlagId, string comment = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Session expired." });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsTrialFlagReviewerProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                var email = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? "";
                var result = await _trialFlagReviewService.RemoveFromIrfAsync(trialFlagId, email, comment);
                return Json(new { ok = result.Ok, message = result.Message });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Remove action failed." });
            }
        }

        /// <summary>
        /// Backfill TrialFlags for first interviews that already have QC merge done (QCDone=1) but no flag row.
        /// Omit interviewId to process the next batch of eligible candidates.
        /// </summary>
        [Authorize]
        [HttpPost]
        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> BackfillTrialFlags(int? interviewId = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Session expired." });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsTrialFlagReviewerProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                var result = await _trialJudgmentService.BackfillMissingTrialFlagsAsync(interviewId);
                // QC Activity: record who ran the backfill (TrialFlagBackfillRun -> metric TrialBackfill).
                var runBy = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? profileName;
                await _trialJudgmentService.RecordBackfillRunAsync(runBy, interviewId, result);
                var details = result.Messages?.Take(30).ToList() ?? new List<string>();
                var summary = result.Failed > 0 && result.Inserted == 0 && result.Skipped == 0
                    ? (details.FirstOrDefault() ?? $"Backfill failed ({result.Failed} error(s)).")
                    : $"Inserted {result.Inserted}, skipped {result.Skipped}, failed {result.Failed}.";
                if (result.Inserted == 0 && result.Skipped > 0)
                {
                    var skipSummary = details.FirstOrDefault(d =>
                        d.StartsWith("Skip summary:", StringComparison.OrdinalIgnoreCase));
                    var firstSkip = details.FirstOrDefault(d =>
                        d.StartsWith("Skipped interview ", StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(skipSummary))
                        summary += " " + skipSummary;
                    else if (!string.IsNullOrEmpty(firstSkip))
                        summary += " Example: " + firstSkip;
                }
                return Json(new
                {
                    ok = result.Failed == 0,
                    inserted = result.Inserted,
                    skipped = result.Skipped,
                    failed = result.Failed,
                    message = summary,
                    details
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var msg = ex.GetBaseException()?.Message ?? ex.Message;
                return Json(new { ok = false, message = "Backfill failed: " + msg });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetPerIrfAutoQcBackfillPreview(int irfId, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Unauthorized" });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsAutoQcOpsProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                if (irfId <= 0)
                    return Json(new { ok = false, message = "Invalid IRF." });

                var (fromIst, toIst) = NormalizeBackfillIstDateRange(from, to);
                var preview = await _autoQcBackfillService.GetPerIrfBackfillPreviewAsync(irfId, fromIst, toIst);
                if (!string.IsNullOrWhiteSpace(preview.ErrorMessage))
                    return Json(new { ok = false, message = preview.ErrorMessage });

                return Json(new
                {
                    ok = true,
                    irfId,
                    verdictCount = preview.VerdictCount,
                    wouldFlagCount = preview.WouldFlagCount,
                    alreadyFlaggedCount = preview.AlreadyFlaggedCount,
                    unmappedVerdictCount = preview.UnmappedVerdictCount,
                    qcMergedInRangeCount = preview.QcMergedInRangeCount,
                    oldestVerdictDate = preview.OldestVerdictDate?.ToString("yyyy-MM-dd"),
                    newestVerdictDate = preview.NewestVerdictDate?.ToString("yyyy-MM-dd"),
                    fromIst = fromIst.ToString("yyyy-MM-dd"),
                    toIst = toIst.ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Failed to load preview count." });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RunPerIrfAutoQcBackfill(int irfId, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Unauthorized" });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsAutoQcOpsProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                var email = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? profileName;
                var (fromIst, toIst) = NormalizeBackfillIstDateRange(from, to);
                var result = await _autoQcBackfillService.RunPerIrfBackfillAsync(irfId, fromIst, toIst, email);
                return Json(new
                {
                    ok = result.Ok,
                    message = result.Message,
                    runId = result.RunId,
                    verdictsScanned = result.VerdictsScanned,
                    newFlagsSet = result.NewFlagsSet,
                    alreadyFlaggedSkipped = result.AlreadyFlaggedSkipped
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Backfill failed; see logs." });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> IrfAuditInterviewerRisk(int irfId, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Unauthorized" });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsAutoQcOpsProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                if (irfId <= 0)
                    return Json(new { ok = false, message = "Invalid IRF." });

                var email = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? "";
                if (!await CanActOnIrfAsync(irfId, email, profileName))
                    return Json(new { ok = false, message = "You are not the Channel Owner for this IRF." });

                var (fromIst, toIst, _) = NormalizeDirectoryIstDateRange(from, to, null);
                var breakdown = await _autoQcBackfillService.GetIrfAuditInterviewerRiskBreakdownAsync(irfId, fromIst, toIst);
                IReadOnlyList<IrfAuditInterviewerRiskDetailVm> details;
                try
                {
                    details = await _autoQcBackfillService.GetIrfAuditInterviewerRiskInterviewDetailsAsync(irfId, fromIst, toIst);
                }
                catch
                {
                    details = Array.Empty<IrfAuditInterviewerRiskDetailVm>();
                }

                IrfAuditWindowQualityVm quality;
                try
                {
                    quality = await _autoQcBackfillService.GetIrfAuditWindowQualityAsync(irfId, fromIst, toIst);
                }
                catch
                {
                    quality = null;
                }

                return Json(new
                {
                    ok = true,
                    irfId,
                    from = fromIst.ToString("yyyy-MM-dd"),
                    to = toIst.ToString("yyyy-MM-dd"),
                    rows = breakdown.Active,
                    removed = breakdown.Removed,
                    details,
                    quality
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var detail = ex.GetBaseException()?.Message ?? ex.Message;
                return Json(new
                {
                    ok = false,
                    message = "Could not load interviewer risk breakdown. " + detail
                        + " — Redeploy Risebird.DB/StoredProcedures/usp_GetIrfAuditInterviewerRiskBreakdown.sql on this database."
                });
            }
        }

        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RefreshIrfRiskCategory(int irfId, DateTime? from = null, DateTime? to = null, bool ignoreMassFireGuard = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Unauthorized" });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsAutoQcOpsProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                if (irfId <= 0)
                    return Json(new { ok = false, message = "Invalid IRF." });

                var email = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? profileName;
                if (!await CanActOnIrfAsync(irfId, email, profileName))
                    return Json(new { ok = false, message = "You are not the Channel Owner for this IRF." });

                var (fromIst, toIst, _) = NormalizeDirectoryIstDateRange(from, to, null);
                var result = await _autoQcBackfillService.BackfillRiskCategoryForIrfAsync(irfId, fromIst, toIst, ignoreMassFireGuard, email);
                return Json(new
                {
                    ok = result.Ok,
                    message = result.Message,
                    inserted = result.Processed,
                    remaining = result.Remaining,
                    aborted = result.Aborted,
                    abortedCheckName = result.AbortedCheckName,
                    abortedFireRatePct = result.AbortedFireRatePct,
                    scanned = result.Scanned,
                    processed = result.Processed,
                    insertedCount = result.Inserted,
                    updated = result.Updated,
                    deleted = result.Deleted,
                    trialFlagsInserted = result.TrialFlagsInserted,
                    syncUpdates = result.SyncUpdates,
                    skipped = result.Skipped,
                    skipReasons = result.SkipReasons,
                    fromIst = result.FromIst?.ToString("yyyy-MM-dd"),
                    toIst = result.ToIst?.ToString("yyyy-MM-dd"),
                    lastBackfillLabel = result.LastBackfillLabel
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Risk refresh failed. Deploy usp_GetRiskCategoryBackfillCandidatesByIrf and usp_AddUpdateRiskCategory." });
            }
        }

        /// <summary>Prefix that marks an IRF audit Keep / Remove made from High-Risk Interviewers (read by usp_QCActivity_CollectRows).</summary>
        internal const string HighRiskReasonPrefix = "[High-Risk]";

        /// <summary>
        /// fromPage = HighRisk: "[High-Risk] " + the risk summary (or the default reason). Any other page: unchanged.
        /// </summary>
        internal static string TagRiskAuditReasonForPage(string riskSummary, string fromPage, string defaultReason)
        {
            if (!string.Equals(fromPage?.Trim(), "HighRisk", StringComparison.OrdinalIgnoreCase))
                return riskSummary;
            var reason = string.IsNullOrWhiteSpace(riskSummary) ? defaultReason : riskSummary.Trim();
            if (reason.StartsWith(HighRiskReasonPrefix, StringComparison.Ordinal))
                return reason;
            return HighRiskReasonPrefix + " " + reason;
        }

        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RemoveInterviewerFromIrfRiskAudit(
            int transInterviewerId,
            int irfId,
            string comment = null,
            string riskSummary = null,
            int? worstSeverityInterviewId = null,
            string fromPage = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Unauthorized" });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsAutoQcOpsProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                var email = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? profileName;
                if (!await CanActOnIrfAsync(irfId, email, profileName))
                    return Json(new { ok = false, message = "You are not the Channel Owner for this IRF." });

                // From the High-Risk page (View expansion): the reason starts with [High-Risk] so QC Activity counts it
                // under High-Risk; the IRF Audit tab keeps the plain reason (IRF audit column).
                riskSummary = TagRiskAuditReasonForPage(riskSummary, fromPage, "Risk audit removal");
                var result = await _autoQcRemovalService.RemoveForRiskAuditAsync(
                    transInterviewerId,
                    irfId,
                    email,
                    riskSummary,
                    worstSeverityInterviewId,
                    comment);
                return Json(new { ok = result.Ok, message = result.Message });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Remove failed." });
            }
        }

        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> KeepInterviewerOnIrfRiskAudit(
            int transInterviewerId,
            int irfId,
            string comment = null,
            string riskSummary = null,
            int? worstSeverityInterviewId = null,
            string fromPage = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Unauthorized" });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsAutoQcOpsProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                var email = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? profileName;
                if (!await CanActOnIrfAsync(irfId, email, profileName))
                    return Json(new { ok = false, message = "You are not the Channel Owner for this IRF." });

                // From the High-Risk page (View expansion): the reason starts with [High-Risk] so QC Activity counts it
                // under High-Risk; the IRF Audit tab keeps the plain reason (IRF audit column).
                riskSummary = TagRiskAuditReasonForPage(riskSummary, fromPage, "Risk audit keep");
                var result = await _autoQcRemovalService.KeepForRiskAuditAsync(
                    transInterviewerId,
                    irfId,
                    email,
                    riskSummary,
                    worstSeverityInterviewId,
                    comment);
                return Json(new { ok = result.Ok, message = result.Message });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Keep failed." });
            }
        }

        /// <summary>
        /// High-Risk Interviewers overview — server-paginated, owner-scoped for HR/SeniorHR.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerRiskOverview(
            DateTime? from = null,
            DateTime? to = null,
            bool showAll = false,
            bool pendingReviewOnly = false,
            int offset = 0,
            int pageSize = 25,
            [FromServices] DeductionReviewSettings deductionReviewSettings = null,
            string sortBy = null,
            bool needsCall = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Unauthorized" });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsAutoQcOpsProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                var (fromIst, toIst, _) = NormalizeDirectoryIstDateRange(from, to, null);
                var sessionEmail = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? "";
                var ownerEmail = IsIrfAuditOwnerProfile(profileName) ? sessionEmail : null;
                decimal? minRatio = showAll ? (decimal?)null : DmIvAutoQcAuditConstants.IrfAuditOverviewDefaultMinRiskRatio;

                // sortBy: Impact (default, decision D1) or Ratio; needsCall: High-Risk interviewers with no connected call.
                var page = await _autoQcBackfillService.GetInterviewerRiskOverviewAsync(
                    fromIst, toIst, ownerEmail, minRatio, pendingReviewOnly, offset, pageSize, sortBy, needsCall);
                // Deduction review: upheld rate per AI check for the header (labels + "consider taking out" flag).
                if (page.Summary?.ReviewChecks != null)
                    page.Summary.ReviewChecks = Risebird.Business.DeductionReviewService
                        .BuildCheckStats(page.Summary.ReviewChecks, deductionReviewSettings).ToList();

                return Json(new
                {
                    ok = true,
                    from = fromIst.ToString("yyyy-MM-dd"),
                    to = toIst.ToString("yyyy-MM-dd"),
                    offset = page.Offset,
                    pageSize = page.PageSize,
                    totalRows = page.TotalRows,
                    summary = page.Summary,
                    rows = page.Rows,
                    removeRatio = DmIvAutoQcAuditConstants.IrfAuditRemoveRatio,
                    defaultMinRiskRatio = DmIvAutoQcAuditConstants.IrfAuditOverviewDefaultMinRiskRatio
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var detail = ex.GetBaseException()?.Message ?? ex.Message;
                return Json(new
                {
                    ok = false,
                    message = "Could not load interviewer risk overview. " + detail
                        + " — Deploy Risebird.DB/StoredProcedures/usp_GetInterviewerRiskOverview.sql."
                });
            }
        }

        /// <summary>Parses "1,2,3" into distinct positive ids (at most <paramref name="max"/>).</summary>
        internal static List<int> ParseIdList(string ids, int max = 200) =>
            (ids ?? "")
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x.Trim(), out var n) ? n : 0)
                .Where(n => n > 0)
                .Distinct()
                .Take(max)
                .ToList();

        /// <summary>
        /// QC coverage block on each High-Risk interviewer row (QC Tracking Delta 2): last QC touch, last connected
        /// call, touches in 30 days and open items, for up to 200 interviewers (usp_GetInterviewerQcCoverage, 90 days).
        /// The list never waits for this call; available = false when the procedure is missing (the block stays hidden).
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerRiskCoverage(string ids)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Unauthorized" });
                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsAutoQcOpsProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                var idList = ParseIdList(ids);
                if (idList.Count == 0)
                    return Json(new { ok = true, available = true, rows = Array.Empty<InterviewerQcCoverageRowVm>() });

                var rows = await _autoQcBackfillService.GetInterviewerQcCoverageAsync(idList, 90);
                if (rows == null)
                    return Json(new { ok = true, available = false, rows = Array.Empty<InterviewerQcCoverageRowVm>() });
                return Json(new { ok = true, available = true, lookbackDays = 90, rows });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, available = false, message = "Could not load QC coverage." });
            }
        }

        /// <summary>
        /// High-Risk Interviewers, Reports tab (QC Tracking Delta 4): one row per graded report (P0-P5) in the window,
        /// paged at 50 server-side; worst and unreviewed first. Filters: see <see cref="HighRiskReportFilters"/>.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> HighRiskReports(
            DateTime? from = null,
            DateTime? to = null,
            string filter = null,
            int? interviewerId = null,
            int? irfId = null,
            int offset = 0,
            int pageSize = 50)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Unauthorized" });
                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsAutoQcOpsProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                var (fromIst, toIst, _) = NormalizeDirectoryIstDateRange(from, to, null);
                var sessionEmail = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? "";
                var ownerEmail = IsIrfAuditOwnerProfile(profileName) ? sessionEmail : null;

                var page = await _autoQcBackfillService.GetHighRiskReportsAsync(
                    fromIst, toIst, ownerEmail, filter, interviewerId, irfId, offset, pageSize);
                foreach (var row in page.Rows)
                {
                    if (row.InterviewStatus.HasValue && Enum.IsDefined(typeof(InterviewStatus), row.InterviewStatus.Value))
                        row.InterviewStatusText = ((InterviewStatus)row.InterviewStatus.Value).GetDescription();
                }
                return Json(new
                {
                    ok = true,
                    available = page.Available,
                    from = fromIst.ToString("yyyy-MM-dd"),
                    to = toIst.ToString("yyyy-MM-dd"),
                    filter = HighRiskReportFilters.Normalize(filter),
                    offset = page.Offset,
                    pageSize = page.PageSize,
                    totalRows = page.TotalRows,
                    rows = page.Rows,
                    canQcForm = profileName == "Quality Controller" || profileName == "Admin" || profileName == "SupportAdmin"
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var detail = ex.GetBaseException()?.Message ?? ex.Message;
                return Json(new
                {
                    ok = false,
                    message = "Could not load reports. " + detail + " — Deploy Risebird.DB/StoredProcedures/usp_GetHighRiskReports.sql."
                });
            }
        }

        /// <summary>
        /// PLI / PQI signals for the listed interviewers in the window (QC Tracking Delta 4 / 5): per interviewer (the
        /// Process line on the Interviewers tab, rates recomputed from totals) and per channel.
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerProcessSignals(string ids, DateTime? from = null, DateTime? to = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Unauthorized" });
                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsAutoQcOpsProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                var (fromIst, toIst, _) = NormalizeDirectoryIstDateRange(from, to, null);
                var sessionEmail = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? "";
                var ownerEmail = IsIrfAuditOwnerProfile(profileName) ? sessionEmail : null;

                var result = await _autoQcBackfillService.GetInterviewerProcessSignalsAsync(ParseIdList(ids), fromIst, toIst, ownerEmail);
                return Json(new
                {
                    ok = true,
                    available = result.Available,
                    from = fromIst.ToString("yyyy-MM-dd"),
                    to = toIst.ToString("yyyy-MM-dd"),
                    interviewers = result.Interviewers,
                    channels = result.Channels
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, available = false, message = "Could not load process signals." });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerRiskOverviewChannels(
            int interviewerId,
            DateTime? from = null,
            DateTime? to = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return Json(new { ok = false, message = "Unauthorized" });

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsAutoQcOpsProfile(profileName))
                    return Json(new { ok = false, message = "Not authorized." });

                if (interviewerId <= 0)
                    return Json(new { ok = false, message = "Invalid interviewer." });

                var (fromIst, toIst, _) = NormalizeDirectoryIstDateRange(from, to, null);
                var sessionEmail = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? "";
                var ownerEmail = IsIrfAuditOwnerProfile(profileName) ? sessionEmail : null;

                var rows = await _autoQcBackfillService.GetInterviewerRiskOverviewChannelsAsync(
                    interviewerId, fromIst, toIst, ownerEmail);

                return Json(new
                {
                    ok = true,
                    interviewerId,
                    from = fromIst.ToString("yyyy-MM-dd"),
                    to = toIst.ToString("yyyy-MM-dd"),
                    rows
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var detail = ex.GetBaseException()?.Message ?? ex.Message;
                return Json(new
                {
                    ok = false,
                    message = "Could not load channel breakdown. " + detail
                        + " — Deploy Risebird.DB/StoredProcedures/usp_GetInterviewerRiskOverviewChannels.sql."
                });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerRiskOverviewExport(
            DateTime? from = null,
            DateTime? to = null,
            bool showAll = false,
            bool pendingReviewOnly = false,
            string sortBy = null,
            bool needsCall = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return RedirectToAction("SignOut", "Session");

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (!IsAutoQcOpsProfile(profileName))
                    return RedirectToAction("SignOut", "Session");

                var (fromIst, toIst, _) = NormalizeDirectoryIstDateRange(from, to, null);
                var sessionEmail = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "") ?? "";
                var ownerEmail = IsIrfAuditOwnerProfile(profileName) ? sessionEmail : null;
                decimal? minRatio = showAll ? (decimal?)null : DmIvAutoQcAuditConstants.IrfAuditOverviewDefaultMinRiskRatio;

                var overviewRows = await _autoQcBackfillService.GetInterviewerRiskOverviewExportRowsAsync(
                    fromIst, toIst, ownerEmail, minRatio, pendingReviewOnly, sortBy, needsCall);

                var channelExport = new List<(InterviewerRiskOverviewRowVm Iv, InterviewerRiskOverviewChannelRowVm Ch)>();
                foreach (var iv in overviewRows)
                {
                    var ch = await _autoQcBackfillService.GetInterviewerRiskOverviewChannelsAsync(
                        iv.InterviewerId, fromIst, toIst, ownerEmail);
                    foreach (var c in ch)
                        channelExport.Add((iv, c));
                }

                ExcelPackage Ep = new ExcelPackage();
                var sheet1 = Ep.Workbook.Worksheets.Add("High-Risk Interviewers");
                sheet1.Cells[1, 1].Value = "InterviewerId";
                sheet1.Cells[1, 2].Value = "CodedId";
                sheet1.Cells[1, 3].Value = "Name";
                sheet1.Cells[1, 4].Value = "Email";
                sheet1.Cells[1, 5].Value = "Mobile";
                sheet1.Cells[1, 6].Value = "ChannelsActive";
                sheet1.Cells[1, 7].Value = "TotalCompleted";
                sheet1.Cells[1, 8].Value = "TotalBad";
                sheet1.Cells[1, 9].Value = "OverallRiskRatio";
                sheet1.Cells[1, 10].Value = "WorstSeverity";
                sheet1.Cells[1, 11].Value = "PriorityTier";
                sheet1.Cells[1, 12].Value = "HighRiskChannels";
                sheet1.Cells[1, 13].Value = "PendingReviewChannels";
                sheet1.Cells[1, 14].Value = "CoverageIncomplete";
                sheet1.Cells[1, 15].Value = "P0";
                sheet1.Cells[1, 16].Value = "P1";
                sheet1.Cells[1, 17].Value = "P2";
                sheet1.Cells[1, 18].Value = "P3";
                sheet1.Cells[1, 19].Value = "P4";
                sheet1.Cells[1, 20].Value = "P5";
                sheet1.Cells[1, 21].Value = "WindowFromIst";
                sheet1.Cells[1, 22].Value = "WindowToIst";
                // AI penalties applied automatically (window), profile, lifetime (usp_GetInterviewerRiskOverview).
                var extraHeaders = new[]
                {
                    "AiPenalties", "AiAutoApproved", "AiCheckedByQc", "AiAvgPct",
                    "Resume", "Company", "Acknowledgement", "VideoKyc", "PaymentOnHold", "PortalAccess", "ActiveSuspicious",
                    "LifetimeInterviews", "LifetimeCompleted", "LifetimeSelected", "LifetimeRejected",
                    "InterviewerNoShows", "CandidateNoShows", "RedoRequests", "AiTranscriptFlagged",
                    "LateJoins", "Penalties", "ReliabilityScore",
                    // QC Tracking (appended): Impact, Removed-from, Trend, High-Risk decision
                    "Impact", "ChannelsRemoved", "ChannelsTotal", "BadPct", "PrevBadPct", "Trend",
                    "HighRiskStatus", "HighRiskStatusBy", "HighRiskStatusOn"
                };
                for (var h = 0; h < extraHeaders.Length; h++)
                    sheet1.Cells[1, 23 + h].Value = extraHeaders[h];
                sheet1.Cells[1, 1, 1, 22 + extraHeaders.Length].Style.Font.Bold = true;

                var r = 2;
                foreach (var item in overviewRows)
                {
                    sheet1.Cells[r, 1].Value = item.InterviewerId;
                    sheet1.Cells[r, 2].Value = item.CodedId;
                    sheet1.Cells[r, 3].Value = item.InterviewerName;
                    sheet1.Cells[r, 4].Value = item.InterviewerEmail;
                    sheet1.Cells[r, 5].Value = item.MobileNumber;
                    sheet1.Cells[r, 6].Value = item.ChannelsActive;
                    sheet1.Cells[r, 7].Value = item.TotalCompleted;
                    sheet1.Cells[r, 8].Value = item.TotalBad;
                    sheet1.Cells[r, 9].Value = item.OverallRiskRatio;
                    sheet1.Cells[r, 10].Value = item.WorstSeverity;
                    sheet1.Cells[r, 11].Value = item.PriorityTier;
                    sheet1.Cells[r, 12].Value = item.HighRiskChannels;
                    sheet1.Cells[r, 13].Value = item.PendingReviewChannels;
                    sheet1.Cells[r, 14].Value = item.CoverageIncomplete ? "Yes" : "No";
                    sheet1.Cells[r, 15].Value = item.P0Count;
                    sheet1.Cells[r, 16].Value = item.P1Count;
                    sheet1.Cells[r, 17].Value = item.P2Count;
                    sheet1.Cells[r, 18].Value = item.P3Count;
                    sheet1.Cells[r, 19].Value = item.P4Count;
                    sheet1.Cells[r, 20].Value = item.P5Count;
                    sheet1.Cells[r, 21].Value = fromIst.ToString("yyyy-MM-dd");
                    sheet1.Cells[r, 22].Value = toIst.ToString("yyyy-MM-dd");
                    sheet1.Cells[r, 23].Value = item.AiPenalised;
                    sheet1.Cells[r, 24].Value = item.AiAutoApproved;
                    sheet1.Cells[r, 25].Value = item.AiHumanReviewed;
                    sheet1.Cells[r, 26].Value = item.AiAvgPct;
                    sheet1.Cells[r, 27].Value = item.ResumeLink;
                    sheet1.Cells[r, 28].Value = item.CompanyName;
                    sheet1.Cells[r, 29].Value = item.AcknowledgementStatus;
                    sheet1.Cells[r, 30].Value = item.IsVideoApproved ? "Verified" : (item.HasVideoKyc ? "Pending" : "Missing");
                    sheet1.Cells[r, 31].Value = item.IsHoldPayment ? "Yes" : "No";
                    sheet1.Cells[r, 32].Value = item.HasPortalAccess ? "Yes" : "No";
                    sheet1.Cells[r, 33].Value = item.ActiveSuspicious ? "Yes" : "No";
                    sheet1.Cells[r, 34].Value = item.LifetimeInterviews;
                    sheet1.Cells[r, 35].Value = item.LifetimeCompleted;
                    sheet1.Cells[r, 36].Value = item.LifetimeSelected;
                    sheet1.Cells[r, 37].Value = item.LifetimeRejected;
                    sheet1.Cells[r, 38].Value = item.InterviewerNoShows;
                    sheet1.Cells[r, 39].Value = item.CandidateNoShows;
                    sheet1.Cells[r, 40].Value = item.RedoRequests;
                    sheet1.Cells[r, 41].Value = item.AiTranscriptFlagged;
                    sheet1.Cells[r, 42].Value = item.LateJoins;
                    sheet1.Cells[r, 43].Value = item.Penalties;
                    sheet1.Cells[r, 44].Value = item.ReliabilityScore;
                    sheet1.Cells[r, 45].Value = item.Impact;
                    sheet1.Cells[r, 46].Value = item.ChannelsRemoved;
                    sheet1.Cells[r, 47].Value = item.ChannelsTotal;
                    sheet1.Cells[r, 48].Value = item.BadPct;
                    sheet1.Cells[r, 49].Value = item.PrevBadPct;
                    sheet1.Cells[r, 50].Value = item.Trend;
                    sheet1.Cells[r, 51].Value = item.HighRiskStatus == 1 ? "Under review" : item.HighRiskStatus == 2 ? "Reviewed" : item.HighRiskStatus == 3 ? "Removed" : "";
                    sheet1.Cells[r, 52].Value = item.HighRiskStatusBy;
                    sheet1.Cells[r, 53].Value = item.HighRiskStatusOn?.ToString("yyyy-MM-dd HH:mm");
                    r++;
                }

                var sheet2 = Ep.Workbook.Worksheets.Add("Per-Channel Breakdown");
                sheet2.Cells[1, 1].Value = "InterviewerId";
                sheet2.Cells[1, 2].Value = "InterviewerEmail";
                sheet2.Cells[1, 3].Value = "IrfId";
                sheet2.Cells[1, 4].Value = "IrfName";
                sheet2.Cells[1, 5].Value = "ChannelOwner";
                sheet2.Cells[1, 6].Value = "Completed";
                sheet2.Cells[1, 7].Value = "Bad";
                sheet2.Cells[1, 8].Value = "RiskRatio";
                sheet2.Cells[1, 9].Value = "Recommendation";
                sheet2.Cells[1, 10].Value = "ReviewState";
                sheet2.Cells[1, 11].Value = "P0";
                sheet2.Cells[1, 12].Value = "P1";
                sheet2.Cells[1, 13].Value = "P2";
                sheet2.Cells[1, 14].Value = "P3";
                sheet2.Cells[1, 15].Value = "P4";
                sheet2.Cells[1, 16].Value = "P5";
                sheet2.Cells[1, 17].Value = "LastBackfillAtUtc";
                sheet2.Cells[1, 18].Value = "LastBackfillFromIst";
                sheet2.Cells[1, 19].Value = "LastBackfillToIst";
                sheet2.Cells[1, 20].Value = "CoverageIncomplete";
                sheet2.Cells[1, 21].Value = "TransInterviewerId";
                sheet2.Cells["A1:U1"].Style.Font.Bold = true;

                r = 2;
                foreach (var pair in channelExport)
                {
                    var item = pair.Iv;
                    var c = pair.Ch;
                    sheet2.Cells[r, 1].Value = item.InterviewerId;
                    sheet2.Cells[r, 2].Value = item.InterviewerEmail;
                    sheet2.Cells[r, 3].Value = c.IrfId;
                    sheet2.Cells[r, 4].Value = c.IrfName;
                    sheet2.Cells[r, 5].Value = c.ChannelOwnerDisplay ?? c.ChannelOwnerEmail;
                    sheet2.Cells[r, 6].Value = c.Completed;
                    sheet2.Cells[r, 7].Value = c.Bad;
                    sheet2.Cells[r, 8].Value = c.RiskRatio;
                    sheet2.Cells[r, 9].Value = c.Recommendation;
                    sheet2.Cells[r, 10].Value = c.ReviewState;
                    sheet2.Cells[r, 11].Value = c.P0Count;
                    sheet2.Cells[r, 12].Value = c.P1Count;
                    sheet2.Cells[r, 13].Value = c.P2Count;
                    sheet2.Cells[r, 14].Value = c.P3Count;
                    sheet2.Cells[r, 15].Value = c.P4Count;
                    sheet2.Cells[r, 16].Value = c.P5Count;
                    sheet2.Cells[r, 17].Value = c.LastBackfillAtUtc?.ToString("u");
                    sheet2.Cells[r, 18].Value = c.LastBackfillFromIst?.ToString("yyyy-MM-dd");
                    sheet2.Cells[r, 19].Value = c.LastBackfillToIst?.ToString("yyyy-MM-dd");
                    sheet2.Cells[r, 20].Value = c.CoverageIncomplete ? "Yes" : "No";
                    sheet2.Cells[r, 21].Value = c.TransInterviewerId;
                    r++;
                }

                var fileName = "Risebird_HighRiskInterviewers_" + fromIst.ToString("yyyyMMdd") + "_" + toIst.ToString("yyyyMMdd") + ".xlsx";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("AutoRemovalMonitor", new { from, to, view = "interviewers" });
            }
        }

        [Authorize]
        [HttpGet]
        [Obsolete("Use GetPerIrfAutoQcBackfillPreview per IRF.")]
        public Task<IActionResult> GetAutoQcBackfillPreviewCount() =>
            Task.FromResult<IActionResult>(Json(new { ok = false, message = "Global backfill removed. Use per-IRF backfill from IRF Audit Directory." }));

        [Authorize]
        [HttpPost]
        [Obsolete("Use RunPerIrfAutoQcBackfill per IRF.")]
        public Task<IActionResult> RunAutoQcBackfill(bool forceRerun = false) =>
            Task.FromResult<IActionResult>(Json(new { ok = false, message = "Global backfill removed. Use per-IRF backfill from IRF Audit Directory." }));

        /// <summary>
        /// Runs the QC Interview AI merge assistant (same logic as the Azure Functions timer <c>QcInterviewAIMergeJob</c>).
        /// Omit <paramref name="interviewId"/> to process all pending rows; pass an id to attempt a single interview (it must still appear in the pending-merge queue).
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RunQcInterviewAiMerge(int? interviewId = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                    return RedirectToAction("SignOut", "Session");

                var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "") ?? "";
                if (profileName != "Admin" && profileName != "Quality Controller" && profileName != "SupportAdmin" && profileName != "SeniorHR")
                    return Json(new { ok = false, message = "Not authorized to run QC AI merge." });

                if (interviewId.HasValue && interviewId.Value > 0)
                {
                    var (ok, message) = await _qcInterviewAIMergeJobService.ProcessInterviewAsync(interviewId.Value);
                    return Json(new { ok, message });
                }

                //var (processed, failed) = await _qcInterviewAIMergeJobService.ProcessAllPendingAsync();
                //return Json(new { ok = true, processed, failed, message = $"Processed {processed}, failed/skipped {failed}." });
                return Json(new { ok = false, message = "QC AI merge failed" });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "QC AI merge failed; see logs." });
            }
        }

        public async Task<IActionResult> GenerateTranscriptAiEvaluation(int interviewid)
        {
            try
            {
                var item = await _meetingService.GetPendingQcInterviewAIMergeById(interviewid);
                if (item == null)
                {
                    item = new QcInterviewAIMergePendingRow();
                }
                try
                {
                    // OPTIMIZATION: Validate transcript link before attempting download
                    if (string.IsNullOrEmpty(item.AudioTranscriptFileLink))
                    {
                        // Mark as failed - could add a method to update status if needed
                        return Json("Missing transcript link");
                    }

                    //ScreeningRequest requestMode
                    //var aiAnalysis = await _assistantServiceHelper.ScreenAsync(new ScreeningRequest { TranscriptLink = item.AudioTranscriptFileLink, Type = ProfileNames.TranscriptAnalysisAIAssistant });
                    string transcriptText;
                    using (HttpClient client = _httpClientFactory.CreateClient())
                    {
                        transcriptText = await client.GetStringAsync(item.AudioTranscriptFileLink);
                    }

                    // OPTIMIZATION: Pre-screen transcript for validity (saves API calls on corrupted/empty transcripts)
                    if (!string.IsNullOrEmpty(transcriptText))
                    {
                        var (isValid, validationReason) = _transcriptOptimizationHelper.ValidateTranscriptForAnalysis(transcriptText);
                        if (!isValid)
                        {
                            // Mark as failed - could add a method to update status if needed
                            return Json("Transcript validation failed");
                        }

                        // OPTIMIZATION: Reduce token usage by removing filler words, timestamps, and excessive whitespace
                        var estimatedTokensBefore = _transcriptOptimizationHelper.EstimateTokenCount(transcriptText);
                        transcriptText = _transcriptOptimizationHelper.OptimizeTranscriptText(transcriptText);
                        var estimatedTokensAfter = _transcriptOptimizationHelper.EstimateTokenCount(transcriptText);
                    }
                    else
                    {
                        return Json("Transcript is empty or could not be downloaded");
                    }

                    var aiAnalysis = await _assistantServiceHelper.GenerateTranscriptAnalysis(transcriptText);
                    if (aiAnalysis != null)
                    {
                        var model = JsonConvert.DeserializeObject<TranscriptAnalysisSchema>(aiAnalysis.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);
                        if (model != null)
                        {
                            await _meetingService.SaveAITranscriptAnalysis(interviewid, model);
                        }
                        try
                        {
                            var jsonToProcess = aiAnalysis.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value.Replace("```json", "").Replace("```", "");
                            var modelset = JsonConvert.DeserializeObject<TranscriptAnalysisSchema>(jsonToProcess);
                            //TransScript Analysis save to db
                            bool serviceResult = await _meetingService.SaveQcInterviewAIevaluation(interviewid, modelset, jsonToProcess, true);
                            if (serviceResult)
                            {
                                return Json("Successfully generated the AI evaluation");
                            }
                            else
                            {
                                return Json("Fail to generate the AI evaluation");
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return Json("Fail to generate the AI evaluation");
        }

        [HttpPost]
        [Authorize]
        [RequestFormLimits(MultipartBodyLengthLimit = 15728640)]
        public async Task<IActionResult> UploadQcAiTranscript(int interviewId, IFormFile transcriptFile)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                {
                    return Json(new { ok = false, message = "Session expired. Please sign in again." });
                }

                var profile = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profile != ProfileNames.Admin && profile != ProfileNames.SupportAdmin && profile != ProfileNames.QualityController)
                {
                    return Json(new { ok = false, message = "You are not allowed to upload transcripts." });
                }

                if (transcriptFile == null || transcriptFile.Length == 0)
                {
                    return Json(new { ok = false, message = "Please select a file." });
                }

                const long maxBytes = 15 * 1024 * 1024;
                if (transcriptFile.Length > maxBytes)
                {
                    return Json(new { ok = false, message = "File is too large. Maximum size is 15 MB." });
                }

                var ext = Path.GetExtension(transcriptFile.FileName)?.ToLowerInvariant() ?? string.Empty;
                if (ext != ".txt" && ext != ".vtt" && ext != ".json")
                {
                    return Json(new { ok = false, message = "Invalid file type. Use .txt, .vtt, or .json." });
                }

                var safeBase = Path.GetFileNameWithoutExtension(transcriptFile.FileName);
                if (string.IsNullOrWhiteSpace(safeBase))
                {
                    safeBase = "transcript";
                }

                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    safeBase = safeBase.Replace(c, '_');
                }

                var blobName = $"{interviewId}_{Guid.NewGuid():N}_{safeBase}{ext}";
                var contentType = ext == ".json" ? "application/json" : (ext == ".vtt" ? "text/vtt" : "text/plain");

                string blobUrl;
                using (var stream = transcriptFile.OpenReadStream())
                {
                    blobUrl = await _azureStorageHelper.InsertBlobCloudCompare(blobName, "transcripts", null, stream, null, contentType, null);
                }

                if (string.IsNullOrEmpty(blobUrl))
                {
                    return Json(new { ok = false, message = "Could not store the file. Please try again." });
                }

                var updated = await _meetingRespository.UpdateTranscriptLink(interviewId, blobUrl, null);
                if (!updated)
                {
                    return Json(new { ok = false, message = "Could not update the interview. Please try again." });
                }

                return Json(new { ok = true, message = "Transcript uploaded successfully." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Upload failed. Please try again." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> SaveRowActions(string refType, string refId, string actions, int? irfIntId = null, string weekendBucket = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                {
                    return Json(new { success = false });
                }

                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profileName != "Admin" && profileName != "HR" && profileName != "SeniorHR" && profileName != "SupportAdmin")
                {
                    return Json(new { success = false });
                }

                var emailId = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "") ?? string.Empty;
                _dashboardService.SaveRowActions(refType, refId, actions ?? string.Empty, emailId);

                // New spec: planner rows are spotlight IRFs — persist ActionsTaken on WeekendDriveSpotlight.
                if (string.Equals(refType, DriveWeekendActionRefs.RefType, StringComparison.OrdinalIgnoreCase)
                    && irfIntId.GetValueOrDefault() > 0)
                {
                    var bucket = string.IsNullOrWhiteSpace(weekendBucket) ? "This" : weekendBucket.Trim();
                    await _dashboardService.UpsertWeekendDriveSpotlight(
                        irfIntId.Value, bucket, actions ?? string.Empty, remove: false, userEmail: emailId);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false });
            }
        }
    }
}
