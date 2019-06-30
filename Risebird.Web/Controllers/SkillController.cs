using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using System;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class SkillController : Controller
    {
        private ISkillService _skillService;
        public SkillController(ISkillService skillService)
        {
            _skillService = skillService;
        }

        public IActionResult List()
        {
            try
            {
                return View(_skillService.GetSkills());
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IActionResult SkillLevel()
        {
            try
            {
                return View(_skillService.GetSkillLevels());
            }
            catch (Exception)
            {
                return null;
            }
            
        }
    }
}