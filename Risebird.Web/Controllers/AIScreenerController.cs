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
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
                            saveOperationResult = await _irfService.SaveInterviewerOpenAIScreeningResults(irfId, userId, responseInJson.Replace("'", ""));
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
                            bool saveOperationResult = false;
                            string aiResponseUrl = string.Empty;
                            if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
                            {
                                saveOperationResult = await _interviewerService.SaveInterviewerPreOnboardingVerdict(emailId, primarySpecialization, responseInJson.Replace("'", ""), loggedInUser);
                            }
                            if (saveOperationResult)
                            {
                                aiResponseUrl = Url.Action("PreOnboardingAIVerdictDetails", "AIScreener",
                                new { emailId = emailId, primarySpecialization = primarySpecialization, resumeLink = uploadedFileURL },
                                Request.Scheme);

                                //aiResponseUrl = Request.Scheme + "://" + Request.Host.Value + $"/AIScreener/PreOnboardingAIVerdictDetails?emailId={emailId}&primarySpecialization={primarySpecialization}";
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
                            else
                            {
                                AIVerdict = "Sorry! Could not be determined!";//
                            }
                            // AIVerdict now contains 'No' or 'Yes' based on the condition
                            if (AIVerdict == "Yes")
                            {
                                return Json(new { isfavourableVerdict = true, ResumeLink = uploadedFileURL, aiResponseHref = aiResponseUrl, errorMessage = string.Empty });//, aiResponse = response.Data?.FirstOrDefault()?.Content?.Where(x=> x.Text.Value.Contains("Evaluate the resume")).FirstOrDefault()?.Text.Value

                            }
                            else if (AIVerdict == "No")
                            {
                                return Json(new { isfavourableVerdict = false, ResumeLink = uploadedFileURL, aiResponseHref = aiResponseUrl, errorMessage = string.Empty });
                            }
                            else
                            {
                                return Json(new { isfavourableVerdict = false, ResumeLink = uploadedFileURL, errorMessage = "API Failure! Verdict bypassed!" });//we bypass the verdict in this case as the api is not a 100% reliable
                            }

                        }
                        else
                        {
                            return Json(new { isfavourableVerdict = false, ResumeLink = uploadedFileURL, aiResponseHref = string.Empty, errorMessage = "Please select a file." });
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


        public async Task<IActionResult> PreOnboardingAIVerdictDetails(string emailId, string primarySpecialization, string resumeLink)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        var response = await _interviewerService.GetInterviewerPreOnboardingAIResponse(emailId, primarySpecialization);
                        ViewBag.InterviewerEmail = emailId;
                        ViewBag.ResumeLink = resumeLink;
                        ViewBag.PrimarySpecialization = primarySpecialization;

                        if (response != null)
                        {
                            var modal = JsonConvert.DeserializeObject<RunResult>(response);
                            return View(modal);

                        }
                        else
                        {
                            return View(new RunResult());
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
            catch (System.Exception ex)
            {

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View(new RunResult());
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApplicantAIScreening(int id)//, string applicantEmailId, string jdTitle, string ResumeLink
        {
            try
            {
                //string decodedResumeUrl = string.Empty;
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var loggedInUser = HttpContext.Session.GetString("email")?.Trim();
                    loggedInUser = loggedInUser.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        //if (!string.IsNullOrEmpty(ResumeLink))
                        //{
                        //    decodedResumeUrl = WebUtility.UrlDecode(ResumeLink);
                        //}
                        var aiEvaluation = await _interviewerService.GetApplicantInterviewerAIEvaluation(id);
                        //ViewBag.InterviewerEmail = aiEvaluation.EmailId;
                        //ViewBag.ResumeLink = aiEvaluation.ResumeLink;
                        //ViewBag.PrimarySpecialization = aiEvaluation.JDTiltle;

                        if (aiEvaluation != null)
                        {
                            if (aiEvaluation.OpenAIResponse != null)
                            {
                                var modal = JsonConvert.DeserializeObject<RunResult>(aiEvaluation.OpenAIResponse);
                                return View(modal);

                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(aiEvaluation.ResumeLink))
                                {
                                    //decodedResumeUrl = WebUtility.UrlDecode(ResumeLink);
                                    var request = new ScreeningRequest
                                    {
                                        //Skills = skills,
                                        JDTitle = aiEvaluation.JDTiltle,
                                        ResumeLink = aiEvaluation.ResumeLink,
                                        Type = ProfileNames.Interviewer
                                    };
                                    var response = await _assistantServiceHelper.ScreenAsync(request);
                                    var responseInJson = JsonConvert.SerializeObject(response);
                                    bool saveOperationResult = false;
                                    string aiResponseUrl = string.Empty;
                                    if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
                                    {
                                        saveOperationResult = await _interviewerService.SaveApplicantInterviewerAIVerdict(id, aiEvaluation.EmailId, aiEvaluation.JDTiltle, responseInJson.Replace("'", ""), loggedInUser);
                                    }

                                    if (saveOperationResult)
                                    {
                                        aiResponseUrl = Url.Action("ApplicantAIEvaluation", "AIScreener",
                                        new { id = id },
                                        Request.Scheme);

                                        //aiResponseUrl = Request.Scheme + "://" + Request.Host.Value + $"/AIScreener/ApplicantAIEvaluation?emailId={emailId}&primarySpecialization={primarySpecialization}";
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
                                    else
                                    {
                                        AIVerdict = string.Empty;
                                    }

                                    // AIVerdict now contains 'No' or 'Yes' based on the condition
                                    if (AIVerdict == "Yes")
                                    {
                                        if (aiEvaluation.IsEmployed && aiEvaluation.IsEmployed)
                                        {
                                            //call method to add Interviewer here
                                            var obj = new InterviewerProfileVM();
                                            obj.EmailId = aiEvaluation.EmailId;
                                            obj.MobileNumber = aiEvaluation.Phone;
                                            obj.ResumeLink = aiEvaluation.ResumeLink;
                                            obj.CompanyName = aiEvaluation.CompanyName;
                                            obj.isVerified = aiEvaluation.isVerified;
                                            obj.IsEmployed = aiEvaluation.IsEmployed;
                                            obj.DOB = aiEvaluation.DOB.HasValue ? aiEvaluation.DOB.Value : DateTime.Now;
                                            obj.FirstName = aiEvaluation.FirstName;
                                            obj.LastName = aiEvaluation.LastName;
                                            obj.TargetIrfId = aiEvaluation.IRFId;
                                            obj.AssessmentGroupId = aiEvaluation.AssessmentGroupId.ToString();
                                            obj.PrimarySpecializationForAIVerdict = aiEvaluation.JDTiltle;
                                            obj.Country = "";
                                            obj.PrimarySpecializationForAcknowledgement = aiEvaluation.JDTiltle;
                                            obj.AIResponse = aiEvaluation.OpenAIResponse;
                                            obj.AssessmentGroupArray = new string[] { aiEvaluation.AssessmentGroupId.ToString() };
                                            int userId;
                                            string agList;
                                            _interviewerService.AddInterviewerProfile(obj, loggedInUser, out agList, out userId, true);
                                        }
                                        return Json(new { isfavourableVerdict = true, aiResponseHref = aiResponseUrl, errorMessage = string.Empty });//, aiResponse = response.Data?.FirstOrDefault()?.Content?.Where(x=> x.Text.Value.Contains("Evaluate the resume")).FirstOrDefault()?.Text.Value

                                    }
                                    else if (AIVerdict == "No")
                                    {
                                        return Json(new { isfavourableVerdict = false, aiResponseHref = aiResponseUrl, errorMessage = string.Empty });

                                    }
                                    else
                                    {
                                        return Json(new { isfavourableVerdict = false, aiResponseHref = string.Empty, errorMessage = "API Failure! Verdict bypassed!" });
                                    }

                                }
                                else
                                {
                                    //return Json("Error generating AI response.");
                                    return Json(new { isfavourableVerdict = false, aiResponseHref = string.Empty, errorMessage = "Resume Missing!" });
                                }
                            }

                        }
                        else
                        {
                            return Json(new { errorMessage = "Not Found!" });

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
                //return Json("Error generating AI response.");
                return Json(new { isfavourableVerdict = false, aiResponseHref = string.Empty, errorMessage = "Failed to generate verdict. Please try again." });
            }
        }

        public async Task<IActionResult> ApplicantAIEvaluation(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        var aiEvaluation = await _interviewerService.GetApplicantInterviewerAIEvaluation(id);
                        ViewBag.InterviewerEmail = aiEvaluation.EmailId;
                        ViewBag.ResumeLink = aiEvaluation.ResumeLink;
                        ViewBag.PrimarySpecialization = aiEvaluation.JDTiltle;

                        if (aiEvaluation != null)
                        {
                            var modal = JsonConvert.DeserializeObject<RunResult>(aiEvaluation.OpenAIResponse);
                            return View("PreOnboardingAIVerdictDetails", modal);

                        }
                        else
                        {
                            return View("PreOnboardingAIVerdictDetails", new RunResult());
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
            catch (System.Exception ex)
            {

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View("PreOnboardingAIVerdictDetails", new RunResult());
            }
        }
    }
}
