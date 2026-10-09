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
    /// Total Interviews actions, Plivo calls to interviewers). Every number drills down to
    /// the records behind it.
    ///
    /// Access: managers see everyone; a Quality Controller sees only their own row and records
    /// (enforced here for Summary and Details); any other profile is signed out.
    /// </summary>
    public class QCActivityController : Controller
    {
        private static readonly string[] ManagerProfiles =
        {
            "Admin", "SupportAdmin", "SeniorHR", "SeniorInterviewerManagement", "InterviewerManagement"
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
                restrictToActorEmail: isManager ? null : email), d => d.ErrorDetail = null);
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
