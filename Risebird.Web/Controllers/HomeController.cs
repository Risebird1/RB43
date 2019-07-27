using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using System.Security.Claims;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private IUserService _userService;
        public HomeController(IUserService userService)
        {
            _userService = userService;
        }
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userClaims = User.Identity as ClaimsIdentity;
                //You get the user’s first and last name below:
                string name = userClaims?.FindFirst("name")?.Value;
                // The 'Name' claim can be used for showing the username                
                string emailId = userClaims?.FindFirst(ClaimTypes.Email)?.Value;
                var menus = _userService.GetMenuBasedOnUserEmailld(emailId);
                if (menus != null)
                {                   
                   HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                   HttpContext.Session.SetString("profileName", JsonConvert.SerializeObject(menus.FirstOrDefault()?.profileName));
                   HttpContext.Session.SetString("email", JsonConvert.SerializeObject(emailId));
                    var menuAccess = menus.Where(x => x.dAccess)?.FirstOrDefault();
                    if (menuAccess == null)
                    {
                        return RedirectToAction("SignOut", "Account");
                    }
                    var menuslink = menuAccess.sUrl.Split('/');
                    return RedirectToAction(menuslink.Last(), menuslink.GetValue(1).ToString());
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            return RedirectToAction("SignOut", "Account");
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


