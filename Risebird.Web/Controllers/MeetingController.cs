using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;

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
            return View(_meetingService.GetMeetingList());
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