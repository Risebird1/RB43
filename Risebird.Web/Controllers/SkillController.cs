using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Threading.Tasks;
using Risebird.Entities.ViewModel;

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
                return View("SkillLevelAdd");
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
                return View("SkillLevelAdd");
            }

        }
    }
}