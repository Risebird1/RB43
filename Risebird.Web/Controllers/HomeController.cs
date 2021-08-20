using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using System;
using System.Linq;
using System.Security.Claims;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IUserService _userService;
        private readonly ISettingsService _settingsService;
        public HomeController(IUserService userService, ISettingsService settingsService)
        {
            _userService = userService;

            _settingsService = settingsService;
        }

        public IActionResult Index(string url = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userClaims = User.Identity as ClaimsIdentity;
                //You get the user’s first and last name below:
                string name = userClaims?.FindFirst("name")?.Value;
                // The 'Name' claim can be used for showing the username 
                string emailId = userClaims?.FindFirst("emails")?.Value;

                var menus = _userService.GetMenuBasedOnUserEmailld(emailId).GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                var settings = _settingsService.GetAllSettings();
                var currenttheme = _userService.GetCurrenttheme(emailId);
                if (String.IsNullOrEmpty(currenttheme))
                {
                    HttpContext.Session.SetString("Currenttheme", "");
                }
                else
                    HttpContext.Session.SetString("Currenttheme", currenttheme);

                if (menus.Count > 0)
                {

                    if (String.IsNullOrEmpty(menus.FirstOrDefault().LogoLink))
                    {
                        HttpContext.Session.SetString("Logo", "");
                    }
                    else
                        HttpContext.Session.SetString("Logo", menus.FirstOrDefault().LogoLink);

                }
                else
                {
                    HttpContext.Session.SetString("Logo", "");
                }
                if (settings != null)
                {
                    HttpContext.Session.SetString("settings", JsonConvert.SerializeObject(settings));

                }
                if (menus != null)
                {
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                    HttpContext.Session.SetString("profileName", JsonConvert.SerializeObject(menus.FirstOrDefault()?.profileName));
                    //HttpContext.Session.SetString("NavigationShow", JsonConvert.SerializeObject(menus.FirstOrDefault()?.NavigationShow == null));
                    HttpContext.Session.SetString("NavigationShow", JsonConvert.SerializeObject(false));
                    HttpContext.Session.SetString("email", JsonConvert.SerializeObject(emailId));
                    HttpContext.Session.SetString("UserLogedIn", "Yes");
                    HttpContext.Session.SetString("UserLogedInVedioId", "");
                    var menuAccess = menus.Where(x => x.dAccess)?.FirstOrDefault();
                    if (menuAccess == null)
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                    var menuslink = menuAccess.sUrl.Split('/');
                    if (url != null)
                    {
                        var urlArray = url.Split("/");
                        if (urlArray.Length > 4)
                        {
                            return RedirectToAction(urlArray[0], urlArray[1], new { interviewId = urlArray[2], irfId = urlArray[3], isClient = urlArray[4], isClientSelected = urlArray[5] });

                        }
                        else
                        {
                            return RedirectToAction(urlArray[0], urlArray[1], new { interviewId = urlArray[2], irfId = urlArray[3] });
                        }

                    }
                    else
                    {
                        return RedirectToAction(menuslink.Last(), menuslink.GetValue(1).ToString());
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            return RedirectToAction("SignOut", "Session");
        }
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(); //new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}