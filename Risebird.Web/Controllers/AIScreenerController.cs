using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Microsoft.AspNetCore.Mvc;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
//using System.Web.Mvc;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class AIScreenerController : Controller
    {
        private IApplicationErrorLogger _applicationErrorLogger;
        private IAssistantServiceHelper _assistantServiceHelper;

        public AIScreenerController(IAssistantServiceHelper assistantServiceHelper, IApplicationErrorLogger applicationErrorLogger)
        {
            _assistantServiceHelper = assistantServiceHelper;
            _applicationErrorLogger = applicationErrorLogger;
        }

        

        //[HttpPost("screen")]
        public async Task<IActionResult> ScreenCandidate(string resumeLink, string jdTitle)//, string skills
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        //HttpClient myWebClient = new HttpClient();
                        //using (var reader = new StreamReader(myWebClient.GetStreamAsync(resumeLink).Result))//new StreamReader(resumeFile.OpenReadStream())
                        //{
                        //    var resume = await reader.ReadToEndAsync();
                        //    var request = new CandidateScreeningRequest
                        //    {
                        //        Skills = skills,
                        //        ResumeLink = resumeLink
                        //    };
                        //    var response = await _assistantServiceHelper.ScreenCandidateAsync(request);
                        //    return Json(response);
                        //}
                        var request = new CandidateScreeningRequest
                        {
                            //Skills = skills,
                            JDTitle = jdTitle,
                            ResumeLink = resumeLink
                        };
                        var response = await _assistantServiceHelper.ScreenCandidateAsync(request);
                        return Json(response);
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
            catch (System.Exception ex)
            {

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(null);
            }
        }
    }
}
