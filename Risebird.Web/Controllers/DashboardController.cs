using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Dynamic;
using System.Data;

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
                    var Totinvites = _dashboardService.GetInvitecnt();
                    ViewBag.Totinvites = Totinvites.Count;
                    var TotInterviewer = _dashboardService.GetInterviwercnt();
                    ViewBag.TotInterviewer = TotInterviewer.Count;
                    var Totcandidate = _dashboardService.GetCandidatecnt();
                    ViewBag.Totcandidate = Totcandidate.Count;
                    /* bind graph */
                    var pendinginterview = _dashboardService.GetDoughnutGraphDetails("A");
                    double acceptinterview = 100 - Convert.ToDouble(pendinginterview);
                    List<DataPoint> dataPoints = new List<DataPoint>
                    {
                        new DataPoint("Confirmed", Convert.ToDouble(pendinginterview), "#6db8bc"),
                        new DataPoint("Cancelled", acceptinterview, "#ccc")
                    };
                    ViewBag.DataPoints = JsonConvert.SerializeObject(dataPoints);

                    List<DataPoint> dataPoints1 = new List<DataPoint>
                    {
                        new DataPoint("Accepted", acceptinterview, ""),
                        new DataPoint("", Convert.ToDouble(pendinginterview), "#ccc")
                    };
                    ViewBag.DataPoints1 = JsonConvert.SerializeObject(dataPoints1);

                    var complete = _dashboardService.GetDoughnutGraphDetails("C");
                    double schedule = 100 - Convert.ToDouble(complete);
                    List<DataPoint> dataPoints2 = new List<DataPoint>
                    {
                        new DataPoint("Approved", Convert.ToDouble(complete), "#5184c1"),
                        new DataPoint("Not Approved", Convert.ToDouble(schedule), "")
                    };
                    ViewBag.DataPoints2 = JsonConvert.SerializeObject(dataPoints2);
                    return View();
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception)
            {
                return null;

            }
        }

        //public async Task<IActionResult> Index1()
        //{
        //    try
        //    {
        //        if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
        //        {
        //            return View();

        //        }
        //        else
        //        {
        //            return RedirectToAction("SignOut", "Session");
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }
        //}

        public async Task<IActionResult> Index1()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    int addDays = 0;
                    var model = new DashboardVM();
                    model.Meetings = await _dashboardService.GetDateWiseMeetings(addDays);
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
                            AddProperty(expando, irf, model.Meetings.Where(x => x.IRFID == irf.Split("(")[0] && x.interviewDate==item).Count());
                        }
                        iData.Add(expando);
                    }

                    ViewBag.MeetingsData = JsonConvert.SerializeObject(iData);
                    ViewBag.MeetingsDataLabels = JsonConvert.SerializeObject(irfList);
                   

                    return View();
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception)
            {
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
    }
}