using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    public class UserAnalyticsController : Controller
    {
        
        private readonly ITrackPublicUrlAccessor _trackPublicUrlAccessor;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public UserAnalyticsController(ITrackPublicUrlAccessor trackPublicUrlAccessor, IApplicationErrorLogger applicationErrorLogger)
        {
            _trackPublicUrlAccessor = trackPublicUrlAccessor;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> PageViews()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var Emailid = HttpContext.Session.GetString("email")?.Trim();
                    Emailid = Emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Quality Controller" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        var model = await _trackPublicUrlAccessor.FetchTelemetryData();
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
    }
}
