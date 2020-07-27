using Microsoft.AspNetCore.Mvc;

namespace Risebird.Web.Controllers
{
    public class ScheduleMeetingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}