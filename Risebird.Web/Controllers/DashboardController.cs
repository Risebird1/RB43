using Microsoft.AspNetCore.Mvc;

namespace Risebird.Web.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {           
            return View();
        }
    }
}