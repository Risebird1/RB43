using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    public class RiskCategoryController : Controller
    {
        private readonly IRiskCategoryService _riskCategoryService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        private const string SessionKey = "RiskCategoryFilters";

        public RiskCategoryController(IRiskCategoryService riskCategoryService, IApplicationErrorLogger applicationErrorLogger)
        {
            _riskCategoryService = riskCategoryService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null)
                    return RedirectToAction("Login", "Account");

                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                ViewBag.Profile = profileName;

                DateTime startDate = DateTime.Now.Date.AddDays(-30);
                DateTime endDate   = DateTime.Now.Date.AddDays(1);

                if (HttpContext.Session.GetString(SessionKey) != null)
                {
                    var filter = JsonConvert.DeserializeObject<RiskCategoryFilter>(
                        HttpContext.Session.GetString(SessionKey));

                    if (filter.StartDate.HasValue)
                        startDate = filter.StartDate.Value.Date;

                    if (filter.EndDate.HasValue)
                        endDate = filter.EndDate.Value.Date.AddDays(1);
                }

                var model = await _riskCategoryService.GetRiskCategories(startDate, endDate);
                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View(null);
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult Index(RiskCategoryFilter filter)
        {
            if (filter != null)
                HttpContext.Session.SetString(SessionKey, JsonConvert.SerializeObject(filter));

            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet]
        public IActionResult ResetFilter()
        {
            HttpContext.Session.Remove(SessionKey);
            return RedirectToAction("Index");
        }
    }
}
