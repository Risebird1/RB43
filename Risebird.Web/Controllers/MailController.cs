using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class MailController : Controller
    {
        private INotificationHelper _notificationHelper;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IMailService _mailService;
        public MailController(INotificationHelper notificationHelper, IApplicationErrorLogger applicationErrorLogger, IMailService mailService)
        {
            _notificationHelper = notificationHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _mailService = mailService;
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
                    var result = await _notificationHelper.SendHelpsupportMail(mail);
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
                    var result = await _notificationHelper.SendHelpsupportMail(mail);
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MailRecipients(string recipientType)
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Client")
                    {
                        if (string.IsNullOrEmpty(recipientType))
                        {
                            recipientType = "Candidate";
                        }

                        ViewBag.recipientType = recipientType;

                        var recipients = await _mailService.GetRecipients(recipientType);

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
        public async Task<IActionResult> GetRecipientMailActivities(string recipientEmail)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    var model = await _mailService.GetEmailRecipientActivities(recipientEmail);
                    ViewBag.RecipientEmail = recipientEmail;
                    return PartialView("GetRecipientMailActivities", model);
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
        public async Task<IActionResult> RecipientActivityDetails(string recipientType)
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Client")
                    {
                        if (string.IsNullOrEmpty(recipientType))
                        {
                            recipientType = "Candidate";
                        }

                        ViewBag.recipientType = recipientType;

                        var recipients = await _mailService.GetEmailRecipientsLatestActivity(recipientType);

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

        [HttpGet]
        public async Task<IActionResult> GetRecipientMailActivitiesByMessageId(string recipientEmail, string messageId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    var model = (await _mailService.GetRecipientMailActivitiesByMessageId(recipientEmail, messageId))?.ToList() ;
                    ViewBag.RecipientEmail = recipientEmail;
                    return PartialView("GetRecipientMailActivitiesByMessageId", model);
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
    }
}