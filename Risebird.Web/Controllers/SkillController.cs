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
                var model = new SkillLevelVM();
                return View("SkillLevelAdd", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View("SkillLevel");
            }

        }

        [HttpPost]
        public async Task<IActionResult> AddSkillLevel(SkillLevelVM skillLevelVM)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _skillService.AddSkillLevel(skillLevelVM);
                    TempData["SuccessErrorMessage"] = "Skill Level is added Successfully";
                    return RedirectToAction("SkillLevel");
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
                return View("SkillLevel");
            }

        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                SkillLevelVM skillLevelToUpdate = _skillService.GetSkillLevel(id);

                SkillLevel sk = new SkillLevel();

                TempData["SkillLevelIdToUpdate"] = id;
               
                if (skillLevelToUpdate != null)
                    return View("SkillLevelUpdate", skillLevelToUpdate);
                else
                    return View("SkillLevel");

            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return View("SkillLevel");
            }
        }
        public async Task<IActionResult> Update(SkillLevelVM skillLevelUpdated)
        {
            try
            {
                int skillLevelIdToUpdate = (int)TempData.Peek("skillLevelIdToUpdate");

                SkillLevel skillLevelToUpdate = new SkillLevel
                {
                    id = skillLevelIdToUpdate,
                    Name = skillLevelUpdated.Name,
                    Description = skillLevelUpdated.Description
                };

                if (ModelState.IsValid)
                {
                    _skillService.UpdateSkillLevel(skillLevelToUpdate);
                    TempData["SuccessErrorMessage"] = "Skill Level is updated Successfully";
                    return RedirectToAction("SkillLevel");
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
    }

}
