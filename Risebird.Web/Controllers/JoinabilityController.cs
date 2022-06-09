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
    public class JoinabilityController : Controller
    {
        private IJoinabilityMatrixService _joinabilityService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public JoinabilityController(IJoinabilityMatrixService joinabilityService, IApplicationErrorLogger applicationErrorLogger)
        {
            _joinabilityService = joinabilityService;
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
                    return View(_joinabilityService.GetJoinabilityMatrix());
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddJoinability(JoinabilityMatrix model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (ModelState.IsValid)
                    {
                        if (_joinabilityService.AddJoinabilityMatrix(model))
                        {
                            TempData["SuccessErrorMessage"] = "Joinability Matrix has been added Successfully!";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Failed to add Joinability Matrix!";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        return View("AddJoinability");
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

        [HttpGet]
        public async Task<IActionResult> AddJoinability()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var model = new JoinabilityMatrix();
                    return View(model);
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
        public async Task<IActionResult> EditJoinability(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    JoinabilityMatrix skillToUpdate = _joinabilityService.GetJoinabilityMatrixById(id);

                    if (skillToUpdate != null)
                        return View("EditJoinability", skillToUpdate);
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
        public async Task<IActionResult> EditJoinability(JoinabilityMatrix skillUpdated)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {
                        if (_joinabilityService.UpdateJoinabilityMatrix(skillUpdated))
                        {
                            TempData["SuccessErrorMessage"] = "Joinability has been updated Successfully!";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Failed to update Joinability!";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        return View("EditJoinability");
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
                TempData["SuccessErrorMessage"] = "Updating Joinability Level is Failed";
                return View("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteJoinabilityMatrix(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_joinabilityService.DeleteJoinabilityMatrix(id))
                    {
                        TempData["SuccessErrorMessage"] = "Joinability has been deleted Successfully!";
                        return Json(new { url = Url.Action("Index", "Joinability") });
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Failed to delete Joinability!";
                        return Json(new { url = Url.Action("Index", "Joinability") });
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
                TempData["SuccessErrorMessage"] = "Deletion of Joinability Failed";
                return RedirectToAction("Index");
            }
        }
    }
}