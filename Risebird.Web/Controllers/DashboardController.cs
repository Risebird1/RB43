using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private IDashboardService _dashboardService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public DashboardController(IDashboardService dashboardService, IApplicationErrorLogger applicationErrorLogger)
        {
            _dashboardService = dashboardService;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
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
                    List<DataPoint> dataPoints = new List<DataPoint>();
                    dataPoints.Add(new DataPoint("Pending", Convert.ToDouble(pendinginterview), "#6db8bc"));
                    dataPoints.Add(new DataPoint("", acceptinterview, "#ccc"));
                    ViewBag.DataPoints = JsonConvert.SerializeObject(dataPoints);

                    List<DataPoint> dataPoints1 = new List<DataPoint>();
                    dataPoints1.Add(new DataPoint("Accepted", acceptinterview, ""));
                    dataPoints1.Add(new DataPoint("", Convert.ToDouble(pendinginterview), "#ccc"));
                    ViewBag.DataPoints1 = JsonConvert.SerializeObject(dataPoints1);

                    var complete = _dashboardService.GetDoughnutGraphDetails("C");
                    double schedule = 100 - Convert.ToDouble(complete);
                    List<DataPoint> dataPoints2 = new List<DataPoint>();
                    dataPoints2.Add(new DataPoint("Scheduled", schedule, "#5184c1"));
                    dataPoints2.Add(new DataPoint("Completed", Convert.ToDouble(complete), ""));
                    ViewBag.DataPoints2 = JsonConvert.SerializeObject(dataPoints2);
                    return View();
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            catch (Exception)
            {
                return null;

            }
        }
    }
}