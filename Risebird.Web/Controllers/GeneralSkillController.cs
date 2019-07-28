using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;
using Risebird.Entities.Data;
using Microsoft.AspNetCore.Http;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class GeneralSkillController : Controller
    {
        private IGeneralSkillMatrixService _generalskillService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public GeneralSkillController(IGeneralSkillMatrixService generalskillService, IApplicationErrorLogger applicationErrorLogger)
        {
            _generalskillService = generalskillService;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    return View(_generalskillService.GetGeneralSkillMatrix());
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
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
        public async Task<IActionResult> Add(GeneralSkillMatrix model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    _generalskillService.AddGeneralSkillMatrix(model);
                    return RedirectToAction("Index");
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    var model = new GeneralSkillMatrix();
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View();
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    GeneralSkillMatrix skillToUpdate = _generalskillService.GetGeneralSkillMatrixByiId(id);

                    if (skillToUpdate != null)
                        return View("Edit", skillToUpdate);
                    else
                        return RedirectToAction("Index");
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(GeneralSkillMatrix skillUpdated)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    if (ModelState.IsValid)
                    {
                        if (_generalskillService.UpdateGeneralSkillMatrix(skillUpdated))
                        {
                            TempData["SuccessErrorMessage"] = "GeneralSkill is updated Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Updating GenralSkill is Failed";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        return View("Edit");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Updating GenearlSkill Level is Failed";
                return View("Index");
            }
        }
        public async Task<IActionResult> DeleteGeneralSkill(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    if (_generalskillService.DeleteGeneralSkillMatrix(id))
                    {
                        TempData["SuccessErrorMessage"] = "Skill is deleted Successfully";
                        return Json(new { url = Url.Action("Index", "GeneralSkill") });
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Deletion of SKill Failed";
                        return Json(new { url = Url.Action("Index", "GeneralSkill") });
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                TempData["SuccessErrorMessage"] = "Deletion of SKill Failed";
                return RedirectToAction("Index");
            }
        }
    }
}