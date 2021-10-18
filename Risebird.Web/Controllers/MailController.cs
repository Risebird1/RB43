using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class MailController : Controller
    {
        private INotificationHelper _notificationHelper;
        private IApplicationErrorLogger _applicationErrorLogger;
        public MailController(INotificationHelper notificationHelper, IApplicationErrorLogger applicationErrorLogger)
        {
            _notificationHelper = notificationHelper;
            _applicationErrorLogger = applicationErrorLogger;
        }

        [HttpGet]
        public async Task<IActionResult> Mail()
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
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Mail(Mail mail)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var result = _notificationHelper.SendHelpsupportMail(mail);
                    ViewBag.Message = result;
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
                return null;
            }
        }

        [HttpPost]
        public async Task<JsonResult> SuportMail(string To, string Subject, string Body)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    Mail mail = new Mail();
                    mail.TO = To;
                    mail.Subject = Subject;
                    mail.Body = Body;
                    var result = _notificationHelper.SendHelpsupportMail(mail);
                    if (result == "Mail sent successfully.")
                    {
                        return Json("Success");
                    }
                    else
                    {
                        return Json("Failed");
                    }
                }
                else
                {
                    return Json("Failed");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed");
            }
        }

    }
}