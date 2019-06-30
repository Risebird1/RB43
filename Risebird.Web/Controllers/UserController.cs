using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        public IActionResult Index()
        {
            try
            {
                return View(_userService.GetRisebirdUsers());
            }
            catch (Exception)
            {
                return null;
            }
        }
        public IActionResult Interviewers()
        {
            try
            {
                return View(_userService.GetInterviewers());
            }
            catch (Exception)
            {
                return null;
            }
        }
        public IActionResult ClientUsers()
        {
            try
            {
                return View(_userService.GetClientUsers());
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}