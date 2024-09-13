using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.OpenAIModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System.Threading.Tasks;
using System;
using System.Net.Http.Headers;
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
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IInterviewerService _interviewerService;

        public AIScreenerController(IAssistantServiceHelper assistantServiceHelper,
            IApplicationErrorLogger applicationErrorLogger, ICandidateService candidateService,
            IIRFService irfService, IFileMimeTypeHelper fileMimeTypeHelper,
            IAzureStorageHelper azureStorageHelper, IInterviewerService interviewerService)
        {
            _assistantServiceHelper = assistantServiceHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _candidateService = candidateService;
            _irfService = irfService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _azureStorageHelper = azureStorageHelper;
            _interviewerService = interviewerService;
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

                        var request = new ScreeningRequest
                        {
                            //Skills = skills,
                            JDTitle = jdTitle,
                            ResumeLink = candidate.ResumeLink
                        };
                        //// https://portal.risebird.io/Interview/AIClientReport?InterviewId=296708&irfId=8717

                        //var request = new ScreeningRequest
                        //{
                        //    //Skills = skills,
                        //    //JDTitle = jdTitle,
                        //    Type = ProfileNames.QualityController,
                        //    ReportUrl = "https://portal.risebird.io/Interview/AIClientReport?InterviewId=296708&irfId=8717"
                        //    //ResumeLink = candidate.ResumeLink
                        //};
                        var response = await _assistantServiceHelper.ScreenAsync(request);

                        var responseInJson = JsonConvert.SerializeObject(response);
                        //save the response in db
                        bool saveOperationResult = true;
                        if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
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
                        //    var request = new ScreeningRequest
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

        public async Task<IActionResult> ScreenInterviewer(string jdTitle, int irfId, int userId, string irfName, string resumeLink = null, bool isRedo = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var interviewer = new TransInterviewer();
                        //check if the interviewer's screening is already done and saved in db(as json string)

                        interviewer = await _irfService.GetInterviewerOpenAIScreeningResults(irfId, userId);


                        ViewBag.InterviewerEmail = interviewer?.EmailId;
                        ViewBag.ResumeLink = interviewer?.ResumeLink;
                        ViewBag.IRFName = irfName;
                        ViewBag.SaveOperationResult = "NA";
                        ViewBag.JobTitle = jdTitle;

                        if (interviewer != null && !string.IsNullOrEmpty(interviewer.OpenAIScreeningResponse) && !isRedo)
                        {
                            return View(JsonConvert.DeserializeObject<RunResult>(interviewer.OpenAIScreeningResponse));
                        }

                        var request = new ScreeningRequest
                        {
                            //Skills = skills,
                            JDTitle = jdTitle,
                            ResumeLink = interviewer?.ResumeLink ?? resumeLink,
                            Type = ProfileNames.Interviewer
                        };
                        var response = await _assistantServiceHelper.ScreenAsync(request);

                        var responseInJson = JsonConvert.SerializeObject(response);
                        //save the response in db
                        bool saveOperationResult = true;
                        if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
                        {
                            saveOperationResult = await _irfService.SaveInterviewerOpenAIScreeningResults(irfId, userId, responseInJson);
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

        [HttpPost]
        public async Task<IActionResult> ScreenInterviewerBeforeOnboarding()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var loggedInUser = HttpContext.Session.GetString("email")?.Trim();
                    loggedInUser = loggedInUser.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        var file = Request.Form.Files;
                        var nvc = Request.Form;
                        string primarySpecialization = string.Empty;
                        string emailId = string.Empty;
                        //string currentUserEmail = string.Empty;
                        if (!string.IsNullOrEmpty(nvc["emailId"]))
                        {
                            emailId = nvc["emailId"];
                        }
                        if (!string.IsNullOrEmpty(nvc["primarySpecialization"]))
                        {
                            primarySpecialization = nvc["primarySpecialization"];
                        }

                        var fileName = ContentDispositionHeaderValue.Parse(file[0].ContentDisposition).FileName.Trim('"');
                        string uploadedFileURL = string.Empty;
                        if (file != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file[0]);
                            if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                                            || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                            {
                                await _azureStorageHelper.InsertBlob(fileName, "resumes", file[0]);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                var blob = containerRef.GetBlockBlobReference(fileName);
                                uploadedFileURL = blob.Uri.ToString();
                            }
                            else
                            {
                                return null;
                            }
                            var request = new ScreeningRequest
                            {
                                //Skills = skills,
                                JDTitle = primarySpecialization,
                                ResumeLink = uploadedFileURL,
                                Type = ProfileNames.Interviewer
                            };
                            var response = await _assistantServiceHelper.ScreenAsync(request);
                            var responseInJson = JsonConvert.SerializeObject(response);
                            bool saveOperationResult;
                            if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
                            {
                                saveOperationResult = await _interviewerService.SaveInterviewerPreOnboardingVerdict(emailId, primarySpecialization, responseInJson, loggedInUser);
                            }
                            string OpenAIScreeningResponse = responseInJson; // Example input string
                            string AIVerdict = "Yes"; // Default value
                            int verdictIndex = OpenAIScreeningResponse.ToLower().IndexOf("verdict");

                            if (verdictIndex != -1)
                            {
                                // Extract substring after the word 'verdict'
                                string verdictSubstr = OpenAIScreeningResponse.Substring(verdictIndex + 7, Math.Min(50, OpenAIScreeningResponse.Length - (verdictIndex + 7)));

                                // Remove 'annotation' and 'annotations' (case-insensitive)
                                verdictSubstr = verdictSubstr.Replace("annotation", "", StringComparison.OrdinalIgnoreCase)
                                                             .Replace("annotations", "", StringComparison.OrdinalIgnoreCase);

                                // Convert to lowercase for case-insensitive comparison
                                string lowerVerdict = verdictSubstr.ToLower();

                                // Check if it contains 'no', 'not', or 'doesn't'
                                if (lowerVerdict.Contains("no") || lowerVerdict.Contains("not") || lowerVerdict.Contains("doesn't"))
                                {
                                    AIVerdict = "No";
                                }
                            }

                            // AIVerdict now contains 'No' or 'Yes' based on the condition
                            if (AIVerdict == "Yes")
                            {
                                return Json(new { isfavourableVerdict = true, ResumeLink = uploadedFileURL, errorMessage = string.Empty });

                            }
                            else
                            {
                                return Json( new { isfavourableVerdict = false, ResumeLink = uploadedFileURL, errorMessage = string.Empty });
                            }

                        }
                        else
                        {
                            return Json(new { isfavourableVerdict = false, ResumeLink = uploadedFileURL, errorMessage = "Please select a file." });
                        }
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
                return Json(new { isfavourableVerdict = false, resumeLink = string.Empty, errorMessage = "Failed to generate verdict. Please try again." });
            }
        }

    }
}
