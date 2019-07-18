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
                var Totinvites = _dashboardService.GetInvitecnt();
                ViewBag.Totinvites = Totinvites.Count;
                var TotInterviewer = _dashboardService.GetInterviwercnt();
                ViewBag.TotInterviewer = TotInterviewer.Count;
                var Totcandidate = _dashboardService.GetCandidatecnt();
                ViewBag.Totcandidate = Totcandidate.Count;
                return View();
            }
             catch (Exception)
            {
              return null;

            }
        }
    }
}