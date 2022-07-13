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
    public class SupportController : Controller
    {
        private IApplicationErrorLogger _applicationErrorLogger;
        private ISupportService _supportService;
        private IUserService _userService;
        public SupportController(ISupportService supportService, IApplicationErrorLogger applicationErrorLogger, IUserService userService)
        {
            _supportService = supportService;
            _applicationErrorLogger = applicationErrorLogger;
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Index(string controllerName = null, string actMethod = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid?.Replace("\"", "");
                    if (HttpContext.Session.GetString("NavigationShow") == "true")
                    {
                        
                        _userService.UpdateUserSigInDetails(emailid);
                        HttpContext.Session.SetString("NavigationShow", "false");
                    }

                    var model = new SupportList();
                    model.Supports = _supportService.GetAllSupportHR(emailid);
                    ViewBag.Profile = profileName;
                    if (TempData["SuccessErrorMessageSupport"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageSupport"].ToString();
                        TempData.Remove("SuccessErrorMessageSupport");
                    }

                    if (controllerName != null && actMethod != null)
                    {
                        model.BackController = controllerName;
                        model.BackMethod = actMethod;
                    }
                    return View(model);

                }
                else
                {

                    var model = new SupportList();
                    model.Supports = _supportService.GetAllSupportHR();

                    if (TempData["SuccessErrorMessageSupport"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageSupport"].ToString();
                        TempData.Remove("SuccessErrorMessageSupport");
                    }

                    if (controllerName != null && actMethod != null)
                    {
                        model.BackController = controllerName;
                        model.BackMethod = actMethod;
                    }
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> SuportIndex()
        {
            try
            {
                string emailId = null;
                if(HttpContext.Session.GetString("email") != null)
                {
                    emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId?.Replace("\"", "");
                }
                
                var model = new SupportList();
                model.Supports = _supportService.GetAllSupportHR(emailId);
                return PartialView("SupportIndex", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AddSupport()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        var model = new Support();
                        return View(model);

                    }
                    else
                    {
                        return RedirectToAction("Index");
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

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddSupport(Support hrDetails)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (_supportService.AddSupportHR(hrDetails))
                    {
                        TempData["SuccessErrorMessageSupport"] = "Support HR has been added Successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageSupport"] = "Sorry! Could not add Support HR!";
                        return RedirectToAction("Index");
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
                TempData["SuccessErrorMessageSupport"] = "Sorry! Could not add Support HR!";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        var hrSupport = _supportService.GetSupportHR(id);
                        return View(hrSupport);
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

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(Support hrDetails)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (_supportService.UpdateSupportHR(hrDetails))
                    {
                        TempData["SuccessErrorMessageSupport"] = "Support HR has been updated Successfully!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageSupport"] = "Sorry! Could not update Support HR!";
                        return RedirectToAction("Index");
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
                TempData["SuccessErrorMessageSupport"] = "Sorry! Could not update Support HR!";
                return RedirectToAction("Index");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        if (_supportService.DeleteSupportHR(id))
                        {
                            //TempData["SuccessErrorMessageSupport"] = "Support HR deleted successfully.";
                            return Json(new { url = Url.Action("Index", "Support") });
                        }
                        else
                        {
                            //TempData["SuccessErrorMessageSupport"] = "Sorry! Could not delete Support HR!";
                            return Json(new { url = Url.Action("Index", "Support") });
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
                //TempData["SuccessErrorMessageSupport"] = "Sorry! Could not delete Support HR!";
                return RedirectToAction("Index");
            }
        }

    }
}