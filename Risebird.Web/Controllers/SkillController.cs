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

        #region Skill Functions

        public async Task<IActionResult> List()
        {
            try
            {
                ViewBag.Message = TempData["SuccessErrorMessage"];
                return View(_skillService.GetSkills());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [HttpGet]
        public async Task<IActionResult> AddSkill()
        {
            try
            {
                var model = new Skill();
                return View("SkillAdd", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("List");
            }

        }

        [HttpPost]
        public async Task<IActionResult> AddSkill(Skill skill)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (_skillService.AddSkill(skill))
                    {
                        TempData["SuccessErrorMessage"] = "Skill is added Successfully";
                        return RedirectToAction("List");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Adding Skill is Failed";
                        return RedirectToAction("List");
                    }
                }
                else
                {
                    return View("SkillAdd");
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
        public async Task<IActionResult> EditSkill(int id)
        {
            try
            {
                Skill skillToUpdate = _skillService.GetSkill(id);

                if (skillToUpdate != null)
                    return View("SkillUpdate", skillToUpdate);
                else
                    return RedirectToAction("List");

            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("List");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditSkill(Skill skillUpdated)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (_skillService.UpdateSkill(skillUpdated))
                    {
                        TempData["SuccessErrorMessage"] = "Skill is updated Successfully";
                        return RedirectToAction("List");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Updating Skill is Failed";
                        return RedirectToAction("List");
                    }
                }
                else
                {
                    return View("SkillUpdate");
                }
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Updating Skill Level is Failed";
                return View("SkillLevel");
            }
        }

        public async Task<IActionResult> DeleteSkill(int id)
        {
            try
            {
                if (_skillService.DeleteSkill(id))
                {
                    TempData["SuccessErrorMessage"] = "Skill is deleted Successfully";
                    return Json(new { url = Url.Action("List", "Skill") });
                }
                else
                {
                    TempData["SuccessErrorMessage"] = "Deletion of SKill Failed";
                    return Json(new { url = Url.Action("List", "Skill") });
                }
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                TempData["SuccessErrorMessage"] = "Deletion of SKill Failed";
                return RedirectToAction("List");
            }
        }

        #endregion

        #region Skill Level Functions

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
                    return View("SkillLevelAdd");
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

        public async Task<IActionResult> DeleteSkillLevel(int id)
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

        #endregion
    }

}
