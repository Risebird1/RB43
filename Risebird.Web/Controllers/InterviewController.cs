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
    public class InterviewController : Controller
    {
        private IMeetingService _meetingService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public InterviewController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger)
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


        public async Task<IActionResult> OpenInterview()
        {
            try
            {
                return View(_meetingService.GetAllOpenInterviews());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IActionResult> MyInterview()
        {
            try
            {
                 var myInterview=  _meetingService.GetAllOpenInterviews();
                return View(myInterview);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IActionResult> QCInterview()
        {
            try
            {
                return View(_meetingService.GetAllOpenInterviews());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> AssignYourself(int id, string InterviewDate, string CandidateEmail)
        {
            try
            {
                AssignYourselfVM assinAssignYourselfVm = new AssignYourselfVM();
                assinAssignYourselfVm.id = id;
                assinAssignYourselfVm.InterviewDate = InterviewDate;
                assinAssignYourselfVm.CandidateEmail = CandidateEmail;
                return View(assinAssignYourselfVm);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AssignYourself(AssignYourselfVM model)
        {
            try
            {
                var result=_meetingService.AssignYourself(model.id,model.Interviewer);
                ViewBag.Message = result;
                return RedirectToAction("OpenInterview");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("OpenInterview");
            }
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UnassignYourself(AssignYourselfVM model)
        {
            try
            {
                var result = _meetingService.UnassignYourself(model.id);
                ViewBag.Message = result;
                return RedirectToAction("MyInterview");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }
        }
    }

}