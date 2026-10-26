using DinkToPdf;
using DinkToPdf.Contracts;
using ExcelDataReader.Log;
using Google.Apis.Drive.v3.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rest;
using RestSharp;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository;
using Risebird.Repository.Interface;
using Risebird.Web.Helpers;
using Risebird.Web.Services.Proctoring;
using Risebird.Web.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Twilio.Types;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace Risebird.Web.Controllers
{
    public class AIInterviewController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private static readonly Guid SessionId; //= Guid.NewGuid();
        private readonly IInterviewApiService _api;
        private readonly IOpenAISpeechToTextService _speechToTextService;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly ISASUrlHelper _sasUrlHelper;
        private readonly IAIInterviewerService _aiinterviewerService;
        private readonly ISynthesiaTextToVideoService _synthesiaTextToVideoService;
        private readonly IPdfReaderHelper _pdfReaderHelper;
        private readonly IIRFService _iRFService;
        private readonly IAWSRekognitionService _aWSRekognitionService;
        private readonly ICandidateService _candidateService;
        private readonly IIRFRepository _irfRepository;
        private readonly IOpenAIInterviewerAssistantHelper _openAIAssistantHelper;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IProctoringTokenService _proctoringTokenService;
        private readonly IProctoringSessionEventRepository _proctoringSessionEventRepository;
        private readonly IProctoringScreenshotAnalysisService _proctoringScreenshotAnalysisService;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConverter _converter;
        private readonly ICodeExecutionService _codeExecutionService;
        private readonly IMcqAssessmentService _mcqAssessmentService;
        private readonly ICodeProjectQuestionService _codeProjectQuestionService;
        private readonly Risebird.Web.Services.CodeProjectBackgroundQueue _codeProjectQueue;

        private readonly IHttpClientFactory _httpClientFactory;

        public AIInterviewController(IConfiguration configuration, IAzureStorageHelper azureStorageHelper,
            IApplicationErrorLogger applicationErrorLogger, IInterviewApiService api,
            IOpenAISpeechToTextService speechToTextService, IFileMimeTypeHelper fileMimeTypeHelper,
            ISASUrlHelper sasUrlHelper, IAIInterviewerService aiinterviewerService,
            ISynthesiaTextToVideoService synthesiaTextToVideoService, IPdfReaderHelper pdfReaderHelper,
            IIRFService iRFService, IAWSRekognitionService aWSRekognitionService, ICandidateService candidateService,
            IIRFRepository irfRepository, IOpenAIInterviewerAssistantHelper openAIAssistantHelper,
            IAssistantServiceHelper assistantServiceHelper,
            IProctoringTokenService proctoringTokenService, IProctoringSessionEventRepository proctoringSessionEventRepository,
            IProctoringScreenshotAnalysisService proctoringScreenshotAnalysisService,
            ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider,
            IHttpContextAccessor contextAccessor, IConverter converter,
            ICodeExecutionService codeExecutionService,
            IMcqAssessmentService mcqAssessmentService,
            ICodeProjectQuestionService codeProjectQuestionService,
            Risebird.Web.Services.CodeProjectBackgroundQueue codeProjectQueue, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _codeProjectQuestionService = codeProjectQuestionService;
            _codeProjectQueue = codeProjectQueue;
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _api = api;
            _speechToTextService = speechToTextService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _sasUrlHelper = sasUrlHelper;
            _aiinterviewerService = aiinterviewerService;
            _synthesiaTextToVideoService = synthesiaTextToVideoService;
            _pdfReaderHelper = pdfReaderHelper;
            _iRFService = iRFService;
            _aWSRekognitionService = aWSRekognitionService;
            _candidateService = candidateService;
            _irfRepository = irfRepository;
            _openAIAssistantHelper = openAIAssistantHelper;
            _assistantServiceHelper = assistantServiceHelper;
            _proctoringTokenService = proctoringTokenService;
            _proctoringSessionEventRepository = proctoringSessionEventRepository;
            _proctoringScreenshotAnalysisService = proctoringScreenshotAnalysisService;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _contextAccessor = contextAccessor;
            _converter = converter;
            _codeExecutionService = codeExecutionService;
            _mcqAssessmentService = mcqAssessmentService;
        }

        /// <summary>
        /// Serves the next question of a preset interview.
        ///
        /// If the channel deals question sets, the interview is bound to exactly one set on first use
        /// and every question from then on comes from that set alone. If a set cannot be resolved the
        /// interview stops here: widening the query back to the whole channel pool would let two
        /// candidates in a batch draw the same paper, which is the leak the sets exist to prevent.
        ///
        /// A channel with no sets keeps the pre-feature picker, so channels that have not adopted the
        /// feature behave exactly as they did before.
        /// </summary>
        private async Task<TechSkillComments> GetNextPresetQuestionAsync(int interviewId, int irfId)
        {
            var resolution = await _iRFService.ResolveNextPresetQuestion(interviewId);

            if (resolution == null || resolution.UsesQuestionSets)
            {
                if (resolution == null || !resolution.SetResolved)
                {
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Error,
                        $"Could not resolve an AI question set for InterviewId {interviewId} on IRFId {irfId}. Refusing to serve questions from outside a set. {resolution?.Message}");
                    return null;
                }

                return resolution.Question;
            }

            return await _aiinterviewerService.GetRandomQuestionForSession(interviewId, irfId);
        }
        /// <summary>
        /// access an interview (and start it) with the session id (sid)
        /// </summary>
        /// <param name="sid"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Index(Guid sid)
        {
            try
            {
                //get all the data relevant interview data for that sid
                var meetingDetails = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(sid);

                //two scenarios possible, one where the interview hasnt started as yet. The second where the interview has already started.
                if (meetingDetails != null)
                {
                    if (IsAadhaarVerificationBlocking(meetingDetails))
                        return AadhaarRequiredExitView(sid, meetingDetails);

                    await ApplyAiProctoringViewBagAsync(meetingDetails, sid);
                    ViewBag.CandidateTranscriptReview = IsCandidateTranscriptReviewEnabled(meetingDetails);
                    if (meetingDetails.AIInterviewType == (int)AIInterviewTypeEnum.Preset)
                    {
                        var transcript = await _aiinterviewerService.GetAIInterviewTranscriptBySessionId(sid);

                        //not started yet
                        if (transcript == null || transcript.Count() == 0)
                        {
                            var sampleQuestion = await GetNextPresetQuestionAsync(meetingDetails.InterviewId, meetingDetails.irfIntId);

                            if (sampleQuestion != null)
                            {
                                var model = new ChatMessageViewModel();
                                model.SessionId = sid;
                                model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(sampleQuestion.Message), sampleQuestionId = sampleQuestion.id });

                                //var latestReply = CleanMessage(sampleQuestion.Message);

                                if (!string.IsNullOrEmpty(sampleQuestion.Message))
                                {
                                    model.SynthesizedVideoUrl = sampleQuestion.AIQuestionVideoLink;
                                    model.CurrentQuestion = CleanMessage(sampleQuestion.Message);
                                    model.CurrentQuestionId = sampleQuestion.id;
                                    model.IsTypingSpeedQuestion = sampleQuestion.IsTypingSpeedAssessment;
                                    model.IsListeningComprehensionQuestion = sampleQuestion.IsListeningComprehension;
                                    model.ListeningAudioUrl = sampleQuestion.ListeningAudioUrl;
                                    model.ListeningAudioTranscription = sampleQuestion.ListeningAudioTranscription;
                                    model.IsCodingQuestion = sampleQuestion.IsCodingQuestion;
                                    model.CodingTimeLimitInMinutes = sampleQuestion.CodingTimeLimitInMinutes;
                                    model.IsCodeProjectQuestion = sampleQuestion.IsCodeProjectQuestion;

                                    // Fetch random typing sample text from database if it's a typing speed question
                                    if (sampleQuestion.IsTypingSpeedAssessment)
                                    {
                                        var typingSample = await _aiinterviewerService.GetRandomTypingSpeedSampleText();
                                        if (typingSample != null && !string.IsNullOrWhiteSpace(typingSample.SampleText))
                                        {
                                            model.TypingSampleTextFromDB = typingSample.SampleText;
                                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Debug,
                                                new Exception($"Typing sample text loaded successfully. Length: {typingSample.SampleText.Length}"));
                                        }
                                        else
                                        {
                                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                                                new Exception("No typing sample text available in database. Please add sample texts to TypingSpeedSampleTexts table."));
                                        }
                                    }

                                    await _aiinterviewerService.LogTranscript(sid, "assistant", model.CurrentQuestion, sampleQuestion.id);
                                    QueueCodeProjectLookAhead(sid.ToString(), meetingDetails.InterviewId, meetingDetails.irfIntId, model);

                                }


                                model.IsAdaptive = meetingDetails.IsAdaptive;
                                model.InterviewId = meetingDetails.InterviewId;
                                model.IRFId = meetingDetails.irfIntId;
                                model.TimeLeftInSeconds = meetingDetails.TimeLeftInSeconds;
                                model.candidateName = meetingDetails.candidateName;
                                model.AssistantId = meetingDetails.AssistantId;
                                model.AIInterviewType = meetingDetails.AIInterviewType;
                                model.showChatBox = meetingDetails.showChatBox;
                                model.isMultiLingual = meetingDetails.isMultiLingual;
                                return View(model);
                            }
                            else
                            {
                                var exitModel = new AIInterviewExitModalViewModel
                                {
                                    Title = "No Questions",
                                    Message = "Error fetching questions.(NQ)",
                                    IconClass = "fa-exclamation-triangle",
                                    IconColor = "#dc3545",
                                    ButtonText = "Close",
                                    ButtonClass = "btn-danger",
                                    CandidateName = meetingDetails.candidateName ?? "Candidate",
                                    InterviewDate = meetingDetails.interviewDate,
                                    SessionId = sid.ToString(),
                                    ExitTime = DateTime.Now,
                                    IsFromIndex = true // Add this flag to identify the source
                                };
                                return View("ExitModalPartial", exitModel);
                            }

                        }
                        else
                        {
                            var model = new ChatMessageViewModel();
                            model.SessionId = sid;

                            //check the first entry in the transcript and check the role to determine who made that response. That is the lastest for that session.

                            var latestEntry = transcript?.FirstOrDefault();

                            if (latestEntry != null)
                            {
                                var allQuestions = transcript.Where(x => x.Role.ToLower() == "assistant");
                                //if (allQuestions != null && allQuestions.Count() <= 10)
                                if (allQuestions != null && allQuestions.Count() <= 35)
                                //if(allQuestions != null && allQuestions.Count() <= 3)
                                {
                                    //if (latestEntry.Role.ToLower() == "assistant" && allQuestions.Count() <= 10)
                                    if (latestEntry.Role.ToLower() == "assistant" && allQuestions.Count() <= 35)
                                    //if (latestEntry.Role.ToLower() == "assistant" && allQuestions.Count() <= 3)
                                    {
                                        var candidateLatestEntry = transcript.Where(x => x.Role.ToLower() == "user")?.FirstOrDefault();

                                        var latestQuestion = latestEntry.Message;
                                        model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(latestEntry.Message), sampleQuestionId = latestEntry.DeliveryHeadQuestionId });

                                        if (candidateLatestEntry != null)
                                        {
                                            model.ChatLog.Add(new ChatLine { Role = "User", Message = candidateLatestEntry.Message });

                                        }
                                        //foreach (var question in allQuestions)
                                        //{
                                        //    model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(question.Message), sampleQuestionId = question.DeliveryHeadQuestionId });
                                        //}

                                        if (!string.IsNullOrEmpty(latestQuestion))
                                        {
                                            model.SynthesizedVideoUrl = latestEntry.AIQuestionVideoLink;
                                            model.CurrentQuestion = latestQuestion;
                                            model.CurrentQuestionId = latestEntry.DeliveryHeadQuestionId;
                                            await ApplyCodingQuestionAttributesAsync(model, latestEntry.DeliveryHeadQuestionId);
                                        }
                                    }
                                    //else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() < 10)
                                    else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() < 35)
                                    //else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() < 3)
                                    {
                                        model.ChatLog.Add(new ChatLine { Role = "User", Message = latestEntry.Message });
                                        //var replies = await _api.SendReply(sid, latestEntry.Message, meetingDetails.AssistantId);

                                        //foreach (var reply in replies)
                                        //{
                                        //    model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(reply) });
                                        //}
                                        //var latestReply = CleanMessage(replies.FirstOrDefault());

                                        // Guard: only move to the next question if the candidate video for the previously asked
                                        // assistant question was successfully saved. This prevents transcript-only advancement on refresh.
                                        var lastAssistantQuestion = transcript
                                            .FirstOrDefault(x => x.Role != null && x.Role.ToLower() == "assistant");
                                        var lastAssistantQuestionId = lastAssistantQuestion?.DeliveryHeadQuestionId;

                                        // The MCQ assessment section needs no exemption here: it writes no
                                        // assistant transcript entry, so the last assistant question is always
                                        // a spoken one, which does have a video to wait for.
                                        if (lastAssistantQuestionId.HasValue)
                                        {
                                            var savedVideos = await _aiinterviewerService.GetBotInterviewVideoDetails(sid);
                                            var hasCandidateVideoForLastQuestion = savedVideos != null &&
                                                savedVideos.Any(v =>
                                                    v.DeliveryHeadQuestionId == lastAssistantQuestionId.Value &&
                                                    !string.IsNullOrWhiteSpace(v.VideoLink));

                                            if (!hasCandidateVideoForLastQuestion)
                                            {
                                                // Do NOT log a new assistant message or fetch a new question.
                                                // Keep the candidate on the last asked question until the video is saved.
                                                model.ChatLog.Add(new ChatLine
                                                {
                                                    Role = "Bot",
                                                    Message = CleanMessage(lastAssistantQuestion?.Message),
                                                    sampleQuestionId = lastAssistantQuestionId.Value
                                                });

                                                model.SynthesizedVideoUrl = lastAssistantQuestion?.AIQuestionVideoLink;
                                                model.CurrentQuestion = CleanMessage(lastAssistantQuestion?.Message);
                                                model.CurrentQuestionId = lastAssistantQuestionId.Value;
                                                await ApplyCodingQuestionAttributesAsync(model, lastAssistantQuestionId.Value);

                                                // Optional flag for the view to show a warning if desired.
                                                ViewData["VideoUploadError"] = true;

                                                model.IsAdaptive = meetingDetails.IsAdaptive;
                                                model.InterviewId = meetingDetails.InterviewId;
                                                model.IRFId = meetingDetails.irfIntId;
                                                model.TimeLeftInSeconds = meetingDetails.TimeLeftInSeconds;
                                                model.candidateName = meetingDetails.candidateName;
                                                model.AssistantId = meetingDetails.AssistantId;
                                                model.AIInterviewType = meetingDetails.AIInterviewType;
                                                model.showChatBox = meetingDetails.showChatBox;
                                                model.isMultiLingual = meetingDetails.isMultiLingual;

                                                return View(model);
                                            }
                                        }

                                        var sampleQuestion = await GetNextPresetQuestionAsync(meetingDetails.InterviewId, meetingDetails.irfIntId);

                                        if (sampleQuestion != null)
                                        {
                                            model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(sampleQuestion.Message), sampleQuestionId = sampleQuestion.id });

                                            //var latestReply = CleanMessage(sampleQuestion.Message);

                                            if (!string.IsNullOrEmpty(sampleQuestion.Message))
                                            {
                                                model.SynthesizedVideoUrl = sampleQuestion.AIQuestionVideoLink;//latestEntry.AIQuestionVideoLink;
                                                model.CurrentQuestion = CleanMessage(sampleQuestion.Message);
                                                model.CurrentQuestionId = sampleQuestion.id;//latestEntry.DeliveryHeadQuestionId;
                                                model.IsTypingSpeedQuestion = sampleQuestion.IsTypingSpeedAssessment;
                                                model.IsListeningComprehensionQuestion = sampleQuestion.IsListeningComprehension;
                                                model.ListeningAudioUrl = sampleQuestion.ListeningAudioUrl;
                                                model.ListeningAudioTranscription = sampleQuestion.ListeningAudioTranscription;
                                                model.IsCodingQuestion = sampleQuestion.IsCodingQuestion;
                                                model.CodingTimeLimitInMinutes = sampleQuestion.CodingTimeLimitInMinutes;
                                                model.IsCodeProjectQuestion = sampleQuestion.IsCodeProjectQuestion;

                                                // Fetch random typing sample text from database if it's a typing speed question
                                                if (sampleQuestion.IsTypingSpeedAssessment)
                                                {
                                                    var typingSample = await _aiinterviewerService.GetRandomTypingSpeedSampleText();
                                                    if (typingSample != null && !string.IsNullOrWhiteSpace(typingSample.SampleText))
                                                    {
                                                        model.TypingSampleTextFromDB = typingSample.SampleText;
                                                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Debug,
                                                            new Exception($"Typing sample text loaded successfully. Length: {typingSample.SampleText.Length}"));
                                                    }
                                                    else
                                                    {
                                                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                                                            new Exception("No typing sample text available in database. Please add sample texts to TypingSpeedSampleTexts table."));
                                                    }
                                                }

                                                await _aiinterviewerService.LogTranscript(sid, "assistant", model.CurrentQuestion, sampleQuestion.id);
                                                QueueCodeProjectLookAhead(sid.ToString(), meetingDetails.InterviewId, meetingDetails.irfIntId, model);

                                            }
                                        }
                                        else
                                        {
                                            // Out of spoken questions. On a channel opted in to the
                                            // MCQ assessment section, serve that instead of closing;
                                            // it runs after the spoken questions, and a reload while
                                            // it is outstanding lands back on it.
                                            if (await TryServeMcqAssessmentSectionAsync(model, meetingDetails))
                                            {
                                                return View(model);
                                            }

                                            //check if the interview is already marked completed in the first details object above and if not, mark it as completed
                                            if (meetingDetails.MeetingStatus != (int)MeetingStatus.Completed)
                                            {
                                                await _aiinterviewerService.UpdateMeetingCompletedStatusForAIInterview(model.SessionId.ToString());

                                            }

                                            var exitModel = new AIInterviewExitModalViewModel
                                            {
                                                Title = "Interview Complete",
                                                Message = "The interviewer is through with all the questions. This interview is now closed.",
                                                IconClass = "fa-check-circle",
                                                IconColor = "#28a745",
                                                ButtonText = "Close",
                                                ButtonClass = "btn-success",
                                                CandidateName = meetingDetails.candidateName ?? "",
                                                InterviewDate = meetingDetails.interviewDate,
                                                SessionId = sid.ToString(),
                                                ExitTime = DateTime.Now,
                                                IsFromIndex = true // Add this flag to identify the source
                                            };
                                            return View("ExitModalPartial", exitModel);
                                        }


                                    }
                                    //else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() == 10)
                                    else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() == 35)
                                    //else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() == 3)
                                    {
                                        // INTERVIEW COMPLETE - Return ExitModalPartial view
                                        //check if the interview is already marked completed in the first details object above and if not, mark it as completed
                                        if (meetingDetails.MeetingStatus != (int)MeetingStatus.Completed)
                                        {
                                            await _aiinterviewerService.UpdateMeetingCompletedStatusForAIInterview(model.SessionId.ToString());

                                        }

                                        var exitModel = new AIInterviewExitModalViewModel
                                        {
                                            Title = "Interview Complete",
                                            Message = "The interviewer is through with all the questions. This interview is now closed.",
                                            IconClass = "fa-check-circle",
                                            IconColor = "#28a745",
                                            ButtonText = "Close",
                                            ButtonClass = "btn-success",
                                            CandidateName = meetingDetails.candidateName ?? "",
                                            InterviewDate = meetingDetails.interviewDate,
                                            SessionId = sid.ToString(),
                                            ExitTime = DateTime.Now,
                                            IsFromIndex = true // Add this flag to identify the source
                                        };
                                        return View("ExitModalPartial", exitModel);
                                    }
                                }
                                else
                                {
                                    // INTERVIEW COMPLETE - Return ExitModalPartial view
                                    //check if the interview is already marked completed in the first details object above and if not, mark it as completed
                                    if (meetingDetails.MeetingStatus != (int)MeetingStatus.Completed)
                                    {
                                        await _aiinterviewerService.UpdateMeetingCompletedStatusForAIInterview(model.SessionId.ToString());

                                    }
                                    var exitModel = new AIInterviewExitModalViewModel
                                    {
                                        Title = "Interview Complete",
                                        Message = "The interviewer is through with all the questions. This interview is now closed.",
                                        IconClass = "fa-check-circle",
                                        IconColor = "#28a745",
                                        ButtonText = "Close",
                                        ButtonClass = "btn-success",
                                        CandidateName = meetingDetails.candidateName ?? "",
                                        InterviewDate = meetingDetails.interviewDate,
                                        SessionId = sid.ToString(),
                                        ExitTime = DateTime.Now,
                                        IsFromIndex = true // Add this flag to identify the source
                                    };
                                    return View("ExitModalPartial", exitModel);
                                }


                            }
                            model.Message = "";
                            model.TimeLeftInSeconds = meetingDetails.TimeLeftInSeconds;
                            model.InterviewId = meetingDetails.InterviewId;
                            model.IRFId = meetingDetails.irfIntId;
                            model.candidateName = meetingDetails.candidateName;
                            model.IsAdaptive = meetingDetails.IsAdaptive;
                            model.AssistantId = meetingDetails.AssistantId;
                            model.AIInterviewType = meetingDetails.AIInterviewType;
                            model.showChatBox = meetingDetails.showChatBox;
                            model.isMultiLingual = meetingDetails.isMultiLingual;
                            return View(model);

                        }
                    }
                    else
                    {
                        //not started yet
                        if (string.IsNullOrEmpty(meetingDetails.ThreadId))
                        {

                            string resumeStringContent = string.Empty;
                            string techSkillsJson = string.Empty;
                            string jobDescriptionStringContent = string.Empty;

                            if (!string.IsNullOrEmpty(meetingDetails.resumeLink))
                            {
                                resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(meetingDetails.resumeLink);
                            }
                            if (!string.IsNullOrEmpty(meetingDetails.TechSkillsInJson))
                            {
                                techSkillsJson = meetingDetails.TechSkillsInJson;
                            }
                            // For adaptive AI interviews, the assistant now also expects the job description as a string.
                            // The JD file (pdf/doc) is stored against the skill channel/IRF; convert it to text the same way the resume is handled.
                            if (meetingDetails.AIInterviewType == (int)AIInterviewTypeEnum.Adaptive && !string.IsNullOrEmpty(meetingDetails.JDLink))
                            {
                                jobDescriptionStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(meetingDetails.JDLink);
                            }
                            string candidatePrompt;
                            if (!string.IsNullOrEmpty(resumeStringContent))
                            {
                                candidatePrompt = $"You will now interview the candidate for {meetingDetails.JDTiltle} according to the tech rubric in json here: {techSkillsJson}. The resume of the candidate in string format is as follows:{resumeStringContent ?? string.Empty}. The job description in string format is as follows:{jobDescriptionStringContent ?? string.Empty}. Proceed as per the instructions fed to you.";

                            }
                            else
                            {
                                candidatePrompt = $"You will now interview the candidate for {meetingDetails.JDTiltle} according to the tech rubric in json here: {techSkillsJson}. The candidate's resume is not available. The job description in string format is as follows:{jobDescriptionStringContent ?? string.Empty}. Proceed without the resume as per the instructions fed to you.";

                            }
                            // Ask the assistant to flag the end of the interview so we can auto-complete it (see AdaptiveInterviewCompletionMarker).
                            candidatePrompt += AdaptiveCompletionInstruction;
                            var result = await _api.StartConversation(sid, candidatePrompt, meetingDetails.AssistantId);

                            var model = new ChatMessageViewModel
                            {
                                SessionId = sid,
                                ChatLog = result.Replies.Select(r => new ChatLine { Role = "Bot", Message = CleanMessage(r) }).ToList(),

                            };


                            var latestReply = CleanMessage(result.Replies.FirstOrDefault());

                            if (!string.IsNullOrEmpty(latestReply))
                            {
                                var audioBytes = await _speechToTextService.ConvertTextToSpeechAsync(latestReply);
                                var base64Audio = Convert.ToBase64String(audioBytes);
                                ViewData["AssistantAudioBase64"] = base64Audio;
                                model.AIInterviewerAudioLink = await UploadAIInterviewerAudio(audioBytes, model.SessionId);
                                model.CurrentQuestion = latestReply;

                            }
                            //var videoId = await _synthesiaTextToVideoService.CreateVideoAsync(result.Replies.FirstOrDefault(), "your_template_id_here");

                            //// Wait/poll until video is ready (Synthesia usually takes 30–60s)
                            //string videoUrl;
                            //do
                            //{
                            //    await Task.Delay(8000);
                            //    videoUrl = await _synthesiaTextToVideoService.GetVideoUrlAsync(videoId);
                            //} while (string.IsNullOrWhiteSpace(videoUrl));

                            //model.SynthesizedVideoUrl = videoUrl;

                            //model.SynthesizedVideoUrl = "https://synthesia-ttv-data.s3.amazonaws.com/video_data/239be418-3e6d-4c92-916d-0a2054d81269/transfers/rendered_video.mp4?response-content-disposition=attachment%3Bfilename%3D%22AI%20Interviewer%20-%20Harsh.mp4%22&AWSAccessKeyId=AKIA32NGJ5TS3KPZZ23Z&Signature=cf9ZVjfK2wBH33gb7HsQDMGgeRM%3D&Expires=1752553930";
                            //ViewBag.IsAdaptive = true;
                            model.IsAdaptive = meetingDetails.IsAdaptive;
                            model.InterviewId = meetingDetails.InterviewId;
                            model.IRFId = meetingDetails.irfIntId;
                            model.TimeLeftInSeconds = meetingDetails.TimeLeftInSeconds;
                            model.candidateName = meetingDetails.candidateName;
                            model.AssistantId = meetingDetails.AssistantId;
                            model.AIInterviewType = meetingDetails.AIInterviewType;
                            model.showChatBox = meetingDetails.showChatBox;
                            model.isMultiLingual = meetingDetails.isMultiLingual;
                            return View(model);
                        }
                        else
                        {
                            var model = new ChatMessageViewModel();
                            model.SessionId = sid;
                            //get the entire transcript by session Id(in descending order by time). By entire, it means the responses of both the candidate and the assistant/aiinterviewer
                            var transcript = await _aiinterviewerService.GetAIInterviewTranscriptBySessionId(sid);

                            //check the first entry in the transcript and check the role to determine who made that response. That is the lastest for that session.

                            var latestEntry = transcript?.FirstOrDefault();

                            if (latestEntry != null)
                            {
                                var allQuestions = transcript.Where(x => x.Role.ToLower() == "assistant");
                                //if (allQuestions != null && allQuestions.Count() <= 10)
                                if (allQuestions != null && allQuestions.Count() <= 35)
                                //if(allQuestions != null && allQuestions.Count() <= 3)
                                {
                                    //if (latestEntry.Role.ToLower() == "assistant" && allQuestions.Count() <= 10)
                                    if (latestEntry.Role.ToLower() == "assistant" && allQuestions.Count() <= 35)
                                    //if (latestEntry.Role.ToLower() == "assistant" && allQuestions.Count() <= 3)
                                    {
                                        // Reload after the interviewer already concluded: finish instead of re-showing the closing message.
                                        if (MessageSignalsInterviewCompletion(latestEntry.Message))
                                        {
                                            model.candidateName = meetingDetails.candidateName;
                                            return await CompleteAdaptiveInterviewAsync(model);
                                        }

                                        var candidateLatestEntry = transcript.Where(x => x.Role.ToLower() == "user")?.FirstOrDefault();
                                        foreach (var question in allQuestions)
                                        {
                                            model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(question.Message) });
                                        }
                                        if (candidateLatestEntry != null)
                                        {
                                            model.ChatLog.Add(new ChatLine { Role = "User", Message = candidateLatestEntry.Message });

                                        }

                                        var latestQuestion = latestEntry.Message;
                                        if (!string.IsNullOrEmpty(latestQuestion))
                                        {
                                            var audioBytes = await _speechToTextService.ConvertTextToSpeechAsync(latestQuestion);
                                            var base64Audio = Convert.ToBase64String(audioBytes);
                                            ViewData["AssistantAudioBase64"] = base64Audio;
                                            model.AIInterviewerAudioLink = await UploadAIInterviewerAudio(audioBytes, model.SessionId);
                                            model.CurrentQuestion = latestQuestion;
                                        }
                                    }
                                    //else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() < 10)
                                    else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() < 35)
                                    //else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() < 3)
                                    {
                                        model.ChatLog.Add(new ChatLine { Role = "User", Message = latestEntry.Message });
                                        var replies = await _api.SendReply(sid, latestEntry.Message, meetingDetails.AssistantId);
                                        //var question = allQuestions.FirstOrDefault().Message;

                                        // The assistant may conclude the interview on this resume turn; if so, complete it.
                                        if (RepliesSignalInterviewCompletion(replies))
                                        {
                                            model.candidateName = meetingDetails.candidateName;
                                            return await CompleteAdaptiveInterviewAsync(model);
                                        }

                                        foreach (var reply in replies)
                                        {
                                            model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(reply) });
                                        }
                                        var latestReply = CleanMessage(replies.FirstOrDefault());

                                        if (!string.IsNullOrEmpty(latestReply))
                                        {
                                            var audioBytes = await _speechToTextService.ConvertTextToSpeechAsync(latestReply);
                                            var base64Audio = Convert.ToBase64String(audioBytes);
                                            ViewData["AssistantAudioBase64"] = base64Audio;
                                            model.AIInterviewerAudioLink = await UploadAIInterviewerAudio(audioBytes, model.SessionId);
                                            model.CurrentQuestion = latestReply;

                                        }
                                    }
                                    //else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() == 10)
                                    else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() == 35)
                                    //else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() == 3)
                                    {
                                        //check if the interview is already marked completed in the first details object above and if not, mark it as completed
                                        if (meetingDetails.MeetingStatus != (int)MeetingStatus.Completed)
                                        {
                                            await _aiinterviewerService.UpdateMeetingCompletedStatusForAIInterview(model.SessionId.ToString());

                                        }
                                        // INTERVIEW COMPLETE - Return ExitModalPartial view

                                        var exitModel = new AIInterviewExitModalViewModel
                                        {
                                            Title = "Interview Complete",
                                            Message = "The interviewer is through with all the questions. This interview is now closed.",
                                            IconClass = "fa-check-circle",
                                            IconColor = "#28a745",
                                            ButtonText = "Close",
                                            ButtonClass = "btn-success",
                                            CandidateName = meetingDetails.candidateName ?? "",
                                            InterviewDate = meetingDetails.interviewDate,
                                            SessionId = sid.ToString(),
                                            ExitTime = DateTime.Now,
                                            IsFromIndex = true // Add this flag to identify the source
                                        };
                                        return View("ExitModalPartial", exitModel);

                                    }
                                }
                                else
                                {
                                    // INTERVIEW COMPLETE - Return ExitModalPartial view
                                    //check if the interview is already marked completed in the first details object above and if not, mark it as completed
                                    if (meetingDetails.MeetingStatus != (int)MeetingStatus.Completed)
                                    {
                                        await _aiinterviewerService.UpdateMeetingCompletedStatusForAIInterview(model.SessionId.ToString());

                                    }
                                    var exitModel = new AIInterviewExitModalViewModel
                                    {
                                        Title = "Interview Complete",
                                        Message = "The interviewer is through with all the questions. This interview is now closed.",
                                        IconClass = "fa-check-circle",
                                        IconColor = "#28a745",
                                        ButtonText = "Close",
                                        ButtonClass = "btn-success",
                                        CandidateName = meetingDetails.candidateName ?? "",
                                        InterviewDate = meetingDetails.interviewDate,
                                        SessionId = sid.ToString(),
                                        ExitTime = DateTime.Now,
                                        IsFromIndex = true // Add this flag to identify the source
                                    };
                                    return View("ExitModalPartial", exitModel);
                                }


                            }
                            else if (transcript == null || latestEntry == null) // this would mean that the thread was created but an error occured before the first question could be saved, therefore, start from the first question
                            {
                                string resumeStringContent = string.Empty;
                                string techSkillsJson = string.Empty;
                                string jobDescriptionStringContent = string.Empty;

                                if (!string.IsNullOrEmpty(meetingDetails.resumeLink))
                                {
                                    resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(meetingDetails.resumeLink);
                                }
                                if (!string.IsNullOrEmpty(meetingDetails.TechSkillsInJson))
                                {
                                    techSkillsJson = meetingDetails.TechSkillsInJson;
                                }
                                // For adaptive AI interviews, the assistant now also expects the job description as a string.
                                // The JD file (pdf/doc) is stored against the skill channel/IRF; convert it to text the same way the resume is handled.
                                if (meetingDetails.AIInterviewType == (int)AIInterviewTypeEnum.Adaptive && !string.IsNullOrEmpty(meetingDetails.JDLink))
                                {
                                    jobDescriptionStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(meetingDetails.JDLink);
                                }
                                string candidatePrompt;
                                //if (!string.IsNullOrEmpty(resumeStringContent))
                                //{
                                //    candidatePrompt = $"Due to some reason, you are required to start over the interview from the top. Please start with the first question. You will interview the candidate for {meetingDetails.JDTiltle} according to the tech rubric in json here: {techSkillsJson}. The resume of the candidate in string format is as follows:{resumeStringContent ?? string.Empty}. Proceed as per the instructions fed to you.";

                                //}
                                //else
                                //{
                                //    candidatePrompt = $"Due to some reason, you are required to start over the interview from the top. Please start with the first question. You will interview the candidate for {meetingDetails.JDTiltle} according to the tech rubric in json here: {techSkillsJson}. The candidate's resume is not available. Proceed without the resume as per the instructions fed to you.";

                                //}

                                ////var replies = await _api.SendReply(sid, candidatePrompt, meetingDetails.AssistantId);

                                //// Use SendSystemPrompt so this internal recovery context is sent to OpenAI
                                //// but is NOT logged to TransCriptsConv as a "user" turn.
                                //var replies = await _api.SendSystemPrompt(sid, candidatePrompt, meetingDetails.AssistantId);

                                //model = new ChatMessageViewModel
                                //{
                                //    SessionId = sid,
                                //    ChatLog = replies.Select(r => new ChatLine { Role = "Bot", Message = CleanMessage(r) }).ToList(),
                                //};

                                ////
                                if (!string.IsNullOrEmpty(resumeStringContent))
                                {
                                    candidatePrompt = $"You will now interview the candidate for {meetingDetails.JDTiltle} according to the tech rubric in json here: {techSkillsJson}. The resume of the candidate in string format is as follows:{resumeStringContent ?? string.Empty}. The job description in string format is as follows:{jobDescriptionStringContent ?? string.Empty}. Proceed as per the instructions fed to you.";

                                }
                                else
                                {
                                    candidatePrompt = $"You will now interview the candidate for {meetingDetails.JDTiltle} according to the tech rubric in json here: {techSkillsJson}. The candidate's resume is not available. The job description in string format is as follows:{jobDescriptionStringContent ?? string.Empty}. Proceed without the resume as per the instructions fed to you.";

                                }
                                // Ask the assistant to flag the end of the interview so we can auto-complete it (see AdaptiveInterviewCompletionMarker).
                                candidatePrompt += AdaptiveCompletionInstruction;
                                var result = await _api.StartConversation(sid, candidatePrompt, meetingDetails.AssistantId);

                                model = new ChatMessageViewModel
                                {
                                    SessionId = sid,
                                    ChatLog = result.Replies.Select(r => new ChatLine { Role = "Bot", Message = CleanMessage(r) }).ToList(),

                                };

                                ////

                                //var latestReply = CleanMessage(replies.FirstOrDefault());
                                var latestReply = CleanMessage(result.Replies.FirstOrDefault());

                                if (!string.IsNullOrEmpty(latestReply))
                                {
                                    var audioBytes = await _speechToTextService.ConvertTextToSpeechAsync(latestReply);
                                    var base64Audio = Convert.ToBase64String(audioBytes);
                                    ViewData["AssistantAudioBase64"] = base64Audio;
                                    model.AIInterviewerAudioLink = await UploadAIInterviewerAudio(audioBytes, model.SessionId);
                                    model.CurrentQuestion = latestReply;

                                }
                                //var videoId = await _synthesiaTextToVideoService.CreateVideoAsync(result.Replies.FirstOrDefault(), "your_template_id_here");

                                //// Wait/poll until video is ready (Synthesia usually takes 30–60s)
                                //string videoUrl;
                                //do
                                //{
                                //    await Task.Delay(8000);
                                //    videoUrl = await _synthesiaTextToVideoService.GetVideoUrlAsync(videoId);
                                //} while (string.IsNullOrWhiteSpace(videoUrl));

                                //model.SynthesizedVideoUrl = videoUrl;

                                //model.SynthesizedVideoUrl = "https://synthesia-ttv-data.s3.amazonaws.com/video_data/239be418-3e6d-4c92-916d-0a2054d81269/transfers/rendered_video.mp4?response-content-disposition=attachment%3Bfilename%3D%22AI%20Interviewer%20-%20Harsh.mp4%22&AWSAccessKeyId=AKIA32NGJ5TS3KPZZ23Z&Signature=cf9ZVjfK2wBH33gb7HsQDMGgeRM%3D&Expires=1752553930";

                                //return View(model);
                            }



                            //var replies = await _api.SendReply(model.SessionId, userMessage);

                            //get all the questions asked already from the database in descending order by time
                            //var questions = new List<string>();
                            //var latestQuestion = questions.FirstOrDefault();//replies?.Skip(1)?.Take(1);
                            ////model.ChatLog.Add(new ChatLine { Role = "User", Message = userMessage });

                            //foreach (var reply in questions)
                            //{
                            //    model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(reply) });
                            //}

                            ////UploadBotInterviewVideo(model, question.FirstOrDefault(), userMessage);

                            ////var latestReply = CleanMessage(replies.FirstOrDefault());

                            //if (!string.IsNullOrEmpty(latestQuestion))
                            //{
                            //    var audioBytes = await _speechToTextService.ConvertTextToSpeechAsync(latestQuestion);
                            //    var base64Audio = Convert.ToBase64String(audioBytes);
                            //    ViewData["AssistantAudioBase64"] = base64Audio;
                            //}


                            //var videoId = await _synthesiaTextToVideoService.CreateVideoAsync(latestReply, "your_template_id_here");

                            //// Wait/poll until video is ready (Synthesia usually takes 30–60s)
                            //string videoUrl;
                            //do
                            //{
                            //    await Task.Delay(8000);
                            //    videoUrl = await _synthesiaTextToVideoService.GetVideoUrlAsync(videoId);
                            //} while (string.IsNullOrWhiteSpace(videoUrl));
                            //model.SynthesizedVideoUrl = videoUrl;
                            //model.SynthesizedVideoUrl = "https://synthesia-ttv-data.s3.amazonaws.com/video_data/239be418-3e6d-4c92-916d-0a2054d81269/transfers/rendered_video.mp4?response-content-disposition=attachment%3Bfilename%3D%22AI%20Interviewer%20-%20Harsh.mp4%22&AWSAccessKeyId=AKIA32NGJ5TS3KPZZ23Z&Signature=cf9ZVjfK2wBH33gb7HsQDMGgeRM%3D&Expires=1752553930";
                            model.Message = "";
                            model.TimeLeftInSeconds = meetingDetails.TimeLeftInSeconds;
                            model.InterviewId = meetingDetails.InterviewId;
                            model.IRFId = meetingDetails.irfIntId;
                            model.candidateName = meetingDetails.candidateName;
                            model.IsAdaptive = meetingDetails.IsAdaptive;
                            model.AssistantId = meetingDetails.AssistantId;
                            model.AIInterviewType = meetingDetails.AIInterviewType;
                            model.showChatBox = meetingDetails.showChatBox;
                            model.isMultiLingual = meetingDetails.isMultiLingual;
                            return View(model);

                        }
                    }


                }

                // Handle null meetingDetails case
                var exitModelError = new AIInterviewExitModalViewModel
                {
                    Title = "Interview Not Found",
                    Message = "The interview session could not be found. Please check the session ID and try again.",
                    IconClass = "fa-exclamation-triangle",
                    IconColor = "#dc3545",
                    ButtonText = "Close",
                    ButtonClass = "btn-danger",
                    SessionId = sid.ToString()
                };
                return View("ExitModalPartial", exitModelError);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                await _aiinterviewerService.LogAIInterviewException(sid.ToString(), "GET", ex);
                var exitModelError = new AIInterviewExitModalViewModel
                {
                    Title = "Error",
                    Message = "There seems to be an error. Please try again in a little while.",
                    IconClass = "fa-exclamation-triangle",
                    IconColor = "#dc3545",
                    ButtonText = "Close",
                    ButtonClass = "btn-danger",
                    SessionId = sid.ToString()
                };
                return View("ExitModalPartial", exitModelError);
            }
        }

        /// <summary>Chrome extension: session token + policy for AI interview page (meeting not in a terminal state).</summary>
        private async Task ApplyAiProctoringViewBagAsync(MeetingVM meetingDetails, Guid sid)
        {
            if (meetingDetails == null || !meetingDetails.IsProctoring)
            {
                ViewBag.ProctoringToken = null;
                ViewBag.ProctoringInterviewId = null;
                ViewBag.ProctoringResumeCaptureAfterInterrupt = false;
                return;
            }

            // Always emit proctoring bootstrap on the live AI interview page when we have a session + interview row.
            // Do not gate on InterviewComments.MeetingStatus (often stale vs. a valid candidate session).
            ProctoringViewBagHelper.ApplyForMeeting(
                this,
                meetingDetails,
                _proctoringTokenService,
                _configuration,
                sid.ToString(),
                issueProctoringBootstrapEvenIfMeetingStatusTerminal: true);
            ViewBag.ProctoringResumeCaptureAfterInterrupt =
                await ShouldResumeProctoringCaptureAfterInterruptAsync(meetingDetails);
        }

        private static bool IsAadhaarVerificationBlocking(MeetingVM meetingDetails)
        {
            return meetingDetails != null
                && meetingDetails.IsAadhaarVerificationRequired == true
                && meetingDetails.AadharVerificationId <= 0;
        }

        private IActionResult AadhaarRequiredExitView(Guid sid, MeetingVM meetingDetails)
        {
            return View("ExitModalPartial", new AIInterviewExitModalViewModel
            {
                Title = "Identity verification required",
                Message = "Please complete identity verification from your interview page before joining this AI interview.",
                IconClass = "fa-exclamation-triangle",
                IconColor = "#dc3545",
                ButtonText = "Close",
                ButtonClass = "btn-danger",
                CandidateName = meetingDetails?.candidateName ?? "Candidate",
                InterviewDate = meetingDetails?.interviewDate,
                SessionId = sid.ToString(),
                ExitTime = DateTime.Now,
                IsFromIndex = true,
                IsSuccess = false
            });
        }

        /// <summary>
        /// True when the candidate is resuming an in-progress AI interview (abrupt stop, audio error, manual exit)
        /// but not when the meeting is completed or they have never started.
        /// </summary>
        private async Task<bool> ShouldResumeProctoringCaptureAfterInterruptAsync(MeetingVM meetingDetails)
        {
            if (meetingDetails == null)
                return false;
            if (meetingDetails.MeetingStatus == (int)MeetingStatus.Completed)
                return false;

            var interviewId = meetingDetails.InterviewId > 0
                ? meetingDetails.InterviewId
                : (meetingDetails.id > 0 ? meetingDetails.id : meetingDetails.imid);
            if (interviewId <= 0)
                return false;

            try
            {
                var eventCount = await _proctoringSessionEventRepository.GetCountByInterviewIdAsync(interviewId);
                if (eventCount > 0)
                    return true;
            }
            catch
            {
                /* optional telemetry — fall through to timer heuristic */
            }

            var durationSec = (meetingDetails.interviewDuration > 0 ? meetingDetails.interviewDuration : 60) * 60;
            var timeLeft = meetingDetails.TimeLeftInSeconds;
            return durationSec > 0 && timeLeft > 0 && timeLeft < durationSec;
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Index([FromForm] ChatMessageViewModel model)
        {
            try
            {
                if (model == null || model.SessionId == Guid.Empty)
                {
                    return View("ExitModalPartial", CreateAnswerSubmitErrorView(
                        Guid.Empty,
                        "Session expired",
                        "Your interview session was not recognized. Please reopen the interview link from your invitation email."));
                }

                var meetingDetails = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(model.SessionId);
                if (meetingDetails == null)
                {
                    return View("ExitModalPartial", CreateAnswerSubmitErrorView(
                        model.SessionId,
                        "Interview not found",
                        "This interview session could not be found. Please reopen the original interview link."));
                }

                if (IsAadhaarVerificationBlocking(meetingDetails))
                    return AadhaarRequiredExitView(model.SessionId, meetingDetails);

                HydrateModelFromMeeting(model, meetingDetails);

                // Whether this submission is the MCQ section is decided from server state, never
                // from the post. The flag exempts the submission from the "a recorded answer must
                // be attached" checks, so trusting the form field would let a candidate assert it
                // on an ordinary question and skip answering altogether.
                //
                // In the preset branch every authored question has an id, so a zero id can only be
                // the MCQ section - which is served without a backing row once the authored
                // questions run out. The channel must be opted in, and the section must still be
                // outstanding, for the claim to be honoured.
                model.IsMcqAssessmentQuestion =
                    meetingDetails.HasMcqAssessmentSection
                    && model.AIInterviewType == (int)AIInterviewTypeEnum.Preset
                    && model.CurrentQuestionId <= 0
                    && !await HasCapturedMcqAssessmentAsync(model.SessionId);

                try
                {
                    await ApplyAiProctoringViewBagAsync(meetingDetails, model.SessionId);
                    ViewBag.CandidateTranscriptReview = IsCandidateTranscriptReviewEnabled(meetingDetails);
                }
                catch (Exception proctorEx)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, proctorEx);
                }

                if (model.AIInterviewType == (int)AIInterviewTypeEnum.Preset)
                {
                    string question = model.CurrentQuestion;
                    string userMessage = model.Message;
                    string tranScribedResponse = string.Empty;
                    string codeResponse = string.Empty;

                    // Decided server-side from the question id, like the MCQ section: the flag waives the
                    // recorded-answer checks, so it must not be something the browser can assert.
                    model.IsCodeProjectQuestion = model.CurrentQuestionId > 0
                        && (await _codeProjectQuestionService.GetDetails(model.CurrentQuestionId)) != null;

                    // The MCQ section has no spoken or typed answer to capture - the candidate
                    // answered inside the embedded test, and the MCQ side already scored it. What
                    // goes into the transcript is a summary of that result, so the interview's own
                    // evaluator prompt weighs the written test alongside the spoken answers rather
                    // than seeing a silent gap where a question should be.
                    if (model.IsMcqAssessmentQuestion)
                    {
                        userMessage = await CaptureMcqAssessmentResultAsync(model, meetingDetails);
                    }
                    // A code project answer is the candidate's last commit, graded against the hidden
                    // tests. SubmitCodeProject normally ran just before this post; SubmitAsync is
                    // idempotent, so this only grades here if the page posted without it (time ran out).
                    // The transcript gets the requirement results and the diff, so the interview's
                    // evaluator scores the project against the same rubric as every other answer.
                    else if (model.IsCodeProjectQuestion)
                    {
                        var (projectAssessment, projectGrade) = await _codeProjectQuestionService.SubmitAsync(
                            model.SessionId.ToString(), model.CurrentQuestionId, model.candidateName);
                        var projectDetails = await _codeProjectQuestionService.GetDetails(model.CurrentQuestionId);
                        userMessage = _codeProjectQuestionService.BuildTranscriptAnswer(projectDetails, projectAssessment, projectGrade);
                    }
                    // A coding answer is the written code plus anything the candidate said while writing it.
                    // Both are merged into one transcript entry so evaluation and the report see a single answer.
                    else if (model.IsCodingQuestion)
                    {
                        if (model.AudioFile != null && model.AudioFile.Length > 500)
                        {
                            try
                            {
                                using var stream = model.AudioFile.OpenReadStream();
                                tranScribedResponse = await _speechToTextService.TranscribeAsync(stream, model.AudioFile.FileName, model.isMultiLingual);
                            }
                            catch (Exception sttEx)
                            {
                                // A transcription failure must never cost the candidate their code
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, sttEx);
                            }

                            if (LooksLikeSilenceHallucination(tranScribedResponse))
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                                    new Exception($"Discarded a hallucinated transcript for silent coding answer in session {model.SessionId}."));
                                tranScribedResponse = string.Empty;
                            }
                        }

                        codeResponse = string.IsNullOrWhiteSpace(model.CodingSubmittedCode)
                            ? "(no code was submitted)"
                            : model.CodingSubmittedCode;

                        userMessage = BuildCodingAnswerTranscript(tranScribedResponse, model.CodingLanguage, codeResponse);
                    }
                    // For typing speed or listening comprehension assessments, skip audio transcription
                    else if (model.IsTypingSpeedQuestion || model.IsListeningComprehensionQuestion)
                    {
                        userMessage = "Typed Out";
                    }
                    else if (!model.showChatBox)
                    {
                        if (model.AudioFile == null || model.AudioFile.Length == 0)
                        {
                            return await RetryAnswerAsync(
                                model,
                                "Missing audio",
                                "Your recording was not attached. Please record your answer again, and wait until processing finishes before submitting.");
                        }

                        if (model.AudioFile.Length < 500)
                        {
                            return await RetryAnswerAsync(
                                model,
                                "Recording too short",
                                "Your recording was too short to transcribe. Please speak for at least a few seconds and try again.");
                        }

                        if (model.AudioFile != null && model.AudioFile.Length > 0)
                        {
                            // The candidate has already read this transcript and corrected it if it
                            // was wrong, so it is not transcribed again - what they confirmed is the
                            // answer. TranscribeAnswer produced it from this same recording.
                            var transcriptReview = BuildTranscriptReview(model, meetingDetails);

                            if (transcriptReview != null)
                            {
                                userMessage = transcriptReview.ConfirmedTranscript;
                            }
                            else
                            {
                                try
                                {
                                    using var stream = model.AudioFile.OpenReadStream();
                                    userMessage = await _speechToTextService.TranscribeAsync(stream, model.AudioFile.FileName, model.isMultiLingual);
                                }
                                catch (Exception sttEx)
                                {
                                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, sttEx);
                                    return await RetryAnswerAsync(
                                        model,
                                        "Could not read your recording",
                                        "We could not transcribe your recording. Please check your microphone and record your answer again.");
                                }
                            }

                            // A confirmed transcript is the candidate's own words, so it is not held
                            // to the speech-to-text quality check - that check exists to catch a bad
                            // recording, which is exactly what the review step already let them fix.
                            if (transcriptReview == null && !IsTranscriptionValid(userMessage, model.isMultiLingual))//!model.isMultiLingual &&
                            {
                                return await RetryAnswerAsync(
                                    model,
                                    "We could not make out your answer",
                                    "Please check that your microphone is working and speak at a normal volume rather than a whisper, then record your answer again.");
                            }
                        }
                    }
                    else
                    {
                        if (model.AudioFile != null && model.AudioFile.Length > 0)
                        {
                            if (model.AudioFile.Length < 500)
                            {
                                return await RetryAnswerAsync(
                                    model,
                                    "Recording too short",
                                    "Your recording was too short to transcribe. Please speak for at least a few seconds and try again.");
                            }

                            try
                            {
                                using var stream = model.AudioFile.OpenReadStream();
                                tranScribedResponse = await _speechToTextService.TranscribeAsync(stream, model.AudioFile.FileName, model.isMultiLingual);
                            }
                            catch (Exception sttEx)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, sttEx);
                                return await RetryAnswerAsync(
                                    model,
                                    "Could not read your recording",
                                    "We could not transcribe your recording. Please check your microphone and record your answer again.");
                            }

                            //if (!IsTranscriptionValid(tranScribedResponse))
                            //{
                            //    // Rewind the stream to re-run transcription
                            //    var exitModel = new AIInterviewExitModalViewModel
                            //    {
                            //        Title = "Error with audio!",
                            //        Message = "Please ensure your audio equipment is functioning correctly, and refrain from speaking in a whisper. Maintain an audible volume throughout the session.",
                            //        IconClass = "fa-exclamation-triangle",
                            //        IconColor = "#dc3545",
                            //        ButtonText = "Close",
                            //        ButtonClass = "btn-danger",
                            //        CandidateName = model.candidateName ?? "Candidate",
                            //        InterviewDate = DateTime.Now,
                            //        SessionId = model.SessionId.ToString(),
                            //        IsFromIndex = true
                            //    };
                            //    return View("ExitModalPartial", exitModel);
                            //}
                            if (!string.IsNullOrEmpty(model.Message))
                            {
                                userMessage = $"Verbal Explanation : {tranScribedResponse}{Environment.NewLine}Written Code :{Environment.NewLine}{model.Message}";

                            }
                            else
                            {
                                userMessage = tranScribedResponse;
                                if (!IsTranscriptionValid(userMessage, model.isMultiLingual)) //
                                {
                                    return await RetryAnswerAsync(
                                        model,
                                        "We could not make out your answer",
                                        "Please check that your microphone is working and speak at a normal volume rather than a whisper, then record your answer again.");
                                }
                            }
                        }
                    }


                    var transcript = await _aiinterviewerService.GetAIInterviewTranscriptBySessionId(model.SessionId)
                        ?? Enumerable.Empty<TranscriptConv>();
                    var assistantQuestionCount = CountAssistantQuestions(transcript);
                    if (assistantQuestionCount == 35)
                    {
                        var payloadError = await ValidatePresetAnswerPayloadBeforeSave(model, userMessage);
                        if (payloadError != null) return payloadError;

                        bool iscandidateVideoSavedTodb = await UploadBotInterviewVideo(model, question, userMessage, meetingDetails);
                        var exitModel = new AIInterviewExitModalViewModel();
                        if (!iscandidateVideoSavedTodb)
                        {
                            exitModel = new AIInterviewExitModalViewModel
                            {
                                Title = "Error!",
                                Message = "Error saving video Response. Please check your network's connectivity.",
                                IconClass = "fa-exclamation-triangle",
                                IconColor = "#dc3545",
                                ButtonText = "Close",
                                ButtonClass = "btn-danger",
                                CandidateName = model.candidateName ?? "Candidate",
                                InterviewDate = DateTime.Now,
                                SessionId = model.SessionId.ToString(),
                                IsFromIndex = true
                            };
                            return View("ExitModalPartial", exitModel);
                        }
                        await _aiinterviewerService.LogTranscript(model.SessionId, "user", userMessage);

                        // INTERVIEW COMPLETE - Return ExitModalPartial view
                        //update the db for MeetingStatus = 4 here.
                        await _aiinterviewerService.UpdateMeetingCompletedStatusForAIInterview(model.SessionId.ToString());
                        _api.GenerateReport(model.SessionId, string.Empty, (int)AIInterviewTypeEnum.Preset);


                        exitModel = new AIInterviewExitModalViewModel
                        {
                            Title = "Interview Complete",
                            Message = "The interviewer is through with all the questions. This interview is now closed.",
                            IconClass = "fa-check-circle",
                            IconColor = "#28a745",
                            ButtonText = "Close",
                            ButtonClass = "btn-success",
                            CandidateName = model.candidateName ?? "Candidate",
                            InterviewDate = DateTime.Now,
                            SessionId = model.SessionId.ToString(),
                            IsFromIndex = true
                        };
                        return View("ExitModalPartial", exitModel);
                    }
                    else
                    {
                        //var replies = new List<string>();
                        //try
                        //{
                        //    replies = await _api.SendReply(model.SessionId, userMessage, model.AssistantId);
                        //}
                        //catch (Exception ex)
                        //{
                        //    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        //}

                        //model.ChatLog.Add(new ChatLine { Role = "User", Message = userMessage });

                        //if (replies != null)
                        //{
                        //    foreach (var reply in replies)
                        //    {
                        //        model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(reply) });
                        //    }
                        //}

                        var payloadError = await ValidatePresetAnswerPayloadBeforeSave(model, userMessage);
                        if (payloadError != null) return payloadError;

                        bool iscandidateVideoSavedTodb = await UploadBotInterviewVideo(model, question, userMessage, meetingDetails);
                        if (!iscandidateVideoSavedTodb)
                        {
                            var exitModel = new AIInterviewExitModalViewModel
                            {
                                Title = "Error!",
                                Message = "Error saving video Response. Please check your network's connectivity.",
                                IconClass = "fa-exclamation-triangle",
                                IconColor = "#dc3545",
                                ButtonText = "Close",
                                ButtonClass = "btn-danger",
                                CandidateName = model.candidateName ?? "Candidate",
                                InterviewDate = DateTime.Now,
                                SessionId = model.SessionId.ToString(),
                                IsFromIndex = true
                            };
                            return View("ExitModalPartial", exitModel);
                        }

                        // Save typing speed assessment if this was a typing speed question
                        if (model.IsTypingSpeedQuestion && !string.IsNullOrEmpty(model.TypedText))
                        {
                            try
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Debug,
                                    new Exception($"Starting OpenAI typing assessment analysis for session {model.SessionId}"));

                                // Compute candidate's actual WPM from typed word count and time taken
                                int typedWordCount = string.IsNullOrWhiteSpace(model.TypedText) ? 0 :
                                    model.TypedText.Trim().Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                                decimal totalMinutesTaken = model.TypingTimeTaken > 0 ? model.TypingTimeTaken / 60M : 2.00M;
                                decimal actualWpm = totalMinutesTaken > 0 ? Math.Round((decimal)typedWordCount / totalMinutesTaken, 2) : 0M;

                                // Prepare input data for OpenAI assistant (assistant is pre-configured with instructions)
                                var inputs = new TypingInputs
                                {
                                    reference_paragraph = model.TypingSampleText,
                                    user_typed_text = model.TypedText,
                                    words_per_minute = actualWpm, // Candidate's actual WPM (words per single minute)
                                    total_minutes = totalMinutesTaken
                                };

                                // Serialize inputs to JSON
                                string inputsJson = JsonConvert.SerializeObject(inputs, Formatting.Indented);

                                // Send only the inputs to the assistant
                                string assistantMessage = inputsJson;

                                string threadId = null;
                                string runId = null;
                                string openAIResponse = null;
                                var useResponsesApi = _configuration.GetValue("OpenAI:UseResponsesApi", false);
                                if (useResponsesApi)
                                {
                                    var runResult = await _assistantServiceHelper.GenerateTypingAssessment(assistantMessage);
                                    openAIResponse = runResult?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;
                                }
                                else
                                {
                                    // Create OpenAI thread and run
                                    threadId = await _openAIAssistantHelper.CreateThreadAsync();
                                    await _openAIAssistantHelper.AddMessageAsync(threadId, "user", assistantMessage);
                                    runId = await _openAIAssistantHelper.CreateRunAsync(threadId, OpenAIInterviewerAssistants.TypingAssessmentAssistant);

                                    // Wait for completion
                                    string runStatus = await _openAIAssistantHelper.WaitForRunAsync(threadId, runId);
                                    if (runStatus == "completed")
                                    {
                                        var messages = await _openAIAssistantHelper.GetMessagesAsync(threadId);
                                        openAIResponse = messages.FirstOrDefault() ?? "{}";
                                    }
                                    else
                                    {
                                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                                            new Exception($"OpenAI typing assessment run status: {runStatus}. ThreadId: {threadId}, RunId: {runId}"));
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(openAIResponse))
                                {
                                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Debug,
                                        new Exception($"OpenAI typing assessment response received. Length: {openAIResponse.Length}"));

                                    // Parse OpenAI response
                                    TypingAssessmentOpenAIResponse analysisResult = null;
                                    try
                                    {
                                        analysisResult = JsonConvert.DeserializeObject<TypingAssessmentOpenAIResponse>(openAIResponse);
                                    }
                                    catch (JsonException jsonEx)
                                    {
                                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                                            new Exception($"Failed to parse OpenAI response as JSON: {jsonEx.Message}. Raw response: {openAIResponse}"));
                                    }

                                    // Determine pass/fail based on OpenAI analysis
                                    bool isPassed = false;
                                    decimal wordAccuracy = 0;
                                    decimal charAccuracy = 0;

                                    if (analysisResult != null && analysisResult.scores != null)
                                    {
                                        wordAccuracy = analysisResult.scores.word_level_accuracy_percent;
                                        charAccuracy = analysisResult.scores.character_level_accuracy_percent;
                                        // Pass only if word accuracy >= 97% AND WPM >= 30
                                        decimal wpmForVerdict = analysisResult.meta?.words_per_minute > 0
                                            ? analysisResult.meta.words_per_minute
                                            : actualWpm;
                                        isPassed = wordAccuracy >= 97.00M && wpmForVerdict >= 30.00M;
                                    }

                                    var typingAssessment = new TypingSpeedAssessment
                                    {
                                        SessionId = model.SessionId.ToString(),
                                        QuestionId = model.CurrentQuestionId,
                                        SampleText = model.TypingSampleText,
                                        TypedText = model.TypedText,
                                        TimeStarted = TryParseInterviewDateTime(model.TypingStartTime) ?? DateTime.Now,
                                        TimeCompleted = TryParseInterviewDateTime(model.TypingEndTime) ?? DateTime.Now,
                                        TimeTakenInSeconds = model.TypingTimeTaken,
                                        TotalCharacters = analysisResult?.meta?.user_total_words_typed ?? 0,
                                        CorrectCharacters = analysisResult?.counts?.correct_words ?? 0,
                                        AccuracyPercentage = wordAccuracy,
                                        WordsPerMinute = analysisResult?.meta?.words_per_minute > 0
                                            ? analysisResult.meta.words_per_minute
                                            : actualWpm,
                                        PassingWPM = 30.00M,
                                        PassingAccuracyPercentage = 97.00M,
                                        IsPassed = isPassed,
                                        OpenAIAnalysisJson = openAIResponse,
                                        OpenAIThreadId = threadId,
                                        OpenAIRunId = runId,
                                        CreatedBy = model.candidateName
                                    };

                                    await _aiinterviewerService.SaveTypingSpeedAssessment(typingAssessment);

                                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Debug,
                                        new Exception($"Typing assessment saved successfully. Passed: {isPassed}, Accuracy: {wordAccuracy}%"));
                                }
                                else if (!useResponsesApi)
                                {
                                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                                        new Exception($"OpenAI typing assessment returned no response. ThreadId: {threadId}, RunId: {runId}"));
                                }
                                else
                                {
                                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                                        new Exception("OpenAI typing assessment returned no response via Responses API."));
                                }
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                // Continue even if typing speed save fails - don't block the interview
                            }
                        }

                        // Save listening comprehension assessment if this was a listening comprehension question
                        if (model.IsListeningComprehensionQuestion && !string.IsNullOrEmpty(model.ListeningSummary))
                        {
                            try
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Debug,
                                    new Exception($"Starting OpenAI listening comprehension analysis for session {model.SessionId}"));

                                // Read the transcription and expected answer from the database rather than
                                // from the posted form. Both are grading material: if they were round-tripped
                                // through the browser the candidate could read them off the page.
                                var listeningQuestion = await _aiinterviewerService
                                    .GetListeningComprehensionQuestionDetails(model.CurrentQuestionId);

                                string audioTranscription = listeningQuestion?.ListeningAudioTranscription
                                    ?? model.ListeningAudioTranscription;
                                string expectedAnswer = listeningQuestion?.ExpectedAnswer;

                                var inputs = new ListeningComprehensionInputs
                                {
                                    audio_transcription = audioTranscription,
                                    expected_answer = expectedAnswer,
                                    candidate_summary = model.ListeningSummary,
                                    time_taken_seconds = model.ListeningTimeTaken,
                                    audio_play_count = model.ListeningAudioPlayCount,
                                    time_expired = model.ListeningIsTimeExpired
                                };

                                string inputsJson = JsonConvert.SerializeObject(inputs, Formatting.Indented);

                                // Scored via the Responses API with a stored prompt. When the prompt id
                                // is not configured yet the helper returns nothing and the answer is
                                // still saved below, unscored, so it can be graded retrospectively.
                                var (runResult, responseId) = await _assistantServiceHelper
                                    .GenerateListeningComprehensionAssessment(inputsJson);

                                string openAIResponse = runResult?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;

                                ListeningComprehensionOpenAIResponse analysisResult = null;
                                if (!string.IsNullOrWhiteSpace(openAIResponse))
                                {
                                    try
                                    {
                                        analysisResult = JsonConvert.DeserializeObject<ListeningComprehensionOpenAIResponse>(openAIResponse);
                                    }
                                    catch (JsonException jsonEx)
                                    {
                                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                                            new Exception($"Failed to parse listening comprehension OpenAI response as JSON: {jsonEx.Message}. Raw response: {openAIResponse}"));
                                    }
                                }

                                decimal? score = analysisResult?.scores?.comprehension_score;
                                decimal? maxScore = analysisResult?.scores?.max_score;
                                bool? isPassed = null;
                                if (analysisResult?.verdict != null)
                                {
                                    isPassed = analysisResult.verdict.recommended;
                                }

                                var listeningAssessment = new ListeningComprehensionAssessment
                                {
                                    SessionId = model.SessionId.ToString(),
                                    QuestionId = model.CurrentQuestionId,
                                    AudioUrl = model.ListeningAudioUrl,
                                    AudioTranscription = audioTranscription,
                                    CandidateSummary = model.ListeningSummary,
                                    ExpectedAnswer = expectedAnswer,
                                    TimeStarted = TryParseInterviewDateTime(model.ListeningStartTime) ?? DateTime.Now,
                                    TimeCompleted = TryParseInterviewDateTime(model.ListeningEndTime) ?? DateTime.Now,
                                    TimeTakenInSeconds = model.ListeningTimeTaken,
                                    AudioPlayCount = model.ListeningAudioPlayCount,
                                    IsTimeExpired = model.ListeningIsTimeExpired,
                                    Score = score,
                                    MaxScore = maxScore,
                                    IsPassed = isPassed,
                                    OpenAIAnalysisJson = openAIResponse,
                                    OpenAIResponseId = responseId,
                                    CreatedBy = model.candidateName
                                };

                                await _aiinterviewerService.SaveListeningComprehensionAssessment(listeningAssessment);

                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Debug,
                                    new Exception($"Listening comprehension assessment saved for session {model.SessionId}. Score: {score?.ToString() ?? "not scored"}"));
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                // Continue even if listening assessment save fails - don't block the interview
                            }
                        }

                        // Save coding assessment if this was a coding question
                        if (model.IsCodingQuestion)
                        {
                            try
                            {
                                var codingAssessment = new CodingAssessment
                                {
                                    SessionId = model.SessionId.ToString(),
                                    QuestionId = model.CurrentQuestionId,
                                    QuestionText = model.CurrentQuestion,
                                    CodeLanguage = model.CodingLanguage,
                                    SubmittedCode = model.CodingSubmittedCode,
                                    TimeLimitInMinutes = model.CodingTimeLimitInMinutes,
                                    TimeStarted = TryParseInterviewDateTime(model.CodingStartTime) ?? DateTime.Now,
                                    TimeCompleted = TryParseInterviewDateTime(model.CodingEndTime) ?? DateTime.Now,
                                    TimeTakenInSeconds = model.CodingTimeTaken,
                                    IsTimeExpired = model.CodingIsTimeExpired,
                                    StdInput = model.CodingStdInput,
                                    LastRunOutput = model.CodingLastRunOutput,
                                    LastRunStatus = model.CodingLastRunStatus,
                                    RunCount = model.CodingRunCount,
                                    CreatedBy = model.candidateName
                                };
                                await _aiinterviewerService.SaveCodingAssessment(codingAssessment);
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                // Continue even if coding assessment save fails - don't block the interview
                            }
                        }

                        await _aiinterviewerService.LogTranscript(model.SessionId, "user", userMessage);

                        var sampleQuestion = await GetNextPresetQuestionAsync(model.InterviewId, model.IRFId);


                        if (sampleQuestion != null)
                        {
                            model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(sampleQuestion.Message), sampleQuestionId = sampleQuestion.id });
                            model.ChatLog.Add(new ChatLine { Role = "User", Message = userMessage });


                            //var latestReply = CleanMessage(replies?.FirstOrDefault());

                            //current question changes to the latest reply by the assistant and is set only after the earlier question-answer response is saved using UploadBotInterviewVideo()
                            model.CurrentQuestion = CleanMessage(sampleQuestion.Message);
                            model.CurrentQuestionId = sampleQuestion.id;
                            model.IsTypingSpeedQuestion = sampleQuestion.IsTypingSpeedAssessment;
                            model.IsListeningComprehensionQuestion = sampleQuestion.IsListeningComprehension;
                            model.ListeningAudioUrl = sampleQuestion.ListeningAudioUrl;
                            model.ListeningAudioTranscription = sampleQuestion.ListeningAudioTranscription;
                            model.IsCodingQuestion = sampleQuestion.IsCodingQuestion;
                            model.CodingTimeLimitInMinutes = sampleQuestion.CodingTimeLimitInMinutes;
                            model.IsCodeProjectQuestion = sampleQuestion.IsCodeProjectQuestion;
                            // Drop the answer just saved so it is not re-rendered against the next question
                            model.CodingSubmittedCode = null;
                            model.CodingLanguage = null;
                            model.CodingStartTime = null;
                            model.CodingEndTime = null;
                            model.CodingTimeTaken = 0;
                            model.CodingIsTimeExpired = false;
                            model.CodingStdInput = null;
                            model.CodingLastRunOutput = null;
                            model.CodingLastRunStatus = null;
                            model.CodingRunCount = 0;

                            if (!string.IsNullOrEmpty(model.CurrentQuestion))//&& string.IsNullOrEmpty(model.AIInterviewerAudioLink)
                            {
                                model.SynthesizedVideoUrl = sampleQuestion.AIQuestionVideoLink;
                                model.CurrentQuestion = CleanMessage(sampleQuestion.Message);
                                model.CurrentQuestionId = sampleQuestion.id;
                                model.IsTypingSpeedQuestion = sampleQuestion.IsTypingSpeedAssessment;
                                model.IsListeningComprehensionQuestion = sampleQuestion.IsListeningComprehension;
                                model.ListeningAudioUrl = sampleQuestion.ListeningAudioUrl;
                                model.ListeningAudioTranscription = sampleQuestion.ListeningAudioTranscription;
                                model.IsCodingQuestion = sampleQuestion.IsCodingQuestion;
                                model.CodingTimeLimitInMinutes = sampleQuestion.CodingTimeLimitInMinutes;
                                model.IsCodeProjectQuestion = sampleQuestion.IsCodeProjectQuestion;

                                // Fetch random typing sample text from database if it's a typing speed question
                                if (sampleQuestion.IsTypingSpeedAssessment)
                                {
                                    var typingSample = await _aiinterviewerService.GetRandomTypingSpeedSampleText();
                                    if (typingSample != null && !string.IsNullOrWhiteSpace(typingSample.SampleText))
                                    {
                                        model.TypingSampleTextFromDB = typingSample.SampleText;
                                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Debug,
                                            new Exception($"Typing sample text loaded successfully. Length: {typingSample.SampleText.Length}"));
                                    }
                                    else
                                    {
                                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                                            new Exception("No typing sample text available in database. Please add sample texts to TypingSpeedSampleTexts table."));
                                    }
                                }

                                await _aiinterviewerService.LogTranscript(model.SessionId, "assistant", model.CurrentQuestion, sampleQuestion.id);
                                QueueCodeProjectLookAhead(model.SessionId.ToString(), model.InterviewId, model.IRFId, model);

                            }

                            model.Message = "";

                            await RefreshModelMeetingFieldsAsync(model);
                            return View(model);
                        }
                        else
                        {
                            // Out of spoken questions. On a channel opted in to the MCQ assessment
                            // section, that section is served here rather than closing the
                            // interview - it is the last part of the interview, not a question in
                            // SkillChannelSampleQuestions.
                            // The proctoring ViewBag was already applied for this request, near the
                            // top of the POST handler, so the view has what it needs.
                            if (await TryServeMcqAssessmentSectionAsync(model, meetingDetails))
                            {
                                return View(model);
                            }

                            //update the db for MeetingStatus = 4 here.
                            await _aiinterviewerService.UpdateMeetingCompletedStatusForAIInterview(model.SessionId.ToString());
                            _api.GenerateReport(model.SessionId, string.Empty, (int)AIInterviewTypeEnum.Preset);


                            var exitModel = new AIInterviewExitModalViewModel
                            {
                                Title = "Interview Complete",
                                Message = "The interviewer is through with all the questions. This interview is now closed.",
                                IconClass = "fa-check-circle",
                                IconColor = "#28a745",
                                ButtonText = "Close",
                                ButtonClass = "btn-success",
                                CandidateName = model.candidateName ?? "Candidate",
                                InterviewDate = DateTime.Now.Date,
                                SessionId = model.SessionId.ToString(),
                                ExitTime = DateTime.Now,
                                IsFromIndex = true // Add this flag to identify the source
                            };
                            return View("ExitModalPartial", exitModel);
                        }

                    }
                }
                else
                {
                    string question = model.CurrentQuestion;
                    string userMessage = model.Message;
                    CandidateTranscriptReview transcriptReview = null;

                    // Nothing arrived at all - no recording and no typed answer. This used to fall
                    // through to the transcription quality check, which told the candidate to sort
                    // out their microphone volume when the real problem was that the upload never
                    // attached. A typed answer with no audio is still a valid submission, so only
                    // the case where both are missing is refused.
                    if ((model.AudioFile == null || model.AudioFile.Length == 0)
                        && string.IsNullOrWhiteSpace(userMessage))
                    {
                        return await RetryAnswerAsync(
                            model,
                            "Missing audio",
                            "Your recording was not attached. Please record your answer again, and wait until processing finishes before submitting.");
                    }

                    if (model.AudioFile != null && model.AudioFile.Length > 0)
                    {
                        // Already read and corrected by the candidate during review, so it is not
                        // transcribed again - what they confirmed is the answer.
                        transcriptReview = BuildTranscriptReview(model, meetingDetails);

                        if (transcriptReview != null)
                        {
                            userMessage = transcriptReview.ConfirmedTranscript;
                        }
                        else
                        {
                            // A clip this short carries no speech. Sending it anyway returns a
                            // hallucinated sentence that would be filed as the candidate's answer,
                            // so it is refused here the way the preset branch refuses it.
                            if (model.AudioFile.Length < 500)
                            {
                                return await RetryAnswerAsync(
                                    model,
                                    "Recording too short",
                                    "Your recording was too short to transcribe. Please speak for at least a few seconds and try again.");
                            }

                            try
                            {
                                using var stream = model.AudioFile.OpenReadStream();
                                userMessage = await _speechToTextService.TranscribeAsync(stream, model.AudioFile.FileName, model.isMultiLingual);
                            }
                            catch (Exception sttEx)
                            {
                                // Without this the exception escapes the action and the candidate
                                // sees a raw error page mid-interview with no way back in. The
                                // preset branch has caught this for a while; the adaptive one did not.
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, sttEx);
                                return await RetryAnswerAsync(
                                    model,
                                    "Could not read your recording",
                                    "We could not transcribe your recording. Please check your microphone and record your answer again.");
                            }
                        }
                    }

                    // A confirmed transcript is the candidate's own words and is not held to the
                    // speech-to-text quality check; the review step already let them fix a bad one.
                    if (transcriptReview == null && !IsTranscriptionValid(userMessage, model.isMultiLingual))//!model.isMultiLingual &&
                    {
                        return await RetryAnswerAsync(
                            model,
                            "We could not make out your answer",
                            "Please check that your microphone is working and speak at a normal volume rather than a whisper, then record your answer again.");
                    }

                    var transcript = await _aiinterviewerService.GetAIInterviewTranscriptBySessionId(model.SessionId)
                        ?? Enumerable.Empty<TranscriptConv>();
                    var assistantQuestionCount = CountAssistantQuestions(transcript);
                    // Safety-net completion: the assistant should normally end the interview via the completion
                    // marker (handled in the else branch below), but if it keeps asking questions we still stop
                    // once the hard cap is reached so the interview can never run forever.
                    if (assistantQuestionCount >= AdaptiveMaxAssistantQuestions)
                    {
                        var payloadError = await ValidatePresetAnswerPayloadBeforeSave(model, userMessage);
                        if (payloadError != null) return payloadError;

                        bool iscandidateVideoSavedTodb = await UploadBotInterviewVideo(model, question, userMessage, meetingDetails);
                        if (!iscandidateVideoSavedTodb)
                        {
                            var exitModel = new AIInterviewExitModalViewModel
                            {
                                Title = "Error!",
                                Message = "Error saving video Response. Please check your network's connectivity.",
                                IconClass = "fa-exclamation-triangle",
                                IconColor = "#dc3545",
                                ButtonText = "Close",
                                ButtonClass = "btn-danger",
                                CandidateName = model.candidateName ?? "Candidate",
                                InterviewDate = DateTime.Now,
                                SessionId = model.SessionId.ToString(),
                                IsFromIndex = true
                            };
                            return View("ExitModalPartial", exitModel);
                        }

                        await _aiinterviewerService.LogTranscript(model.SessionId, "user", userMessage);

                        // INTERVIEW COMPLETE - mark completed, generate the report and show the exit modal.
                        return await CompleteAdaptiveInterviewAsync(model);
                    }
                    else
                    {
                        bool iscandidateVideoSavedTodb = await UploadBotInterviewVideo(model, question, userMessage, meetingDetails);
                        if (!iscandidateVideoSavedTodb)
                        {
                            var exitModel = new AIInterviewExitModalViewModel
                            {
                                Title = "Error!",
                                Message = "Error saving video Response. Please check your network's connectivity.",
                                IconClass = "fa-exclamation-triangle",
                                IconColor = "#dc3545",
                                ButtonText = "Close",
                                ButtonClass = "btn-danger",
                                CandidateName = model.candidateName ?? "Candidate",
                                InterviewDate = DateTime.Now,
                                SessionId = model.SessionId.ToString(),
                                IsFromIndex = true
                            };
                            return View("ExitModalPartial", exitModel);
                        }

                        var replies = new List<string>();
                        try
                        {
                            replies = await _api.SendReply(model.SessionId, userMessage, model.AssistantId);
                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }

                        // The candidate's answer to the previous question is already saved (UploadBotInterviewVideo
                        // above) and logged by the API. If the assistant now signals it is done, close the interview
                        // instead of trying to render another question - this is the adaptive equivalent of the preset
                        // flow completing when it runs out of questions.
                        if (RepliesSignalInterviewCompletion(replies))
                        {
                            return await CompleteAdaptiveInterviewAsync(model);
                        }

                        // Some prompts leave the marker out and just say goodbye again each time the
                        // candidate replies; a repeated closing statement means the interview is over.
                        var previousAssistantTurn = transcript
                            .FirstOrDefault(x => string.Equals(x.Role, "assistant", StringComparison.OrdinalIgnoreCase))?.Message;
                        if (AIInterviewCompletionDetector.RepeatsClosingTurn(CleanMessage(replies?.FirstOrDefault()), CleanMessage(previousAssistantTurn)))
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                                $"AI interview session {model.SessionId}: the assistant repeated its closing message without the completion marker; completing the interview.");
                            return await CompleteAdaptiveInterviewAsync(model);
                        }

                        model.ChatLog.Add(new ChatLine { Role = "User", Message = userMessage });

                        if (replies != null)
                        {
                            foreach (var reply in replies)
                            {
                                model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(reply) });
                            }
                        }

                        var latestReply = CleanMessage(replies?.FirstOrDefault());

                        // Assistant returned nothing usable (empty/failed run). Treat it as the end of the interview
                        // and complete gracefully rather than reloading a blank question or surfacing a generic error.
                        if (string.IsNullOrWhiteSpace(latestReply))
                        {
                            return await CompleteAdaptiveInterviewAsync(model);
                        }

                        //current question changes to the latest reply by the assistant and is set only after the earlier question-answer response is saved using UploadBotInterviewVideo()
                        model.CurrentQuestion = latestReply;

                        if (!string.IsNullOrEmpty(model.CurrentQuestion))//&& string.IsNullOrEmpty(model.AIInterviewerAudioLink)
                        {
                            // Text-to-speech is best-effort: if it fails we still show the next question (with text),
                            // rather than dumping the candidate to the generic "Error submitting answer" screen.
                            try
                            {
                                var audioBytes = await _speechToTextService.ConvertTextToSpeechAsync(model.CurrentQuestion);
                                var base64Audio = Convert.ToBase64String(audioBytes);
                                ViewData["AssistantAudioBase64"] = base64Audio;
                                model.AIInterviewerAudioLink = await UploadAIInterviewerAudio(audioBytes, model.SessionId);
                            }
                            catch (Exception ttsEx)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ttsEx);
                            }

                        }

                        model.Message = "";

                        await RefreshModelMeetingFieldsAsync(model);
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                var sessionId = model?.SessionId ?? Guid.Empty;
                if (sessionId != Guid.Empty)
                {
                    await _aiinterviewerService.LogAIInterviewException(sessionId.ToString(), "POST", ex);
                }
                return View("ExitModalPartial", CreateAnswerSubmitErrorView(
                    sessionId,
                    "Error submitting answer",
                    "Your answer could not be processed. Please check your network connection and try again. If the problem continues, reload the interview link."));
            }

        }

        private static AIInterviewExitModalViewModel CreateAnswerSubmitErrorView(Guid sessionId, string title, string message)
        {
            return new AIInterviewExitModalViewModel
            {
                Title = title,
                Message = message,
                IconClass = "fa-exclamation-triangle",
                IconColor = "#dc3545",
                ButtonText = "Close",
                ButtonClass = "btn-danger",
                SessionId = sessionId == Guid.Empty ? null : sessionId.ToString(),
                ExitTime = DateTime.Now,
                IsFromIndex = true,
                IsSuccess = false
            };
        }

        private static void HydrateModelFromMeeting(ChatMessageViewModel model, MeetingVM meeting)
        {
            model.InterviewId = meeting.InterviewId;
            model.IRFId = meeting.irfIntId;
            // The page only heartbeats the clock to the db once a minute, so the posted value is the
            // fresher one. Taking the db value here handed back every second since the last
            // heartbeat - and the full clock when each answer took under a minute - because the
            // answer save then wrote that stale value back. A post may only spend time, never add it.
            if (model.TimeLeftInSeconds <= 0)
                model.TimeLeftInSeconds = meeting.TimeLeftInSeconds;
            else if (meeting.TimeLeftInSeconds > 0)
                model.TimeLeftInSeconds = Math.Min(model.TimeLeftInSeconds, meeting.TimeLeftInSeconds);
            model.candidateName = meeting.candidateName;
            model.AssistantId = meeting.AssistantId;
            model.AIInterviewType = meeting.AIInterviewType;
            model.showChatBox = meeting.showChatBox;
            model.isMultiLingual = meeting.isMultiLingual;
            model.IsAdaptive = meeting.IsAdaptive;
        }

        private async Task RefreshModelMeetingFieldsAsync(ChatMessageViewModel model)
        {
            var meeting = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(model.SessionId);
            if (meeting == null) return;
            HydrateModelFromMeeting(model, meeting);
            try
            {
                await ApplyAiProctoringViewBagAsync(meeting, model.SessionId);
                ViewBag.CandidateTranscriptReview = IsCandidateTranscriptReviewEnabled(meeting);
            }
            catch (Exception proctorEx)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, proctorEx);
            }
        }

        private static int CountAssistantQuestions(IEnumerable<TranscriptConv> transcript)
        {
            return transcript?.Count(x => string.Equals(x.Role, "assistant", StringComparison.OrdinalIgnoreCase)) ?? 0;
        }

        /// <summary>
        /// Internal token the adaptive assistant appends when it ends the interview.
        /// Stripped in CleanMessage so it is never shown/spoken to the candidate.
        /// </summary>
        private const string AdaptiveInterviewCompletionMarker = AIInterviewCompletionDetector.Marker;

        /// <summary>
        /// Appended to the start-of-interview prompt so the assistant flags completion for auto-exit.
        /// </summary>
        /// <summary>
        /// What the interview says when it hands over to the MCQ assessment section. Shown in the
        /// chat log and stored as the question this section answers, so the transcript and the
        /// report read coherently. Not spoken by the avatar - there is no video for this section.
        /// </summary>
        private const string McqAssessmentIntroduction =
            "That covers the questions I had for you. The last part of this interview is a short written assessment.";

        private const string AdaptiveCompletionInstruction =
            " When you have finished the interview (topics adequately covered, or the candidate cannot continue productively), " +
            "give a brief polite closing message to the candidate, then append exactly this marker at the very end of your message " +
            "(do not mention, read aloud, or explain the marker to the candidate): " +
            AdaptiveInterviewCompletionMarker;

        /// <summary>
        /// Hard cap / safety net so adaptive interviews cannot run forever. Matches Index resume limits of 35.
        /// </summary>
        private const int AdaptiveMaxAssistantQuestions = 35;

        /// <summary>
        /// Prompt for adaptive report generation. Adaptive replies are rendered as HTML when JSON parse fails.
        /// </summary>
        private const string AdaptiveReportGenerationPrompt =
            "Based on the full interview conversation in this thread, generate a complete evaluation report as clean HTML only " +
            "(no markdown code fences, no preamble). Include these sections with these exact labels where applicable: " +
            "Overall Technical Score; Overall Communication Score; Final Verdict; Final Comment; Communication Score Reasoning; " +
            "LLM Detection / LLM Response Suspected, Evidence Comment, and per-question Suspicious Instances; " +
            "a Score Rubric table (columns: Rubric Item, Expected, Observed, Score); " +
            "Competency Ratings or Questions Discussed & Competency Rating; " +
            "and question-by-question analysis with Strengths, Weaknesses, and Evidence from transcript where useful. " +
            "Use simple semantic HTML (h2/h3, p, table, ul/li) suitable for embedding in a report page. " +
            "After the HTML, append exactly one machine-readable line as an HTML comment and nothing after it, in this " +
            "exact format: <!--RB_EVAL {\"final_verdict\":\"Strongly Selected|Marginally Selected|Marginally Rejected|Strongly Rejected\",\"final_comment\":\"<one or two " +
            "sentence overall verdict summary>\"} RB_EVAL--> . Use only one of those four final_verdict values " +
            "(not Recommended, Not Recommended, select, reject, or borderline), and make sure the JSON is valid with any inner double quotes escaped.";

        private static bool MessageSignalsInterviewCompletion(string message)
        {
            return AIInterviewCompletionDetector.SignalsCompletion(message);
        }

        private static bool RepliesSignalInterviewCompletion(IEnumerable<string> replies)
        {
            return replies != null && replies.Any(MessageSignalsInterviewCompletion);
        }

        /// <summary>
        /// Mark the adaptive interview completed, kick off report generation, and show the exit modal.
        /// </summary>
        private async Task<IActionResult> CompleteAdaptiveInterviewAsync(ChatMessageViewModel model)
        {
            await _aiinterviewerService.UpdateMeetingCompletedStatusForAIInterview(model.SessionId.ToString());

            // Await report generation (previously fire-and-forget). As a not-awaited cross-service HTTP call it could
            // be abandoned once this request completed, so the report (ReportOfAIInterviewByAssistant, and the
            // InterviewStatus/InterviewerComment it writes) frequently never got saved - only MeetingStatus, updated
            // above, changed. Awaiting guarantees the report is generated and persisted at completion time.
            // GenerateReport swallows its own exceptions and returns null on failure, so awaiting will not break the
            // completion modal.
            await _api.GenerateReport(
                model.SessionId,
                AdaptiveReportGenerationPrompt,
                (int)AIInterviewTypeEnum.Adaptive,
                isExit: true);

            var exitModel = new AIInterviewExitModalViewModel
            {
                Title = "Interview Complete",
                Message = "The interviewer is through with all the questions. This interview is now closed.",
                IconClass = "fa-check-circle",
                IconColor = "#28a745",
                ButtonText = "Close",
                ButtonClass = "btn-success",
                CandidateName = model.candidateName ?? "Candidate",
                InterviewDate = DateTime.Now,
                SessionId = model.SessionId.ToString(),
                ExitTime = DateTime.Now,
                IsFromIndex = true
            };

            return View("ExitModalPartial", exitModel);
        }

        private static DateTime? TryParseInterviewDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return DateTime.TryParse(value, out var parsed) ? parsed : null;
        }

        /// <summary>
        /// Refuses a submission that cannot be saved. Two of the four cases are the candidate's
        /// recording not having arrived, which they can fix by recording again, so those return
        /// them to the question. The other two - no question on the post, no interview id - mean
        /// the page itself is in a state a retry cannot repair, and still end the attempt.
        /// </summary>
        private async Task<IActionResult> ValidatePresetAnswerPayloadBeforeSave(ChatMessageViewModel model, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(model.CurrentQuestion))
            {
                return View("ExitModalPartial", CreateAnswerSubmitErrorView(
                    model.SessionId,
                    "Missing question",
                    "The current interview question was not found. Please reload the interview page and try again."));
            }

            if (string.IsNullOrWhiteSpace(userMessage)
                && !model.IsTypingSpeedQuestion
                && !model.IsListeningComprehensionQuestion
                && !model.IsCodingQuestion
                && !model.IsCodeProjectQuestion
                && !model.IsMcqAssessmentQuestion)
            {
                return await RetryAnswerAsync(
                    model,
                    "Missing answer",
                    "Your spoken answer was not captured. Please record your answer again and wait for processing to finish before submitting.");
            }

            // The MCQ assessment section is exempt from the video requirement for the same reason
            // typing and listening assessments are: there is nothing spoken to record. The
            // candidate answered inside the embedded test and the result is read from the MCQ API.
            if (!model.IsTypingSpeedQuestion
                && !model.IsListeningComprehensionQuestion
                && !model.IsCodingQuestion
                && !model.IsCodeProjectQuestion
                && !model.IsMcqAssessmentQuestion
                && (model.VideoFile == null || model.VideoFile.Length == 0))
            {
                return await RetryAnswerAsync(
                    model,
                    "Video not ready",
                    "Your video answer had not finished uploading. Please record your answer again, and wait a moment after stopping before you submit.");
            }

            if (model.InterviewId <= 0)
            {
                return View("ExitModalPartial", CreateAnswerSubmitErrorView(
                    model.SessionId,
                    "Session error",
                    "Interview details were missing for this submission. Please reload the interview link and try again."));
            }

            return null;
        }

        /// <summary>
        /// Restores the coding attributes of a question that was rebuilt from the transcript.
        /// The transcript only stores the question text, so on a page reload the model would
        /// otherwise fall back to IsCodingQuestion = false and the IDE would never open.
        /// </summary>
        private async Task ApplyCodingQuestionAttributesAsync(ChatMessageViewModel model, int questionId)
        {
            if (model == null || questionId <= 0) return;

            try
            {
                var questionDetails = await _iRFService.GetDeliveryHeadSampleQuestionById(questionId);
                if (questionDetails == null) return;

                model.IsCodingQuestion = questionDetails.IsCodingQuestion;
                model.CodingTimeLimitInMinutes = questionDetails.CodingTimeLimitInMinutes;
                model.IsCodeProjectQuestion = questionDetails.IsCodeProjectQuestion;
            }
            catch (Exception ex)
            {
                // A lookup failure must not break the interview; the question still renders normally
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        /// <summary>
        /// Describes the MCQ assessment section for the briefing panel. Does no network work.
        ///
        /// Provisioning is deliberately NOT done here. Building a sectioned paper is one question
        /// generation call per section, which takes upwards of a minute, and doing that inside the
        /// interview page's request would leave the candidate on a blank page for that whole time
        /// with no idea whether anything was happening. Instead the page renders at once with a
        /// briefing screen, and <see cref="PrepareMcqSection"/> does the provisioning when the
        /// candidate presses Start - which is also the point at which the section's clock should
        /// begin.
        ///
        /// The section names shown on the briefing come from the channel's own configuration, so
        /// the panel can name them without waiting on the MCQ side.
        /// </summary>
        private void DescribeMcqAssessmentSection(ChatMessageViewModel model, MeetingVM meetingDetails)
        {
            if (model == null || meetingDetails == null || !model.IsMcqAssessmentQuestion)
                return;

            if (!meetingDetails.HasMcqAssessmentSection || !_mcqAssessmentService.IsConfigured)
            {
                // The question is flagged as the MCQ section but the channel is not switched on
                // (or the integration is not configured at all). Treat it as unavailable so the
                // page offers a way past it instead of rendering an empty overlay.
                model.McqSectionUnavailable = true;
                return;
            }

            model.McqTotalMinutes = meetingDetails.McqAssessmentMinutes ?? 0;
            model.McqSectionTitles = ParseSectionKeys(meetingDetails.McqAssessmentSections)
                .Select(HumanizeSectionKey)
                .ToList();
        }

        /// <summary>
        /// Provisions the MCQ section and hands back the iframe URL. Called from the interview
        /// page when the candidate presses Start on the assessment briefing.
        ///
        /// Idempotent on the MCQ side per interview session, so a retry after a timeout or a
        /// reload returns the same paper and the same launch token rather than a second test.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PrepareMcqSection(Guid sid)
        {
            try
            {
                var meetingDetails = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(sid);
                if (meetingDetails == null)
                    return Json(new { unavailable = true, reason = "session-not-found" });

                if (!meetingDetails.HasMcqAssessmentSection || !_mcqAssessmentService.IsConfigured)
                    return Json(new { unavailable = true, reason = "not-enabled" });

                var sections = ParseSectionKeys(meetingDetails.McqAssessmentSections);

                var result = await _mcqAssessmentService.ProvisionSectionAsync(
                    sid.ToString(),
                    new McqSectionProvisionRequest
                    {
                        // IRF.IRFId is what MCQIRF.PortalIRFId holds on the MCQ side - the channel
                        // link that already exists between the two databases.
                        SkillChannelId = meetingDetails.IRFId,
                        CandidateEmail = meetingDetails.candidateEmail,
                        CandidateName = meetingDetails.candidateName,
                        PrimarySkill = string.IsNullOrWhiteSpace(meetingDetails.PrimarySkill)
                            ? meetingDetails.JDTiltle
                            : meetingDetails.PrimarySkill,
                        JobDescription = await ResolveJobDescriptionTextAsync(meetingDetails),
                        Sections = sections.Count > 0 ? sections : null,
                        TotalMinutes = meetingDetails.McqAssessmentMinutes
                    });

                if (result == null || string.IsNullOrWhiteSpace(result.EmbedUrl))
                    return Json(new { unavailable = true, reason = "provision-failed" });

                return Json(new
                {
                    unavailable = false,
                    embedUrl = result.EmbedUrl,
                    totalMinutes = result.TotalMinutes,
                    questionCount = result.QuestionCount,
                    sections = (result.Sections ?? new List<McqSectionInfo>())
                        .OrderBy(s => s.SortOrder)
                        .Select(s => s.SectionTitle)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { unavailable = true, reason = "error" });
            }
        }


        private static List<string> ParseSectionKeys(string configured)
        {
            return (configured ?? string.Empty)
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }

        /// <summary>
        /// "data-interpretation" -> "Data Interpretation". Only for the briefing panel, which has
        /// to name the sections before the MCQ side has returned their real titles.
        /// </summary>
        private static string HumanizeSectionKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return key;

            var words = key.Replace('_', '-').Split('-', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words.Select(w =>
                w.Length == 1 ? w.ToUpperInvariant() : char.ToUpperInvariant(w[0]) + w.Substring(1)));
        }

        /// <summary>
        /// Reads the finished MCQ section back from screen.risebird.io, caches it in the portal,
        /// and returns a plain-language summary to store as the candidate's answer for this
        /// question.
        ///
        /// The summary is what the interview's evaluator prompt reads, so it states section-level
        /// results in words rather than as JSON - the evaluator is being asked to weigh a written
        /// test, not to parse a payload.
        /// </summary>
        private async Task<string> CaptureMcqAssessmentResultAsync(ChatMessageViewModel model, MeetingVM meetingDetails)
        {
            var sessionId = model.SessionId.ToString();

            try
            {
                var report = await _mcqAssessmentService.FetchReportAsync(sessionId);

                if (report == null)
                {
                    // Either the candidate skipped the section or the MCQ host is unreachable.
                    // The interview continues either way, and the report retries the fetch later -
                    // but a marker has to be left, because an absent row is what tells the question
                    // flow the section is still owed. Without it the candidate would be handed the
                    // section again on the next page load, forever.
                    await MarkMcqAssessmentAttemptedAsync(meetingDetails, sessionId, "not-captured");
                    return "The candidate did not complete the MCQ assessment section.";
                }

                await CacheMcqAssessmentAsync(report, meetingDetails, sessionId);

                if (!report.IsCompleted)
                    return "The candidate started but did not submit the MCQ assessment section.";

                var summary = new StringBuilder();
                summary.AppendLine(
                    $"MCQ assessment section completed: {report.CorrectAnswers} of {report.TotalQuestions} correct " +
                    $"({report.ScorePercent:0.#}%).");

                foreach (var section in report.OrderedSections)
                {
                    summary.AppendLine(
                        $"- {section.SectionTitle}: {section.Correct} of {section.QuestionCount} correct " +
                        $"({section.ScorePercent:0.#}%), {section.Attempted} attempted.");
                }

                if (report.Elapsed.HasValue && report.AllottedMinutes.HasValue)
                {
                    summary.AppendLine(
                        $"Time taken: {report.Elapsed.Value.TotalMinutes:0} of {report.AllottedMinutes} minutes allotted.");
                }

                return summary.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                // Same reason as the null case above: leave the marker, or the section is served
                // again on every subsequent load and the interview can never finish.
                await MarkMcqAssessmentAttemptedAsync(meetingDetails, sessionId, "error");
                return "The MCQ assessment section result could not be retrieved.";
            }
        }

        /// <summary>
        /// Records that this session's MCQ section has been dealt with, even though no result could
        /// be read. Presence of the row - not its contents - is what stops the question flow
        /// offering the section again.
        /// </summary>
        private async Task MarkMcqAssessmentAttemptedAsync(MeetingVM meetingDetails, string sessionId, string status)
        {
            try
            {
                await _aiinterviewerService.SaveAIInterviewMcqAssessment(new McqAssessmentCacheRow
                {
                    SessionId = sessionId,
                    InterviewId = meetingDetails?.InterviewId,
                    IRFId = meetingDetails?.irfIntId,
                    Status = status
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        /// <summary>
        /// Whether this session's MCQ section has already been dealt with.
        ///
        /// A missing row means the section is still owed, so a failure to read has to be treated as
        /// "already done": re-offering the section on a transient database error would trap the
        /// candidate in it, which is far worse than skipping a section they have very likely already sat.
        /// </summary>
        private async Task<bool> HasCapturedMcqAssessmentAsync(Guid sessionId)
        {
            try
            {
                var cached = await _aiinterviewerService.GetAIInterviewMcqAssessment(sessionId.ToString());
                return cached != null;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return true;
            }
        }

        /// <summary>
        /// Turns <paramref name="model"/> into the MCQ assessment section and returns true, when the
        /// skill channel is opted in and the section has not been sat yet.
        ///
        /// Called where the interview has run out of spoken questions and would otherwise close.
        /// The section is therefore served after the spoken questions and before completion, and it
        /// needs no row in SkillChannelSampleQuestions: that table is for questions authored in this
        /// portal, and the MCQ section's questions are not - they are generated and served by
        /// screen.risebird.io. The skill channel's own opt-in is the only setting required.
        ///
        /// Nothing is written to the transcript here. This runs again on every reload while the
        /// section is outstanding, which is what lets a candidate resume it, and a transcript entry
        /// per reload would both pollute the report and inflate the asked-question count.
        /// </summary>
        private async Task<bool> TryServeMcqAssessmentSectionAsync(ChatMessageViewModel model, MeetingVM meetingDetails)
        {
            if (model == null || meetingDetails == null) return false;
            if (!meetingDetails.HasMcqAssessmentSection) return false;

            // With the integration switched off there is nothing to serve, and stalling the
            // interview on a section that cannot load would be worse than closing it.
            if (!_mcqAssessmentService.IsConfigured) return false;

            if (await HasCapturedMcqAssessmentAsync(model.SessionId)) return false;

            model.IsMcqAssessmentQuestion = true;
            // No sample question backs this one. The POST handler reads a zero id in the preset
            // branch as "this was the MCQ section", so it must stay zero.
            model.CurrentQuestionId = 0;
            model.CurrentQuestion = McqAssessmentIntroduction;
            model.SynthesizedVideoUrl = null;
            model.IsCodingQuestion = false;
            model.CodingTimeLimitInMinutes = null;
            model.IsCodeProjectQuestion = false;
            model.IsTypingSpeedQuestion = false;
            model.IsListeningComprehensionQuestion = false;
            model.ListeningAudioUrl = null;
            model.ListeningAudioTranscription = null;

            // Fills in the section names and duration the briefing panel shows. No network call -
            // the iframe URL is fetched separately, when the candidate presses Start.
            DescribeMcqAssessmentSection(model, meetingDetails);

            model.ChatLog.Add(new ChatLine { Role = "Bot", Message = model.CurrentQuestion });
            return true;
        }

        /// <summary>
        /// Writes the fetched MCQ result into the portal's read-through cache. Failures are
        /// logged and swallowed: the cache is an availability aid, and losing a write costs a
        /// later refetch, not the result itself.
        /// </summary>
        private async Task CacheMcqAssessmentAsync(McqAssessmentReport report, MeetingVM meetingDetails, string sessionId)
        {
            try
            {
                await _aiinterviewerService.SaveAIInterviewMcqAssessment(new McqAssessmentCacheRow
                {
                    SessionId = sessionId,
                    InterviewId = meetingDetails?.InterviewId,
                    IRFId = meetingDetails?.irfIntId,
                    McqId = report.McqId,
                    Status = report.Status,
                    TotalQuestions = report.TotalQuestions,
                    CorrectAnswers = report.CorrectAnswers,
                    ScorePercent = report.ScorePercent,
                    RedFlags = report.RedFlags,
                    AllottedMinutes = report.AllottedMinutes,
                    ElapsedSeconds = report.ElapsedSeconds,
                    TabSwitchCount = report.TabSwitchCount,
                    StartedOn = report.StartedOn,
                    CompletedOn = report.CompletedOn,
                    ReportJson = JsonConvert.SerializeObject(report)
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        /// <summary>
        /// The MCQ section result for a report render: live where possible, cached otherwise.
        ///
        /// Live is preferred because a candidate can finish the section after the interview's own
        /// completion write, and a successful fetch refreshes the cache. The cache is what makes
        /// the report render at all when the MCQ host is down, and what makes a PDF generated
        /// months later show what the report showed.
        /// </summary>
        private async Task<McqAssessmentReport> ResolveMcqAssessmentForReportAsync(MeetingVM meetingDetails, Guid sessionId)
        {
            if (meetingDetails == null || !meetingDetails.HasMcqAssessmentSection)
                return null;

            var sessionKey = sessionId.ToString();

            // Why the cached copy is being shown, so the banner can say the true thing. A live
            // lookup that answers "no result for this session" is not an outage, and a report that
            // calls it one sends whoever reads it looking for a fault that does not exist.
            var cacheReason = McqCacheReason.NotConfigured;

            if (_mcqAssessmentService.IsConfigured)
            {
                cacheReason = McqCacheReason.NoLiveResult;
                try
                {
                    var live = await _mcqAssessmentService.FetchReportAsync(sessionKey);
                    if (live != null)
                    {
                        await CacheMcqAssessmentAsync(live, meetingDetails, sessionKey);
                        return live;
                    }
                }
                catch (Exception ex)
                {
                    cacheReason = McqCacheReason.Unreachable;
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }

            try
            {
                var cached = await _aiinterviewerService.GetAIInterviewMcqAssessment(sessionKey);
                if (cached == null) return null;

                if (!string.IsNullOrWhiteSpace(cached.ReportJson))
                {
                    var report = JsonConvert.DeserializeObject<McqAssessmentReport>(cached.ReportJson);
                    if (report != null)
                    {
                        report.IsFromCache = true;
                        report.CacheReason = cacheReason;
                        report.CachedOn = cached.FetchedOn;
                        return report;
                    }
                }

                // A row with no payload is the marker written when the section was reached but no
                // result could be read - the candidate abandoned it, or the MCQ side was down at
                // submit. Returning null here would drop the block from the report entirely, which
                // reads as "this channel has no assessment" rather than "the candidate did not
                // complete it". The denormalised columns are enough to say the latter honestly.
                return new McqAssessmentReport
                {
                    AiSessionId = cached.SessionId,
                    McqId = cached.McqId,
                    Status = string.IsNullOrWhiteSpace(cached.Status) ? "not-completed" : cached.Status,
                    TotalQuestions = cached.TotalQuestions ?? 0,
                    CorrectAnswers = cached.CorrectAnswers ?? 0,
                    ScorePercent = cached.ScorePercent ?? 0m,
                    RedFlags = cached.RedFlags ?? 0,
                    AllottedMinutes = cached.AllottedMinutes,
                    ElapsedSeconds = cached.ElapsedSeconds,
                    TabSwitchCount = cached.TabSwitchCount,
                    StartedOn = cached.StartedOn,
                    CompletedOn = cached.CompletedOn,
                    IsFromCache = true,
                    CacheReason = cacheReason,
                    CachedOn = cached.FetchedOn
                };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        /// <summary>
        /// Text of the job description, used to steer question generation for the role-specific
        /// sections. Falls back to the JD title when the linked document cannot be read - a
        /// weaker prompt is much better than no section.
        /// </summary>
        private async Task<string> ResolveJobDescriptionTextAsync(MeetingVM meetingDetails)
        {
            if (string.IsNullOrWhiteSpace(meetingDetails?.JDLink))
                return meetingDetails?.JDTiltle;

            try
            {
                var text = await _pdfReaderHelper.ReadPdfFromUrlAsync(meetingDetails.JDLink);
                if (string.IsNullOrWhiteSpace(text))
                    return meetingDetails.JDTiltle;

                // The generation prompt does not need the whole document, and a very long JD
                // crowds out the section brief that says what to actually test.
                return text.Length > 8000 ? text.Substring(0, 8000) : text;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return meetingDetails.JDTiltle;
            }
        }

        /// <summary>
        /// Transcribes a recorded answer and hands the text back to the candidate to read and
        /// correct, without submitting anything.
        ///
        /// The answer is only saved when the candidate submits the form afterwards, carrying the
        /// text they confirmed. Nothing here writes to the database, and the interview timer is
        /// untouched — it keeps running while they read, exactly as it does while they think.
        /// </summary>
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 1048576000)]
        [RequestSizeLimit(1048576000)]
        public async Task<JsonResult> TranscribeAnswer(Guid sessionId, bool isMultiLingual = false)
        {
            try
            {
                // Resolved from the channel, not from anything the page posted: this endpoint is
                // what produces the transcript the candidate is then trusted to confirm, so a
                // channel with review switched off must not be able to reach it.
                var transcribeMeeting = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(sessionId);
                if (!IsCandidateTranscriptReviewEnabled(transcribeMeeting))
                    return Json(new { success = false, message = "Transcript review is not enabled." });

                var file = Request.Form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Your recording was not received. Please record again."
                    });
                }

                if (file.Length < 500)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Your recording was too short to transcribe. Please speak for at least a few seconds."
                    });
                }

                string transcript;
                try
                {
                    using var stream = file.OpenReadStream();
                    transcript = await _speechToTextService.TranscribeAsync(stream, file.FileName, isMultiLingual);
                }
                catch (Exception sttEx)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, sttEx);
                    return Json(new
                    {
                        success = false,
                        message = "Could not transcribe your recording. Please check your microphone and try again."
                    });
                }

                if (LooksLikeSilenceHallucination(transcript))
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                        new Exception($"Discarded a hallucinated transcript during review for session {sessionId}."));
                    transcript = string.Empty;
                }

                if (!IsTranscriptionValid(transcript, isMultiLingual))
                {
                    // Not an error the candidate caused, and not a reason to lose the take: they can
                    // type the answer in the box instead of re-recording.
                    return Json(new
                    {
                        success = true,
                        transcript = string.Empty,
                        warning = "We could not make out your recording clearly. Please type your answer below before submitting."
                    });
                }

                return Json(new { success = true, transcript = transcript?.Trim() ?? string.Empty });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not transcribe your recording. Please try again." });
            }
        }

        /// <summary>
        /// Whether candidates review their transcript before submitting. On by default; the switch
        /// exists so it can be turned off without a deploy if it misbehaves in a live interview.
        /// </summary>
        /// <summary>
        /// Whether the candidate reviews their transcript before submitting, for one channel.
        ///
        /// The channel decides when it has an opinion; otherwise the environment's setting applies.
        /// Review costs the candidate time on the clock, which suits a screening conversation and
        /// does not always suit a timed technical round, so this is a per-channel call - but every
        /// channel that predates the column keeps exactly the behaviour it has today.
        /// </summary>
        private bool IsCandidateTranscriptReviewEnabled(MeetingVM meeting) =>
            meeting?.CandidateTranscriptReview
            ?? _configuration.GetValue("AIInterview:CandidateTranscriptReview", true);


        /// <summary>
        /// The reviewed transcript this submission carries, or null when the answer did not go
        /// through review and the server should transcribe the recording itself.
        ///
        /// Both halves have to be present: a confirmed transcript with no raw one beside it would
        /// mean the posted text was never checked against anything the candidate actually said.
        /// </summary>
        private CandidateTranscriptReview BuildTranscriptReview(ChatMessageViewModel model, MeetingVM meeting)
        {
            if (!IsCandidateTranscriptReviewEnabled(meeting)) return null;
            if (model == null) return null;
            if (string.IsNullOrWhiteSpace(model.ConfirmedTranscript)) return null;
            if (model.RawTranscript == null) return null;

            return CandidateTranscriptReview.Build(model.RawTranscript, model.ConfirmedTranscript);
        }

        /// <summary>
        /// Sends the candidate back to the question they were answering, with an explanation,
        /// instead of ending their interview.
        ///
        /// Every audio failure used to return ExitModalPartial, whose only control is a Close
        /// button bound to window.close(). A candidate whose upload never attached, whose recording
        /// was clipped, or whose transcription came back as noise was removed from an interview
        /// they could simply have repeated - and since the answer post is a full form submit, the
        /// page carrying their interview was gone, so there was no way back even in principle.
        ///
        /// The question stays on screen and the clock keeps its server-side value, so a retry costs
        /// the candidate time rather than the interview. Only the answer is cleared: leaving a
        /// stale transcript in the form would let the failed attempt be resubmitted behind them.
        /// </summary>
        private async Task<IActionResult> RetryAnswerAsync(ChatMessageViewModel model, string title, string message)
        {
            await RefreshModelMeetingFieldsAsync(model);

            model.Message = null;
            model.RawTranscript = null;
            model.ConfirmedTranscript = null;
            model.TranscriptTakeCount = (model.TranscriptTakeCount < 1 ? 1 : model.TranscriptTakeCount) + 1;

            // The posted model carries no chat log - each branch builds its own from the transcript
            // on the way to a NEW question, and this render is the same question again. Without
            // this the candidate is returned to a blank conversation and cannot see what they were
            // asked. The earlier turns are not rebuilt; the question they have to answer is.
            if ((model.ChatLog == null || model.ChatLog.Count == 0)
                && !string.IsNullOrWhiteSpace(model.CurrentQuestion))
            {
                model.ChatLog = new List<ChatLine>
                {
                    new ChatLine
                    {
                        Role = "Bot",
                        Message = CleanMessage(model.CurrentQuestion),
                        sampleQuestionId = model.CurrentQuestionId
                    }
                };
            }

            ViewBag.AnswerRetryTitle = title;
            ViewBag.AnswerRetryMessage = message;
            ViewBag.AnswerRetryTakeCount = model.TranscriptTakeCount;

            // Worth a log line even though the candidate recovers: a channel where this fires often
            // has a microphone or encoding problem that no single candidate will ever report.
            await _applicationErrorLogger.LogMessage(
                LogLevelInfo.Info,
                $"AI interview session {model.SessionId}: answer attempt {model.TranscriptTakeCount - 1} " +
                $"was refused ({title}); the candidate was returned to the question to record again.");

            return View("Index", model);
        }

        /// <summary>
        /// Speech-to-text models answer silence with a confident, highly repetitive sentence,
        /// often in an unrelated language. Detects that signature so it can be discarded instead
        /// of being filed as the candidate's spoken answer.
        /// </summary>
        private static bool LooksLikeSilenceHallucination(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var sentences = text
                .Split(new[] { '.', '。', '!', '！', '?', '？', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            if (sentences.Count < 5) return false;

            var distinctCount = sentences.Distinct(StringComparer.OrdinalIgnoreCase).Count();

            // Genuine speech varies; a loop of the same sentence is the hallucination signature
            return distinctCount * 5 <= sentences.Count;
        }

        /// <summary>
        /// Merges the spoken explanation and the submitted code into a single transcript entry,
        /// using the same "Verbal Explanation / Written Code" layout as chat-box coding answers so
        /// the candidate report renders both the same way.
        /// </summary>
        private static string BuildCodingAnswerTranscript(string spokenExplanation, string language, string code)
        {
            var builder = new StringBuilder();

            builder.AppendLine(string.IsNullOrWhiteSpace(spokenExplanation)
                ? "Verbal Explanation : (no spoken explanation was recorded)"
                : $"Verbal Explanation : {spokenExplanation.Trim()}");

            builder.AppendLine();
            builder.AppendLine(string.IsNullOrWhiteSpace(language)
                ? "Written Code :"
                : $"Written Code ({language}) :");
            builder.AppendLine((code ?? string.Empty).Trim());

            return builder.ToString().TrimEnd();
        }

        private bool IsTranscriptionValid(string text, bool isMultilingual)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            if (!isMultilingual)
            {
                var cleaned = new string(text
                    .Where(c => char.IsLetter(c) || char.IsDigit(c))
                    .ToArray());

                // Check ratio of valid characters to overall text
                double validRatio = (double)cleaned.Length / text.Length;
                if (validRatio < 0.5)
                    return false;

            }
            return true;

        }

        private async Task<bool> UploadBotInterviewVideo(ChatMessageViewModel model, string question, string candidateResponse, MeetingVM meeting)
        {
            try
            {
                string VideoLink = string.Empty;
                string candidateAnswerAudioLink = string.Empty;

                // If there's a video file, upload it
                if (model.VideoFile != null && model.VideoFile.Length > 0)
                {
                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.VideoFile);
                    if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4" || fileMimeType == "video/quicktime" || fileMimeType == "video/x-matroska" || fileMimeType == "video/webm")
                    {
                        //var key = await _aWSRekognitionService.UploadVideoAsync(model.VideoFile);
                        //var faceDetection = await _aWSRekognitionService.AnalyzeVideoAsync(key);
                        var file = model.VideoFile;
                        var videoExtension = fileMimeType.Contains("webm", StringComparison.OrdinalIgnoreCase) ? "webm" : "mp4";
                        var filename = model.SessionId.ToString().Replace(".", "_").Replace("@", "a").ToString() + DateTime.Now.ToString("yyyyMMddHHmmssffff") + "." + videoExtension;
                        //await _azureStorageHelper.InsertBlob(filename, "botinterviewvideo", file);
                        await _azureStorageHelper.InsertImageBlob(filename, "botinterviewvideo", file, fileMimeType);
                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("botinterviewvideo");
                        var blob = containerRef.GetBlockBlobReference(filename);

                        // Guard: if the upload didn't result in a usable candidate video link,
                        // do NOT proceed to next question and do NOT write transcripts.
                        // This prevents "phantom" answers when blob upload/URL generation fails intermittently.
                        var uploadedvideoFileURL = blob?.Uri?.ToString();
                        var sas = _sasUrlHelper.CreateSasUrl("botinterviewvideo");
                        var blobExists = blob != null && await blob.ExistsAsync();
                        if (!blobExists || string.IsNullOrWhiteSpace(uploadedvideoFileURL) || string.IsNullOrWhiteSpace(sas))
                        {
                            return false;
                        }

                        VideoLink = uploadedvideoFileURL + "?" + sas;
                    }
                    else
                    {
                        // Invalid video type: treat as a failure for the video-response workflow.
                        return false;
                    }

                    //if (VideoLink != null)
                    //{
                    //    // Get AI Video storage account settings from configuration
                    //    var aiVideoStorageName = _configuration["AIVideoStorageSettings:StorageName"];
                    //    var aiVideoStorageKey = _configuration["AIVideoStorageSettings:SecretKey"];

                    //    HttpClient myWebClient = _httpClientFactory.CreateClient();
                    //    using (Stream myStream = myWebClient.GetStreamAsync(VideoLink).Result)
                    //    {
                    //        var fileName = $"aiVideo__{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")}";
                    //        //long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    //        //fileName = fileName + "_" + unixTime.ToString();
                    //        try
                    //        {
                    //            // Use the new storage account for AI videos
                    //            var uploadedVideoUrl = await _azureStorageHelper.InsertBlobWithCustomStorage(
                    //                fileName,
                    //                "videos",
                    //                aiVideoStorageName,
                    //                aiVideoStorageKey,
                    //                blobFileStream: myStream,
                    //                contentType: "video/mp4");

                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                    //        }

                    //        //generate SAS URL from the uploaded video url
                    //        //var sas = _sasUrlHelper.CreateSasUrl(ContainerName);

                    //        //if (!string.IsNullOrEmpty(uploadedVideoUrl) && !string.IsNullOrEmpty(sas))
                    //        //{
                    //        //    videoSasUrl = uploadedVideoUrl + "?" + sas;
                    //        //}
                    //    }
                    //}
                }

                if (model.AudioFile != null && model.AudioFile.Length > 0)
                {
                    var audioMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);
                    if (string.IsNullOrWhiteSpace(audioMimeType) && !string.IsNullOrWhiteSpace(model.AudioFile.ContentType))
                    {
                        audioMimeType = model.AudioFile.ContentType.ToLowerInvariant();
                    }
                    if (string.IsNullOrWhiteSpace(audioMimeType)
                        && !string.IsNullOrWhiteSpace(model.AudioFile.FileName)
                        && model.AudioFile.FileName.EndsWith(".webm", StringComparison.OrdinalIgnoreCase))
                    {
                        audioMimeType = "audio/webm";
                    }

                    // Safari records audio as MP4, which is sniffed as video/mp4 (same container).
                    if (audioMimeType == "video/mp4") audioMimeType = "audio/mp4";

                    if (audioMimeType == "audio/mpeg" || audioMimeType == "audio/wav" || audioMimeType == "audio/ogg"
                        || audioMimeType == "audio/webm" || audioMimeType == "video/webm" || audioMimeType == "audio/mp4"
                        || audioMimeType == "audio/x-m4a" || audioMimeType == "application/octet-stream")
                    {
                        var uploadContentType = audioMimeType == "application/octet-stream" ? "audio/webm" : audioMimeType;
                        var audioExtension = uploadContentType.Contains("webm", StringComparison.OrdinalIgnoreCase) ? "webm"
                            : uploadContentType.Contains("mp4", StringComparison.OrdinalIgnoreCase) || uploadContentType.Contains("m4a", StringComparison.OrdinalIgnoreCase) ? "m4a"
                            : uploadContentType.Contains("ogg", StringComparison.OrdinalIgnoreCase) ? "ogg"
                            : uploadContentType.Contains("wav", StringComparison.OrdinalIgnoreCase) ? "wav"
                            : uploadContentType.Contains("mpeg", StringComparison.OrdinalIgnoreCase) || uploadContentType.Contains("mp3", StringComparison.OrdinalIgnoreCase) ? "mp3"
                            : "webm";
                        var audioFilename = model.SessionId.ToString().Replace(".", "_").Replace("@", "a")
                            + "_answer_" + DateTime.Now.ToString("yyyyMMddHHmmssffff") + "." + audioExtension;
                        await _azureStorageHelper.InsertImageBlob(audioFilename, "botinterviewvideo", model.AudioFile, uploadContentType);
                        var audioContainerRef = _azureStorageHelper.GetCloudBlobContainer("botinterviewvideo");
                        var audioBlob = audioContainerRef.GetBlockBlobReference(audioFilename);
                        var uploadedAudioFileUrl = audioBlob?.Uri?.ToString();
                        var audioSas = _sasUrlHelper.CreateSasUrl("botinterviewvideo");
                        var audioBlobExists = audioBlob != null && await audioBlob.ExistsAsync();
                        if (audioBlobExists && !string.IsNullOrWhiteSpace(uploadedAudioFileUrl) && !string.IsNullOrWhiteSpace(audioSas))
                        {
                            candidateAnswerAudioLink = uploadedAudioFileUrl + "?" + audioSas;
                        }
                    }
                }

                // If no video file but there's a text/code response, save it with empty VideoLink
                // This allows text/code submissions to be saved successfully

                // Save in db with question.sessionid,videoLink,Datetime (VideoLink can be empty for text responses)
                // For the Adaptive/OpenAI flow, CurrentQuestionId is 0 because there are no preset question IDs.
                // Passing 0 is treated as non-null by the stored proc, which causes it to UPDATE the very first row
                // (DeliveryHeadQuestionId=0) on every subsequent answer instead of INSERTing a new row.
                // Passing null tells the stored proc to always INSERT a new row, creating one entry per answered question.
                int? deliveryHeadQuestionId = model.CurrentQuestionId > 0 ? model.CurrentQuestionId : (int?)null;
                // The MCQ assessment section records nothing - the candidate answered inside the
                // embedded test - so it is the one question type allowed to save without a video.
                // What speech-to-text heard is stored beside what the candidate confirmed, so a
                // dispute can be read back against the recording rather than argued from memory.
                var result = await _aiinterviewerService.UpdateBotInterviewVideoDetails(VideoLink, question, model.SessionId.ToString(), model.SynthesizedVideoUrl, candidateResponse, model.TimeLeftInSeconds, model.InterviewId, model.AIInterviewerAudioLink, deliveryHeadQuestionId, model.IsTypingSpeedQuestion, model.IsListeningComprehensionQuestion, candidateAnswerAudioLink, model.IsMcqAssessmentQuestion, BuildTranscriptReview(model, meeting), model.TranscriptTakeCount);

                // This question is answered, so the next one starts its own count. Without this the
                // posted value rides along on the model and every later question inherits the takes
                // of the one before it.
                if (result) model.TranscriptTakeCount = 0;

                return result;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;

            }
        }

        private async Task<string> UploadAIInterviewerAudio(byte[] audioBytes, Guid sessionId)
        {
            if (audioBytes != null)
            {
                //var fileMimeType = _fileMimeTypeHelper.GetFileMimeTypeFromByteArray(audioBytes);
                var fileMimeType = "audio/wav";
                if (fileMimeType == "audio/mpeg" || fileMimeType == "audio/wav" || fileMimeType == "audio/ogg")
                {
                    // Generate a unique filename
                    var filename = sessionId.ToString()
                        .Replace(".", "_")
                        .Replace("@", "a") +
                        DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".mp3"; // extension based on MIME type

                    // Upload from byte[] (instead of IFormFile)
                    string uploadedAudioUrl;
                    using (var stream = new MemoryStream(audioBytes))
                    {
                        //await _azureStorageHelper.InsertImageBlob(filename, "botintervieweraudio", stream, fileMimeType);
                        uploadedAudioUrl = await _azureStorageHelper.InsertBlobAIInterviewerAudio(filename, "botintervieweraudio", stream, contentType: fileMimeType);
                    }

                    var sas = _sasUrlHelper.CreateSasUrl("botintervieweraudio");
                    var audioLink = string.Empty;
                    if (!string.IsNullOrEmpty(uploadedAudioUrl) && !string.IsNullOrEmpty(sas))
                    {
                        audioLink = uploadedAudioUrl + "?" + sas;
                    }
                    return audioLink;
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExitModalPartial(Guid seId)
        {
            var meetingDetails = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(seId);

            // Exit Interview on an adaptive interview ends it: without this the page said "closed" while
            // the meeting stayed Pending and no report was generated. Guarded on status so a second
            // load of this modal does not regenerate the report.
            if (meetingDetails != null
                && meetingDetails.AIInterviewType == (int)AIInterviewTypeEnum.Adaptive
                && meetingDetails.MeetingStatus != (int)MeetingStatus.Completed)
            {
                try
                {
                    await _aiinterviewerService.UpdateMeetingCompletedStatusForAIInterview(seId.ToString());
                    await _api.GenerateReport(seId, AdaptiveReportGenerationPrompt, (int)AIInterviewTypeEnum.Adaptive, isExit: true);
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    await _aiinterviewerService.LogAIInterviewException(seId.ToString(), "GET Exit", ex);
                }
            }

            var exitModel = new AIInterviewExitModalViewModel
            {
                Title = "Interview Complete",
                Message = "The interviewer is through with all the questions. This interview is now closed.",
                IconClass = "fa-check-circle",
                IconColor = "#28a745",
                ButtonText = "Close",
                ButtonClass = "btn-success",
                IsSuccess = true,
                CandidateName = meetingDetails?.candidateName ?? "Unknown",
                SessionId = seId.ToString(),
                ExitTime = DateTime.Now,
                InterviewDate = meetingDetails?.interviewDate,
                IsFromIndex = true
            };

            return View(exitModel);
        }

        /// <summary>
        /// Renders the candidate's AI interview report.
        ///
        /// This wrapper exists because the app is configured with
        /// <c>UseExceptionHandler("/Home/Index")</c>: any exception escaping this action sends the
        /// user to the site root, which then bounces an operational user to the multi-session
        /// screen. The report appearing to "redirect to the login page" was that - a crash wearing
        /// a login page's clothes, with the real error visible only in the logs.
        ///
        /// Reports pull from several places - the evaluator, blob storage for the media, and the
        /// MCQ system for the assessment section - so a failure in any one of them has to say so
        /// on the page instead of dumping the reader somewhere unrelated.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Report(Guid seId, bool redo = false, bool? isExit = null)
        {
            try
            {
                return await BuildReportAsync(seId, redo, isExit);
            }
            catch (Exception ex)
            {
                // Logged with the session id so the failing report can be found from the message
                // the tester or client reports.
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                    new Exception($"AI interview report failed to render for session {seId}.", ex));
                await _aiinterviewerService.LogAIInterviewException(seId.ToString(), "GET Report", ex);

                return View("ExitModalPartial", new AIInterviewExitModalViewModel
                {
                    Title = "Report unavailable",
                    Message = "This report could not be generated just now. The error has been logged. Please retry in a few minutes, and quote session "
                              + seId + " if it keeps failing.",
                    IconClass = "fa-exclamation-triangle",
                    IconColor = "#dc3545",
                    ButtonText = "Close",
                    ButtonClass = "btn-danger",
                    IsSuccess = false,
                    SessionId = seId.ToString(),
                    ExitTime = DateTime.Now
                });
            }
        }

        private async Task<IActionResult> BuildReportAsync(Guid seId, bool redo = false, bool? isExit = null)//Guid seId
        {
            string profileName = string.Empty;
            profileName = HttpContext?.Session?.GetString("profileName")?.Trim();
            profileName = profileName?.Replace("\"", "");
            ViewBag.ProfileName = profileName;

            var meetingDetails = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(seId);
            if (meetingDetails == null)
            {
                return View("ExitModalPartial", CreateAnswerSubmitErrorView(
                    seId,
                    "Interview not found",
                    "This interview session could not be found. Please reopen the original interview link."));
            }

            if (meetingDetails.AIInterviewType == (int)AIInterviewTypeEnum.Preset)
            {
                InterviewReportResult aiReport = null;
                if (redo)
                {
                    aiReport = await _api.GenerateReport(seId, string.Empty, meetingDetails.AIInterviewType, isExit: isExit);
                    //if (aiReport != null)
                    //{
                    //    await _aiinterviewerService.SaveReportOfAIInterviewByAssistant(seId, JsonConvert.SerializeObject(aiReport), isExit);
                    //}
                }
                else
                {
                    var aiReportJson = await _aiinterviewerService.GetReportOfAIInterviewByAssistant(seId);
                    if (!string.IsNullOrEmpty(aiReportJson))
                    {
                        aiReport = JsonConvert.DeserializeObject<InterviewReportResult>(aiReportJson);
                    }
                    if (aiReport == null)
                    {
                        aiReport = await _api.GenerateReport(seId, string.Empty, meetingDetails.AIInterviewType, isExit: isExit);
                        var list = new List<string>();
                        list.Add(aiReport?.Replies?.FirstOrDefault());
                        if (aiReport != null)
                        {
                            aiReport.Replies = list;

                        }
                        if (aiReport != null)
                        {
                            //var result = await _aiinterviewerService.SaveReportOfAIInterviewByAssistant(seId, JsonConvert.SerializeObject(aiReport), isExit);
                            if (isExit.HasValue && isExit.Value)
                            {
                                if (aiReport.IsReportSavedToDB)
                                {
                                    var exitModel = new AIInterviewExitModalViewModel
                                    {
                                        Title = "Interview Session Complete",
                                        Message = "Meeting closed successfully. Thank you for your time!",
                                        IconClass = "fa-check-circle",
                                        IconColor = "#28a745",
                                        ButtonText = "Close",
                                        ButtonClass = "btn-success",
                                        IsSuccess = true,
                                        CandidateName = meetingDetails.candidateName,
                                        SessionId = seId.ToString(),
                                        ExitTime = DateTime.Now,
                                        InterviewDate = meetingDetails.interviewDate,
                                        IsFromIndex = false // Add this flag to identify the source
                                    };
                                    return View("ExitModalPartial", exitModel);
                                }
                                else
                                {
                                    var exitModel = new AIInterviewExitModalViewModel
                                    {
                                        Title = "Error",
                                        Message = "Error while exiting the interview. Please contact support.",
                                        IconClass = "fa-exclamation-triangle",
                                        IconColor = "#dc3545",
                                        ButtonText = "Close",
                                        ButtonClass = "btn-danger",
                                        IsSuccess = false,
                                        CandidateName = meetingDetails.candidateName,
                                        SessionId = seId.ToString(),
                                        ExitTime = DateTime.Now
                                    };
                                    return View("ExitModalPartial", exitModel);
                                }
                            }
                        }
                    }
                }

                if (isExit.HasValue && isExit.Value)
                {
                    var exitModel = new AIInterviewExitModalViewModel
                    {
                        Title = "Interview Session Complete",
                        Message = "Meeting closed successfully. Thank you for your time!",
                        IconClass = "fa-check-circle",
                        IconColor = "#28a745",
                        ButtonText = "Close",
                        ButtonClass = "btn-success",
                        IsSuccess = true,
                        CandidateName = meetingDetails.candidateName,
                        SessionId = seId.ToString(),
                        ExitTime = DateTime.Now
                    };
                    return View("ExitModalPartial", exitModel);
                }

                var reportModel = new AIInterviewReportWrapperClass();
                reportModel.videoQuestionDetails = await RefreshBotInterviewMediaSasUrlsAsync(
                    await _aiinterviewerService.GetBotInterviewVideoDetails(seId));
                //reportModel.chatMessageViewModel = chatMessageModel; 
                reportModel.AIInterviewEvaluation = TryParseEvaluation(aiReport?.Replies?.FirstOrDefault());
                EnrichQuestionRatingsWithActualQuestions(reportModel.AIInterviewEvaluation, reportModel.videoQuestionDetails);
                //reportModel.TotalQuestions = result.Replies.Count() - 1;
                ViewBag.IsAdaptive = false;
                List<TypingSpeedAssessment> typingAssessments;
                try { typingAssessments = await _aiinterviewerService.GetTypingSpeedAssessmentBySessionId(seId.ToString()) ?? new List<TypingSpeedAssessment>(); } catch { typingAssessments = new List<TypingSpeedAssessment>(); }
                ViewBag.TypingSpeedAssessments = typingAssessments;
                reportModel.TypingSpeedAssessments = typingAssessments;
                reportModel.CodeProjectAssessments = await LoadCodeProjectAssessmentsAsync(seId);
                reportModel.CandidateEmail = meetingDetails.candidateEmail;
                reportModel.CandidateName = meetingDetails.candidateName;
                reportModel.InterviewDate = meetingDetails.interviewDate;
                reportModel.SessionId = seId.ToString();
                reportModel.InterviewId = meetingDetails.InterviewId;
                reportModel.MatchScorePercentage = meetingDetails.MatchScorePercentage;
                reportModel.resumeLink = meetingDetails.resumeLink;
                reportModel.AIResumeScreeningVerdict = meetingDetails.AIResumeScreeningVerdict;
                reportModel.AIResumeScreeningScore = meetingDetails.AIResumeScreeningScore;
                reportModel.ResumeEvaluation = ParseResumeEvaluation(meetingDetails.AIScreeningResultJsonV2);
                if (reportModel.AIInterviewEvaluation != null)
                {
                    reportModel.AIInterviewEvaluation.AadharVerificationId = meetingDetails.AadharVerificationId;

                }
                if (meetingDetails.CandidateAnswerVideoIdAI > 0)
                {
                    reportModel.sentimentViewModel = await _aiinterviewerService.GetSentimentAnalysisById(meetingDetails.CandidateAnswerVideoIdAI);

                }

                // Get candidate details for resume screening link using InterviewId
                var candidate = await _irfRepository.GetCandidateIdAndIrfIdByMeetingId(meetingDetails.InterviewId);
                ViewBag.IRFId = meetingDetails.irfIntId;
                ViewBag.CandidateMasterId = candidate?.CandidateMasterId ?? 0;
                // Determine accessType based on profile - if Client and public access, set to "Public", otherwise empty
                string accessType = string.Empty;
                if (profileName == ProfileNames.Client)
                {
                    // Check if this is a public access scenario - you may need to adjust this logic based on your requirements
                    accessType = "Public";
                }
                ViewBag.AccessType = accessType;

                await SetProctoringEventCountForReportAsync(meetingDetails.InterviewId);

                // MCQ assessment section. Null unless the skill channel is linked to an MCQ channel and
                // opted in, which is what keys the whole MCQ block in the report view.
                reportModel.McqAssessment = await ResolveMcqAssessmentForReportAsync(meetingDetails, seId);

                return View(reportModel);
            }
            else
            {
                string candidatePrompt = AdaptiveReportGenerationPrompt;
                InterviewReportResult aiReport = null;
                if (redo)
                {
                    aiReport = await _api.GenerateReport(seId, candidatePrompt, isExit: isExit);
                    //if (aiReport != null)
                    //{
                    //    await _aiinterviewerService.SaveReportOfAIInterviewByAssistant(seId, JsonConvert.SerializeObject(aiReport), isExit);
                    //}
                }
                else
                {
                    var aiReportJson = await _aiinterviewerService.GetReportOfAIInterviewByAssistant(seId);
                    if (!string.IsNullOrEmpty(aiReportJson))
                    {
                        aiReport = JsonConvert.DeserializeObject<InterviewReportResult>(aiReportJson);
                    }
                    if (aiReport == null)
                    {
                        aiReport = await _api.GenerateReport(seId, candidatePrompt, isExit: isExit);
                        var list = new List<string>();
                        list.Add(aiReport?.Replies?.FirstOrDefault());
                        if (aiReport != null)
                        {
                            aiReport.Replies = list;

                        }
                        if (aiReport != null)
                        {
                            //var result = await _aiinterviewerService.SaveReportOfAIInterviewByAssistant(seId, JsonConvert.SerializeObject(aiReport), isExit);
                            if (isExit.HasValue && isExit.Value)
                            {
                                if (aiReport.IsReportSavedToDB)
                                {
                                    var exitModel = new AIInterviewExitModalViewModel
                                    {
                                        Title = "Interview Session Complete",
                                        Message = "Meeting closed successfully. Thank you for your time!",
                                        IconClass = "fa-check-circle",
                                        IconColor = "#28a745",
                                        ButtonText = "Close",
                                        ButtonClass = "btn-success",
                                        IsSuccess = true,
                                        CandidateName = meetingDetails.candidateName,
                                        SessionId = seId.ToString(),
                                        ExitTime = DateTime.Now,
                                        InterviewDate = meetingDetails.interviewDate,
                                        IsFromIndex = false // Add this flag to identify the source
                                    };
                                    return View("ExitModalPartial", exitModel); ;
                                }
                                else
                                {
                                    var exitModel = new AIInterviewExitModalViewModel
                                    {
                                        Title = "Error",
                                        Message = "Error while exiting the interview. Please contact support.",
                                        IconClass = "fa-exclamation-triangle",
                                        IconColor = "#dc3545",
                                        ButtonText = "Close",
                                        ButtonClass = "btn-danger",
                                        IsSuccess = false,
                                        CandidateName = meetingDetails.candidateName,
                                        SessionId = seId.ToString(),
                                        ExitTime = DateTime.Now
                                    };
                                    return View("ExitModalPartial", exitModel);
                                }
                            }
                        }
                    }
                }
                if (isExit.HasValue && isExit.Value)
                {
                    var exitModel = new AIInterviewExitModalViewModel
                    {
                        Title = "Interview Session Complete",
                        Message = "Meeting closed successfully. Thank you for your time!",
                        IconClass = "fa-check-circle",
                        IconColor = "#28a745",
                        ButtonText = "Close",
                        ButtonClass = "btn-success",
                        IsSuccess = true,
                        CandidateName = meetingDetails.candidateName,
                        SessionId = seId.ToString(),
                        ExitTime = DateTime.Now
                    };
                    return View("ExitModalPartial", exitModel);
                }

                AIInterviewReportWrapperClass reportModel = new AIInterviewReportWrapperClass();
                reportModel.videoQuestionDetails = await RefreshBotInterviewMediaSasUrlsAsync(
                    await _aiinterviewerService.GetBotInterviewVideoDetails(seId));
                //reportModel.chatMessageViewModel = chatMessageModel;

                reportModel.AIInterviewEvaluation = TryParseEvaluation(aiReport?.Replies?.FirstOrDefault()); //JsonConvert.DeserializeObject<AIInterviewEvaluation>(cleaned);
                EnrichQuestionRatingsWithActualQuestions(reportModel.AIInterviewEvaluation, reportModel.videoQuestionDetails);
                // Adaptive interviews return the report as ready-to-render HTML (not the structured
                // JSON that AIInterviewEvaluation is parsed from). When the reply can't be parsed into
                // the structured model, keep the HTML so the view can render it directly instead of
                // showing an empty "AI Analysis & Recommendations" section.
                if (reportModel.AIInterviewEvaluation == null)
                {
                    reportModel.RawAIReportHtml = aiReport?.Replies?.FirstOrDefault();
                }
                //reportModel.TotalQuestions = result.Replies.Count() - 1;
                ViewBag.IsAdaptive = true;
                List<TypingSpeedAssessment> typingAssessmentsAdaptive;
                try { typingAssessmentsAdaptive = await _aiinterviewerService.GetTypingSpeedAssessmentBySessionId(seId.ToString()) ?? new List<TypingSpeedAssessment>(); } catch { typingAssessmentsAdaptive = new List<TypingSpeedAssessment>(); }
                ViewBag.TypingSpeedAssessments = typingAssessmentsAdaptive;
                reportModel.TypingSpeedAssessments = typingAssessmentsAdaptive;
                reportModel.CodeProjectAssessments = await LoadCodeProjectAssessmentsAsync(seId);
                reportModel.CandidateEmail = meetingDetails.candidateEmail;
                reportModel.CandidateName = meetingDetails.candidateName;
                reportModel.resumeLink = meetingDetails.resumeLink;
                reportModel.InterviewDate = meetingDetails.interviewDate;
                reportModel.SessionId = seId.ToString();
                reportModel.InterviewId = meetingDetails.InterviewId;

                // Get candidate details for resume screening link using InterviewId
                var candidate = await _irfRepository.GetCandidateIdAndIrfIdByMeetingId(meetingDetails.InterviewId);
                ViewBag.IRFId = meetingDetails.irfIntId;
                ViewBag.CandidateMasterId = candidate?.CandidateMasterId ?? 0;
                // Determine accessType based on profile - if Client and public access, set to "Public", otherwise empty
                string accessType = string.Empty;
                if (profileName == ProfileNames.Client)
                {
                    // Check if this is a public access scenario - you may need to adjust this logic based on your requirements
                    accessType = "Public";
                }
                ViewBag.AccessType = accessType;

                await SetProctoringEventCountForReportAsync(meetingDetails.InterviewId);

                // MCQ assessment section. Null unless the skill channel is linked to an MCQ channel and
                // opted in, which is what keys the whole MCQ block in the report view.
                reportModel.McqAssessment = await ResolveMcqAssessmentForReportAsync(meetingDetails, seId);

                return View(reportModel);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProctoringSessionEvents(Guid seId, bool downloadPdf = false)
        {
            var meetingDetails = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(seId);
            if (meetingDetails == null)
                return NotFound();

            var events = await _proctoringSessionEventRepository.GetByInterviewIdOrderedAsync(meetingDetails.InterviewId);
            var fallbackScreenshots = await ProctoringAzureMediaLister.ListForInterviewAsync(
                _azureStorageHelper,
                _configuration,
                meetingDetails.InterviewId,
                "ScreenshotContainerName",
                "screenshot");
            var fallbackVideoChunks = await ProctoringAzureMediaLister.ListForInterviewAsync(
                _azureStorageHelper,
                _configuration,
                meetingDetails.InterviewId,
                "ScreenChunkContainerName",
                "screen_chunk");
            var model = new ProctoringSessionEventsResultViewModel
            {
                SessionId = seId,
                InterviewId = meetingDetails.InterviewId,
                CandidateName = meetingDetails.candidateName,
                CandidateEmail = meetingDetails.candidateEmail,
                Events = events,
                FallbackScreenshots = fallbackScreenshots,
                FallbackVideoChunks = fallbackVideoChunks,
                MediaDiagnostics = ProctoringMediaDiagnosticsHelper.Build(
                    meetingDetails,
                    events,
                    fallbackScreenshots,
                    fallbackVideoChunks),
            };
            ViewData["Title"] = "Proctoring session events";

            if (downloadPdf)
            {
                ViewBag.IsServerPdfDownload = true;
                var html = await RenderToStringAsync("ProctoringSessionEvents", model, ViewData);
                if (string.IsNullOrEmpty(html))
                {
                    return RedirectToAction("ProctoringSessionEvents", new { seId });
                }

                var siteRoot = $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase}";
                html = NormalizeHtmlForPdf(html, siteRoot);
                html = EnsurePdfBaseTag(html, siteRoot);
                var pdfBytes = GenerateProctoringEventsPdf(html);
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    return RedirectToAction("ProctoringSessionEvents", new { seId });
                }

                var fileName = $"ProctoringReport_{meetingDetails.InterviewId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }

            return View(model);
        }

        /// <summary>
        /// Sends a sample of the interview's proctoring screenshots to the configured OpenAI prompt and
        /// returns its JSON analysis (candidate eye gaze, suspicious apps).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AnalyzeProctoringScreenshots(Guid seId)
        {
            var meetingDetails = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(seId);
            if (meetingDetails == null)
                return NotFound();

            var result = await _proctoringScreenshotAnalysisService.AnalyzeAsync(
                meetingDetails.InterviewId,
                meetingDetails.candidateEmail,
                HttpContext.RequestAborted);

            if (!result.Ok)
                return StatusCode(500, new { ok = false, error = result.Error });

            // AnalysisJson is already validated JSON, so compose the payload directly to return it un-escaped.
            var payload = "{\"ok\":true,\"screenshotsAnalyzed\":" + result.ScreenshotsAnalyzed + ",\"analysis\":" + result.AnalysisJson + "}";
            return Content(payload, "application/json");
        }

        private async Task<string> RenderToStringAsync(string viewName, object model, ViewDataDictionary viewData = null)
        {
            try
            {
                var httpContext = _contextAccessor.HttpContext;
                var actionContext = new ActionContext(httpContext, httpContext.GetRouteData(), new ActionDescriptor());

                using (var sw = new StringWriter())
                {
                    var viewResult = _viewEngine.FindView(actionContext, viewName, false);
                    if (viewResult.View == null)
                    {
                        throw new ArgumentNullException($"The view '{viewName}' was not found.");
                    }

                    var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                    {
                        Model = model
                    };
                    if (viewData != null)
                    {
                        foreach (var key in viewData.Keys.ToList())
                        {
                            viewDictionary[key] = viewData[key];
                        }
                    }

                    var tempData = new TempDataDictionary(actionContext.HttpContext, _tempDataProvider);
                    var viewContext = new ViewContext(
                        actionContext,
                        viewResult.View,
                        viewDictionary,
                        tempData,
                        sw,
                        new HtmlHelperOptions()
                    );

                    await viewResult.View.RenderAsync(viewContext);
                    return sw.ToString();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        private static string NormalizeHtmlForPdf(string html, string siteRoot)
        {
            if (string.IsNullOrEmpty(html) || string.IsNullOrEmpty(siteRoot))
            {
                return html;
            }

            var root = siteRoot.TrimEnd('/');
            html = Regex.Replace(
                html,
                @"(?<attr>href|src|action)\s*=\s*(?<q>['""])/(?!/)(?<path>[^'""#?]*)(\k<q>)",
                m => $"{m.Groups["attr"].Value}={m.Groups["q"].Value}{root}/{m.Groups["path"].Value.TrimStart('/')}{m.Groups["q"].Value}",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            html = Regex.Replace(
                html,
                @"url\s*\(\s*(['""]?)(/[^'"")]+)\1\s*\)",
                m =>
                {
                    var path = m.Groups[2].Value.TrimStart('/');
                    return $"url('{root}/{path}')";
                },
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            return html;
        }

        private static string EnsurePdfBaseTag(string html, string siteRoot)
        {
            if (string.IsNullOrEmpty(html) || string.IsNullOrEmpty(siteRoot))
            {
                return html;
            }

            if (html.IndexOf("<base ", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return html;
            }

            var root = siteRoot.TrimEnd('/') + "/";
            var headIdx = html.IndexOf("<head", StringComparison.OrdinalIgnoreCase);
            if (headIdx < 0)
            {
                return html;
            }

            var gt = html.IndexOf('>', headIdx);
            if (gt < 0)
            {
                return html;
            }

            return html.Insert(gt + 1, "\n<base href=\"" + root + "\">\n");
        }

        private byte[] GenerateProctoringEventsPdf(string htmlContent)
        {
            const int pdfJsWaitMs = 12000;

            try
            {
                var globalSettings = new GlobalSettings
                {
                    ColorMode = DinkToPdf.ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = DinkToPdf.PaperKind.A4,
                    Margins = new MarginSettings { Top = 0, Bottom = 0, Left = 0, Right = 0 }
                };

                var objectSettings = new ObjectSettings
                {
                    PagesCount = true,
                    HtmlContent = htmlContent,
                    WebSettings =
                    {
                        DefaultEncoding = "utf-8",
                        LoadImages = true,
                        EnableJavascript = true,
                        PrintMediaType = false
                    },
                    LoadSettings = { JSDelay = pdfJsWaitMs },
                    HeaderSettings = { FontName = "Arial", FontSize = 8, Line = false },
                    FooterSettings = new FooterSettings
                    {
                        FontName = "Arial",
                        FontSize = 8,
                        Right = "Page [page] / [toPage]",
                        Line = false
                    }
                };

                var pdf = new HtmlToPdfDocument
                {
                    GlobalSettings = globalSettings,
                    Objects = { objectSettings }
                };

                return _converter.Convert(pdf);
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// Parses the resume screener's stored result. That JSON has no schema of its own —
        /// it is whatever the screener wrote — so a shape change upstream must not be able
        /// to take a whole report down. A record that will not parse yields null, and the
        /// report simply renders no Resume Evaluation section; the score and verdict still
        /// show in the summary because they are read from their own columns.
        /// </summary>
        private static CandidateResumeScreeningVM ParseResumeEvaluation(string screeningJson)
        {
            if (string.IsNullOrWhiteSpace(screeningJson))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<CandidateResumeScreeningVM>(screeningJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private async Task SetProctoringEventCountForReportAsync(int interviewId)
        {
            try
            {
                if (interviewId <= 0)
                {
                    ViewBag.ProctoringEventCount = 0;
                    return;
                }
                ViewBag.ProctoringEventCount = await _proctoringSessionEventRepository.GetCountByInterviewIdAsync(interviewId);
            }
            catch
            {
                ViewBag.ProctoringEventCount = 0;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GenerateAIReportPDFFile(Guid seId)
        {
            try
            {
                string profileName = HttpContext?.Session?.GetString("profileName")?.Trim();
                profileName = profileName?.Replace("\"", "");
                ViewBag.ProfileName = profileName;

                var meetingDetails = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(seId);
                InterviewReportResult aiReport = null;
                var aiReportJson = await _aiinterviewerService.GetReportOfAIInterviewByAssistant(seId);
                if (!string.IsNullOrEmpty(aiReportJson))
                {
                    aiReport = JsonConvert.DeserializeObject<InterviewReportResult>(aiReportJson);
                }
                if (aiReport == null)
                {
                    aiReport = await _api.GenerateReport(seId, string.Empty, meetingDetails?.AIInterviewType ?? 0);
                }

                var reportModel = new AIInterviewReportWrapperClass();
                reportModel.videoQuestionDetails = await RefreshBotInterviewMediaSasUrlsAsync(
                    await _aiinterviewerService.GetBotInterviewVideoDetails(seId));
                reportModel.AIInterviewEvaluation = TryParseEvaluation(aiReport?.Replies?.FirstOrDefault());
                EnrichQuestionRatingsWithActualQuestions(reportModel.AIInterviewEvaluation, reportModel.videoQuestionDetails);
                reportModel.CandidateEmail = meetingDetails?.candidateEmail;
                reportModel.CandidateName = meetingDetails?.candidateName;
                reportModel.InterviewDate = meetingDetails?.interviewDate ?? DateTime.MinValue;
                reportModel.SessionId = seId.ToString();
                reportModel.InterviewId = meetingDetails?.InterviewId ?? 0;
                reportModel.resumeLink = meetingDetails?.resumeLink;
                reportModel.MatchScorePercentage = meetingDetails?.MatchScorePercentage ?? 0;

                // Same three as Report: this action builds its own model, so the resume
                // evaluation has to be resolved here too or it would be on the page and
                // missing from the PDF — which is how it was before this change.
                reportModel.AIResumeScreeningVerdict = meetingDetails?.AIResumeScreeningVerdict;
                reportModel.AIResumeScreeningScore = meetingDetails?.AIResumeScreeningScore ?? 0;
                reportModel.ResumeEvaluation = ParseResumeEvaluation(meetingDetails?.AIScreeningResultJsonV2);

                // The Resume Screening link in the PDF is built from these, exactly as Report
                // does it. Without them the view cannot form the URL, so the link would simply
                // be absent from the exported document. Guarded: a lookup failure must not
                // cost the whole PDF, it just costs the link.
                try
                {
                    var pdfCandidate = meetingDetails != null
                        ? await _irfRepository.GetCandidateIdAndIrfIdByMeetingId(meetingDetails.InterviewId)
                        : null;
                    ViewBag.IRFId = meetingDetails?.irfIntId;
                    ViewBag.CandidateMasterId = pdfCandidate?.CandidateMasterId ?? 0;
                    ViewBag.AccessType = profileName == ProfileNames.Client ? "Public" : string.Empty;
                }
                catch (Exception)
                {
                    ViewBag.IRFId = null;
                    ViewBag.CandidateMasterId = 0;
                    ViewBag.AccessType = string.Empty;
                }

                List<TypingSpeedAssessment> typingAssessmentsPdf;
                try { typingAssessmentsPdf = await _aiinterviewerService.GetTypingSpeedAssessmentBySessionId(seId.ToString()) ?? new List<TypingSpeedAssessment>(); } catch { typingAssessmentsPdf = new List<TypingSpeedAssessment>(); }
                reportModel.TypingSpeedAssessments = typingAssessmentsPdf;
                reportModel.CodeProjectAssessments = await LoadCodeProjectAssessmentsAsync(seId);

                // This action builds its own model rather than reusing Report's, so the MCQ
                // section has to be resolved here too or it would be missing from the PDF while
                // being present on the page.
                reportModel.McqAssessment = await ResolveMcqAssessmentForReportAsync(meetingDetails, seId);

                ViewBag.IsAdaptive = meetingDetails?.AIInterviewType != (int)AIInterviewTypeEnum.Preset;

                return View(reportModel);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Report", new { seId });
            }
        }

        /// <summary>
        /// Opens question/answer video via a resolved, playable blob URL.
        /// Avoids sending users to Azure XML BlobNotFound/AuthenticationFailed pages.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PlayBotInterviewVideo(Guid seId, int qid, string kind = "answer")
        {
            try
            {
                var videos = await _aiinterviewerService.GetBotInterviewVideoDetails(seId);
                var item = videos?.FirstOrDefault(v => v.id == qid);
                if (item == null)
                {
                    return Content(
                        "<html><body style='font-family:Arial;padding:24px'><h3>Video not found</h3><p>No video record exists for this question.</p></body></html>",
                        "text/html");
                }

                var isQuestion = string.Equals(kind, "question", StringComparison.OrdinalIgnoreCase);
                string playableUrl = null;
                if (isQuestion)
                {
                    // Prefer current sample-question URL (often in /videos/) — same as Report page.
                    playableUrl = await ResolveBotInterviewPlayableUrlAsync(item.AIQuestionVideoLink);
                    if (string.IsNullOrWhiteSpace(playableUrl))
                        playableUrl = await ResolveBotInterviewPlayableUrlAsync(item.BotSynthesizedVideoLink);
                }
                else
                {
                    playableUrl = await ResolveBotInterviewPlayableUrlAsync(item.VideoLink);
                }

                if (string.IsNullOrWhiteSpace(playableUrl))
                {
                    var kindLabel = isQuestion ? "question" : "answer";
                    var missingHtml =
                        "<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>" +
                        "<title>" + char.ToUpper(kindLabel[0]) + kindLabel.Substring(1) + " video unavailable</title>" +
                        "<style>" +
                        "body{font-family:Segoe UI,Arial,sans-serif;padding:28px;background:#f7f9fb;color:#1a1a2e;margin:0;}" +
                        ".card{max-width:560px;background:#fff;border:1px solid #e2e8f0;border-radius:12px;padding:22px 24px;" +
                        "box-shadow:0 4px 16px rgba(15,23,42,.06);}" +
                        "h1{font-size:18px;margin:0 0 10px 0;}" +
                        "p{margin:0 0 10px 0;color:#475569;font-size:14px;line-height:1.5;}" +
                        "code{font-size:12px;background:#f1f5f9;padding:1px 6px;border-radius:4px;}" +
                        "a.back-pdf{display:inline-flex;align-items:center;gap:8px;margin-top:14px;padding:10px 16px;" +
                        "background:#fff;color:#0f172a;font-size:13px;font-weight:700;text-decoration:none;" +
                        "border:1px solid #e2e8f0;border-radius:8px;box-shadow:0 1px 3px rgba(15,23,42,.06);}" +
                        "a.back-pdf:hover{background:#f0fdf6;border-color:#4eda92;}" +
                        "a.back-pdf .chev{display:inline-flex;width:18px;height:18px;border-radius:50%;" +
                        "align-items:center;justify-content:center;background:#4eda92;color:#0b0b18;font-size:12px;font-weight:700;}" +
                        "</style></head><body><div class='card'>" +
                        "<h1>" + char.ToUpper(kindLabel[0]) + kindLabel.Substring(1) + " video unavailable</h1>" +
                        "<p>No playable " + kindLabel + " video file was found in Azure Blob Storage " +
                        "(checked <code>videos</code> and <code>botinterviewvideo</code>).</p>" +
                        "<p>The PDF link is only shown when a file resolves successfully. " +
                        "If you still reached this page, the stored URL may be missing, deleted, or invalid.</p>" +
                        "<a class='back-pdf' href='javascript:history.back()' title='Return to the AI interview PDF'>" +
                        "<span class='chev' aria-hidden='true'>&#8592;</span><span>Back to PDF</span></a>" +
                        "</div></body></html>";
                    return Content(missingHtml, "text/html");
                }

                var safeHref = System.Net.WebUtility.HtmlEncode(playableUrl);
                var label = isQuestion ? "question" : "answer";
                var titleLabel = char.ToUpper(label[0]) + label.Substring(1);
                var html =
                    "<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>" +
                    "<title>" + titleLabel + " video</title>" +
                    "<style>" +
                    "body{font-family:Segoe UI,Arial,sans-serif;padding:24px;background:#f7f9fb;color:#1a1a2e;margin:0;}" +
                    ".topbar{display:flex;align-items:center;justify-content:space-between;gap:16px;margin:0 0 18px 0;flex-wrap:wrap;}" +
                    "h1{font-size:20px;margin:0;font-weight:700;}" +
                    ".player{max-width:900px;background:#0b0b18;border-radius:10px;overflow:hidden;" +
                    "box-shadow:0 8px 24px rgba(0,0,0,.12);}" +
                    "video{display:block;width:100%;max-height:80vh;background:#000;}" +
                    "a.back-pdf{display:inline-flex;align-items:center;gap:8px;padding:10px 16px;" +
                    "background:#fff;color:#0f172a;font-size:13px;font-weight:700;text-decoration:none;" +
                    "border:1px solid #e2e8f0;border-radius:8px;box-shadow:0 1px 3px rgba(15,23,42,.06);" +
                    "transition:background .15s ease,border-color .15s ease,box-shadow .15s ease,transform .15s ease;}" +
                    "a.back-pdf:hover{background:#f0fdf6;border-color:#4eda92;box-shadow:0 2px 8px rgba(78,218,146,.25);}" +
                    "a.back-pdf:active{transform:translateY(1px);}" +
                    "a.back-pdf .chev{display:inline-flex;width:18px;height:18px;border-radius:50%;" +
                    "align-items:center;justify-content:center;background:#4eda92;color:#0b0b18;" +
                    "font-size:12px;line-height:1;font-weight:700;}" +
                    "a.back-pdf-bottom{margin-top:18px;}" +
                    "</style></head><body>" +
                    "<div class='topbar'>" +
                    "<h1>" + titleLabel + " video</h1>" +
                    "<a class='back-pdf' href='javascript:history.back()' title='Return to the AI interview PDF'>" +
                    "<span class='chev' aria-hidden='true'>&#8592;</span><span>Back to PDF</span></a>" +
                    "</div>" +
                    "<div class='player'>" +
                    "<video controls playsinline preload='metadata' src='" + safeHref + "'>" +
                    "Your browser does not support HTML5 video. " +
                    "<a href='" + safeHref + "' target='_blank' rel='noopener noreferrer'>Open video</a>" +
                    "</video></div>" +
                    "<a class='back-pdf back-pdf-bottom' href='javascript:history.back()' title='Return to the AI interview PDF'>" +
                    "<span class='chev' aria-hidden='true'>&#8592;</span><span>Back to PDF</span></a>" +
                    "</body></html>";
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Content(
                    "<html><body style='font-family:Arial;padding:24px'><h3>Unable to open video</h3><p>Please try again from the Report page.</p></body></html>",
                    "text/html");
            }
        }

        /// <summary>
        /// Media may live in private containers (videos for AI question clips, botinterviewvideo for
        /// candidate answers). DB often stores bare URLs or expired SAS. Resolve to an existing blob + fresh SAS.
        /// </summary>
        private async Task<List<BotInterviewQuestionVideos>> RefreshBotInterviewMediaSasUrlsAsync(List<BotInterviewQuestionVideos> videos)
        {
            if (videos == null || videos.Count == 0)
                return videos;

            foreach (var v in videos)
            {
                // Same source as Report: prefer SkillChannelSampleQuestions.AIQuestionVideoLink (/videos/...).
                var questionUrl = await ResolveBotInterviewPlayableUrlAsync(v.AIQuestionVideoLink);
                if (string.IsNullOrWhiteSpace(questionUrl))
                    questionUrl = await ResolveBotInterviewPlayableUrlAsync(v.BotSynthesizedVideoLink);
                v.BotSynthesizedVideoLink = questionUrl;

                v.VideoLink = await ResolveBotInterviewPlayableUrlAsync(v.VideoLink);
                v.BotAudioLink = await ResolveBotInterviewPlayableUrlAsync(v.BotAudioLink);
                v.CandidateAnswerAudioLink = await ResolveBotInterviewPlayableUrlAsync(v.CandidateAnswerAudioLink);
            }

            return videos;
        }

        private async Task<string> ResolveBotInterviewPlayableUrlAsync(string storedUrl)
        {
            if (string.IsNullOrWhiteSpace(storedUrl)
                || string.Equals(storedUrl, "no-video", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var isRisebirdBlob = storedUrl.IndexOf("risebirdportal.blob.core.windows.net", StringComparison.OrdinalIgnoreCase) >= 0
                || storedUrl.IndexOf(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase) >= 0;

            // Non-Azure links (e.g. temporary Synthesia) — leave as-is.
            if (!isRisebirdBlob)
                return storedUrl;

            if (!Uri.TryCreate(storedUrl.Split('?')[0], UriKind.Absolute, out var uri))
                return null;

            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
                return null;

            var urlContainer = Uri.UnescapeDataString(segments[0]);
            var blobName = Uri.UnescapeDataString(string.Join("/", segments.Skip(1)));
            if (string.IsNullOrWhiteSpace(blobName))
                return null;

            // Question AI clips are historically in "videos"; candidate answers in "botinterviewvideo".
            var containersToTry = new List<string>();
            if (!string.IsNullOrWhiteSpace(urlContainer))
                containersToTry.Add(urlContainer);
            foreach (var c in new[] { "videos", "botinterviewvideo" })
            {
                if (!containersToTry.Contains(c, StringComparer.OrdinalIgnoreCase))
                    containersToTry.Add(c);
            }

            var nameCandidates = new List<string> { blobName };
            if (!Path.HasExtension(blobName))
            {
                nameCandidates.Add(blobName + ".mp4");
                nameCandidates.Add(blobName + ".webm");
            }
            else
            {
                var withoutExt = Path.GetFileNameWithoutExtension(blobName);
                nameCandidates.Add(withoutExt);
                nameCandidates.Add(Path.ChangeExtension(blobName, ".mp4"));
                nameCandidates.Add(Path.ChangeExtension(blobName, ".webm"));
                nameCandidates.Add(withoutExt + ".mp4");
                nameCandidates.Add(withoutExt + ".webm");
            }

            foreach (var containerName in containersToTry)
            {
                string sasToken = null;
                try
                {
                    sasToken = _sasUrlHelper.CreateSasUrl(containerName);
                }
                catch
                {
                    // try next container
                }

                try
                {
                    var container = _azureStorageHelper.GetCloudBlobContainer(containerName);
                    foreach (var candidate in nameCandidates.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        var blob = container.GetBlockBlobReference(candidate);
                        if (blob == null || !await blob.ExistsAsync())
                            continue;

                        var baseUrl = blob.Uri.ToString().Split('?')[0];
                        return string.IsNullOrWhiteSpace(sasToken) ? baseUrl : baseUrl + "?" + sasToken;
                    }
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }

            return null;
        }

        private string CleanMessage(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            return input
                .Replace(AdaptiveInterviewCompletionMarker, "", StringComparison.OrdinalIgnoreCase) // Strip the internal completion marker so it is never shown/spoken to the candidate
                .Replace("#", "")        // Remove Markdown headers
                .Replace("**", "")       // Remove bold formatting if present
                .Trim();                 // Trim whitespace
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTimeLeft([FromBody] TimeLeftUpdateVM model)
        {
            try
            {
                await _aiinterviewerService.UpdateTimeLeft(model.SessionId, model.TimeLeftInSeconds, model.interviewId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest();
                throw;
            }
        }

        #region Code project questions

        /// <summary>
        /// Look-ahead provisioning. A VM takes 30-60 s to boot, which at fifteen minutes is a real slice
        /// of the slot, so while the candidate is on question N-1 the machine for question N is started.
        /// The current question is also prepared, covering a code project that is the first question
        /// or a resumed session. Both are queued: nothing here waits on Azure.
        /// </summary>
        private void QueueCodeProjectLookAhead(string sessionId, int interviewId, int irfId, ChatMessageViewModel model)
        {
            if (model == null || model.CurrentQuestionId <= 0) return;
            if (model.IsCodeProjectQuestion)
                _codeProjectQueue.Enqueue(new Risebird.Web.Services.CodeProjectBackgroundQueue.PrepareEnvironment(sessionId, model.CurrentQuestionId));
            _codeProjectQueue.Enqueue(new Risebird.Web.Services.CodeProjectBackgroundQueue.PrepareNextEnvironment(sessionId, interviewId, irfId, model.CurrentQuestionId));
        }

        /// <summary>
        /// Candidate endpoints act only on the code project question currently on screen for a live
        /// session, so a session id cannot be used to probe or start machines for other questions.
        /// </summary>
        private async Task<CodeProjectDetails> AuthorizeCodeProjectAsync(Guid sid, int questionId)
        {
            if (sid == Guid.Empty || questionId <= 0) return null;
            var meeting = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(sid);
            if (meeting == null) return null;

            var transcript = await _aiinterviewerService.GetAIInterviewTranscriptBySessionId(sid);
            var current = transcript?.FirstOrDefault(t => string.Equals(t.Role, "assistant", StringComparison.OrdinalIgnoreCase));
            if (current == null || current.DeliveryHeadQuestionId != questionId) return null;

            var details = await _codeProjectQuestionService.GetDetails(questionId);
            if (details == null || details.IRFId != meeting.irfIntId
                || details.ValidationStatus != CodeProjectValidationStatus.Passed)
                return null;
            return details;
        }

        /// <summary>What the candidate sees in "Your task". Built from the sanitized payload — never tier 3.</summary>
        private static object BuildCodeProjectTask(CodeProjectDetails details)
        {
            var requirements = new List<string>();
            var editable = new List<string>();
            var files = new List<string>();
            if (!string.IsNullOrWhiteSpace(details.GeneratedPayload))
            {
                try
                {
                    var p = Newtonsoft.Json.Linq.JObject.Parse(details.GeneratedPayload);
                    requirements = (p["requirements"] as Newtonsoft.Json.Linq.JArray)?.Select(r => r["text"]?.ToString()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? requirements;
                    editable = p["editable_files"]?.ToObject<List<string>>() ?? editable;
                    files = p["project_files"]?.ToObject<List<string>>() ?? files;
                }
                catch (Newtonsoft.Json.JsonException) { }
            }
            return new
            {
                questionText = details.Comment,
                skillName = details.SkillName,
                stackImageCode = details.StackImageCode,
                framework = details.Framework,
                language = details.Language,
                timeLimitMinutes = details.CodingTimeLimitInMinutes ?? CodeProjectQuestionHelper.DefaultTimeLimitMinutes,
                requirements,
                editableFiles = editable,
                files,
                buildCommand = details.BuildCommand,
                testCommand = details.TestCommand,
                runCommand = details.RunCommand
            };
        }

        private static object BuildCodeProjectEnvironmentView(CodeProjectEnvironment env, CodeProjectDetails details,
            CodeProjectEnvironmentStatus connection = null)
        {
            var limit = details.CodingTimeLimitInMinutes ?? CodeProjectQuestionHelper.DefaultTimeLimitMinutes;
            int? secondsRemaining = null;
            if (env?.HandedOverAtUtc != null)
                secondsRemaining = Math.Max(0, (int)(env.HandedOverAtUtc.Value.AddMinutes(limit) - DateTime.UtcNow).TotalSeconds);
            var connected = env?.State == CodeProjectEnvironmentState.HandedOver;
            return new
            {
                state = env?.State ?? CodeProjectEnvironmentState.Requested,
                message = env?.Message,
                // The link and sign-in are only released on handover, which is also when the clock starts.
                connectUrl = connected ? (connection?.ConnectUrl ?? env.ConnectUrl) : null,
                username = connected ? connection?.Username : null,
                password = connected ? connection?.Password : null,
                secondsRemaining,
                timeLimitMinutes = limit
            };
        }

        /// <summary>Task text plus environment state. Polled while the machine comes up.</summary>
        [HttpGet]
        public async Task<JsonResult> CodeProjectEnvironment(Guid sid, int questionId)
        {
            try
            {
                var details = await AuthorizeCodeProjectAsync(sid, questionId);
                if (details == null)
                    return Json(new { success = false, message = "Your interview session could not be verified. Please reload the page." });
                var env = await _codeProjectQuestionService.RefreshEnvironmentAsync(sid.ToString(), questionId);
                var connection = await _codeProjectQuestionService.GetConnectionAsync(env);
                return Json(new { success = true, task = BuildCodeProjectTask(details), environment = BuildCodeProjectEnvironmentView(env, details, connection) });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not check your environment. Retrying…" });
            }
        }

        /// <summary>Report data. A failure here must never take the rest of the report down.</summary>
        private async Task<List<CodeProjectAssessment>> LoadCodeProjectAssessmentsAsync(Guid seId)
        {
            try { return await _codeProjectQuestionService.GetAssessmentsForSession(seId.ToString()) ?? new List<CodeProjectAssessment>(); }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<CodeProjectAssessment>();
            }
        }

        /// <summary>"Launch environment". Hands the machine over once it is ready; the clock starts then.</summary>
        [HttpPost]
        public async Task<JsonResult> CodeProjectLaunch(Guid sid, int questionId)
        {
            try
            {
                var details = await AuthorizeCodeProjectAsync(sid, questionId);
                if (details == null)
                    return Json(new { success = false, message = "Your interview session could not be verified. Please reload the page." });
                var env = await _codeProjectQuestionService.HandOverAsync(sid.ToString(), questionId);
                var connection = await _codeProjectQuestionService.GetConnectionAsync(env);
                return Json(new { success = true, environment = BuildCodeProjectEnvironmentView(env, details, connection) });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not open your environment. Retrying…" });
            }
        }

        /// <summary>
        /// Grades the candidate's last pushed commit. The candidate is not shown the hidden-test
        /// results; the page then posts the answer form to move to the next question.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> SubmitCodeProject(Guid sid, int questionId)
        {
            try
            {
                var details = await AuthorizeCodeProjectAsync(sid, questionId);
                if (details == null)
                    return Json(new { success = false, message = "Your interview session could not be verified. Please reload the page." });
                var meeting = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(sid);
                var (assessment, grade) = await _codeProjectQuestionService.SubmitAsync(sid.ToString(), questionId, meeting?.candidateName);
                return Json(new { success = assessment != null, graded = string.IsNullOrEmpty(grade?.Error) });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Your work is saved on the machine. Submitting again…" });
            }
        }

        #endregion

        /// <summary>
        /// Compiles and runs the code a candidate has written for a coding question.
        /// Read-only with respect to the interview: running code never advances the interview
        /// or records an answer, it only returns console output back to the IDE.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> RunCode([FromBody] CodeRunRequestVM model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Code))
                {
                    return Json(new { success = false, output = "Write some code before running it." });
                }

                if (!_codeExecutionService.IsEnabled)
                {
                    return Json(new { success = false, output = "Running code is not available in this environment. You can still write and submit your answer." });
                }

                // Only an in-flight interview session may execute code. This is the same lookup the
                // interview page itself is built from; GetAIInterviewSpecificDetails is not usable
                // here because it inner joins InterviewSessions, a row Preset interviews never create.
                var meeting = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(model.SessionId);
                if (meeting == null)
                {
                    return Json(new { success = false, output = "Your interview session could not be verified. Please reload the page." });
                }

                var result = await _codeExecutionService.ExecuteAsync(new CodeExecutionRequest
                {
                    LanguageId = model.Language,
                    SourceCode = model.Code,
                    Stdin = model.Stdin
                });

                return Json(new
                {
                    success = result.Success,
                    status = result.Status,
                    output = result.CombinedOutput,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, output = "The code runner is temporarily unavailable. Your code is safe — you can still submit it." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetAIInterviewUrl(Guid sid)
        {
            try
            {
                // Construct the AI interview URL
                string aiInterviewUrl = "https://" + Request.Host.ToString() + "/AIInterview/Index?sid=" + sid;
                return Json(aiInterviewUrl);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json("Failed");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAIQuestionVideo(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                {
                    return Json(new { success = false, message = "Unauthorized." });
                }

                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Trim('"');
                if (!DeliveryHeadInstructionViewMode.IsOperationalProfile(profileName))
                {
                    return Json(new { success = false, message = "You do not have permission to generate AI question videos." });
                }

                if (id <= 0)
                {
                    return Json(new { success = false, message = "Invalid question id." });
                }

                var questionDetails = await _iRFService.GetDeliveryHeadSampleQuestionById(id);
                if (questionDetails == null)
                {
                    return Json(new { success = false, message = "Question not found." });
                }

                if (string.IsNullOrWhiteSpace(questionDetails.Comment))
                {
                    return Json(new { success = false, message = "Question text is required before generating a video." });
                }

                // Prefer the gist whenever it exists — that is what the avatar should speak.
                // Coding questions always need a gist because source code cannot be narrated.
                var narrationText = !string.IsNullOrWhiteSpace(questionDetails.QuestionGist)
                    ? questionDetails.QuestionGist
                    : questionDetails.Comment;

                if ((questionDetails.IsCodingQuestion || questionDetails.IsCodeProjectQuestion) && string.IsNullOrWhiteSpace(questionDetails.QuestionGist))
                {
                    return Json(new { success = false, message = "Add a question gist in plain language before generating a video. The avatar reads the gist for coding questions, not the code." });
                }

                // A listening comprehension question needs its audio clip in place, and needs a
                // gist for the avatar to speak: the avatar sets up the exercise, so it must not
                // read out the question text, which describes the audio the candidate is
                // supposed to be listening for.
                if (questionDetails.IsListeningComprehension)
                {
                    if (string.IsNullOrWhiteSpace(questionDetails.ListeningAudioUrl))
                    {
                        return Json(new { success = false, message = "Upload the listening audio file before generating a video for a listening comprehension question." });
                    }

                    if (string.IsNullOrWhiteSpace(questionDetails.QuestionGist))
                    {
                        return Json(new { success = false, message = "Add a question gist in plain language before generating a video. For listening comprehension questions the avatar reads the gist, which introduces the exercise without revealing the audio content." });
                    }
                }

                var irfVm = await _iRFService.GetEditIRF(questionDetails.IRFId, 0, false);
                if (irfVm == null || !DmSynthesiaVideoViewMode.UsesSynthesiaVideos(irfVm.IsAIInterview, irfVm.AIInterviewType))
                {
                    return Json(new { success = false, message = "AI question videos are only available for Preset AI interview channels." });
                }

                SynthesiaCreateResult createResult;
                try
                {
                    createResult = await _synthesiaTextToVideoService.CreateVideoWithDetailsAsync(narrationText, SynthesiaAIAvatarTemplates.Reema);
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    return Json(new { success = false, message = "Could not reach Synthesia API. Check configuration and try again." });
                }

                if (createResult == null || !createResult.IsSuccess)
                {
                    // Repeat Synthesia's own reason. "Synthesia did not return a video id" told the
                    // user nothing they could act on and hid the refusal behind a generic message.
                    var reason = createResult?.ErrorMessage;
                    return Json(new
                    {
                        success = false,
                        message = string.IsNullOrWhiteSpace(reason)
                            ? "Synthesia did not start the render and gave no reason."
                            : "Could not start the render. " + reason,
                        narrationLength = narrationText.Length
                    });
                }

                var videoId = createResult.VideoId;

                // Synthesia takes minutes to render, and how many is not predictable. Waiting for it
                // inside this request meant the browser gave up — or the request hit its own limit —
                // and the user was told the generation had failed while it was in fact still running.
                // So the render is only kicked off here; the caller polls GetAIQuestionVideoStatus
                // with this video id and the video is saved by whichever poll finds it finished.
                return Json(new
                {
                    success = true,
                    pending = true,
                    videoId = videoId,
                    message = "Generating the AI video. This usually takes a few minutes."
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Video generation failed: " + ex.Message });
            }
        }

        /// <summary>
        /// One non-blocking check on a render started by CreateAIQuestionVideo. Returns
        /// ready:false while Synthesia is still working, and on the poll that finds the render
        /// finished it also downloads the video, stores it and links it to the question.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetAIQuestionVideoStatus(int id, string videoId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") == null || HttpContext.Session.GetString("profileName") == null)
                {
                    return Json(new { success = false, message = "Unauthorized." });
                }

                var profileName = HttpContext.Session.GetString("profileName")?.Trim().Trim('"');
                if (!DeliveryHeadInstructionViewMode.IsOperationalProfile(profileName))
                {
                    return Json(new { success = false, message = "You do not have permission to generate AI question videos." });
                }

                if (id <= 0 || string.IsNullOrWhiteSpace(videoId))
                {
                    return Json(new { success = false, message = "Invalid video request." });
                }

                var questionDetails = await _iRFService.GetDeliveryHeadSampleQuestionById(id);
                if (questionDetails == null)
                {
                    return Json(new { success = false, message = "Question not found." });
                }

                var state = await _synthesiaTextToVideoService.GetVideoStatusAsync(videoId);

                if (state == null || !state.IsComplete)
                {
                    // The finished download link is the only signal trusted to end the wait, which is
                    // what the original code did. Synthesia's status strings are reported back for
                    // diagnostics but never end the wait on their own: a freshly created render can
                    // read oddly for the first few seconds, and failing on that put the button back
                    // to declaring failure almost immediately. The caller's overall time cap is what
                    // stops a render that really is dead.
                    return Json(new
                    {
                        success = true,
                        pending = true,
                        ready = false,
                        videoId = videoId,
                        status = state?.Status
                    });
                }

                string uploadedVideoUrl = string.Empty;
                using (var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
                using (var myStream = await httpClient.GetStreamAsync(state.DownloadUrl))
                {
                    var fileName = $"aiVideo_{id}_{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}";
                    try
                    {
                        // Match Report / historical AI question videos: container "videos"
                        uploadedVideoUrl = await _azureStorageHelper.InsertBlobCloudCompare(fileName, "videos", null, myStream, contentType: "video/mp4");
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        return Json(new { success = false, message = "Video was generated but could not be saved to storage." });
                    }
                }

                if (string.IsNullOrWhiteSpace(uploadedVideoUrl))
                {
                    return Json(new { success = false, message = "Video upload failed." });
                }

                var saved = await _iRFService.SaveSampleQuestionAIVideoLink(id, uploadedVideoUrl);
                if (!saved)
                {
                    return Json(new { success = false, message = "Video was created but could not be linked to this question." });
                }

                var narrationText = !string.IsNullOrWhiteSpace(questionDetails.QuestionGist)
                    ? questionDetails.QuestionGist
                    : (questionDetails.Comment ?? string.Empty);
                var preview = narrationText.Length > 25
                    ? narrationText.Substring(0, 25) + "…"
                    : narrationText;

                return Json(new
                {
                    success = true,
                    pending = false,
                    ready = true,
                    videoUrl = uploadedVideoUrl,
                    message = $"Successfully created an AI video for the question starting with '{preview}'."
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not save the generated video: " + ex.Message });
            }
        }

        private void EnrichQuestionRatingsWithActualQuestions(AIInterviewEvaluation evaluation, List<BotInterviewQuestionVideos> videoDetails)
        {
            if (evaluation?.QuestionRatings == null || videoDetails == null || !videoDetails.Any())
                return;

            for (int i = 0; i < evaluation.QuestionRatings.Count; i++)
            {
                var qr = evaluation.QuestionRatings[i];
                if (string.IsNullOrWhiteSpace(qr.Question) || System.Text.RegularExpressions.Regex.IsMatch(qr.Question, @"^Q\d+$"))
                {
                    int index = -1;
                    var match = System.Text.RegularExpressions.Regex.Match(qr.Question ?? "", @"\d+");
                    if (match.Success)
                        index = int.Parse(match.Value) - 1;
                    else
                        index = i;

                    if (index >= 0 && index < videoDetails.Count && !string.IsNullOrWhiteSpace(videoDetails[index].question))
                    {
                        qr.Question = videoDetails[index].question;
                    }
                }
            }
        }

        private AIInterviewEvaluation TryParseEvaluation(string raw)
            => AIInterviewEvaluation.TryParse(raw);

        [HttpPost]
        public async Task<IActionResult> SaveCandidateAnswerById(int questionId, string candidateAnswer)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var email = HttpContext.Session.GetString("email")?.Trim();
                // Attribute the edit to the person, not the role (this used to overwrite email with profileName).
                email = email?.Replace("\"", "");
                if (profileName == "Admin" || profileName == ProfileNames.QualityController)
                {
                    var model = await _aiinterviewerService.UpdateCandidateAnswerById(questionId, candidateAnswer, email);
                    if (model)
                    {
                        return Json(true);
                    }
                    else
                    {
                        return Json(false);
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


        [HttpPost]
        public async Task<IActionResult> FlagQuestionForSentimentAnalysis(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "SeniorHR" || profileName == "SeniorInterviewerManagement")
                    {
                        var questionDetails = await _iRFService.GetDeliveryHeadSampleQuestionById(id);


                        //save the video link in our db against the above id

                        var result = await _iRFService.FlagQuestionForSentimentAnalysis(id);
                        var comment = questionDetails.Comment ?? string.Empty;
                        var snippet = comment.Length > 25
                            ? comment.Substring(0, 25) + "..."
                            : comment;
                        if (result)
                        {

                            return Json(new { issuccessful = true, message = $"Successfully flagged the question starting with '{snippet}' for sentiment analysis." });//, aiResponse = response.Data?.FirstOrDefault()?.Content?.Where(x=> x.Text.Value.Contains("Evaluate the resume")).FirstOrDefault()?.Text.Value

                            //return Json($"Successfully flagged the question starting with '{questionDetails.Comment.Substring(0, 25)}' for sentiment analysis.");
                        }
                        else
                        {
                            return Json(new { issuccessful = false, message = $"Failed to flag the question starting with '{snippet}' for sentiment analysis." });//, aiResponse = response.Data?.FirstOrDefault()?.Content?.Where(x=> x.Text.Value.Contains("Evaluate the resume")).FirstOrDefault()?.Text.Value

                            //return Json($"Failed to flag the question starting with '{questionDetails.Comment.Substring(0, 25)}' for sentiment analysis.");
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
                return Json(new { issuccessful = false, message = $"Failed." });

                //return Json(false);

            }
        }

    }
}
