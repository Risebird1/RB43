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
using Newtonsoft.Json;
using Risebird.Entities.Data.OpenAIModels;
//using System.Web.Mvc;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class AIScreenerController : Controller
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly ICandidateService _candidateService;

        public AIScreenerController(IAssistantServiceHelper assistantServiceHelper, IApplicationErrorLogger applicationErrorLogger, ICandidateService candidateService)
        {
            _assistantServiceHelper = assistantServiceHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _candidateService = candidateService;
        }

        

        //[HttpPost("screen")]
        public async Task<IActionResult> ScreenCandidate(string resumeLink, string jdTitle, string emailId, int irfId, int candidateMasterId, string irfName)//, string skills
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {

                        //check if the candidate's screening is already done and saved in db(as json string)

                        var screeningResultsJsonStringFromDB = await _candidateService.GetCandidateOpenAIScreeningResults(irfId, candidateMasterId);
                        
                        ViewBag.CandidateEmail = emailId;
                        ViewBag.JobTitle = jdTitle;
                        ViewBag.ResumeLink = resumeLink;
                        ViewBag.IRFName = irfName;
                        ViewBag.SaveOperationResult = "NA";

                        if (!string.IsNullOrEmpty(screeningResultsJsonStringFromDB))
                        {
                            return View(JsonConvert.DeserializeObject<RunResult>(screeningResultsJsonStringFromDB));
                        }

                        var request = new CandidateScreeningRequest
                        {
                            //Skills = skills,
                            JDTitle = jdTitle,
                            ResumeLink = resumeLink
                        };
                        var response = await _assistantServiceHelper.ScreenCandidateAsync(request);

                        var responseInJson = JsonConvert.SerializeObject(response);
                        //save the response in db
                        bool saveOperationResult = true;
                        if(!string.IsNullOrEmpty(responseInJson))
                        {
                            saveOperationResult = await _candidateService.SaveCandidateOpenAIScreeningResults(irfId, candidateMasterId, responseInJson);
                        }
                        
                        if (saveOperationResult)
                        {
                            ViewBag.SaveOperationResult = "Evaluation saved successfully.";
                        }
                        else
                        {
                            ViewBag.SaveOperationResult = "Failed to save the evaluation.";
                        }

                        return View(response);
                        //return Json(response);
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
