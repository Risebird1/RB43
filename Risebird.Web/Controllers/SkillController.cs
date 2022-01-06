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

        [HttpGet]
        public async Task<IActionResult> List()
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
                    return View(_skillService.GetSkills());
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
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
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var model = new Skill();
                    return View("SkillAdd", model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
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
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {
                        if (_skillService.AddSkill(skill))
                        {
                            TempData["SuccessErrorMessage"] = "Skill has been added Successfully!";
                            return RedirectToAction("List");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Failed to add Skill!";
                            return RedirectToAction("List");
                        }
                    }
                    else
                    {
                        return View("SkillAdd");
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
                TempData["SuccessErrorMessage"] = "Adding Skill Level is Failed";
                return RedirectToAction("SkillLevel");
            }

        }

        [HttpGet]
        public async Task<IActionResult> EditSkill(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    Skill skillToUpdate = _skillService.GetSkill(id);

                    if (skillToUpdate != null)
                        return View("SkillUpdate", skillToUpdate);
                    else
                        return RedirectToAction("List");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
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
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {
                        if (_skillService.UpdateSkill(skillUpdated))
                        {
                            TempData["SuccessErrorMessage"] = "Skill has been updated Successfully!";
                            return RedirectToAction("List");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Failed to update Skill!";
                            return RedirectToAction("List");
                        }
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
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

        [HttpGet]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_skillService.DeleteSkill(id))
                    {
                        TempData["SuccessErrorMessage"] = "Skill has been deleted Successfully!";
                        return Json(new { url = Url.Action("List", "Skill") });
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Failed to delete Skill!";
                        return Json(new { url = Url.Action("List", "Skill") });
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
                TempData["SuccessErrorMessage"] = "Deletion of SKill Failed";
                return RedirectToAction("List");
            }
        }

        #endregion

        #region Skill Level Functions

        [HttpGet]
        public async Task<IActionResult> SkillLevel()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessage"];
                    return View(_skillService.GetSkillLevels());
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
        public async Task<IActionResult> AddSkillLevel()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var model = new SkillLevel();
                    return View("SkillLevelAdd", model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
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
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {
                        if (_skillService.AddSkillLevel(skillLevel))
                        {
                            TempData["SuccessErrorMessage"] = "Skill Level has added Successfully!";
                            return RedirectToAction("SkillLevel");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Failed to add Skill Level!";
                            return RedirectToAction("SkillLevel");
                        }
                    }
                    else
                    {
                        return View("SkillLevelAdd");
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
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    SkillLevel skillLevelToUpdate = _skillService.GetSkillLevel(id);

                    if (skillLevelToUpdate != null)
                        return View("SkillLevelUpdate", skillLevelToUpdate);
                    else
                        return RedirectToAction("SkillLevel");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
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
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (ModelState.IsValid)
                    {
                        if (_skillService.UpdateSkillLevel(skillLevelUpdated))
                        {
                            TempData["SuccessErrorMessage"] = "Skill Level has been updated Successfully!";
                            return RedirectToAction("SkillLevel");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Failed to update Skill Level!";
                            return RedirectToAction("SkillLevel");
                        }
                    }
                    else
                    {
                        return View("SkillLevelUpdate");
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
                TempData["SuccessErrorMessage"] = "Updating Skill Level is Failed";
                return View("SkillLevel");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeleteSkillLevel(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (_skillService.DeleteSkillLevel(id))
                    {
                        TempData["SuccessErrorMessage"] = "Skill Level has been deleted Successfully!";
                        return Json(new { url = Url.Action("SkillLevel", "Skill") });
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Failed to delete Skill Level!";
                        return Json(new { url = Url.Action("SkillLevel", "Skill") });
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
                TempData["SuccessErrorMessage"] = "Deletion of SKill Level Failed";
                return RedirectToAction("SkillLevel");
            }
        }

        #endregion
    }

}
