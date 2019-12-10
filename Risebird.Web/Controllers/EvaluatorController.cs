using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;

namespace Risebird.Web.Controllers
{
    public class EvaluatorController : Controller
    {
        private IEvaluatorService _evaluatorService;
        private IApplicationErrorLogger _applicationErrorLogger;
       
        public EvaluatorController(IEvaluatorService evaluatorService, IApplicationErrorLogger applicationErrorLogger)
        {
            _evaluatorService = evaluatorService;
            _applicationErrorLogger = applicationErrorLogger;
           
        }

        public IActionResult Index()
        {
            return View();
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OpenResumeEvaluations()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Resume Evaluator")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var modelList = _evaluatorService.GetOpenEvaluations(emailid);
                        return View(modelList);
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