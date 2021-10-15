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
    public class SelectabilityController : Controller
    {
        private ISelectabilityMatrixService _selectabilityService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public SelectabilityController(ISelectabilityMatrixService selectabilityService, IApplicationErrorLogger applicationErrorLogger)
        {
            _selectabilityService = selectabilityService;
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
                    return View(_selectabilityService.GetSelectabilityMatrix());
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
        public async Task<IActionResult> AddSelectability(SelectabilityMatrix model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    if (ModelState.IsValid)
                    {
                        if (_selectabilityService.AddSelectabilityMatrix(model))
                        {
                            TempData["SuccessErrorMessage"] = "Selectability Matrix is added Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Adding Selectability Matrix  is Failed";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        return View("AddSelectability");
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
        public async Task<IActionResult> AddSelectability()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var model = new SelectabilityMatrix();
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
        public async Task<IActionResult> EditSelectability(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    SelectabilityMatrix skillToUpdate = _selectabilityService.GetSelectabilityMatrixById(id);

                    if (skillToUpdate != null)
                        return View("EditSelectability", skillToUpdate);
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
        public async Task<IActionResult> EditSelectability(SelectabilityMatrix skillUpdated)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {
                        if (_selectabilityService.UpdateSelectabilityMatrix(skillUpdated))
                        {
                            TempData["SuccessErrorMessage"] = "Selectability is updated Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Updating Selectability is Failed";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        return View("EditSelectability");
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
                TempData["SuccessErrorMessage"] = "Updating Selectability Level is Failed";
                return View("Index");
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> DeleteSelectabilityMatrix(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_selectabilityService.DeleteSelectabilityMatrix(id))
                    {
                        TempData["SuccessErrorMessage"] = "Selectability is deleted Successfully";
                        return Json(new { url = Url.Action("Index", "Selectability") });
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Deletion of Selectability Failed";
                        return Json(new { url = Url.Action("Index", "Selectability") });
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
                TempData["SuccessErrorMessage"] = "Deletion of Selectability Failed";
                return RedirectToAction("Index");
            }
        }
    }
}