using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Threading.Tasks;
using Risebird.Entities.ViewModel;
using Risebird.Entities.Data;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class SkillController : Controller
    {
        private ISkillService _skillService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public SkillController(ISkillService skillService, IApplicationErrorLogger applicationErrorLogger)
        {
            _skillService = skillService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<IActionResult> List()
        {
            try
            {
                return View(_skillService.GetSkills());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IActionResult> SkillLevel()
        {
            try
            {
                ViewBag.Message = TempData["SuccessErrorMessage"];
                return View(_skillService.GetSkillLevels());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View();
            }

        }

        [HttpGet]
        public async Task<IActionResult> AddSkillLevel()
        {
            try
            {
                var model = new SkillLevel();
                return View("SkillLevelAdd", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SkillLevel");
            }

        }

        [HttpPost]
        public async Task<IActionResult> AddSkillLevel(SkillLevel skillLevel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (_skillService.AddSkillLevel(skillLevel))
                    {
                        TempData["SuccessErrorMessage"] = "Skill Level is added Successfully";
                        return RedirectToAction("SkillLevel");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Adding Skill Level is Failed";
                        return RedirectToAction("SkillLevel");
                    }
                }
                else
                {
                    return RedirectToAction("SkillLevel");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Adding Skill Level is Failed";
                return RedirectToAction("SkillLevel");
            }

        }

        [HttpGet]
        public async Task<IActionResult> EditSkillLevel(int id)
        {
            try
            {
                SkillLevel skillLevelToUpdate = _skillService.GetSkillLevel(id);

                TempData["SkillLevelIdToUpdate"] = id;
               
                if (skillLevelToUpdate != null)
                    return View("SkillLevelUpdate", skillLevelToUpdate);
                else
                    return RedirectToAction("SkillLevel");

            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("SkillLevel");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditSkillLevel(SkillLevel skillLevelUpdated)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (_skillService.UpdateSkillLevel(skillLevelUpdated))
                    {
                        TempData["SuccessErrorMessage"] = "Skill Level is updated Successfully";
                        return RedirectToAction("SkillLevel");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Updating Skill Level is Failed";
                        return RedirectToAction("SkillLevel");
                    }
                }
                else
                {
                    return View("SkillLevelUpdate");
                }
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Updating Skill Level is Failed";
                return View("SkillLevel");
            }
        }

        public async Task<IActionResult> DeleteSkillLevl(int id)
        {
            try
            {
                if(_skillService.DeleteSkillLevel(id))
                {
                    TempData["SuccessErrorMessage"] = "Skill Level is deleted Successfully";
                    return Json(new { url = Url.Action("SkillLevel", "Skill") });
                }
                else
                {
                    TempData["SuccessErrorMessage"] = "Deletion of SKill Level Failed";
                    return Json(new { url = Url.Action("SkillLevel", "Skill") });
                }
            }

            catch(Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                TempData["SuccessErrorMessage"] = "Deletion of SKill Level Failed";
                return RedirectToAction("SkillLevel");
            }
        }
    }

}
