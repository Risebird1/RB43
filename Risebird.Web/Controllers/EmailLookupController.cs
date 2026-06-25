using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
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
    }
}
