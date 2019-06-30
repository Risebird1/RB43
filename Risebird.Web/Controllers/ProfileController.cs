using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using System;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private IProfileService _profileService;
        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }
        public IActionResult Index()
        {
            try
            {
                return View(_profileService.GetProfile());
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}