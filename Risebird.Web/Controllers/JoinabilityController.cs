using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;
using Risebird.Entities.Data;
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
        public async Task<IActionResult> Index()
        {
            try
            {
                return View(_joinabilityService.GetJoinabilityMatrix());
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
                _joinabilityService.AddJoinabilityMatrix(model);
                return RedirectToAction("Index");
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
                var model = new JoinabilityMatrix();
                return View(model);
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
                JoinabilityMatrix skillToUpdate = _joinabilityService.GetJoinabilityMatrixById(id);

                if (skillToUpdate != null)
                    return View("EditJoinability", skillToUpdate);
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
        public async Task<IActionResult> EditJoinability(JoinabilityMatrix skillUpdated)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (_joinabilityService.UpdateJoinabilityMatrix(skillUpdated))
                    {
                        TempData["SuccessErrorMessage"] = "Joinability is updated Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Updating Joinability is Failed";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    return View("EditJoinability");
                }
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Updating Joinability Level is Failed";
                return View("Index");
            }
        }
        public async Task<IActionResult> DeleteJoinabilityMatrix(int id)
        {
            try
            {
                if (_joinabilityService.DeleteJoinabilityMatrix(id))
                {
                    TempData["SuccessErrorMessage"] = "Joinability is deleted Successfully";
                    return Json(new { url = Url.Action("Index", "Joinability") });
                }
                else
                {
                    TempData["SuccessErrorMessage"] = "Deletion of Joinability Failed";
                    return Json(new { url = Url.Action("Index", "Joinability") });
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