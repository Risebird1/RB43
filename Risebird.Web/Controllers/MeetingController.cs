using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;

namespace Risebird.Web.Controllers
{
    public class MeetingController : Controller
    {
        private IMeetingService _meetingService;
        public MeetingController(IMeetingService meetingService)
        {
            _meetingService = meetingService;
        }
        public IActionResult Index()
        {
            return View(_meetingService.GetMeetingList());
        }
    }
}