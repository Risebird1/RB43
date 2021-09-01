using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private IApplicationErrorLogger _applicationErrorLogger;
        private ISettingsService _settingsService;
        public SettingsController(ISettingsService settingsService, IApplicationErrorLogger applicationErrorLogger)
        {
            _settingsService = settingsService;
            _applicationErrorLogger = applicationErrorLogger;

        }
        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        if (TempData["SuccessErrorMessageSettings"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageSettings"].ToString();
                            TempData.Remove("SuccessErrorMessageSettings");

                        }
                        if (HttpContext.Session.Keys.Contains("settings"))
                        {
                            var model = JsonConvert.DeserializeObject<Settings>(HttpContext.Session.GetString("settings"));
                            return View(model);

                        }
                        else
                        {
                            return View(_settingsService.GetAllSettings());
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
                return View();
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(Settings settings)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin")
                    {
                        if (_settingsService.UpdateSettings(settings))
                        {
                            HttpContext.Session.Remove("settings");
                            HttpContext.Session.SetString("settings", JsonConvert.SerializeObject(settings));
                            TempData["SuccessErrorMessageSettings"] = "Settings updated successfully.";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageSettings"] = "Sorry! Could not update Settings!";
                            return RedirectToAction("Index");
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
                TempData["SuccessErrorMessageSupport"] = "Sorry! Could not update Settings!";
                return RedirectToAction("Index");
            }
        }

    }
}