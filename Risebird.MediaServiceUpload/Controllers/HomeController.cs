using System.Configuration;
using System.Web.Mvc;

namespace AzureMediaServiceMVC.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public JsonResult Login(string Username, string password)
        {
            if (Username.ToUpper() == ConfigurationManager.AppSettings["UserName"].ToUpper() && password == ConfigurationManager.AppSettings["Password"])
            {
                return Json("success");
            }
            return Json("fail");
        }
    }
}