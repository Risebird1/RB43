using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public DashboardController(IDashboardService dashboardService, IApplicationErrorLogger applicationErrorLogger)
        {
            _dashboardService = dashboardService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var model = new DashboardVM();

                    //daily interview tracking first chart
                    model = GetDateWiseInterviews(DateTime.Now);
                    //daily interview tracking slots
                    model.DailyInterviewSlots = await _dashboardService.GetDateWiseMeetingsSlots(DateTime.Now);
                    model.InterviewerCountObj = await _dashboardService.GetInterviewerCount();
                    //TempData.Remove("ProgressDate");
                    if (TempData["ProgressDate"] != null)
                    {
                        model.ProgressDate=Convert.ToDateTime(TempData["ProgressDate"]);
                       // model.ProgressDate = (DateTime)(TempData["ProgressDate"]);
                        TempData.Keep("ProgressDate");
                    }
                    else
                    {
                        model.ProgressDate = DateTime.Now;
                    }
                    //if (progressDate != null)
                    //{
                    //    model.ProgressDate = progressDate.Value;
                    //}
                    //else
                    //{
                    //    model.ProgressDate = DateTime.Now;

                    //}

                    model.IrfProgressList = (IList<IRF>)(await _dashboardService.GetIRFProgress(model.ProgressDate));

                    model.MeetingResultCountObj = await _dashboardService.GetMeetingResultCount();
                    model.CandidatesCountObj = await _dashboardService.GetCandidatesCount();
                    //model.IrfProgressList = (List<IRF>)(await _dashboardService.GetIRFProgress());
                    //interview status graph
                    model.TotalCompletedMeetings = model.Meetings != null ? model.Meetings.Where(x => x.InterviewStatus.HasValue).Count() : 0;
                    model.ProgressOptions = new SelectList(_dashboardService.GetProgressOptions(), "id", "Name");

                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }

        public DashboardVM GetDateWiseInterviews(DateTime date)
        {
            try
            {
                var model = new DashboardVM();
                model.Meetings = _dashboardService.GetDateWiseMeetings(date).Result;
                var meetingTimes = model.Meetings.Select(x => x.interviewDate).Distinct();
                //var irfList = model.Meetings.Select(x => x.IRFID).Distinct();
                var irfList = model.Meetings.Select(x => x.IRFnSkill).Distinct();
                List<object> iData = new List<object>();

                foreach (var item in meetingTimes)
                {
                    dynamic expando = new ExpandoObject();
                    expando.Time = item;
                    foreach (var irf in irfList)
                    {
                        AddProperty(expando, irf, model.Meetings.Where(x => x.IRFID == irf.Split("(")[0] && x.interviewDate == item).Count());
                    }
                    iData.Add(expando);
                }

                model.MeetingsData = JsonConvert.SerializeObject(iData);
                model.MeetingsDataLabels = JsonConvert.SerializeObject(irfList);
                return model;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public IActionResult GetDateWiseInterviewStatus(DateTime date)
        {
            try
            {
                var model = new DashboardVM();
                model.Meetings = _dashboardService.GetDateWiseMeetings(date).Result;
                model.TotalCompletedMeetings = model.Meetings != null ? model.Meetings.Where(x => x.InterviewStatus.HasValue).Count() : 0;
                return PartialView("InterviewStatusPartial", model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public IActionResult GetDateWiseMeetingsSlots(DateTime date)
        {
            try
            {
                var model = new DashboardVM();
                model.DailyInterviewSlots = _dashboardService.GetDateWiseMeetingsSlots(date).Result;
                return PartialView("InterviewSlotsPartial", model);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        //[HttpPost]
        //public async Task<IActionResult> UpdateIRFProgress(IList<IRF> IrfProgressList)
        //{
        //    try
        //    {
        //        await _dashboardService.UpdateIRFProgress(IrfProgressList);
        //        return RedirectToAction("Index");

        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        return null;
        //    }
        //}
        [HttpPost]
        public async Task<IActionResult> UpdateIRFProgress(DashboardVM model)
        {
            try
            {
                await _dashboardService.UpdateIRFProgress(model.IrfProgressList);
                return RedirectToAction("Index");

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IActionResult> GetDateWiseIRFProgress(DateTime date)
        {
            try
            {
                var model = new DashboardVM();
                model.IrfProgressList = (IList<IRF>)(await _dashboardService.GetIRFProgress(date));
                model.ProgressOptions = new SelectList(_dashboardService.GetProgressOptions(), "id", "Name");
                model.ProgressDate = date;
                TempData["ProgressDate"] = date;
                return PartialView("IRFProgressPartial", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IActionResult> ClientIndex()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = "Client";//profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = "ramanaidu.m@mphasis.com";//emailid.Replace("\"", "");
                    if(profileName == "Client")
                    {
                        var model = new ClientDashboardVM();
                        //daily interview tracking first chart
                        model = GetDateWiseClientInterviews(DateTime.Now, emailid);
                        model.EmailId = emailid;
                        return View(model);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }

                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public ClientDashboardVM GetDateWiseClientInterviews(DateTime date, string emailId)
        {
            try
            {
                var model = new ClientDashboardVM();
                model.Meetings = _dashboardService.GetDateWiseClientMeetings(date, emailId).Result;
                var meetingTimes = model.Meetings.Select(x => x.interviewDate).Distinct();
                //var irfList = model.Meetings.Select(x => x.IRFID).Distinct();
                var irfList = model.Meetings.Select(x => x.IRFnSkill).Distinct();
                List<object> iData = new List<object>();

                foreach (var item in meetingTimes)
                {
                    dynamic expando = new ExpandoObject();
                    expando.Time = item;
                    foreach (var irf in irfList)
                    {
                        AddProperty(expando, irf, model.Meetings.Where(x => x.IRFID == irf.Split("(")[0] && x.interviewDate == item).Count());
                    }
                    iData.Add(expando);
                }

                model.MeetingsData = JsonConvert.SerializeObject(iData);
                model.MeetingsDataLabels = JsonConvert.SerializeObject(irfList);
                return model;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

    }
}