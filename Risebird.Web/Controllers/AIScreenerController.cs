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
        private readonly IIRFService _irfService;

        public AIScreenerController(IAssistantServiceHelper assistantServiceHelper, IApplicationErrorLogger applicationErrorLogger, ICandidateService candidateService, IIRFService irfService)
        {
            _assistantServiceHelper = assistantServiceHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _candidateService = candidateService;
            _irfService = irfService;
        }

        

        //[HttpPost("screen")]
        public async Task<IActionResult> ScreenCandidate(string jdTitle, string emailId, int irfId, int candidateMasterId, string irfName)//, string skillsstring resumeLink, 
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

                        var candidate = await _candidateService.GetCandidateOpenAIScreeningResults(irfId, candidateMasterId);
                        
                        ViewBag.CandidateEmail = emailId;
                        ViewBag.JobTitle = jdTitle;
                        ViewBag.ResumeLink = candidate?.ResumeLink;
                        ViewBag.IRFName = irfName;
                        ViewBag.SaveOperationResult = "NA";

                        if (candidate != null && !string.IsNullOrEmpty(candidate.OpenAIScreeningResponseJson))
                        {
                            return View(JsonConvert.DeserializeObject<RunResult>(candidate.OpenAIScreeningResponseJson));
                        }

                        var request = new CandidateScreeningRequest
                        {
                            //Skills = skills,
                            JDTitle = jdTitle,
                            ResumeLink = candidate.ResumeLink
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

        public async Task<IActionResult> ScreenInterviewer(string jdTitle, string emailId, int irfId, int interviewerProfileId, string irfName, string resumeLink)
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

                        var interviewer = await _irfService.GetInterviewerOpenAIScreeningResults(irfId, interviewerProfileId);

                        ViewBag.InterviewerEmail = emailId;
                        ViewBag.JobTitle = jdTitle;
                        ViewBag.ResumeLink = interviewer?.ResumeLink;
                        ViewBag.IRFName = irfName;
                        ViewBag.SaveOperationResult = "NA";

                        if (interviewer != null && !string.IsNullOrEmpty(interviewer.OpenAIScreeningResponse))
                        {
                            return View(JsonConvert.DeserializeObject<RunResult>(interviewer.OpenAIScreeningResponse));
                        }

                        var request = new CandidateScreeningRequest
                        {
                            //Skills = skills,
                            JDTitle = jdTitle,
                            ResumeLink = interviewer.ResumeLink
                        };
                        var response = await _assistantServiceHelper.ScreenCandidateAsync(request);

                        var responseInJson = JsonConvert.SerializeObject(response);
                        //save the response in db
                        bool saveOperationResult = true;
                        if (!string.IsNullOrEmpty(responseInJson))
                        {
                            saveOperationResult = await _irfService.SaveInterviewerOpenAIScreeningResults(irfId, interviewerProfileId, responseInJson);
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
