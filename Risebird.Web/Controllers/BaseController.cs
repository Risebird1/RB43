using Microsoft.AspNetCore.Mvc;
using static Risebird.Web.Models.Notiffication.Enum;

namespace Risebird.Web.Controllers
{
    public class BaseController : Controller
    {
        public void Alert(string message, NotificationType notificationType)
        {
            var msg = "swal('" + notificationType.ToString().ToUpper() + "', '" + message + "','" + notificationType + "')" + "";
            TempData["notification"] = msg;
        }


    }
}