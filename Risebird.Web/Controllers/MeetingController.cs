using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class MeetingController : Controller
    {
        private IMeetingService _meetingService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public MeetingController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                return View(_meetingService.GetMeetingList());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
            
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Add(MeetingVM model)
        {
            try
            {
                _meetingService.AddMeeting(model);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            try
            {
                   var model = new MeetingVM();
                    return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View();
            }
        }
    }
}