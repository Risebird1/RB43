using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Threading.Tasks;
using Risebird.Entities.Data;

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
        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.Message = TempData["SuccessErrorMessage"];
                return View(_profileService.GetProfile());
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
                var model = new Profile();
                return View("ProfileAdd", model);
            }
            catch(Exception ex)
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
                if (ModelState.IsValid)
                {
                    if (_profileService.AddProfile(profile))
                    {
                        TempData["SuccessErrorMessage"] = "Profile is added Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Adding profile is Failed";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    return View("ProfileAdd");
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
                Profile profileToUpdate = _profileService.GetProfile(id);

                if (profileToUpdate != null)
                    return View("ProfileUpdate", profileToUpdate);
                else
                    return RedirectToAction("Index");

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
                
if (ModelState.IsValid)
                {
                    if (_profileService.UpdateProfile(profile))
                    {
                        TempData["SuccessErrorMessage"] = "Profile is updated Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Updating Profile is Failed";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    return View("ProfileUpdate");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Updating Profile Level is Failed";
                return RedirectToAction("Index");
            }

        }

        public async Task<IActionResult> DeleteProfile(int id)
        {
            try
            {
                if (_profileService.DeleteProfile(id))
                {
                    TempData["SuccessErrorMessage"] = "Profile is deleted Successfully";
                    return Json(new { url = Url.Action("Index", "Profile") });
                }
                else
                {
                    TempData["SuccessErrorMessage"] = "Deletion of Profile Failed";
                    return Json(new { url = Url.Action("Index", "Profile") });
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