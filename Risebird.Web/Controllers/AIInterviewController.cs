using ExcelDataReader.Log;
using Google.Apis.Drive.v3.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        public AIInterviewController(IConfiguration configuration, IAzureStorageHelper azureStorageHelper,
            IApplicationErrorLogger applicationErrorLogger, IInterviewApiService api,
            IOpenAISpeechToTextService speechToTextService, IFileMimeTypeHelper fileMimeTypeHelper,
            ISASUrlHelper sasUrlHelper, IAIInterviewerService aiinterviewerService,
            ISynthesiaTextToVideoService synthesiaTextToVideoService, IPdfReaderHelper pdfReaderHelper,
            IIRFService iRFService, IAWSRekognitionService aWSRekognitionService, ICandidateService candidateService,
            IIRFRepository irfRepository, IOpenAIInterviewerAssistantHelper openAIAssistantHelper)
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
                    if (meetingDetails.AIInterviewType == (int)AIInterviewTypeEnum.Preset)
                    {
                        var transcript = await _aiinterviewerService.GetAIInterviewTranscriptBySessionId(sid);

                        //not started yet
                        if (transcript == null || transcript.Count() == 0)
                        {
                            var sampleQuestion = await _aiinterviewerService.GetRandomQuestionForSession(meetingDetails.InterviewId, meetingDetails.irfIntId);
                            
                            if(sampleQuestion != null)
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

                                        if(sampleQuestion != null)
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
                                            if(meetingDetails.MeetingStatus != (int)MeetingStatus.Completed)
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

                            if (!string.IsNullOrEmpty(meetingDetails.resumeLink))
                            {
                                resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(meetingDetails.resumeLink);
                            }
                            if (!string.IsNullOrEmpty(meetingDetails.TechSkillsInJson))
                            {
                                techSkillsJson = meetingDetails.TechSkillsInJson;
                            }
                            string candidatePrompt;
                            if (!string.IsNullOrEmpty(resumeStringContent))
                            {
                                candidatePrompt = $"You will now interview the candidate for {meetingDetails.JDTiltle} according to the tech rubric in json here: {techSkillsJson}. The resume of the candidate in string format is as follows:{resumeStringContent ?? string.Empty}. Proceed as per the instructions fed to you.";

                            }
                            else
                            {
                                candidatePrompt = $"You will now interview the candidate for {meetingDetails.JDTiltle} according to the tech rubric in json here: {techSkillsJson}. The candidate's resume is not available. Proceed without the resume as per the instructions fed to you.";

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

                                if (!string.IsNullOrEmpty(meetingDetails.resumeLink))
                                {
                                    resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(meetingDetails.resumeLink);
                                }
                                if (!string.IsNullOrEmpty(meetingDetails.TechSkillsInJson))
                                {
                                    techSkillsJson = meetingDetails.TechSkillsInJson;
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
                                    candidatePrompt = $"You will now interview the candidate for {meetingDetails.JDTiltle} according to the tech rubric in json here: {techSkillsJson}. The resume of the candidate in string format is as follows:{resumeStringContent ?? string.Empty}. Proceed as per the instructions fed to you.";

                                }
                                else
                                {
                                    candidatePrompt = $"You will now interview the candidate for {meetingDetails.JDTiltle} according to the tech rubric in json here: {techSkillsJson}. The candidate's resume is not available. Proceed without the resume as per the instructions fed to you.";

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

        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Index([FromForm] ChatMessageViewModel model)
        {

            try
            {
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
                    else if(!model.showChatBox)
                    {
                        if (model.AudioFile != null && model.AudioFile.Length > 0)
                        {
                            using var stream = model.AudioFile.OpenReadStream();
                            userMessage = await _speechToTextService.TranscribeAsync(stream, model.AudioFile.FileName, model.isMultiLingual);

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
                            using var stream = model.AudioFile.OpenReadStream();
                            tranScribedResponse = await _speechToTextService.TranscribeAsync(stream, model.AudioFile.FileName, model.isMultiLingual);

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
                            if(!string.IsNullOrEmpty(model.Message))
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


                    var transcript = await _aiinterviewerService.GetAIInterviewTranscriptBySessionId(model.SessionId);
                    var allQuestions = transcript.Where(x => x.Role.ToLower() == "assistant");
                    //if (allQuestions?.Count() == 10) //&& !string.IsNullOrEmpty(model.CurrentQuestion) //&& !string.IsNullOrEmpty(model.CurrentQuestion)
                    if (allQuestions?.Count() == 35)
                    //if (allQuestions?.Count() == 3)                                 
                    {
                        bool iscandidateVideoSavedTodb =  await UploadBotInterviewVideo(model, question, userMessage);
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

                                // Prepare input data for OpenAI assistant (assistant is pre-configured with instructions)
                                var inputs = new TypingInputs
                                {
                                    reference_paragraph = model.TypingSampleText,
                                    user_typed_text = model.TypedText,
                                    words_per_minute = 30.00M, // Passing criteria
                                    total_minutes = 2.00M // 2 minute duration
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
                                        // Pass if word accuracy >= 97%
                                        isPassed = wordAccuracy >= 97.00M;
                                    }

                                    var typingAssessment = new TypingSpeedAssessment
                                    {
                                        SessionId = model.SessionId.ToString(),
                                        QuestionId = model.CurrentQuestionId,
                                        SampleText = model.TypingSampleText,
                                        TypedText = model.TypedText,
                                        TimeStarted = DateTime.Parse(model.TypingStartTime),
                                        TimeCompleted = DateTime.Parse(model.TypingEndTime),
                                        TimeTakenInSeconds = model.TypingTimeTaken,
                                        TotalCharacters = analysisResult?.meta?.user_total_words_typed ?? 0,
                                        CorrectCharacters = analysisResult?.counts?.correct_words ?? 0,
                                        AccuracyPercentage = wordAccuracy,
                                        WordsPerMinute = 30.00M, // Based on requirement
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
                                    TimeStarted = DateTime.Parse(model.ListeningStartTime),
                                    TimeCompleted = DateTime.Parse(model.ListeningEndTime),
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

                            //model.IsAdaptive = true;
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

                    var transcript = await _aiinterviewerService.GetAIInterviewTranscriptBySessionId(model.SessionId);
                    var allQuestions = transcript.Where(x => x.Role.ToLower() == "assistant");
                    //if (allQuestions?.Count() == 10) //&& !string.IsNullOrEmpty(model.CurrentQuestion)
                    if (allQuestions?.Count() == 35) //&& !string.IsNullOrEmpty(model.CurrentQuestion)
                                                     //if (allQuestions?.Count() == 3)//&& !string.IsNullOrEmpty(model.CurrentQuestion)
                    {
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

                        model.IsAdaptive = true;
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                await _aiinterviewerService.LogAIInterviewException(model.SessionId.ToString(), "POST", ex);
                var exitModelError = new AIInterviewExitModalViewModel
                {
                    Title = "Interview Not Found",
                    Message = "The interview session could not be found. Please check the session ID and try again.",
                    IconClass = "fa-exclamation-triangle",
                    IconColor = "#dc3545",
                    ButtonText = "Close",
                    ButtonClass = "btn-danger",
                    SessionId = model.SessionId.ToString()
                };
                return View("ExitModalPartial", exitModelError);
            }

        }

        private bool IsTranscriptionValid(string text, bool isMultilingual)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            if(!isMultilingual)
            {
                // Remove common punctuation and whitespace for validation
                var cleaned = new string(text
                    .Where(c => char.IsLetter(c) || char.IsDigit(c))
                    .ToArray());

                // Check if output contains too many unknown characters (�)
                if (text.Count(c => c == '�') > 3)
                    return false;

                // Check ratio of valid characters to overall text
                double validRatio = (double)cleaned.Length / text.Length;
                if (validRatio < 0.5) // Less than 50% recognizable text → bad transcription
                    return false;

            }
            return true;

        }

        private async Task<bool> UploadBotInterviewVideo(ChatMessageViewModel model, string question, string candidateResponse)
        {
            try
            {
                string VideoLink = string.Empty;
                
                // If there's a video file, upload it
                if (model.VideoFile != null && model.VideoFile.Length > 0)
                {
                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.VideoFile);
                    if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4" || fileMimeType == "video/quicktime" || fileMimeType == "video/x-matroska" || fileMimeType == "video/webm")
                    {
                        //var key = await _aWSRekognitionService.UploadVideoAsync(model.VideoFile);
                        //var faceDetection = await _aWSRekognitionService.AnalyzeVideoAsync(key);
                        var file = model.VideoFile;
                        var filename = model.SessionId.ToString().Replace(".", "_").Replace("@", "a").ToString() + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".mp4";
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
                // If no video file but there's a text/code response, save it with empty VideoLink
                // This allows text/code submissions to be saved successfully
                
                // Save in db with question.sessionid,videoLink,Datetime (VideoLink can be empty for text responses)
                // For the Adaptive/OpenAI flow, CurrentQuestionId is 0 because there are no preset question IDs.
                // Passing 0 is treated as non-null by the stored proc, which causes it to UPDATE the very first row
                // (DeliveryHeadQuestionId=0) on every subsequent answer instead of INSERTing a new row.
                // Passing null tells the stored proc to always INSERT a new row, creating one entry per answered question.
                int? deliveryHeadQuestionId = model.CurrentQuestionId > 0 ? model.CurrentQuestionId : (int?)null;
                var result = await _aiinterviewerService.UpdateBotInterviewVideoDetails(VideoLink, question, model.SessionId.ToString(), model.SynthesizedVideoUrl, candidateResponse, model.TimeLeftInSeconds, model.InterviewId, model.AIInterviewerAudioLink, deliveryHeadQuestionId, model.IsTypingSpeedQuestion, model.IsListeningComprehensionQuestion);
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
                        if(aiReport!=null)
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
                reportModel.videoQuestionDetails = await _aiinterviewerService.GetBotInterviewVideoDetails(seId);
                //reportModel.chatMessageViewModel = chatMessageModel; 
                reportModel.AIInterviewEvaluation = TryParseEvaluation(aiReport?.Replies?.FirstOrDefault());
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
                if(reportModel.AIInterviewEvaluation!= null)
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
                        aiReport = await _api.GenerateReport(seId, candidatePrompt, isExit:isExit);
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
                                    return View("ExitModalPartial", exitModel);;
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
                reportModel.videoQuestionDetails = await _aiinterviewerService.GetBotInterviewVideoDetails(seId);
                //reportModel.chatMessageViewModel = chatMessageModel;
                
                reportModel.AIInterviewEvaluation = TryParseEvaluation(aiReport?.Replies?.FirstOrDefault()); //JsonConvert.DeserializeObject<AIInterviewEvaluation>(cleaned);
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
                
                return View(reportModel);
            }
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
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "SupportAdmin" || profileName == "SeniorHR" || profileName == "SeniorInterviewerManagement")
                    {
                        var questionDetails = await _iRFService.GetDeliveryHeadSampleQuestionById(id);

                        //now call the synthesia api to generate the video
                        var videoId = await _synthesiaTextToVideoService.CreateVideoAsync(questionDetails.Comment, SynthesiaAIAvatarTemplates.Reema);

                        // Wait/poll until video is ready (Synthesia usually takes 30–60s)
                        string videoUrl;
                        do
                        {
                            await Task.Delay(8000);
                            videoUrl = await _synthesiaTextToVideoService.GetVideoUrlAsync(videoId);
                        } while (string.IsNullOrWhiteSpace(videoUrl));


                        //save the video in our blob/
                        string uploadedVideoUrl = string.Empty;

                        if (videoUrl != null)
                        {
                            HttpClient myWebClient = new HttpClient();
                            using (Stream myStream = myWebClient.GetStreamAsync(videoUrl).Result)
                            {
                                var fileName = $"aiVideo_{id.ToString()}_{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")}";
                                //long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                //fileName = fileName + "_" + unixTime.ToString();
                                try
                                {
                                    uploadedVideoUrl = _azureStorageHelper.InsertBlobCloudCompare(fileName, "botinterviewvideo", null, myStream).Result;

                                }
                                catch (Exception ex)
                                {
                                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                                }

                                //generate SAS URL from the uploaded video url
                                //var sas = _sasUrlHelper.CreateSasUrl(ContainerName);

                                //if (!string.IsNullOrEmpty(uploadedVideoUrl) && !string.IsNullOrEmpty(sas))
                                //{
                                //    videoSasUrl = uploadedVideoUrl + "?" + sas;
                                //}
                            }
                        }

                        //save the video link in our db against the above id

                        var result = await _iRFService.SaveSampleQuestionAIVideoLink(id, uploadedVideoUrl);
                        if (result)
                        {
                            return Json($"Successfully created an AI Video for the question starting with '{questionDetails.Comment.Substring(0, 25)}'");
                        }
                        else
                        {
                            return Json($"Failed to create an AI Video for the question starting with '{questionDetails.Comment.Substring(0, 25)}'");
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
                //return Json($"Failed to create an AI Video for the question starting with '{questionDetails.Comment.Substring(0, 25)}'");

                return Json(false);

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
                    FinalVerdict = root.Value<string>("final_verdict") ?? root.Value<string>("verdict"),
                    FinalComment = root.Value<string>("final_comment") ?? root.Value<string>("comment"),
                    OverallCommunicationScore = root.Value<int?>("overall_communication_score") ?? root.Value<int?>("communication_score") ?? 0,
                    OverallCommunicationScoreReasoning = root.Value<string>("overall_communication_score reasoning") ?? root.Value<string>("overall_communication_score_reasoning")
                };

                var qNode = (JArray)(root["question_ratings"] ?? root["questions"] ?? root["questionRatings"]);
                if (qNode != null)
                {
                    eval.QuestionRatings = qNode.Select(q => new QuestionRating
                    {
                        Question = q.Value<string>("question") ?? q.Value<string>("text"),
                        Verdict = q.Value<string>("verdict"),
                        Score = q.Value<int?>("score") ?? 0,
                        Comment = q.Value<string>("comment"),
                        CommunicationScore = q.Value<int?>("communication_score") ?? 0,
                        CommunicationComment = q.Value<string>("communication_comment")
                    }).ToList();
                }

                var rNode = (JArray)(root["rubric_score_summary"] ?? root["rubric_summary"] ?? root["rubric"]);
                if (rNode != null)
                {
                    eval.RubricScoreSummary = rNode.Select(r => new RubricScoreSummary
                    {
                        RubricItem = r.Value<string>("rubric_item") ?? r.Value<string>("item"),
                        ExpectedLevel = r.Value<string>("expected_level") ?? r.Value<string>("expected"),
                        ObservedLevel = r.Value<string>("observed_level") ?? r.Value<string>("observed"),
                        Score = r.Value<int?>("score") ?? 0
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
                            
                            return Json(new { issuccessful = true, message = $"Successfully flagged the question starting with '{snippet}' for sentiment analysis."});//, aiResponse = response.Data?.FirstOrDefault()?.Content?.Where(x=> x.Text.Value.Contains("Evaluate the resume")).FirstOrDefault()?.Text.Value

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