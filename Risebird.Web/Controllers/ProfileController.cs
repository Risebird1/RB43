using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using System;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private IProfileService _profileService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public ProfileController(IProfileService profileService, IApplicationErrorLogger applicationErrorLogger)
        {
            _profileService = profileService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (TempData["SuccessErrorMessage"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                        TempData.Remove("SuccessErrorMessage");

                    }
                    return View(_profileService.GetProfile());
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

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var model = new Profile();
                    return View("ProfileAdd", model);
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
        public async Task<IActionResult> Add(Profile profile)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {
                        if (_profileService.AddProfile(profile))
                        {
                            TempData["SuccessErrorMessage"] = "Profile has been added Successfully!";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Failed to add Profile!";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        return View("ProfileAdd");
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
                TempData["SuccessErrorMessage"] = "Adding Profile Level is Failed";
                return RedirectToAction("Index");
            }

        }

        [HttpGet]
        public async Task<IActionResult> EditProfile(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    Profile profileToUpdate = _profileService.GetProfile(id);

                    if (profileToUpdate != null)
                        return View("ProfileUpdate", profileToUpdate);
                    else
                        return RedirectToAction("Index");
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
        public async Task<IActionResult> EditProfile(Profile profile)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {
                        if (_profileService.UpdateProfile(profile))
                        {
                            TempData["SuccessErrorMessage"] = "Profile has been updated Successfully!";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Failed to update Profile!";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        return View("ProfileUpdate");
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
                TempData["SuccessErrorMessage"] = "Updating Profile Level is Failed";
                return RedirectToAction("Index");
            }

        }

        [HttpGet]
        public async Task<IActionResult> DeleteProfile(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_profileService.DeleteProfile(id))
                    {
                        TempData["SuccessErrorMessage"] = "Profile has been deleted Successfully!";
                        return Json(new { url = Url.Action("Index", "Profile") });
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Failed to delete Profile!";
                        return Json(new { url = Url.Action("Index", "Profile") });
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
                TempData["SuccessErrorMessage"] = "Deletion of Profile Failed";
                return RedirectToAction("List");
            }
        }

    }
}