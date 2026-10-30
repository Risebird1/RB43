using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class EmailLookupController : Controller
    {
        private readonly IEmailLookupService _emailLookupService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public EmailLookupController(IEmailLookupService emailLookupService, IApplicationErrorLogger applicationErrorLogger)
        {
            _emailLookupService = emailLookupService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("email") == null)
                return RedirectToAction("SignOut", "Session");

            ViewBag.profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            return View(null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string emailId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return RedirectToAction("SignOut", "Session");

                ViewBag.profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");

                if (string.IsNullOrWhiteSpace(emailId))
                    return View(null);

                var result = await _emailLookupService.GetEmailLookupData(emailId.Trim());
                return View(result);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View(null);
            }
        }

        private static readonly string[] OpsToolkitProfiles = { "Admin", "SeniorHR", "SupportAdmin" };
        private const int OpsToolkitMaxRangeDays = 92;

        private bool CanUseOpsToolkit(out string profileName)
        {
            profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            return HttpContext.Session.GetString("email") != null && Array.IndexOf(OpsToolkitProfiles, profileName) >= 0;
        }

        // Ops Toolkit: one page for fixing common operational problems. Each tool is a tab picked with ?tool=.
        // Date range (emergency unassign tool) defaults to the last 7 days (IST, matching InterviewMeetingsHistory.PerformedAt).
        [HttpGet]
        public async Task<IActionResult> OpsToolkit(DateTime? fromDate, DateTime? toDate, string tool)
        {
            tool = OpsToolkitVM.NormalizeTool(tool);
            try
            {
                if (!CanUseOpsToolkit(out var profileName))
                    return RedirectToAction("SignOut", "Session");

                ViewBag.profileName = profileName;

                var today = DateTime.UtcNow.AddMinutes(330).Date;
                var to = (toDate ?? today).Date;
                var from = (fromDate ?? to.AddDays(-6)).Date;
                if (from > to)
                    (from, to) = (to, from);
                if ((to - from).TotalDays > OpsToolkitMaxRangeDays)
                    from = to.AddDays(-OpsToolkitMaxRangeDays);

                var model = await _emailLookupService.GetOpsToolkitData(from, to, tool);
                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var today = DateTime.UtcNow.AddMinutes(330).Date;
                return View(new OpsToolkitVM { FromDate = today.AddDays(-6), ToDate = today, Tool = tool });
            }
        }

        // irfIntId = IRF.id for one skill channel, or null to add the link to every channel that can be fixed.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIRFScheduleLink(int? irfIntId)
        {
            try
            {
                if (!CanUseOpsToolkit(out _))
                    return Json(new OpsToolkitActionResultVM { Success = false, Message = "You are not allowed to do this." });

                var performedBy = HttpContext.Session.GetString("email")?.Replace("\"", "");
                return Json(await _emailLookupService.AddIRFScheduleLink(irfIntId, performedBy));
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new OpsToolkitActionResultVM { Success = false, Message = "Could not add the scheduler link." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> OpsToolkitInterviewHistory(int interviewId)
        {
            try
            {
                if (!CanUseOpsToolkit(out _))
                    return Unauthorized();

                return Json(await _emailLookupService.GetInterviewPointsHistory(interviewId));
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new OpsToolkitInterviewHistoryVM { InterviewId = interviewId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUnassignPenalty(int interviewPointsId, string reason)
        {
            try
            {
                if (!CanUseOpsToolkit(out _))
                    return Json(new OpsToolkitActionResultVM { Success = false, Message = "You are not allowed to do this." });

                var performedBy = HttpContext.Session.GetString("email")?.Replace("\"", "");
                var result = await _emailLookupService.RemoveUnassignPenalty(interviewPointsId, reason, performedBy);
                return Json(result);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new OpsToolkitActionResultVM { Success = false, Message = "Could not remove the penalty." });
            }
        }
    }
}
