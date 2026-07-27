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
        private readonly IProctoringTokenService _proctoringTokenService;
        private readonly IProctoringSessionEventRepository _proctoringSessionEventRepository;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IConverter _converter;

        public AIInterviewController(IConfiguration configuration, IAzureStorageHelper azureStorageHelper,
            IApplicationErrorLogger applicationErrorLogger, IInterviewApiService api,
            IOpenAISpeechToTextService speechToTextService, IFileMimeTypeHelper fileMimeTypeHelper,
            ISASUrlHelper sasUrlHelper, IAIInterviewerService aiinterviewerService,
            ISynthesiaTextToVideoService synthesiaTextToVideoService, IPdfReaderHelper pdfReaderHelper,
            IIRFService iRFService, IAWSRekognitionService aWSRekognitionService, ICandidateService candidateService,
            IIRFRepository irfRepository, IOpenAIInterviewerAssistantHelper openAIAssistantHelper,
            IProctoringTokenService proctoringTokenService, IProctoringSessionEventRepository proctoringSessionEventRepository,
            ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider,
            IHttpContextAccessor contextAccessor, IConverter converter)
        {
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
            _proctoringTokenService = proctoringTokenService;
            _proctoringSessionEventRepository = proctoringSessionEventRepository;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _contextAccessor = contextAccessor;
            _converter = converter;
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
                    await ApplyAiProctoringViewBagAsync(meetingDetails, sid);
                    if (meetingDetails.AIInterviewType == (int)AIInterviewTypeEnum.Preset)
                    {
                        var transcript = await _aiinterviewerService.GetAIInterviewTranscriptBySessionId(sid);

                        //not started yet
                        if (transcript == null || transcript.Count() == 0)
                        {
                            var sampleQuestion = await _aiinterviewerService.GetRandomQuestionForSession(meetingDetails.InterviewId, meetingDetails.irfIntId);

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

                                        var sampleQuestion = await _aiinterviewerService.GetRandomQuestionForSession(meetingDetails.InterviewId, meetingDetails.irfIntId);

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

                                            }
                                        }
                                        else
                                        {
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

                HydrateModelFromMeeting(model, meetingDetails);

                try
                {
                    await ApplyAiProctoringViewBagAsync(meetingDetails, model.SessionId);
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

                    // For typing speed or listening comprehension assessments, skip audio transcription
                    if (model.IsTypingSpeedQuestion || model.IsListeningComprehensionQuestion)
                    {
                        userMessage = "Typed Out";
                    }
                    else if (!model.showChatBox)
                    {
                        if (model.AudioFile == null || model.AudioFile.Length == 0)
                        {
                            return View("ExitModalPartial", CreateAnswerSubmitErrorView(
                                model.SessionId,
                                "Missing audio",
                                "Your recording was not attached. Please record again and wait until processing finishes before the page reloads."));
                        }

                        if (model.AudioFile.Length < 500)
                        {
                            return View("ExitModalPartial", CreateAnswerSubmitErrorView(
                                model.SessionId,
                                "Recording too short",
                                "Your recording was too short to transcribe. Please speak for at least a few seconds and try again."));
                        }

                        if (model.AudioFile != null && model.AudioFile.Length > 0)
                        {
                            try
                            {
                                using var stream = model.AudioFile.OpenReadStream();
                                userMessage = await _speechToTextService.TranscribeAsync(stream, model.AudioFile.FileName, model.isMultiLingual);
                            }
                            catch (Exception sttEx)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, sttEx);
                                return View("ExitModalPartial", CreateAnswerSubmitErrorView(
                                    model.SessionId,
                                    "Error with audio",
                                    "Could not transcribe your recording. Please check your microphone and try again."));
                            }

                            if (!IsTranscriptionValid(userMessage, model.isMultiLingual))//!model.isMultiLingual &&
                            {
                                // Rewind the stream to re-run transcription
                                var exitModel = new AIInterviewExitModalViewModel
                                {
                                    Title = "Error with audio!",
                                    Message = "Please ensure your audio equipment is functioning correctly, and refrain from speaking in a whisper. Maintain an audible volume throughout the session.",
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
                        }
                    }
                    else
                    {
                        if (model.AudioFile != null && model.AudioFile.Length > 0)
                        {
                            if (model.AudioFile.Length < 500)
                            {
                                return View("ExitModalPartial", CreateAnswerSubmitErrorView(
                                    model.SessionId,
                                    "Recording too short",
                                    "Your recording was too short to transcribe. Please speak for at least a few seconds and try again."));
                            }

                            try
                            {
                                using var stream = model.AudioFile.OpenReadStream();
                                tranScribedResponse = await _speechToTextService.TranscribeAsync(stream, model.AudioFile.FileName, model.isMultiLingual);
                            }
                            catch (Exception sttEx)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, sttEx);
                                return View("ExitModalPartial", CreateAnswerSubmitErrorView(
                                    model.SessionId,
                                    "Error with audio",
                                    "Could not transcribe your recording. Please check your microphone and try again."));
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
                                    // Rewind the stream to re-run transcription
                                    var exitModel = new AIInterviewExitModalViewModel
                                    {
                                        Title = "Error with audio!",
                                        Message = "Please ensure your audio equipment is functioning correctly, and refrain from speaking in a whisper. Maintain an audible volume throughout the session.",
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
                            }
                        }
                    }


                    var transcript = await _aiinterviewerService.GetAIInterviewTranscriptBySessionId(model.SessionId)
                        ?? Enumerable.Empty<TranscriptConv>();
                    var assistantQuestionCount = CountAssistantQuestions(transcript);
                    if (assistantQuestionCount == 35)
                    {
                        var payloadError = ValidatePresetAnswerPayloadBeforeSave(model, userMessage);
                        if (payloadError != null) return payloadError;

                        bool iscandidateVideoSavedTodb = await UploadBotInterviewVideo(model, question, userMessage);
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

                        var payloadError = ValidatePresetAnswerPayloadBeforeSave(model, userMessage);
                        if (payloadError != null) return payloadError;

                        bool iscandidateVideoSavedTodb = await UploadBotInterviewVideo(model, question, userMessage);
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

                                // Create OpenAI thread and run
                                string threadId = await _openAIAssistantHelper.CreateThreadAsync();
                                await _openAIAssistantHelper.AddMessageAsync(threadId, "user", assistantMessage);
                                string runId = await _openAIAssistantHelper.CreateRunAsync(threadId, OpenAIInterviewerAssistants.TypingAssessmentAssistant);

                                // Wait for completion
                                string runStatus = await _openAIAssistantHelper.WaitForRunAsync(threadId, runId);

                                if (runStatus == "completed")
                                {
                                    // Get response
                                    var messages = await _openAIAssistantHelper.GetMessagesAsync(threadId);
                                    string openAIResponse = messages.FirstOrDefault() ?? "{}";

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
                                else
                                {
                                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Warning,
                                        new Exception($"OpenAI typing assessment run status: {runStatus}. ThreadId: {threadId}, RunId: {runId}"));
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
                                var listeningAssessment = new ListeningComprehensionAssessment
                                {
                                    SessionId = model.SessionId.ToString(),
                                    QuestionId = model.CurrentQuestionId,
                                    AudioUrl = model.ListeningAudioUrl,
                                    AudioTranscription = model.ListeningAudioTranscription,
                                    CandidateSummary = model.ListeningSummary,
                                    TimeStarted = TryParseInterviewDateTime(model.ListeningStartTime) ?? DateTime.Now,
                                    TimeCompleted = TryParseInterviewDateTime(model.ListeningEndTime) ?? DateTime.Now,
                                    TimeTakenInSeconds = model.ListeningTimeTaken,
                                    CreatedBy = model.candidateName
                                };
                                await _aiinterviewerService.SaveListeningComprehensionAssessment(listeningAssessment);
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                                // Continue even if listening assessment save fails - don't block the interview
                            }
                        }

                        await _aiinterviewerService.LogTranscript(model.SessionId, "user", userMessage);

                        var sampleQuestion = await _aiinterviewerService.GetRandomQuestionForSession(model.InterviewId, model.IRFId);


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

                            if (!string.IsNullOrEmpty(model.CurrentQuestion))//&& string.IsNullOrEmpty(model.AIInterviewerAudioLink)
                            {
                                model.SynthesizedVideoUrl = sampleQuestion.AIQuestionVideoLink;
                                model.CurrentQuestion = CleanMessage(sampleQuestion.Message);
                                model.CurrentQuestionId = sampleQuestion.id;
                                model.IsTypingSpeedQuestion = sampleQuestion.IsTypingSpeedAssessment;
                                model.IsListeningComprehensionQuestion = sampleQuestion.IsListeningComprehension;
                                model.ListeningAudioUrl = sampleQuestion.ListeningAudioUrl;
                                model.ListeningAudioTranscription = sampleQuestion.ListeningAudioTranscription;

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

                            }

                            model.Message = "";

                            await RefreshModelMeetingFieldsAsync(model);
                            return View(model);
                        }
                        else
                        {

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
                    if (model.AudioFile != null && model.AudioFile.Length > 0)
                    {
                        using var stream = model.AudioFile.OpenReadStream();
                        userMessage = await _speechToTextService.TranscribeAsync(stream, model.AudioFile.FileName, model.isMultiLingual);
                    }

                    if (!IsTranscriptionValid(userMessage, model.isMultiLingual))//!model.isMultiLingual && 
                    {
                        // Rewind the stream to re-run transcription
                        var exitModel = new AIInterviewExitModalViewModel
                        {
                            Title = "Error with audio!",
                            Message = "Please ensure your audio equipment is functioning correctly, and refrain from speaking in a whisper. Maintain an audible volume throughout the session.",
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

                    var transcript = await _aiinterviewerService.GetAIInterviewTranscriptBySessionId(model.SessionId)
                        ?? Enumerable.Empty<TranscriptConv>();
                    var assistantQuestionCount = CountAssistantQuestions(transcript);
                    if (assistantQuestionCount == 35)
                    {
                        var payloadError = ValidatePresetAnswerPayloadBeforeSave(model, userMessage);
                        if (payloadError != null) return payloadError;

                        bool iscandidateVideoSavedTodb = await UploadBotInterviewVideo(model, question, userMessage);
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
                        _api.GenerateReport(model.SessionId, string.Empty, (int)AIInterviewTypeEnum.Adaptive);


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
                        bool iscandidateVideoSavedTodb = await UploadBotInterviewVideo(model, question, userMessage);
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

                        model.ChatLog.Add(new ChatLine { Role = "User", Message = userMessage });

                        if (replies != null)
                        {
                            foreach (var reply in replies)
                            {
                                model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(reply) });
                            }
                        }

                        var latestReply = CleanMessage(replies?.FirstOrDefault());

                        //current question changes to the latest reply by the assistant and is set only after the earlier question-answer response is saved using UploadBotInterviewVideo()
                        model.CurrentQuestion = latestReply;

                        if (!string.IsNullOrEmpty(model.CurrentQuestion))//&& string.IsNullOrEmpty(model.AIInterviewerAudioLink)
                        {
                            var audioBytes = await _speechToTextService.ConvertTextToSpeechAsync(model.CurrentQuestion);
                            var base64Audio = Convert.ToBase64String(audioBytes);
                            ViewData["AssistantAudioBase64"] = base64Audio;
                            model.AIInterviewerAudioLink = await UploadAIInterviewerAudio(audioBytes, model.SessionId);

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
            model.TimeLeftInSeconds = meeting.TimeLeftInSeconds;
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

        private static DateTime? TryParseInterviewDateTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return DateTime.TryParse(value, out var parsed) ? parsed : null;
        }

        private IActionResult ValidatePresetAnswerPayloadBeforeSave(ChatMessageViewModel model, string userMessage)
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
                && !model.IsListeningComprehensionQuestion)
            {
                return View("ExitModalPartial", CreateAnswerSubmitErrorView(
                    model.SessionId,
                    "Missing answer",
                    "Your spoken answer was not captured. Please record again and wait for processing to finish."));
            }

            if (!model.IsTypingSpeedQuestion
                && !model.IsListeningComprehensionQuestion
                && (model.VideoFile == null || model.VideoFile.Length == 0))
            {
                return View("ExitModalPartial", CreateAnswerSubmitErrorView(
                    model.SessionId,
                    "Video not ready",
                    "Your video answer was not attached yet. Please stop recording and wait a moment, then try again."));
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

        private async Task<bool> UploadBotInterviewVideo(ChatMessageViewModel model, string question, string candidateResponse)
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

                    //    HttpClient myWebClient = new HttpClient();
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

                    if (audioMimeType == "audio/mpeg" || audioMimeType == "audio/wav" || audioMimeType == "audio/ogg"
                        || audioMimeType == "audio/webm" || audioMimeType == "video/webm" || audioMimeType == "audio/mp4"
                        || audioMimeType == "audio/x-m4a" || audioMimeType == "application/octet-stream")
                    {
                        var uploadContentType = audioMimeType == "application/octet-stream" ? "audio/webm" : audioMimeType;
                        var audioExtension = uploadContentType.Contains("webm", StringComparison.OrdinalIgnoreCase) ? "webm"
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
                var result = await _aiinterviewerService.UpdateBotInterviewVideoDetails(VideoLink, question, model.SessionId.ToString(), model.SynthesizedVideoUrl, candidateResponse, model.TimeLeftInSeconds, model.InterviewId, model.AIInterviewerAudioLink, deliveryHeadQuestionId, model.IsTypingSpeedQuestion, model.IsListeningComprehensionQuestion, candidateAnswerAudioLink);
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

        [HttpGet]
        public async Task<IActionResult> Report(Guid seId, bool redo = false, bool? isExit = null)//Guid seId
        {
            string profileName = string.Empty;
            profileName = HttpContext?.Session?.GetString("profileName")?.Trim();
            profileName = profileName?.Replace("\"", "");
            ViewBag.ProfileName = profileName;

            var meetingDetails = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(seId);
            if (meetingDetails?.AIInterviewType == (int)AIInterviewTypeEnum.Preset)
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
                reportModel.CandidateEmail = meetingDetails.candidateEmail;
                reportModel.CandidateName = meetingDetails.candidateName;
                reportModel.InterviewDate = meetingDetails.interviewDate;
                reportModel.SessionId = seId.ToString();
                reportModel.InterviewId = meetingDetails.InterviewId;
                reportModel.MatchScorePercentage = meetingDetails.MatchScorePercentage;
                reportModel.resumeLink = meetingDetails.resumeLink;
                reportModel.AIResumeScreeningVerdict = meetingDetails.AIResumeScreeningVerdict;
                reportModel.AIResumeScreeningScore = meetingDetails.AIResumeScreeningScore;
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

                return View(reportModel);
            }
            else
            {
                string candidatePrompt = "Consider the interview finished. I want you to generate a report for this interview, as directed in the instructions. Format your response for readability as this response will be fed as it is into a Razor (.cshtml) page.";
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

                List<TypingSpeedAssessment> typingAssessmentsPdf;
                try { typingAssessmentsPdf = await _aiinterviewerService.GetTypingSpeedAssessmentBySessionId(seId.ToString()) ?? new List<TypingSpeedAssessment>(); } catch { typingAssessmentsPdf = new List<TypingSpeedAssessment>(); }
                reportModel.TypingSpeedAssessments = typingAssessmentsPdf;

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
                    return Content(
                        "<html><body style='font-family:Arial;padding:24px'>" +
                        "<h3>Video file missing</h3>" +
                        "<p>The question/answer video file could not be found in Azure Blob Storage " +
                        "(checked <code>videos</code> and <code>botinterviewvideo</code>).</p>" +
                        "</body></html>",
                        "text/html");
                }

                // Step 1 (from PDF): land only on this page — do not load/redirect/open the blob yet.
                // Step 2: user clicks Open → navigate to the playable blob URL.
                var safeHref = System.Net.WebUtility.HtmlEncode(playableUrl);
                var label = isQuestion ? "question" : "answer";
                var html =
                    "<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>" +
                    "<title>Open " + label + " video</title>" +
                    "<style>" +
                    "body{font-family:Segoe UI,Arial,sans-serif;padding:28px;background:#f7f9fb;color:#1a1a2e;margin:0;}" +
                    "h1{font-size:20px;margin:0 0 10px 0;}" +
                    "p{margin:0 0 18px 0;color:#555;font-size:14px;}" +
                    "a.btn{display:inline-block;padding:12px 18px;background:#4eda92;color:#0b0b18;font-weight:700;" +
                    "text-decoration:none;border-radius:6px;}" +
                    "a.back{display:inline-block;margin-left:14px;color:#0c7bb3;font-size:14px;}" +
                    "</style></head><body>" +
                    "<h1>Open " + label + " video</h1>" +
                    "<p>Click the button below to play the video.</p>" +
                    "<a class='btn' href='" + safeHref + "' target='_blank' rel='noopener noreferrer'>Open " + label + " video</a>" +
                    "<a class='back' href='javascript:history.back()'>Back</a>" +
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

                var irfVm = await _iRFService.GetEditIRF(questionDetails.IRFId, 0, false);
                if (irfVm == null || !DmSynthesiaVideoViewMode.UsesSynthesiaVideos(irfVm.IsAIInterview, irfVm.AIInterviewType))
                {
                    return Json(new { success = false, message = "AI question videos are only available for Preset AI interview channels." });
                }

                string videoId;
                try
                {
                    videoId = await _synthesiaTextToVideoService.CreateVideoAsync(questionDetails.Comment, SynthesiaAIAvatarTemplates.Reema);
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    return Json(new { success = false, message = "Could not reach Synthesia API. Check configuration and try again." });
                }

                if (string.IsNullOrWhiteSpace(videoId))
                {
                    return Json(new { success = false, message = "Synthesia did not return a video id." });
                }

                string synthesiaDownloadUrl = null;
                const int maxPollAttempts = 20;
                for (var attempt = 0; attempt < maxPollAttempts; attempt++)
                {
                    await Task.Delay(8000);
                    try
                    {
                        synthesiaDownloadUrl = await _synthesiaTextToVideoService.GetVideoUrlAsync(videoId);
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }

                    if (!string.IsNullOrWhiteSpace(synthesiaDownloadUrl))
                        break;
                }

                if (string.IsNullOrWhiteSpace(synthesiaDownloadUrl))
                {
                    return Json(new { success = false, message = "Synthesia video is still processing. Please try again in a minute." });
                }

                string uploadedVideoUrl = string.Empty;
                using (var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
                using (var myStream = await httpClient.GetStreamAsync(synthesiaDownloadUrl))
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

                var preview = questionDetails.Comment.Length > 25
                    ? questionDetails.Comment.Substring(0, 25) + "…"
                    : questionDetails.Comment;
                return Json(new
                {
                    success = true,
                    videoUrl = uploadedVideoUrl,
                    message = $"Successfully created an AI video for the question starting with '{preview}'."
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Video generation failed: " + ex.Message });
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
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            var cleaned = raw.Trim();
            if (cleaned.StartsWith("```json")) cleaned = cleaned.Substring(6).TrimStart();
            if (cleaned.EndsWith("```")) cleaned = cleaned.Substring(0, cleaned.Length - 3).TrimEnd();
            cleaned = cleaned.TrimStart('n');

            try
            {
                var strict = JsonConvert.DeserializeObject<AIInterviewEvaluation>(cleaned);
                if (strict != null &&
                    (strict.QuestionRatings != null || strict.RubricScoreSummary != null || strict.LlmDetection != null ||
                     !string.IsNullOrEmpty(strict.FinalVerdict) || !string.IsNullOrEmpty(strict.FinalComment) ||
                     strict.OverallTechnicalScore != 0 || strict.OverallCommunicationScore != 0))
                {
                    var rootForFix = JsonConvert.DeserializeObject<JObject>(cleaned);

                    if (strict.RubricScoreSummary != null && strict.RubricScoreSummary.Any(r => string.IsNullOrEmpty(r.RubricItem)))
                    {
                        var rArr = rootForFix?["rubric_score_summary"] as JArray;
                        if (rArr != null)
                        {
                            strict.RubricScoreSummary = rArr.Select(r => new RubricScoreSummary
                            {
                                RubricItem = r.Value<string>("rubric_item") ?? r.Value<string>("parameter") ?? r.Value<string>("item") ?? r.Value<string>("name"),
                                ExpectedLevel = r.Value<string>("expected_level") ?? r.Value<string>("expected") ?? r.Value<string>("evaluated_at"),
                                ObservedLevel = r.Value<string>("observed_level") ?? r.Value<string>("observed") ?? r.Value<string>("observed_at"),
                                Score = r.Value<float?>("score") ?? 0
                            }).ToList();
                        }
                    }

                    if (strict.QuestionRatings == null && rootForFix != null)
                    {
                        var qArr = (JArray)(rootForFix["question_ratings"] ?? rootForFix["questions"] ?? rootForFix["questionRatings"] ?? rootForFix["question_evaluations"]);
                        if (qArr != null)
                        {
                            strict.QuestionRatings = qArr.Select(q => new QuestionRating
                            {
                                Question = q.Value<string>("question") ?? q.Value<string>("text") ?? q.Value<string>("question_id"),
                                Verdict = q.Value<string>("verdict") ?? q.Value<string>("evaluation_summary"),
                                Score = q.Value<int?>("score") ?? 0,
                                Comment = q.Value<string>("comment") ?? q.Value<string>("evaluation_summary"),
                                CommunicationScore = q.Value<int?>("communication_score") ?? 0,
                                CommunicationComment = q.Value<string>("communication_comment")
                            }).ToList();
                        }
                    }

                    if (strict.RubricScoreSummary == null && rootForFix != null)
                    {
                        var rArr = (JArray)(rootForFix["rubric_score_summary"] ?? rootForFix["category_scores"] ?? rootForFix["rubric_summary"] ?? rootForFix["rubric"]);
                        if (rArr != null)
                        {
                            strict.RubricScoreSummary = rArr.Select(r => new RubricScoreSummary
                            {
                                RubricItem = r.Value<string>("rubric_item") ?? r.Value<string>("score_rubric_parameter") ?? r.Value<string>("parameter") ?? r.Value<string>("item") ?? r.Value<string>("name"),
                                ExpectedLevel = r.Value<string>("expected_level") ?? r.Value<string>("expected") ?? r.Value<string>("evaluated_at"),
                                ObservedLevel = r.Value<string>("observed_level") ?? r.Value<string>("observed") ?? r.Value<string>("summary"),
                                Score = r.Value<float?>("score") ?? r.Value<float?>("average_score") ?? 0
                            }).ToList();
                        }
                    }

                    if (string.IsNullOrEmpty(strict.FinalVerdict) && rootForFix != null)
                    {
                        strict.FinalVerdict = rootForFix.Value<string>("final_verdict") ?? rootForFix.Value<string>("verdict") ?? rootForFix.Value<string>("overall_recommendation");
                    }
                    if (string.IsNullOrEmpty(strict.FinalComment) && rootForFix != null)
                    {
                        strict.FinalComment = rootForFix.Value<string>("final_comment") ?? rootForFix.Value<string>("comment") ?? rootForFix.Value<string>("overall_summary");
                    }

                    return strict;
                }
            }
            catch { }

            try
            {
                var root = JsonConvert.DeserializeObject<JObject>(cleaned);
                if (root == null) return null;

                var eval = new AIInterviewEvaluation
                {
                    OverallTechnicalScore = root.Value<int?>("overall_technical_score") ?? root.Value<int?>("overall_score") ?? 0,
                    FinalVerdict = root.Value<string>("final_verdict") ?? root.Value<string>("verdict") ?? root.Value<string>("overall_recommendation"),
                    FinalComment = root.Value<string>("final_comment") ?? root.Value<string>("comment") ?? root.Value<string>("overall_summary"),
                    OverallCommunicationScore = root.Value<int?>("overall_communication_score") ?? root.Value<int?>("communication_score") ?? 0,
                    OverallCommunicationScoreReasoning = root.Value<string>("overall_communication_score reasoning") ?? root.Value<string>("overall_communication_score_reasoning")
                };

                var qNode = (JArray)(root["question_ratings"] ?? root["questions"] ?? root["questionRatings"] ?? root["question_evaluations"]);
                if (qNode != null)
                {
                    eval.QuestionRatings = qNode.Select(q => new QuestionRating
                    {
                        Question = q.Value<string>("question") ?? q.Value<string>("text") ?? q.Value<string>("question_id"),
                        Verdict = q.Value<string>("verdict") ?? q.Value<string>("evaluation_summary"),
                        Score = q.Value<int?>("score") ?? 0,
                        Comment = q.Value<string>("comment") ?? q.Value<string>("evaluation_summary"),
                        CommunicationScore = q.Value<int?>("communication_score") ?? 0,
                        CommunicationComment = q.Value<string>("communication_comment")
                    }).ToList();
                }

                var rNode = (JArray)(root["rubric_score_summary"] ?? root["rubric_summary"] ?? root["rubric"] ?? root["category_scores"]);
                if (rNode != null)
                {
                    eval.RubricScoreSummary = rNode.Select(r => new RubricScoreSummary
                    {
                        RubricItem = r.Value<string>("rubric_item") ?? r.Value<string>("score_rubric_parameter") ?? r.Value<string>("parameter") ?? r.Value<string>("item") ?? r.Value<string>("name"),
                        ExpectedLevel = r.Value<string>("expected_level") ?? r.Value<string>("expected") ?? r.Value<string>("evaluated_at"),
                        ObservedLevel = r.Value<string>("observed_level") ?? r.Value<string>("observed") ?? r.Value<string>("summary"),
                        Score = r.Value<float?>("score") ?? r.Value<float?>("average_score") ?? 0
                    }).ToList();
                }

                var lNode = (JObject)(root["llm_detection"] ?? root["llmDetection"]);
                if (lNode != null)
                {
                    eval.LlmDetection = new LlmDetection
                    {
                        LlmResponseSuspected = lNode.Value<bool?>("llm_response_suspected") ?? lNode.Value<bool?>("suspected") ?? false,
                        EvidenceComment = lNode.Value<string>("evidence_comment") ?? lNode.Value<string>("evidence"),
                        SuspiciousInstances = lNode["suspicious_instances"] is JArray arr ? arr.Select(x => x.ToString()).ToList() : null
                    };
                }

                return eval;
            }
            catch { return null; }
        }

        [HttpPost]
        public async Task<IActionResult> SaveCandidateAnswerById(int questionId, string candidateAnswer)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var email = HttpContext.Session.GetString("email")?.Trim();
                email = profileName.Replace("\"", "");
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