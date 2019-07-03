using Microsoft.AspNetCore.Mvc;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Controllers
{
    public class MailController : Controller
    {
        private INotificationHelper _notificationHelper;
        public MailController(INotificationHelper notificationHelper)
        {
            _notificationHelper = notificationHelper;
        }

        [HttpGet]
        public IActionResult Mail()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Mail(Mail mail)
        {
            var result = _notificationHelper.SendHelpsupportMail(mail);
            ViewBag.Message = result;
            return View();
        }

    }
}