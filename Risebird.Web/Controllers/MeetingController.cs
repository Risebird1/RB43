using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.Entities.ViewModel;
using System;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class MeetingController : Controller
    {
        private IMeetingService _meetingService;
        public MeetingController(IMeetingService meetingService)
        {
            _meetingService = meetingService;
        }
        public IActionResult Index()
        {
            try
            {
                return View(_meetingService.GetMeetingList());
            }
            catch (Exception)
            {
                return null;
            }
            
        }

       
        public ActionResult Add()
        {
            try
            {
                   var model = new MeetingVM();
                    return View(model);
            }
            catch (Exception ex)
            {
                return View();
            }
        }
    }
}