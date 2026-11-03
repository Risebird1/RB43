using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Microsoft.Extensions.Hosting;
using Risebird.Entities.ViewModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// QC Activity dashboard: what each Quality Controller did in an IST date range
    /// (reports reviewed / penalised, trial kept / removed, IRF audits, Total Red Marker and
    /// Total Interviews actions, Plivo calls to interviewers, report edits and interviewer status
    /// changes). Every number drills down to the records behind it.
    ///
    /// Access: managers see everyone; a Quality Controller sees only their own row and records
    /// (enforced here for Summary and Details); any other profile is signed out.
    /// </summary>
    public class QCActivityController : Controller
    {
        private static readonly string[] ManagerProfiles =
        {
            "Admin", "SupportAdmin", "SeniorHR", "SeniorInterviewerManagement", "InterviewerManagement", "Finance Support"
        };

        private static readonly string[] DiagnosticsProfiles = { "Admin", "SupportAdmin" };

        private const string QualityControllerProfile = "Quality Controller";

        private readonly IQCActivityDashboardService _qcActivityDashboardService;
        private readonly IInterviewerCoachingService _interviewerCoachingService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IWebHostEnvironment _environment;

        public QCActivityController(IQCActivityDashboardService qcActivityDashboardService,
            IInterviewerCoachingService interviewerCoachingService, IApplicationErrorLogger applicationErrorLogger,
            IWebHostEnvironment environment)
        {
            _qcActivityDashboardService = qcActivityDashboardService;
            _interviewerCoachingService = interviewerCoachingService;
            _applicationErrorLogger = applicationErrorLogger;
            _environment = environment;
        }

        /// <summary>Profiles that may open QC Activity and the Interviewer QC View (managers + Quality Controller).</summary>
        internal static bool CanOpen(string profileName) =>
            IsManagerProfile(profileName) || IsQualityControllerProfile(profileName);

        internal static bool IsManagerProfile(string profileName) =>
            !string.IsNullOrWhiteSpace(profileName) && ManagerProfiles.Contains(profileName.Trim());

        internal static bool IsQualityControllerProfile(string profileName) =>
            string.Equals(profileName?.Trim(), QualityControllerProfile, StringComparison.Ordinal);

        // GET: /QCActivity or /QCActivity/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var (email, profileName) = GetSessionUser();
                if (email == null || profileName == null)
                    return RedirectToAction("SignOut", "Session");

                var isManager = IsManagerProfile(profileName);
                if (!isManager && !IsQualityControllerProfile(profileName))
                    return RedirectToAction("SignOut", "Session");

                ViewBag.Profile = profileName;
                ViewBag.ProfileName = profileName;
                ViewBag.Email = email;
                ViewBag.IsManager = isManager;
                ViewBag.DefaultRangeDays = QCActivityMetrics.DefaultRangeDays;
                ViewBag.MaxRangeDays = QCActivityMetrics.MaxRangeDays;
                return View();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        // GET: /QCActivity/Summary?from=yyyy-MM-dd&to=yyyy-MM-dd&onlyQc=true
        [HttpGet]
        public async Task<IActionResult> Summary(DateTime? from = null, DateTime? to = null, bool onlyQc = true)
        {
            var (email, profileName) = GetSessionUser();
            if (email == null || profileName == null)
                return Unauthorized();

            var isManager = IsManagerProfile(profileName);
            if (!isManager && !IsQualityControllerProfile(profileName))
                return StatusCode(StatusCodes.Status403Forbidden);

            // A Quality Controller only ever sees their own row.
            return await JsonOrErrorAsync(() => _qcActivityDashboardService.GetSummaryAsync(
                from, to,
                onlyQualityController: !isManager || onlyQc,
                restrictToActorEmail: isManager ? null : email,
                cancellationToken: HttpContext.RequestAborted), d => d.ErrorDetail = null);
        }

        // GET: /QCActivity/Details?actor=<email>&metric=<key>&from=yyyy-MM-dd&to=yyyy-MM-dd
        [HttpGet]
        public async Task<IActionResult> Details(string actor, string metric, DateTime? from = null, DateTime? to = null)
        {
            var (email, profileName) = GetSessionUser();
            if (email == null || profileName == null)
                return Unauthorized();

            var isManager = IsManagerProfile(profileName);
            if (!isManager && !IsQualityControllerProfile(profileName))
                return StatusCode(StatusCodes.Status403Forbidden);

            // A Quality Controller asking for someone else still gets only their own records.
            return await JsonOrErrorAsync(() => _qcActivityDashboardService.GetDetailsAsync(
                actor, metric, from, to,
                restrictToActorEmail: isManager ? null : email), d => d.ErrorDetail = null);
        }

        // GET: /QCActivity/Flags?actor=<email>&from=yyyy-MM-dd&to=yyyy-MM-dd
        // Flags panel (bursts, reviewed with no call, reversed by another QC, short calls, kept despite a P0,
        // actions without evidence). Managers: one person, or everyone when actor is empty. A Quality
        // Controller only ever gets their own flags.
        [HttpGet]
        public async Task<IActionResult> Flags(string actor = null, DateTime? from = null, DateTime? to = null)
        {
            var (email, profileName) = GetSessionUser();
            if (email == null || profileName == null)
                return Unauthorized();

            var isManager = IsManagerProfile(profileName);
            if (!isManager && !IsQualityControllerProfile(profileName))
                return StatusCode(StatusCodes.Status403Forbidden);

            return await JsonOrErrorAsync(() => _qcActivityDashboardService.GetFlagsAsync(
                actor, from, to,
                restrictToActorEmail: isManager ? null : email), d => d.ErrorDetail = null);
        }

        // GET: /QCActivity/Coverage?from=yyyy-MM-dd&to=yyyy-MM-dd
        // High-Risk interviewers in the window that any QC person called, coached or actioned, and the untouched list.
        [HttpGet]
        public async Task<IActionResult> Coverage(DateTime? from = null, DateTime? to = null)
        {
            var denied = DenyUnlessAllowed(out _);
            if (denied != null) return denied;
            return await JsonOrErrorAsync(() => _qcActivityDashboardService.GetCoverageAsync(from, to), d => d.ErrorDetail = null);
        }

        // GET: /QCActivity/ReportEdit?id=<ReportEditLog.Id>
        // Full text before and after one report edit ("View change"). Managers can open any edit; a Quality
        // Controller only their own (EditedBy = session email). Anything else answers "Edit not found".
        [HttpGet]
        public async Task<IActionResult> ReportEdit(int id)
        {
            var (email, profileName) = GetSessionUser();
            if (email == null || profileName == null)
                return Unauthorized();

            var isManager = IsManagerProfile(profileName);
            if (!isManager && !IsQualityControllerProfile(profileName))
                return StatusCode(StatusCodes.Status403Forbidden);

            ReportEditVM edit;
            try
            {
                edit = await _qcActivityDashboardService.GetReportEditAsync(id);
            }
            catch (Exception)
            {
                // Already logged by the service.
                return Json(new ReportEditVM { Error = "Could not load the edit. Please try again." });
            }

            if (edit == null || edit.Id <= 0 || !CanSeeReportEdit(isManager, email, edit.EditedBy))
                return Json(new ReportEditVM { Error = "Edit not found" });

            return Json(edit);
        }

        /// <summary>Managers see every report edit; anyone else only the edits stored under their own email.</summary>
        internal static bool CanSeeReportEdit(bool isManager, string sessionEmail, string editedBy) =>
            isManager || (!string.IsNullOrWhiteSpace(sessionEmail)
                          && string.Equals(sessionEmail.Trim(), editedBy?.Trim(), StringComparison.OrdinalIgnoreCase));

        // ---------------- Interviewer QC View ----------------
        // Every allowed viewer, including a Quality Controller, sees all interviewers: QCs need it to coach.

        // GET: /QCActivity/Interviewers?interviewer=<email>
        public async Task<IActionResult> Interviewers(string interviewer = null)
        {
            try
            {
                var (email, profileName) = GetSessionUser();
                if (email == null || profileName == null || !CanOpen(profileName))
                    return RedirectToAction("SignOut", "Session");

                ViewBag.Profile = profileName;
                ViewBag.ProfileName = profileName;
                ViewBag.Email = email;
                ViewBag.Interviewer = string.IsNullOrWhiteSpace(interviewer) ? null : interviewer.Trim();
                ViewBag.DefaultRangeDays = QCActivityMetrics.DefaultRangeDays;
                ViewBag.MaxRangeDays = QCActivityMetrics.MaxRangeDays;
                return View();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        // GET: /QCActivity/InterviewerList?from&to&search&minInterviews=3&includeOpenFlags=false
        [HttpGet]
        public async Task<IActionResult> InterviewerList(DateTime? from = null, DateTime? to = null, string search = null,
            int? minInterviews = null, bool includeOpenFlags = false)
        {
            var denied = DenyUnlessAllowed(out _);
            if (denied != null) return denied;
            return await JsonOrErrorAsync(() => _qcActivityDashboardService.GetInterviewerViewAsync(
                from, to, search, minInterviews, includeOpenFlags), d => d.ErrorDetail = null);
        }

        // GET: /QCActivity/Diagnostics?from=yyyy-MM-dd&to=yyyy-MM-dd  (Admin and SupportAdmin only, read-only)
        // For every source the dashboard reads: who the rows in the range are stored under, so a 0 on the page
        // can be told apart from "no rows", "rows under a non-QC user" and "actor value is not an email".
        [HttpGet]
        public async Task<IActionResult> Diagnostics(DateTime? from = null, DateTime? to = null)
        {
            var (email, profileName) = GetSessionUser();
            if (email == null || profileName == null)
                return Unauthorized();
            if (!CanSeeDiagnostics(profileName))
                return StatusCode(StatusCodes.Status403Forbidden);

            return await JsonOrErrorAsync(() => _qcActivityDashboardService.GetDiagnosticsAsync(from, to), d => d.ErrorDetail = null);
        }

        /// <summary>Profiles that may open /QCActivity/Diagnostics.</summary>
        internal static bool CanSeeDiagnostics(string profileName) =>
            !string.IsNullOrWhiteSpace(profileName) && DiagnosticsProfiles.Contains(profileName.Trim());

        // GET: /QCActivity/InterviewerTimeline?interviewer=<email>&from&to
        [HttpGet]
        public async Task<IActionResult> InterviewerTimeline(string interviewer, DateTime? from = null, DateTime? to = null)
        {
            var denied = DenyUnlessAllowed(out _);
            if (denied != null) return denied;
            return await JsonOrErrorAsync(() => _qcActivityDashboardService.GetInterviewerTimelineAsync(interviewer, from, to), d => d.ErrorDetail = null);
        }

        // GET: /QCActivity/InterviewerBrief?interviewer=<email>&from&to
        [HttpGet]
        public async Task<IActionResult> InterviewerBrief(string interviewer, DateTime? from = null, DateTime? to = null)
        {
            var denied = DenyUnlessAllowed(out _);
            if (denied != null) return denied;
            return await JsonOrErrorAsync(() => _interviewerCoachingService.GetBriefAsync(interviewer, from, to), d => d.ErrorDetail = null);
        }

        // POST: /QCActivity/CoachingEmailPreview  (JSON body, anti-forgery header)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CoachingEmailPreview([FromBody] CoachingEmailRequestVM request)
        {
            var denied = DenyUnlessAllowed(out _);
            if (denied != null) return denied;
            return Json(await _interviewerCoachingService.PreviewEmailAsync(request ?? new CoachingEmailRequestVM()));
        }

        // POST: /QCActivity/SendCoachingEmail  (JSON body, anti-forgery header)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCoachingEmail([FromBody] CoachingEmailRequestVM request)
        {
            var denied = DenyUnlessAllowed(out var coachEmail);
            if (denied != null) return denied;
            return Json(await _interviewerCoachingService.SendEmailAsync(request ?? new CoachingEmailRequestVM(), coachEmail));
        }

        // POST: /QCActivity/LogCoachingSession  (JSON body, anti-forgery header)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogCoachingSession([FromBody] LogCoachingSessionRequestVM request)
        {
            var denied = DenyUnlessAllowed(out var coachEmail);
            if (denied != null) return denied;
            return Json(await _interviewerCoachingService.LogSessionAsync(request ?? new LogCoachingSessionRequestVM(), coachEmail));
        }

        // ---------------- Deduction review requests (My Quality phase 2) ----------------

        // GET: /QCActivity/ReviewRequests — open requests (oldest first), recent decisions, AI accuracy by check.
        public async Task<IActionResult> ReviewRequests([FromServices] IDeductionReviewService deductionReviewService)
        {
            try
            {
                var (email, profileName) = GetSessionUser();
                if (email == null || profileName == null || !CanOpen(profileName))
                    return RedirectToAction("SignOut", "Session");

                ViewBag.Profile = profileName;
                ViewBag.ProfileName = profileName;
                ViewBag.Email = email;
                var vm = await deductionReviewService.GetQueueAsync(IsManagerProfile(profileName));
                return View(vm);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }

        // POST: /QCActivity/DecideReviewRequest  (form fields, anti-forgery header) — Stands / Reduce / Refund, final.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecideReviewRequest(DeductionReviewDecisionInputVM input,
            [FromServices] IDeductionReviewService deductionReviewService)
        {
            var denied = DenyUnlessAllowed(out var qcEmail);
            if (denied != null) return denied;
            return Json(await deductionReviewService.DecideAsync(qcEmail, input ?? new DeductionReviewDecisionInputVM()));
        }

        // POST: /QCActivity/SetReviewCheckPolicy  (managers only) — take an AI check out of auto-approval or put it back.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetReviewCheckPolicy(string aiCheck, bool exclude,
            [FromServices] IDeductionReviewService deductionReviewService)
        {
            var (email, profileName) = GetSessionUser();
            if (email == null || profileName == null)
                return Unauthorized();
            if (!IsManagerProfile(profileName))
                return StatusCode(StatusCodes.Status403Forbidden);
            return Json(await deductionReviewService.SetCheckPolicyAsync(aiCheck, exclude, email));
        }

        /// <summary>
        /// Runs a page-data load and always answers with JSON the page understands: the result, or an error object
        /// (never a bare 500). The underlying exception message is only included when running in Development.
        /// </summary>
        private async Task<IActionResult> JsonOrErrorAsync<T>(Func<Task<T>> load, Action<T> clearErrorDetail) where T : class
        {
            try
            {
                var data = await load();
                if (data != null && !_environment.IsDevelopment())
                    clearErrorDetail(data);
                return Json(data);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new
                {
                    error = "Could not load QC activity. Please try again or narrow the date range.",
                    errorDetail = _environment.IsDevelopment() ? ex.GetBaseException().Message : null,
                    rows = Array.Empty<object>(),
                    irfs = Array.Empty<object>(),
                    events = Array.Empty<object>(),
                    themes = Array.Empty<object>()
                });
            }
        }

        private IActionResult DenyUnlessAllowed(out string email)
        {
            var (sessionEmail, profileName) = GetSessionUser();
            email = sessionEmail;
            if (sessionEmail == null || profileName == null)
                return Unauthorized();
            if (!CanOpen(profileName))
                return StatusCode(StatusCodes.Status403Forbidden);
            return null;
        }

        private (string Email, string ProfileName) GetSessionUser()
        {
            var email = HttpContext.Session.GetString("email")?.Trim()?.Replace("\"", "");
            var profileName = HttpContext.Session.GetString("profileName")?.Trim()?.Replace("\"", "");
            return (string.IsNullOrWhiteSpace(email) ? null : email,
                    string.IsNullOrWhiteSpace(profileName) ? null : profileName);
        }
    }
}
