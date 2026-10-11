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
using ExcelDataReader.Log;
using Risebird.CommonHelper;
using static SkiaSharp.HarfBuzz.SKShaper;
using Newtonsoft.Json.Linq;
using Risebird.Web.Helpers;
using Risebird.Web.Services;
using Microsoft.Extensions.Configuration;

namespace Risebird.Web.Controllers
{
    //[Authorize]
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
        private readonly IPdfReaderHelper _pdfReaderHelper;
        private readonly ITranscriptOptimizationHelper _transcriptOptimizationHelper;
        private readonly AdvancedResumeScreeningService _advancedResumeScreeningService;
        private readonly ResumeScreeningBackgroundQueue _resumeScreeningQueue;
        private readonly IConfiguration _configuration;

        private readonly IHttpClientFactory _httpClientFactory;

        public AIScreenerController(IAssistantServiceHelper assistantServiceHelper,
            IApplicationErrorLogger applicationErrorLogger, ICandidateService candidateService,
            IIRFService irfService, IFileMimeTypeHelper fileMimeTypeHelper,
            IAzureStorageHelper azureStorageHelper, IInterviewerService interviewerService,
            IIRFRepository irfRepository, IMeetingService meetingService,
            IActivityTrackerService activityTrackerService, IPdfReaderHelper pdfReaderHelper,
            ITranscriptOptimizationHelper transcriptOptimizationHelper,
            AdvancedResumeScreeningService advancedResumeScreeningService,
            ResumeScreeningBackgroundQueue resumeScreeningQueue, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _advancedResumeScreeningService = advancedResumeScreeningService;
            _resumeScreeningQueue = resumeScreeningQueue;
            _configuration = configuration;
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
            _pdfReaderHelper = pdfReaderHelper;
            _transcriptOptimizationHelper = transcriptOptimizationHelper;
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
                        //HttpClient myWebClient = _httpClientFactory.CreateClient();
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


        public async Task<IActionResult> ScreenCandidateV2(string emailId, int irfId, int candidateMasterId, string accessType = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin" || (!string.IsNullOrEmpty(accessType) && accessType.ToLower() == "public" && profileName == ProfileNames.Client))
                    {
                        var details = await _candidateService.GetDetailsForCandidateResumeScreeningByAI(irfId, candidateMasterId);
                        if (details == null)
                        {
                            // The advanced page reports a missing candidate; the V2 view has no empty state.
                            return RedirectToAction(nameof(AdvancedScreenCandidate), new { emailId, irfId, candidateMasterId, accessType });
                        }

                        // Advanced (rubric-based) screening supersedes this one. Its result is shown
                        // whenever there is one, and a candidate never screened goes straight to it.
                        // A stored V2 result still renders here, with an option to re-screen on the
                        // rubric. V2 screening only runs when the advanced screener is not configured.
                        if (!string.IsNullOrEmpty(details.AIAdvancedScreeningResultJson)
                            || (IsAdvancedScreeningConfigured() && string.IsNullOrEmpty(details.AIScreeningResultJsonV2)))
                        {
                            return RedirectToAction(nameof(AdvancedScreenCandidate), new { emailId, irfId, candidateMasterId, accessType });
                        }
                        ViewBag.CanRescreenAdvanced = IsAdvancedScreeningConfigured() && IsInternalScreeningProfile(profileName);
                        ViewBag.IRFId = irfId;
                        ViewBag.CandidateMasterId = candidateMasterId;
                        ViewBag.AccessType = accessType;

                        //check if the candidate's screening is already done and saved in db(as json string)

                        //var candidate = await _candidateService.GetCandidateOpenAIScreeningResults(irfId, candidateMasterId);

                        ViewBag.CandidateEmail = emailId;
                        ViewBag.JobTitle = details.JDTiltle;
                        ViewBag.ResumeLink = details?.ResumeLink;
                        ViewBag.IRFName = details.IRFName;
                        ViewBag.SaveOperationResult = "NA";

                        if (details != null && !string.IsNullOrEmpty(details.AIScreeningResultJsonV2))
                        {
                            return View(JsonConvert.DeserializeObject<CandidateResumeScreeningVM>(details.AIScreeningResultJsonV2));
                        }

                        #region
                        CandidateResumeScreeningVM candidateResumeScreeningData = null;

                        if (!string.IsNullOrEmpty(details.ResumeLink))
                        {
                            //undo the following comments that parsed the resume using open ai and initiated the variable candidateParsedData when you iron out the bugs/erratics of open ai and are ready to switch back to it as the first choice resume parser.

                            string resumeLinkToParse = string.Empty;
                            if (!details.ResumeLink.Contains(".pdf"))
                            {
                                resumeLinkToParse = await _azureStorageHelper.DownloadAndConvertFromDocToPdfAsync(details.ResumeLink);
                            }
                            else
                            {
                                resumeLinkToParse = details.ResumeLink;
                            }
                            var resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(resumeLinkToParse);

                            if (!string.IsNullOrEmpty(resumeStringContent))
                            {
                                var runResult = await _assistantServiceHelper.GenerateCandidateScreening(
                                    resumeStringContent,
                                    details.AssessmentGroup,
                                    useResumeParserCopyPrompt: true);
                                var replyText = runResult?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;

                                try
                                {
                                    //candidateResumeScreeningData = JsonConvert.DeserializeObject<CandidateResumeScreeningVM>(replyText);
                                    candidateResumeScreeningData = TryParseCandidateResumeScreeningEvaluation(replyText);

                                }
                                catch (Exception ex)
                                {

                                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                                }
                                var responseInJson = JsonConvert.SerializeObject(candidateResumeScreeningData);
                                //save the response in db
                                bool saveOperationResult = true;
                                if (!string.IsNullOrEmpty(responseInJson) && responseInJson != "null")
                                {
                                    saveOperationResult = await _candidateService.SaveCandidateOpenAIScreeningResultsV2(irfId, candidateMasterId, responseInJson, candidateResumeScreeningData.Verdict, candidateResumeScreeningData.TotalScore);
                                }

                                if (saveOperationResult)
                                {
                                    ViewBag.SaveOperationResult = "Evaluation saved successfully.";
                                }
                                else
                                {
                                    ViewBag.SaveOperationResult = "Failed to save the evaluation.";
                                }

                            }
                        }
                        else
                        {
                            //candidateResumeScreeningData.ErrorMessage = "Resume Link missing!";
                            ViewBag.ErrorMessage = "Resume Link missing!";
                        }
                        return View(candidateResumeScreeningData);

                        #endregion



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


        private CandidateResumeScreeningVM TryParseCandidateResumeScreeningEvaluation(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var cleaned = raw.Trim();

            // Strip ```json ... ``` wrappers if present
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(7).TrimStart();

            if (cleaned.EndsWith("```"))
                cleaned = cleaned.Substring(0, cleaned.Length - 3).TrimEnd();

            cleaned = cleaned.TrimStart('\n');

            //
            // 1️⃣ Strict Deserialize
            //
            try
            {
                var strict = JsonConvert.DeserializeObject<CandidateResumeScreeningVM>(cleaned);
                if (strict != null &&
                    (strict.MustHaveTechnicalCriteria != null ||
                     strict.EvaluationCriteriaScores != null ||
                     strict.SectionScores != null))
                {
                    return strict;
                }
            }
            catch
            {
                // fallback mode below
            }

            //
            // 2️⃣ Loose Fallback Parsing
            //
            try
            {
                var root = JsonConvert.DeserializeObject<JObject>(cleaned);
                if (root == null) return null;

                var result = new CandidateResumeScreeningVM
                {
                    TotalScore = root.Value<int?>("totalScore") ?? 0,
                    MaxScore = root.Value<int?>("maxScore") ?? 0,
                    StabilityNotes = root.Value<string>("stabilityNotes"),
                    EmployerFraudNotes = root.Value<string>("employerFraudNotes"),
                    FitmentSummary = root.Value<string>("fitmentSummary"),
                    Verdict = root.Value<string>("verdict")
                };

                // Must Have Criteria
                var mustArr = (JArray)root["mustHaveTechnicalCriteria"];
                if (mustArr != null)
                {
                    result.MustHaveTechnicalCriteria = mustArr.Select(m => new MustHaveTechnicalCriterion
                    {
                        CriterionId = m.Value<string>("criterionId"),
                        TechnicalSkill = m.Value<string>("technicalSkill"),
                        DepthExpected = m.Value<string>("depthExpected"),
                        ComplexityExpected = m.Value<string>("complexityExpected"),
                        IsPrimarySkill = m.Value<bool?>("isPrimarySkill") ?? false
                    }).ToList();
                }

                // Evaluation Criteria Scores
                var scoresArr = (JArray)root["evaluationCriteriaScores"];
                if (scoresArr != null)
                {
                    result.EvaluationCriteriaScores = scoresArr.Select(s => new EvaluationCriteriaScore
                    {
                        CriterionId = s.Value<string>("criterionId"),
                        TechnicalSkill = s.Value<string>("technicalSkill"),
                        Score = s.Value<int?>("score") ?? 0,
                        MaxScore = s.Value<int?>("maxScore") ?? 0,
                        Reason = s.Value<string>("reason")
                    }).ToList();
                }

                // Section Scores
                var secScoresNode = (JObject)root["sectionScores"];
                if (secScoresNode != null)
                {
                    result.SectionScores = secScoresNode.ToObject<SectionScores>();
                }

                // Disqualification Flags
                var flagsNode = (JObject)root["disqualificationFlags"];
                if (flagsNode != null)
                {
                    result.DisqualificationFlags = flagsNode.ToObject<DisqualificationFlags>();
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        // ------------------------------------------------------------------------------------
        // Advanced (rubric-based) resume screening, ported from screen.risebird.io. Each IRF
        // carries a three-section rubric (skills, work experience, education) of weighted
        // criteria; a resume is scored against it criterion by criterion, with a reason for each.
        // ------------------------------------------------------------------------------------

        private bool IsAdvancedScreeningConfigured()
        {
            return !string.IsNullOrWhiteSpace(_configuration["OpenAI:AdvancedScreenerPromptId"]);
        }

        /// <summary>The profiles that may run, re-run and configure screening - the same set ScreenCandidateV2 admits.</summary>
        private static bool IsInternalScreeningProfile(string profileName)
        {
            return profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin";
        }

        private (string Email, string ProfileName) CurrentSessionUser()
        {
            var email = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");
            var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
            return (email, profileName);
        }

        /// <param name="isRedo">Ignore the stored result and screen again (internal profiles only).</param>
        public async Task<IActionResult> AdvancedScreenCandidate(string emailId, int irfId, int candidateMasterId, string accessType = null, bool isRedo = false)
        {
            try
            {
                var (sessionEmail, profileName) = CurrentSessionUser();
                if (sessionEmail == null || profileName == null)
                {
                    return RedirectToAction("SignOut", "Session");
                }

                var isInternal = IsInternalScreeningProfile(profileName);
                var isPublicClient = !string.IsNullOrEmpty(accessType) && accessType.ToLower() == "public" && profileName == ProfileNames.Client;
                if (!isInternal && !isPublicClient)
                {
                    return RedirectToAction("SignOut", "Session");
                }

                ViewBag.CandidateEmail = emailId;
                ViewBag.IRFId = irfId;
                ViewBag.CandidateMasterId = candidateMasterId;
                ViewBag.AccessType = accessType;
                ViewBag.CanRescreen = isInternal;
                ViewBag.CanEditRubric = isInternal;

                var details = await _candidateService.GetDetailsForCandidateResumeScreeningByAI(irfId, candidateMasterId);
                if (details == null)
                {
                    ViewBag.ErrorMessage = "This candidate could not be found on the skill channel.";
                    return View((string)null);
                }

                ViewBag.JobTitle = details.JDTiltle;
                ViewBag.ResumeLink = details.ResumeLink;
                ViewBag.IRFName = details.IRFName;
                ViewBag.AIVerdict = details.AIResumeScreeningVerdict;
                ViewBag.AIScreeningScore = details.AIResumeScreeningScore;

                if (!string.IsNullOrEmpty(details.AIAdvancedScreeningResultJson) && !(isRedo && isInternal))
                {
                    return View((object)details.AIAdvancedScreeningResultJson);
                }

                var outcome = await _advancedResumeScreeningService.ScreenCandidateAsync(irfId, candidateMasterId, details.ResumeLink, sessionEmail);
                if (!outcome.Success)
                {
                    ViewBag.ErrorMessage = outcome.Error;
                    // A failed re-screen keeps showing the previous result rather than an empty page.
                    return View((object)(outcome.ResultJson ?? details.AIAdvancedScreeningResultJson));
                }

                ViewBag.SaveOperationResult = "Evaluation saved successfully.";
                ViewBag.AIVerdict = outcome.Verdict;
                ViewBag.AIScreeningScore = outcome.FitmentPercentage;
                return View((object)outcome.ResultJson);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.ErrorMessage = "Something went wrong screening this resume. Try again in a moment.";
                return View((string)null);
            }
        }

        /// <summary>
        /// Queues advanced screening for every candidate on the IRF with a resume and no advanced
        /// result yet. Returns at once; the work runs in ResumeScreeningBackgroundWorker.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> BulkAdvancedScreenCandidate(int irfId)
        {
            try
            {
                var (sessionEmail, profileName) = CurrentSessionUser();
                if (sessionEmail == null || !IsInternalScreeningProfile(profileName))
                {
                    return Json(new { success = false, message = "You are not allowed to screen candidates on this skill channel." });
                }
                if (!IsAdvancedScreeningConfigured())
                {
                    return Json(new { success = false, message = "Advanced resume screening is not configured (OpenAI:AdvancedScreenerPromptId)." });
                }

                var pending = await _candidateService.GetCandidatesForAdvancedResumeScreening(irfId);
                var count = pending?.Count() ?? 0;
                if (count == 0)
                {
                    return Json(new { success = true, message = "Every candidate with a resume on this skill channel has already been screened." });
                }

                if (!_resumeScreeningQueue.TryEnqueue(new ResumeScreeningBackgroundQueue.BulkScreenIrf(irfId, sessionEmail)))
                {
                    return Json(new { success = true, message = "Resume screening is already running for this skill channel. Refresh the page in a few minutes to see the AI verdicts." });
                }

                return Json(new { success = true, message = $"Resume screening has started for {count} candidate(s). It takes some time - refresh the page in a few minutes to see the AI verdicts." });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Failed to start resume screening. Try again after some time." });
            }
        }

        /// <summary>
        /// The review screen for an IRF's screening rubric. With no saved rubric - or with
        /// regenerate - a fresh one is generated and shown as an unsaved draft; nothing is stored
        /// until it is saved.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ScreeningCriteria(int irfId, bool regenerate = false)
        {
            try
            {
                var (sessionEmail, profileName) = CurrentSessionUser();
                if (sessionEmail == null || !IsInternalScreeningProfile(profileName))
                {
                    return RedirectToAction("SignOut", "Session");
                }

                var model = await _candidateService.GetIRFResumeScreeningCriteria(irfId);
                if (model == null)
                {
                    ViewBag.ErrorMessage = "This skill channel could not be found.";
                    model = new ResumeScreeningCriteriaVM { IRFId = irfId };
                }
                else if (!model.HasCriteria || regenerate)
                {
                    var (generated, error) = await _advancedResumeScreeningService.GenerateCriteriaAsync(model);
                    if (generated != null)
                    {
                        model = generated;
                        ViewBag.IsUnsavedDraft = true;
                    }
                    else
                    {
                        ViewBag.ErrorMessage = error;
                    }
                }

                ViewBag.Criteria = AdvancedResumeScreening.BuildCriteriaRows(model);
                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.ErrorMessage = "Something went wrong loading the screening rubric.";
                var empty = new ResumeScreeningCriteriaVM { IRFId = irfId };
                ViewBag.Criteria = AdvancedResumeScreening.BuildCriteriaRows(empty);
                return View(empty);
            }
        }

        /// <summary>
        /// Saves the rubric. The posted form carries only the criteria, so the stored record is
        /// re-read and the criteria overlaid, rather than trusting a posted copy of the IRF context.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScreeningCriteria(ResumeScreeningCriteriaVM model)
        {
            var (sessionEmail, profileName) = CurrentSessionUser();
            if (sessionEmail == null || !IsInternalScreeningProfile(profileName))
            {
                return RedirectToAction("SignOut", "Session");
            }

            try
            {
                var stored = await _candidateService.GetIRFResumeScreeningCriteria(model.IRFId);
                if (stored == null)
                {
                    ViewBag.ErrorMessage = "This skill channel could not be found.";
                    ViewBag.Criteria = AdvancedResumeScreening.BuildCriteriaRows(model);
                    return View(model);
                }

                var wasSaved = stored.HasCriteria;
                AdvancedResumeScreening.OverlayEditedCriteria(model, stored);

                // JD Title and Primary Skills are the two criteria the rubric cannot do without.
                if (string.IsNullOrWhiteSpace(stored.Title) || stored.JDTitleMaxScore < 1)
                {
                    ModelState.AddModelError(string.Empty, "JD Title needs a description and at least 1 point.");
                }
                if (string.IsNullOrWhiteSpace(stored.PrimarySkill) || stored.PrimarySkillsMaxScore < 1)
                {
                    ModelState.AddModelError(string.Empty, "Primary Skills needs a description and at least 1 point.");
                }
                if (AdvancedResumeScreening.BuildCriteriaRows(stored).Any(r => r.Score < 0))
                {
                    ModelState.AddModelError(string.Empty, "Weights cannot be negative. Use 0 to switch a criterion off.");
                }
                foreach (var overlong in AdvancedResumeScreening.OverlongCriteria(stored))
                {
                    ModelState.AddModelError(string.Empty, $"Shorten {overlong}.");
                }
                if (!ModelState.IsValid)
                {
                    ViewBag.IsUnsavedDraft = !wasSaved;
                    ViewBag.Criteria = AdvancedResumeScreening.BuildCriteriaRows(stored);
                    return View(stored);
                }

                if (!await _candidateService.SaveIRFResumeScreeningCriteria(stored, isAutoGenerated: false, savedBy: sessionEmail))
                {
                    ViewBag.ErrorMessage = "The screening rubric could not be saved. Try again in a moment.";
                    ViewBag.IsUnsavedDraft = !wasSaved;
                    ViewBag.Criteria = AdvancedResumeScreening.BuildCriteriaRows(stored);
                    return View(stored);
                }

                TempData["SuccessMessage"] = "Screening rubric saved. Candidates screened from now on are scored against it; re-screen a candidate to update an earlier result.";
                return RedirectToAction(nameof(ScreeningCriteria), new { irfId = model.IRFId });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ViewBag.ErrorMessage = "Something went wrong saving the screening rubric.";
                ViewBag.Criteria = AdvancedResumeScreening.BuildCriteriaRows(model);
                return View(model);
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
                            aiAnalysisModel = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(response.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value.Replace("```json", "").Replace("```", ""));
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
                var resultDe = await _activityTrackerService.DebugLogger($"AI Resume Screener function error : {ex.Message}, {ex.StackTrace}", "aiScreener", string.Empty);

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(null);
            }
        }

        /// <summary>
        /// Same pipeline as ScreenInterviewer; returns JSON for in-page DM Approval updates.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ScreenInterviewerAjax(string jdTitle, int irfId, int userId, string irfName, bool isRedo = false)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                {
                    return Json(new { ok = false, message = "Session expired." });
                }

                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                if (profileName != "Admin" && profileName != "SeniorHR" && profileName != "SupportAdmin"
                    && profileName != "InterviewerManagement" && profileName != "SeniorInterviewerManagement")
                {
                    return Json(new { ok = false, message = "Not authorized." });
                }

                var interviewer = await _irfService.GetInterviewerOpenAIScreeningResults(irfId, userId);
                if (interviewer == null || string.IsNullOrWhiteSpace(interviewer.ResumeLink))
                {
                    return Json(new { ok = false, message = "No resume on file to screen." });
                }

                if (!isRedo && !string.IsNullOrEmpty(interviewer.OpenAIScreeningResponse))
                {
                    return Json(new
                    {
                        ok = true,
                        cached = true,
                        aiVerdict = interviewer.AIVerdict,
                        aiScore = interviewer.AIScreeningScore,
                        reasoning = interviewer.OpenAIScreeningResponse
                    });
                }

                var resumeStringContent = await ReadPdfFromUrlAsync(interviewer.ResumeLink);
                if (string.IsNullOrWhiteSpace(resumeStringContent))
                {
                    return Json(new { ok = false, message = "Could not read resume PDF." });
                }

                var response = await _assistantServiceHelper.GenerateResumeAnalysis(resumeStringContent, jdTitle);
                var saveOperationResult = false;
                var aiVerdict = interviewer.AIVerdict;
                var aiScore = interviewer.AIScreeningScore;
                var reasoning = interviewer.OpenAIScreeningResponse;

                if (response != null)
                {
                    var aiAnalysisModel = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(
                        response.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value.Replace("```json", "").Replace("```", ""));
                    if (aiAnalysisModel != null)
                    {
                        saveOperationResult = await _irfService.SaveInterviewerOpenAIScreeningResultsV2(irfId, userId, aiAnalysisModel);
                        aiVerdict = aiAnalysisModel.verdict;
                        aiScore = aiAnalysisModel.fitmentScore;
                        reasoning = aiAnalysisModel.completeFitmentAnalysis;
                    }
                }

                if (!saveOperationResult)
                {
                    return Json(new { ok = false, message = "Failed to save screening results." });
                }

                return Json(new
                {
                    ok = true,
                    cached = false,
                    aiVerdict,
                    aiScore,
                    reasoning,
                    screenedAt = DateTime.UtcNow.ToString("o")
                });
            }
            catch (System.Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { ok = false, message = "Screening failed." });
            }
        }

        public async Task<string> ReadPdfFromUrlAsync(string pdfUrl)
        {

            try
            {
                using (HttpClient client = _httpClientFactory.CreateClient())
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
                var resultDe = await _activityTrackerService.DebugLogger($"AI Resume Screener function error : {ex.Message}, {ex.StackTrace}", "aiScreener", string.Empty);

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
                // 1. Validate session first
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                {
                    return Json(new { isfavourableVerdict = false, resumeLink = string.Empty, errorMessage = "Session expired. Please login again." });
                }

                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");
                var loggedInUser = HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");

                // 2. Validate authorization
                var allowedRoles = new[] { "Admin", "SeniorHR", "SupportAdmin", "InterviewerManagement", "SeniorInterviewerManagement" };
                if (!allowedRoles.Contains(profileName))
                {
                    return RedirectToAction("SignOut", "Session");
                }

                // 3. Get and validate form data
                var file = Request.Form.Files;
                var nvc = Request.Form;

                if (!nvc.TryGetValue("emailId", out var emailId) || string.IsNullOrWhiteSpace(emailId))
                {
                    return Json(new { isfavourableVerdict = false, resumeLink = string.Empty, errorMessage = "Email ID is required." });
                }

                if (!nvc.TryGetValue("primarySpecialization", out var primarySpecialization) || string.IsNullOrWhiteSpace(primarySpecialization))
                {
                    return Json(new { isfavourableVerdict = false, resumeLink = string.Empty, errorMessage = "Primary Specialization is required." });
                }

                // 4. Validate file upload
                if (file == null || file.Count == 0)
                {
                    return Json(new { isfavourableVerdict = false, resumeLink = string.Empty, errorMessage = "Please select a file." });
                }

                var uploadedFile = file[0];

                // Validate file size (max 10MB)
                const long maxFileSize = 10 * 1024 * 1024;
                if (uploadedFile.Length > maxFileSize)
                {
                    return Json(new { isfavourableVerdict = false, resumeLink = string.Empty, errorMessage = "File size exceeds 10MB limit." });
                }

                var fileName = ContentDispositionHeaderValue.Parse(uploadedFile.ContentDisposition).FileName.Trim('"');
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(uploadedFile);

                // 5. Validate file type
                var allowedMimeTypes = new[]
                {
                    "application/pdf",
                    "application/msword",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                };

                if (!allowedMimeTypes.Contains(fileMimeType))
                {
                    return Json(new { isfavourableVerdict = false, resumeLink = string.Empty, errorMessage = "Invalid file type. Only PDF, DOC, and DOCX files are allowed." });
                }

                // 6. Upload file
                await _azureStorageHelper.InsertBlob(fileName, "resumes", uploadedFile);
                var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                var blob = containerRef.GetBlockBlobReference(fileName);
                string uploadedFileURL = blob.Uri.ToString();

                // 7. Process resume
                var resumeStringContent = await ReadPdfFromUrlAsync(uploadedFileURL);
                if (string.IsNullOrEmpty(resumeStringContent))
                {
                    return Json(new { isfavourableVerdict = false, resumeLink = uploadedFileURL, errorMessage = "Failed to read resume content." });
                }

                var response = await _assistantServiceHelper.GenerateResumeAnalysis(resumeStringContent, primarySpecialization);

                if (response == null)
                {
                    return Json(new { isfavourableVerdict = false, resumeLink = uploadedFileURL, errorMessage = "Failed to generate verdict. Please try again." });
                }

                // 8. Parse AI response
                InterviewerResumeFitmentAnalysisSchema aiAnalysisModel = null;
                try
                {
                    var jsonContent = response.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value
                        ?.Replace("```json", "").Replace("```", "");

                    if (string.IsNullOrEmpty(jsonContent))
                    {
                        return Json(new { isfavourableVerdict = false, resumeLink = uploadedFileURL, errorMessage = "AI response is empty." });
                    }

                    aiAnalysisModel = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(jsonContent);
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    return Json(new { isfavourableVerdict = false, resumeLink = uploadedFileURL, errorMessage = "Failed to parse AI response." });
                }

                if (aiAnalysisModel == null)
                {
                    return Json(new { isfavourableVerdict = false, resumeLink = uploadedFileURL, errorMessage = "Failed to parse AI response." });
                }

                // 9. Save verdict
                var saveOperationResult = await _interviewerService.SaveInterviewerPreOnboardingVerdictV2(
                    emailId,
                    primarySpecialization,
                    aiAnalysisModel,
                    loggedInUser);

                if (!saveOperationResult.Success)
                {
                    return Json(new { isfavourableVerdict = false, resumeLink = uploadedFileURL, errorMessage = saveOperationResult.ErrorMessage });
                }

                int preOnboardingVerdictId = saveOperationResult.Id ?? 0;

                // 10. Generate response URL
                var aiResponseUrl = Url.Action("PreOnboardingAIVerdictDetails", "AIScreener",
                    new { emailId = emailId, primarySpecialization = primarySpecialization, resumeLink = uploadedFileURL },
                    Request.Scheme);

                // 11. Evaluate verdict with improved logic
                string aiVerdict = aiAnalysisModel.verdict?.Trim().ToLowerInvariant();

                if (string.IsNullOrEmpty(aiVerdict))
                {
                    return Json(new { isfavourableVerdict = false, resumeLink = uploadedFileURL, aiResponseHref = aiResponseUrl, errorMessage = "AI verdict is empty." });
                }

                // Use more precise matching - check for exact words not substrings
                bool isFavorable = aiVerdict != null && aiVerdict.ToLower().Contains("yes");
                bool isUnfavorable = aiVerdict == "no" || aiVerdict.StartsWith("no,") || aiVerdict.StartsWith("no.");

                if (isFavorable)
                {
                    return Json(new { isfavourableVerdict = true, ResumeLink = uploadedFileURL, aiResponseHref = aiResponseUrl, preOnboardingVerdictId = preOnboardingVerdictId, errorMessage = string.Empty });
                }
                else if (isUnfavorable)
                {
                    return Json(new { isfavourableVerdict = false, ResumeLink = uploadedFileURL, aiResponseHref = aiResponseUrl, errorMessage = aiAnalysisModel.verdict });
                }
                else
                {
                    // Don't bypass - require manual review for unclear verdicts
                    return Json(new { isfavourableVerdict = false, ResumeLink = uploadedFileURL, aiResponseHref = aiResponseUrl, errorMessage = $"Unclear AI verdict: '{aiAnalysisModel.verdict}'. Manual review required." });
                }
            }
            catch (Exception ex)
            {
                await _activityTrackerService.DebugLogger(
                    $"ScreenInterviewerBeforeOnboarding function error : {ex.Message}, {ex.StackTrace}",
                    "openai",
                    string.Empty);
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
                                        aiAnalysisModel = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(response.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value.Replace("```json", "").Replace("```", ""));

                                        string AIVerdict = aiAnalysisModel.verdict;
                                        if (!string.IsNullOrEmpty(AIVerdict) && (AIVerdict.ToLower().Contains("yes") || AIVerdict.ToLower() == "no"))
                                        {
                                            saveOperationResult = await _interviewerService.SaveApplicantInterviewerAIVerdictV2(id, aiAnalysisModel, loggedInUser);
                                            if (saveOperationResult)
                                            {
                                                aiResponseUrl = Url.Action("ApplicantAIEvaluation", "AIScreener",
                                                new { id = id },
                                                Request.Scheme);

                                            }
                                            if (AIVerdict.ToLower().Contains("yes"))// == "yes"
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
                                                    string uniqueid;
                                                    // AddInterviewerProfile returns false when Azure AD B2C provisioning fails, and
                                                    // then no interviewer profile was written at all. Reporting a favourable verdict
                                                    // regardless would silently drop an applicant who passed screening.
                                                    if (!_interviewerService.AddInterviewerProfile(obj, loggedInUser, out agList, out userId, out uniqueid, true))
                                                    {
                                                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Error,
                                                            "AI screening passed but the interviewer profile could not be created, so the applicant was not onboarded.",
                                                            aiEvaluation.EmailId);
                                                        return Json(new { isfavourableVerdict = false, aiResponseHref = aiResponseUrl, errorMessage = "The candidate passed screening, but their interviewer profile could not be created. Please retry, or add them manually.", aiAnalysisScore = aiEvaluation.AIAnalysisScore });
                                                    }
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
                                aiAnalysisModel = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(response.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value.Replace("```json", "").Replace("```", ""));

                                string AIVerdict = aiAnalysisModel.verdict;
                                if (!string.IsNullOrEmpty(AIVerdict) && (AIVerdict.ToLower().Contains("yes") || AIVerdict.ToLower() == "no"))
                                {
                                    saveOperationResult = await _interviewerService.SaveApplicantInterviewerAIVerdictV2(id, aiAnalysisModel, aiEvaluation.EmailId, aiEvaluation.ResumeLink);
                                    if (saveOperationResult)
                                    {
                                        aiResponseUrl = Url.Action("ApplicantAIEvaluation", "AIScreener",
                                        new { id = id },
                                        Request.Scheme);

                                    }
                                    if (AIVerdict.ToLower().Contains("yes"))//== "yes"
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
                                            string uniqueid;
                                            // As above: a false return means no interviewer profile was written, so the
                                            // applicant must not be reported as successfully screened.
                                            if (!_interviewerService.AddInterviewerProfile(obj, aiEvaluation.EmailId, out agList, out userId, out uniqueid, true))
                                            {
                                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Error,
                                                    "AI screening passed but the interviewer profile could not be created, so the applicant was not onboarded.",
                                                    aiEvaluation.EmailId);
                                                return Json(new { isfavourableVerdict = false, aiResponseHref = aiResponseUrl, errorMessage = "The candidate passed screening, but their interviewer profile could not be created. Please retry, or add them manually.", aiAnalysisScore = aiEvaluation.AIAnalysisScore });
                                            }
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
                            if (model != null && model.Rubric != null)
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
                                    if (skill.CoreConcepts != null)
                                    {
                                        techSkill.Concepts = string.Join(",", skill.CoreConcepts);
                                    }
                                    techSkill.Natureofquestion = skill.KeyExpectations;
                                    techSkill.Timelimit = Convert.ToInt16(skill.EvaluationTime);
                                    techSkill.isEliminationskill = (skill.Mandatory != null && skill.Mandatory.ToLower() == "yes") ? true : false;

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
                if (!CompletedInterviewsViewMode.TryAuthorizeEvidenceEndpoints(HttpContext, out _))
                {
                    return RedirectToAction("SignOut", "Session");
                }

                var report = await _meetingService.GetInteviewReport(interviewId, irfId);

                ViewBag.InterviewerCodedName = report?.NickName;
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


                // OPTIMIZATION: Validate transcript link before attempting download
                if (string.IsNullOrEmpty(report.AudioTranscriptFileLink))
                {
                    ViewBag.SaveOperationResult = "Transcript link is missing. Cannot perform analysis.";
                    return View("SavedTransciptAnalysis", string.Empty);
                }

                string transcriptText;
                using (HttpClient client = _httpClientFactory.CreateClient())
                {
                    transcriptText = await client.GetStringAsync(report.AudioTranscriptFileLink);
                }

                // OPTIMIZATION: Pre-screen transcript for validity (saves API calls on corrupted/empty transcripts)
                if (!string.IsNullOrEmpty(transcriptText))
                {
                    var (isValid, validationReason) = _transcriptOptimizationHelper.ValidateTranscriptForAnalysis(transcriptText);
                    if (!isValid)
                    {
                        await _activityTrackerService.DebugLogger($"TranscriptAnalysis: Interview {interviewId} validation failed: {validationReason}", "transcriptOptimization", string.Empty);
                        ViewBag.SaveOperationResult = $"Transcript validation failed: {validationReason}";
                        return View("SavedTransciptAnalysis", string.Empty);
                    }

                    // OPTIMIZATION: Reduce token usage by removing filler words, timestamps, and excessive whitespace
                    var estimatedTokensBefore = _transcriptOptimizationHelper.EstimateTokenCount(transcriptText);
                    transcriptText = _transcriptOptimizationHelper.OptimizeTranscriptText(transcriptText);
                    var estimatedTokensAfter = _transcriptOptimizationHelper.EstimateTokenCount(transcriptText);

                    await _activityTrackerService.DebugLogger($"TranscriptAnalysis: Interview {interviewId} optimization: {estimatedTokensBefore} tokens -> {estimatedTokensAfter} tokens (saved {estimatedTokensBefore - estimatedTokensAfter})", "transcriptOptimization", string.Empty);
                }
                else
                {
                    await _activityTrackerService.DebugLogger($"TranscriptAnalysis: Interview {interviewId} transcript is empty or could not be downloaded", "transcriptOptimization", string.Empty);
                    ViewBag.SaveOperationResult = "Transcript is empty or could not be downloaded.";
                    return View("SavedTransciptAnalysis", string.Empty);
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
                    try
                    {
                        var jsonToProcess = aiAnalysis.Data?.First()?.Content?.FirstOrDefault()?.Text?.Value.Replace("```json", "").Replace("```", "");
                        var modelset = JsonConvert.DeserializeObject<TranscriptAnalysisSchema>(jsonToProcess);
                        //TransScript Analysis save to db
                        bool serviceResult = await _meetingService.SaveQcInterviewAIevaluation(interviewId, modelset, jsonToProcess, true);
                    }
                    catch (Exception ex)
                    {

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

            }
            catch (System.Exception ex)
            {

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(null);
            }
        }
    }
}
