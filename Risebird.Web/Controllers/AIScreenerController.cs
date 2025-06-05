using Google.Apis.Calendar.v3.Data;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.OpenAIModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static iTextSharp.text.pdf.AcroFields;
//using System.Web.Mvc;
using UglyToad.PdfPig;
using Microsoft.Graph.Models.Security;

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
        private readonly IMeetingService _meetingService;
        private readonly IIRFRepository _irfRepository;
        private readonly IActivityTrackerService _activityTrackerService;

        public AIScreenerController(IAssistantServiceHelper assistantServiceHelper,
            IApplicationErrorLogger applicationErrorLogger, ICandidateService candidateService,
            IIRFService irfService, IFileMimeTypeHelper fileMimeTypeHelper,
            IAzureStorageHelper azureStorageHelper, IInterviewerService interviewerService,
            IIRFRepository irfRepository, IMeetingService meetingService,
            IActivityTrackerService activityTrackerService)
        {
            _assistantServiceHelper = assistantServiceHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _candidateService = candidateService;
            _irfService = irfService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _azureStorageHelper = azureStorageHelper;
            _interviewerService = interviewerService;
            _irfRepository = irfRepository;
            _meetingService = meetingService;
            _activityTrackerService = activityTrackerService;
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

        public async Task<IActionResult> ScreenInterviewer(string jdTitle, int irfId, int userId, string irfName, bool isRedo = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        var interviewer = new TransInterviewer();
                        //check if the interviewer's screening is already done and saved in db(as json string)

                        interviewer = await _irfService.GetInterviewerOpenAIScreeningResults(irfId, userId);


                        ViewBag.InterviewerEmail = interviewer?.EmailId;
                        ViewBag.ResumeLink = interviewer?.ResumeLink;
                        ViewBag.IRFName = irfName;
                        ViewBag.SaveOperationResult = "NA";
                        ViewBag.JobTitle = jdTitle;
                        ViewBag.AIVerdict = interviewer?.AIVerdict;
                        ViewBag.AIScreeningScore = interviewer?.AIScreeningScore;

                        if (interviewer != null && !string.IsNullOrEmpty(interviewer.OpenAIScreeningResponse) && !isRedo)
                        {
                            return View("ScreenInterviewerV2", interviewer.OpenAIScreeningResponse);

                            //return View(JsonConvert.DeserializeObject<RunResult>(interviewer.OpenAIScreeningResponse));
                        }

                        //var request = new ScreeningRequest
                        //{
                        //    //Skills = skills,
                        //    JDTitle = jdTitle,
                        //    ResumeLink = interviewer?.ResumeLink ?? resumeLink,
                        //    Type = ProfileNames.Interviewer
                        //};


                        var resumeStringContent = await ReadPdfFromUrlAsync(interviewer?.ResumeLink);
                        //ReadPdfContentAsync();
                        //var response = await _assistantServiceHelper.ScreenAsync(request);
                        //var response = await _assistantServiceHelper.GenerateResumeAnalysis(resumeStringContent, jdTitle);

                        //var responseInJson = JsonConvert.SerializeObject(response);
                        ////save the response in db
                        //bool saveOperationResult = true;
                        //if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
                        //{
                        //    saveOperationResult = await _irfService.SaveInterviewerOpenAIScreeningResults(irfId, userId, responseInJson.Replace("'", ""));
                        //}

                        //if (saveOperationResult)
                        //{
                        //    ViewBag.SaveOperationResult = "Evaluation saved successfully.";
                        //}
                        //else
                        //{
                        //    ViewBag.SaveOperationResult = "Failed to save the evaluation.";
                        //}

                        //return View(response);

                        var response = await _assistantServiceHelper.GenerateResumeAnalysis(resumeStringContent, jdTitle);
                        
                        bool saveOperationResult = true;
                        var aiAnalysisModel = new InterviewerResumeFitmentAnalysisSchema();
                        if (response != null)
                        {
                            aiAnalysisModel = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(response.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);
                            if (aiAnalysisModel != null)
                            {
                                //saveOperationResult = await _meetingService.SaveAITranscriptAnalysis(interviewId, aiAnalysisModel);
                                saveOperationResult = await _irfService.SaveInterviewerOpenAIScreeningResultsV2(irfId, userId, aiAnalysisModel);//responseInJson.Replace("'", "")

                                ViewBag.AIVerdict = aiAnalysisModel.verdict;
                                ViewBag.AIScreeningScore = aiAnalysisModel.fitmentScore;
                            }
                        }
                        if (saveOperationResult)
                        {
                            ViewBag.SaveOperationResult = "Resume analysis saved successfully.";
                        }
                        else
                        {
                            ViewBag.SaveOperationResult = "Failed to save Resume analysis";
                        }

                        //return View(aiAnalysis);
                        //return View(response);
                        return View("ScreenInterviewerV2", aiAnalysisModel.completeFitmentAnalysis);

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
                var resultDe = _activityTrackerService.DebugLogger($"AI Resume Screener function error : {ex.Message}, {ex.StackTrace}", "aiScreener", string.Empty).Result;

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(null);
            }
        }

        public async Task<string> ReadPdfFromUrlAsync(string pdfUrl)
        {
            
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] pdfBytes = await client.GetByteArrayAsync(pdfUrl);

                    using (MemoryStream stream = new MemoryStream(pdfBytes))
                    {
                        using (var pdfDocument = UglyToad.PdfPig.PdfDocument.Open(stream))
                        {
                            string text = "";
                            foreach (var page in pdfDocument.GetPages())
                            {
                                text += page.Text + "\n";
                            }
                            return text;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var resultDe = _activityTrackerService.DebugLogger($"AI Resume Screener function error : {ex.Message}, {ex.StackTrace}", "aiScreener", string.Empty).Result;

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                return null;
            }
        }
        public static async Task<string> ReadPdfContentAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return "Invalid file.";

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0; // Reset stream position

                using (var reader = new PdfReader(stream.ToArray()))
                {
                    string text = "";
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        text += PdfTextExtractor.GetTextFromPage(reader, i);
                    }
                    return text;
                }
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
                            //var request = new ScreeningRequest
                            //{
                            //    //Skills = skills,
                            //    JDTitle = primarySpecialization,
                            //    ResumeLink = uploadedFileURL,
                            //    Type = ProfileNames.Interviewer
                            //};
                            //var response = await _assistantServiceHelper.ScreenAsync(request);

                            var resumeStringContent = await ReadPdfFromUrlAsync(uploadedFileURL);
                            var response = await _assistantServiceHelper.GenerateResumeAnalysis(resumeStringContent, primarySpecialization);

                            var aiAnalysisModel = new InterviewerResumeFitmentAnalysisSchema();
                            //var responseInJson = JsonConvert.SerializeObject(response);
                            bool saveOperationResult = false;
                            string aiResponseUrl = string.Empty;
                            if (response != null )
                            {
                                aiAnalysisModel = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(response.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);

                                //saveOperationResult = await _interviewerService.SaveInterviewerPreOnboardingVerdict(emailId, primarySpecialization, responseInJson.Replace("'", ""), loggedInUser);
                                saveOperationResult = await _interviewerService.SaveInterviewerPreOnboardingVerdictV2(emailId, primarySpecialization, aiAnalysisModel, loggedInUser);
                                //aiAnalysisModel = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(response.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);
                                
                            }
                            if (saveOperationResult)
                            {
                                aiResponseUrl = Url.Action("PreOnboardingAIVerdictDetails", "AIScreener",
                                new { emailId = emailId, primarySpecialization = primarySpecialization, resumeLink = uploadedFileURL },
                                Request.Scheme);

                            }
                            else
                            {
                                return Json(new { isfavourableVerdict = false, resumeLink = string.Empty, errorMessage = "Pre-onboarding failed. Please try again." });

                            }

                            string AIVerdict = aiAnalysisModel.verdict; // Default value
                            
                            if(!string.IsNullOrEmpty(AIVerdict) && (AIVerdict.ToLower().Contains("no") || AIVerdict.ToLower().Contains("not") || AIVerdict.ToLower().Contains("doesn't")))
                            {
                                return Json(new { isfavourableVerdict = false, ResumeLink = uploadedFileURL, aiResponseHref = aiResponseUrl, errorMessage = string.Empty });
                            }
                            else if(!string.IsNullOrEmpty(AIVerdict) && AIVerdict.ToLower().Contains("yes"))
                            {
                                return Json(new { isfavourableVerdict = true, ResumeLink = uploadedFileURL, aiResponseHref = aiResponseUrl, errorMessage = string.Empty });//, aiResponse = response.Data?.FirstOrDefault()?.Content?.Where(x=> x.Text.Value.Contains("Evaluate the resume")).FirstOrDefault()?.Text.Value

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
                var resultDe = _activityTrackerService.DebugLogger($"ScreenInterviewerBeforeOnboarding function error : {ex.Message}, {ex.StackTrace}", "openai", string.Empty).Result;

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
                        //var response = await _interviewerService.GetInterviewerPreOnboardingAIResponse(emailId, primarySpecialization);
                        var response = await _interviewerService.GetInterviewerPreOnboardingAIResponse(emailId, primarySpecialization);
                        ViewBag.InterviewerEmail = emailId;
                        ViewBag.ResumeLink = resumeLink;
                        ViewBag.PrimarySpecialization = primarySpecialization;
                        ViewBag.AIVerdict = response?.AIVerdict;
                        ViewBag.AIScreeningScore = response?.AIScreeningScore;

                        if (response != null)
                        {
                            //var modal = JsonConvert.DeserializeObject<RunResult>(response);
                            //return View(modal);
                            return View("PreOnboardingAIVerdictDetailsV2", response.AIResponse);

                        }
                        else
                        {
                            //return View(new RunResult());
                            return View("PreOnboardingAIVerdictDetailsV2", string.Empty);
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
                        string uploadedFileURL = string.Empty;

                        //if (id == 0)
                        //{
                        //    var file = Request.Form.Files;
                        //    var nvc = Request.Form;

                        //    //string currentUserEmail = string.Empty;
                        //    if (!string.IsNullOrEmpty(nvc["id"]))
                        //    {
                        //        id = Convert.ToInt32(nvc["id"]);
                        //    }

                        //    var fileName = ContentDispositionHeaderValue.Parse(file[0].ContentDisposition).FileName.Trim('"');
                        //    if (file != null)
                        //    {
                        //        var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(file[0]);
                        //        if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                        //                                        || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                        //        {
                        //            await _azureStorageHelper.InsertBlob(fileName, "resumes", file[0]);
                        //            var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                        //            var blob = containerRef.GetBlockBlobReference(fileName);
                        //            uploadedFileURL = blob.Uri.ToString();
                        //        }
                        //        else
                        //        {
                        //            return null;
                        //        }
                        //    }
                        //}
                        

                        var aiEvaluation = await _interviewerService.GetApplicantInterviewerAIEvaluation(id);
                        
                        ViewBag.InterviewerEmail = aiEvaluation.EmailId;

                        if (!string.IsNullOrEmpty(uploadedFileURL))
                        {
                            aiEvaluation.ResumeLink = uploadedFileURL;
                        }
                        
                        ViewBag.ResumeLink = aiEvaluation.ResumeLink;

                        ViewBag.PrimarySpecialization = aiEvaluation.JDTiltle;
                        ViewBag.AIVerdict = aiEvaluation.AIVerdict;
                        ViewBag.AIScreeningScore = aiEvaluation.AIAnalysisScore;

                        if (aiEvaluation != null)
                        {
                            if (aiEvaluation.OpenAIResponse != null && aiEvaluation.IsAIScreeningAttempted == true)
                            {
                                //var modal = JsonConvert.DeserializeObject<RunResult>(aiEvaluation.OpenAIResponse);
                                //return View(modal);
                                return View("PreOnboardingAIVerdictDetailsV2", aiEvaluation.OpenAIResponse);

                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(aiEvaluation.ResumeLink))
                                {
                                    
                                    var resumeStringContent = await ReadPdfFromUrlAsync(aiEvaluation.ResumeLink);
                                    var response = await _assistantServiceHelper.GenerateResumeAnalysis(resumeStringContent, aiEvaluation.JDTiltle);

                                    var aiAnalysisModel = new InterviewerResumeFitmentAnalysisSchema();
                                    bool saveOperationResult = false;
                                    string aiResponseUrl = string.Empty;

                                    if (response != null)
                                    {
                                        aiAnalysisModel = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(response.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);

                                        string AIVerdict = aiAnalysisModel.verdict;
                                        if(!string.IsNullOrEmpty(AIVerdict) && (AIVerdict.ToLower() == "yes" || AIVerdict.ToLower() == "no"))
                                        {
                                            saveOperationResult = await _interviewerService.SaveApplicantInterviewerAIVerdictV2(id, aiAnalysisModel, loggedInUser);
                                            if (saveOperationResult)
                                            {
                                                aiResponseUrl = Url.Action("ApplicantAIEvaluation", "AIScreener",
                                                new { id = id },
                                                Request.Scheme);

                                            }
                                            if (AIVerdict.ToLower() == "yes")
                                            {
                                                if (aiEvaluation.IsEmployed)
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
                                                return Json(new { isfavourableVerdict = true, aiResponseHref = aiResponseUrl, errorMessage = string.Empty, aiAnalysisScore = aiEvaluation.AIAnalysisScore });//, aiResponse = response.Data?.FirstOrDefault()?.Content?.Where(x=> x.Text.Value.Contains("Evaluate the resume")).FirstOrDefault()?.Text.Value

                                            }
                                            else
                                            {
                                                return Json(new { isfavourableVerdict = false, aiResponseHref = aiResponseUrl, errorMessage = string.Empty, aiAnalysisScore = aiEvaluation.AIAnalysisScore });

                                            }
                                        }
                                        else
                                        {
                                            await _interviewerService.SaveApplicantErrorMessage(id, "AI Screening (API) failure. No value in the verdict property.");

                                            return Json(new { isfavourableVerdict = false, aiResponseHref = string.Empty, errorMessage = "Failed to generate verdict. Please try again." });

                                        }

                                    }
                                    else
                                    {
                                        return Json(new { isfavourableVerdict = false, aiResponseHref = string.Empty, errorMessage = "Failed to generate verdict. Please try again." });

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
                await _interviewerService.SaveApplicantErrorMessage(id, $"AI Screening (API) failure. Exception Message : {ex.Message}");
                return Json(new { isfavourableVerdict = false, aiResponseHref = string.Empty, errorMessage = "Failed to generate verdict. Please try again." });
            }
        }


        [HttpPost]
        public async Task<IActionResult> ApplicantAIScreeningRenewed()//, string applicantEmailId, string jdTitle, string ResumeLink
        {
            int id = 0;
            try
            {
                //string decodedResumeUrl = string.Empty;
                

                    string uploadedFileURL = string.Empty;

                    var file = Request.Form.Files;
                    var nvc = Request.Form;
                    //string currentUserEmail = string.Empty;
                    if (!string.IsNullOrEmpty(nvc["id"]))
                    {
                        id = Convert.ToInt32(nvc["id"]);

                    }

                    var fileName = ContentDispositionHeaderValue.Parse(file[0].ContentDisposition).FileName.Trim('"');
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
                    }


                    var aiEvaluation = await _interviewerService.GetApplicantInterviewerAIEvaluation(id);

                    ViewBag.InterviewerEmail = aiEvaluation.EmailId;

                    if (!string.IsNullOrEmpty(uploadedFileURL))
                    {
                        aiEvaluation.ResumeLink = uploadedFileURL;
                    }

                    ViewBag.ResumeLink = aiEvaluation.ResumeLink;

                    ViewBag.PrimarySpecialization = aiEvaluation.JDTiltle;
                    ViewBag.AIVerdict = aiEvaluation.AIVerdict;
                    ViewBag.AIScreeningScore = aiEvaluation.AIAnalysisScore;

                    if (aiEvaluation != null)
                    {
                        if (aiEvaluation.OpenAIResponse != null && aiEvaluation.IsAIScreeningAttempted == true)
                        {
                            //var modal = JsonConvert.DeserializeObject<RunResult>(aiEvaluation.OpenAIResponse);
                            //return View(modal);
                            return View("PreOnboardingAIVerdictDetailsV2", aiEvaluation.OpenAIResponse);

                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(aiEvaluation.ResumeLink))
                            {
                                //save resume in db 
                                await _interviewerService.SaveExternalApplicantResume(id, aiEvaluation.ResumeLink);

                                var resumeStringContent = await ReadPdfFromUrlAsync(aiEvaluation.ResumeLink);
                                var response = await _assistantServiceHelper.GenerateResumeAnalysis(resumeStringContent, aiEvaluation.JDTiltle);

                                var aiAnalysisModel = new InterviewerResumeFitmentAnalysisSchema();
                                bool saveOperationResult = false;
                                string aiResponseUrl = string.Empty;

                                if (response != null)
                                {
                                    aiAnalysisModel = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(response.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);

                                    string AIVerdict = aiAnalysisModel.verdict;
                                    if (!string.IsNullOrEmpty(AIVerdict) && (AIVerdict.ToLower() == "yes" || AIVerdict.ToLower() == "no"))
                                    {
                                        saveOperationResult = await _interviewerService.SaveApplicantInterviewerAIVerdictV2(id, aiAnalysisModel, aiEvaluation.EmailId, aiEvaluation.ResumeLink);
                                        if (saveOperationResult)
                                        {
                                            aiResponseUrl = Url.Action("ApplicantAIEvaluation", "AIScreener",
                                            new { id = id },
                                            Request.Scheme);

                                        }
                                        if (AIVerdict.ToLower() == "yes")
                                        {
                                            if (aiEvaluation.IsEmployed)
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
                                                _interviewerService.AddInterviewerProfile(obj, aiEvaluation.EmailId, out agList, out userId, true);
                                            }
                                            return Json(new { isfavourableVerdict = true, aiResponseHref = aiResponseUrl, errorMessage = string.Empty, aiAnalysisScore = aiEvaluation.AIAnalysisScore });//, aiResponse = response.Data?.FirstOrDefault()?.Content?.Where(x=> x.Text.Value.Contains("Evaluate the resume")).FirstOrDefault()?.Text.Value

                                        }
                                        else
                                        {
                                            return Json(new { isfavourableVerdict = false, aiResponseHref = aiResponseUrl, errorMessage = string.Empty, aiAnalysisScore = aiEvaluation.AIAnalysisScore });

                                        }
                                    }
                                    else
                                    {
                                        await _interviewerService.SaveApplicantErrorMessage(id, "AI Screening (API) failure. No value in the verdict property.");

                                        return Json(new { isfavourableVerdict = false, aiResponseHref = string.Empty, errorMessage = "Failed to generate verdict. Please try again." });

                                    }

                                }
                                else
                                {
                                    return Json(new { isfavourableVerdict = false, aiResponseHref = string.Empty, errorMessage = "Failed to generate verdict. Please try again." });

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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                await _interviewerService.SaveApplicantErrorMessage(id, $"AI Screening (API) failure. Exception Message : {ex.Message}");
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
                        //var aiEvaluation = await _interviewerService.GetApplicantInterviewerAIEvaluation(id);
                        //ViewBag.InterviewerEmail = aiEvaluation.EmailId;
                        //ViewBag.ResumeLink = aiEvaluation.ResumeLink;
                        //ViewBag.PrimarySpecialization = aiEvaluation.JDTiltle;

                        var aiEvaluation = await _interviewerService.GetApplicantInterviewerAIEvaluation(id);
                        ViewBag.InterviewerEmail = aiEvaluation.EmailId;
                        ViewBag.ResumeLink = aiEvaluation.ResumeLink;
                        ViewBag.PrimarySpecialization = aiEvaluation.JDTiltle;
                        ViewBag.AIVerdict = aiEvaluation.AIVerdict;
                        ViewBag.AIScreeningScore = aiEvaluation.AIAnalysisScore;

                        if (aiEvaluation != null)
                        {
                            //var modal = JsonConvert.DeserializeObject<RunResult>(aiEvaluation.OpenAIResponse);
                            //return View("PreOnboardingAIVerdictDetails", modal);
                            return View("PreOnboardingAIVerdictDetailsV2", aiEvaluation.OpenAIResponse);

                        }
                        else
                        {
                            //return View("PreOnboardingAIVerdictDetails", new RunResult());
                            return View("PreOnboardingAIVerdictDetailsV2", string.Empty);
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

        public async Task<IActionResult> GenerateTechRubricUsingOpenAI(string jobTitle)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "InterviewerManagement" || profileName == "SeniorInterviewerManagement")
                    {
                        var result = await _assistantServiceHelper.GenerateTechRubric(jobTitle);
                        ViewBag.jobTitle = jobTitle;
                        
                        if (result != null)
                        {
                            var model = JsonConvert.DeserializeObject<RubricSchema>(result.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);
                            var techSkills = new List<TransSkill>();
                            if(model != null && model.Rubric != null)
                            {
                                foreach (var skill in model.Rubric)
                                {
                                    var techSkill = new TransSkill();

                                    string levelDesc = skill.ProficiencyLevel; // The string to match

                                    // Parse the string into an enum value
                                    if (Enum.TryParse(typeof(LevelTechEnum), levelDesc, true, out var parsedLevel))
                                    {
                                        // Cast the result to its integer value
                                        techSkill.LevelId = (int)parsedLevel;
                                    }
                                    techSkill.SkillName = skill.Skill;
                                    techSkill.Weightage = skill.Weight;
                                    if(skill.CoreConcepts != null)
                                    {
                                        techSkill.Concepts = string.Join(",", skill.CoreConcepts);
                                    }
                                    techSkill.Natureofquestion = skill.KeyExpectations;
                                    techSkill.Timelimit = Convert.ToInt16(skill.EvaluationTime);
                                    techSkill.isEliminationskill = (skill.Mandatory != null && skill.Mandatory.ToLower()=="yes") ? true : false;

                                    techSkills.Add(techSkill);

                                }
                            }
                            var viewModal = new IRFVm();
                            viewModal.SkillCollection = techSkills;
                            return PartialView("_TechRubricUsingOpenAIPartial", viewModal);

                        }
                        else
                        {
                            return View("_TechRubricUsingOpenAIPartial", new IRFVm());
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
                return null;
            }
        }

        public async Task<IActionResult> TransciptAnalysis(int interviewId, int irfId, bool isRedo = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == ProfileNames.Admin || profileName == ProfileNames.SupportAdmin || profileName == ProfileNames.SeniorHR || profileName == ProfileNames.Interviewer || profileName == ProfileNames.Finance || profileName == ProfileNames.QualityController)
                    {
                        var report = await _meetingService.GetInteviewReport(interviewId, irfId);
                        
                        ViewBag.InterviewerEmail = report?.Interviewer;
                        ViewBag.CandidateEmail = report?.CandidateEmail;
                        ViewBag.IRFName = report?.IRFName;
                        ViewBag.InterviewDate = report?.interviewDate;
                        ViewBag.AITranscriptVerdict = report?.AITranscriptVerdict;
                        ViewBag.AITranscriptAnalysisScore = report?.AITranscriptAnalysisScore;
                        //ViewBag.JobTitle = jdTitle;

                        if (report != null && !string.IsNullOrEmpty(report.AITranscriptAnalysis) && !isRedo)
                        {
                            //return View(JsonConvert.DeserializeObject<RunResult>(report.AITranscriptAnalysis));
                            return View("SavedTransciptAnalysis", report.AITranscriptAnalysis);
                        }


                        string transcriptText;
                        using (HttpClient client = new HttpClient())
                        {
                            transcriptText = await client.GetStringAsync(report.AudioTranscriptFileLink);
                        }
                        var aiAnalysis = await _assistantServiceHelper.GenerateTranscriptAnalysis(transcriptText);
                        
                        bool saveOperationResult = true;
                        var aiAnalysisModel = new TranscriptAnalysisSchema();
                        if (aiAnalysis != null)
                        {
                            aiAnalysisModel = JsonConvert.DeserializeObject<TranscriptAnalysisSchema>(aiAnalysis.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value);
                            if (aiAnalysisModel != null)
                            {
                                saveOperationResult = await _meetingService.SaveAITranscriptAnalysis(interviewId, aiAnalysisModel);

                                ViewBag.AITranscriptVerdict = aiAnalysisModel.verdict;
                                ViewBag.AITranscriptAnalysisScore = aiAnalysisModel.score;
                            }
                        }
                        if (saveOperationResult)
                        {
                            ViewBag.SaveOperationResult = "Transcript analysis saved successfully.";
                        }
                        else
                        {
                            ViewBag.SaveOperationResult = "Failed to save Transcript analysis";
                        }

                        //return View(aiAnalysis);
                        return View("SavedTransciptAnalysis", aiAnalysisModel.completeAnalysis);
                        //var request = new ScreeningRequest
                        //{
                        //    //Skills = skills,
                        //    JDTitle = jdTitle,
                        //    ResumeLink = interviewer?.ResumeLink ?? resumeLink,
                        //    Type = ProfileNames.Interviewer
                        //};
                        //var response = await _assistantServiceHelper.ScreenAsync(request);

                        //var responseInJson = JsonConvert.SerializeObject(response);
                        ////save the response in db
                        //bool saveOperationResult = true;
                        //if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
                        //{
                        //    saveOperationResult = await _irfService.SaveInterviewerOpenAIScreeningResults(irfId, userId, responseInJson.Replace("'", ""));
                        //}

                        //if (saveOperationResult)
                        //{
                        //    ViewBag.SaveOperationResult = "Evaluation saved successfully.";
                        //}
                        //else
                        //{
                        //    ViewBag.SaveOperationResult = "Failed to save the evaluation.";
                        //}

                        //return View(response);

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
