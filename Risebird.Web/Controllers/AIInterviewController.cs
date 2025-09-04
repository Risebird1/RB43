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
//using static SkiaSharp.HarfBuzz.SKShaper;

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

        public AIInterviewController(IConfiguration configuration, IAzureStorageHelper azureStorageHelper,
            IApplicationErrorLogger applicationErrorLogger, IInterviewApiService api,
            IOpenAISpeechToTextService speechToTextService, IFileMimeTypeHelper fileMimeTypeHelper,
            ISASUrlHelper sasUrlHelper, IAIInterviewerService aiinterviewerService,
            ISynthesiaTextToVideoService synthesiaTextToVideoService, IPdfReaderHelper pdfReaderHelper,
            IIRFService iRFService)
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
        }
        /// <summary>
        /// access an interview (and start it) with the session id (sid)
        /// </summary>
        /// <param name="sid"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Index(Guid sid)
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
                        var model = new ChatMessageViewModel();
                        model.SessionId = sid;
                        model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(sampleQuestion.Message), sampleQuestionId = sampleQuestion.id });

                        //var latestReply = CleanMessage(sampleQuestion.Message);

                        if (!string.IsNullOrEmpty(sampleQuestion.Message))
                        {
                            model.SynthesizedVideoUrl = sampleQuestion.AIQuestionVideoLink;
                            model.CurrentQuestion = CleanMessage(sampleQuestion.Message);
                            model.CurrentQuestionId = sampleQuestion.id;
                            await _aiinterviewerService.LogTranscript(sid, "assistant", model.CurrentQuestion, sampleQuestion.id);

                        }


                        model.IsAdaptive = meetingDetails.IsAdaptive;
                        model.InterviewId = meetingDetails.InterviewId;
                        model.IRFId = meetingDetails.irfIntId;
                        model.TimeLeftInSeconds = meetingDetails.TimeLeftInSeconds;
                        model.candidateName = meetingDetails.candidateName;
                        model.AssistantId = meetingDetails.AssistantId;
                        model.AIInterviewType = meetingDetails.AIInterviewType;
                        return View(model);
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
                            if (allQuestions != null && allQuestions.Count() <= 25)
                            //if(allQuestions != null && allQuestions.Count() <= 3)
                            {
                                if (latestEntry.Role.ToLower() == "assistant" && allQuestions.Count() <= 10)
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
                                else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() < 25)
                                //else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() < 3)
                                {
                                    model.ChatLog.Add(new ChatLine { Role = "User", Message = latestEntry.Message });
                                    //var replies = await _api.SendReply(sid, latestEntry.Message, meetingDetails.AssistantId);

                                    //foreach (var reply in replies)
                                    //{
                                    //    model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(reply) });
                                    //}
                                    //var latestReply = CleanMessage(replies.FirstOrDefault());

                                    var sampleQuestion = await _aiinterviewerService.GetRandomQuestionForSession(meetingDetails.InterviewId, meetingDetails.irfIntId);

                                    model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(sampleQuestion.Message), sampleQuestionId = sampleQuestion.id });

                                    //var latestReply = CleanMessage(sampleQuestion.Message);

                                    if (!string.IsNullOrEmpty(sampleQuestion.Message))
                                    {
                                        model.SynthesizedVideoUrl = latestEntry.AIQuestionVideoLink;
                                        model.CurrentQuestion = CleanMessage(sampleQuestion.Message);
                                        model.CurrentQuestionId = latestEntry.DeliveryHeadQuestionId;
                                        await _aiinterviewerService.LogTranscript(sid, "assistant", model.CurrentQuestion, sampleQuestion.id);

                                    }

                                }
                                //else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() == 10)
                                else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() == 25)
                                //else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() == 3)
                                {
                                    return Json("The interviewer is through with all the questions. This interview is now closed.");

                                }
                            }
                            else
                            {
                                return Json("The interviewer is through with all the questions. This interview is now closed.");
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
                            if (allQuestions != null && allQuestions.Count() <= 25)
                            //if(allQuestions != null && allQuestions.Count() <= 3)
                            {
                                //if (latestEntry.Role.ToLower() == "assistant" && allQuestions.Count() <= 10)
                                if (latestEntry.Role.ToLower() == "assistant" && allQuestions.Count() <= 25)
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
                                else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() < 25)
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
                                else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() == 25)
                                //else if (latestEntry.Role.ToLower() == "user" && allQuestions.Count() == 3)
                                {
                                    return Json("The interviewer is through with all the questions. This interview is now closed.");

                                }
                            }
                            else
                            {
                                return Json("The interviewer is through with all the questions. This interview is now closed.");
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
                            if (!string.IsNullOrEmpty(resumeStringContent))
                            {
                                candidatePrompt = $"Due to some reason, you are required to start over the interview from the top. Please start with the first question. You will interview the candidate for {meetingDetails.JDTiltle} according to the tech rubric in json here: {techSkillsJson}. The resume of the candidate in string format is as follows:{resumeStringContent ?? string.Empty}. Proceed as per the instructions fed to you.";

                            }
                            else
                            {
                                candidatePrompt = $"Due to some reason, you are required to start over the interview from the top. Please start with the first question. You will interview the candidate for {meetingDetails.JDTiltle} according to the tech rubric in json here: {techSkillsJson}. The candidate's resume is not available. Proceed without the resume as per the instructions fed to you.";

                            }
                            var replies = await _api.SendReply(sid, candidatePrompt, meetingDetails.AssistantId);

                            model = new ChatMessageViewModel
                            {
                                SessionId = sid,
                                ChatLog = replies.Select(r => new ChatLine { Role = "Bot", Message = CleanMessage(r) }).ToList(),
                            };


                            var latestReply = CleanMessage(replies.FirstOrDefault());

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
                        return View(model);

                    }
                }


            }

            return View(null);
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Index([FromForm] ChatMessageViewModel model)
        {

            if (model.AIInterviewType == (int)AIInterviewTypeEnum.Preset)
            {
                string question = model.CurrentQuestion;
                string userMessage = model.Message;
                if (model.AudioFile != null && model.AudioFile.Length > 0)
                {
                    using var stream = model.AudioFile.OpenReadStream();
                    userMessage = await _speechToTextService.TranscribeAsync(stream, model.AudioFile.FileName);
                }

                var transcript = await _aiinterviewerService.GetAIInterviewTranscriptBySessionId(model.SessionId);
                var allQuestions = transcript.Where(x => x.Role.ToLower() == "assistant");
                //if (allQuestions?.Count() == 10) //&& !string.IsNullOrEmpty(model.CurrentQuestion) //&& !string.IsNullOrEmpty(model.CurrentQuestion)
                if (allQuestions?.Count() == 25)
                //if (allQuestions?.Count() == 3)                                 
                {
                    await _aiinterviewerService.LogTranscript(model.SessionId, "user", userMessage);
                    await UploadBotInterviewVideo(model, question, userMessage);
                    return Json("The interviewer is through with all the questions. This interview is now closed.");
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
                    await _aiinterviewerService.LogTranscript(model.SessionId, "user", userMessage);


                    var sampleQuestion = await _aiinterviewerService.GetRandomQuestionForSession(model.InterviewId, model.IRFId);

                    model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(sampleQuestion.Message), sampleQuestionId = sampleQuestion.id });
                    model.ChatLog.Add(new ChatLine { Role = "User", Message = userMessage });

                    await UploadBotInterviewVideo(model, question, userMessage);

                    //var latestReply = CleanMessage(replies?.FirstOrDefault());

                    //current question changes to the latest reply by the assistant and is set only after the earlier question-answer response is saved using UploadBotInterviewVideo()
                    model.CurrentQuestion = CleanMessage(sampleQuestion.Message);
                    model.CurrentQuestionId = sampleQuestion.id;

                    if (!string.IsNullOrEmpty(model.CurrentQuestion))//&& string.IsNullOrEmpty(model.AIInterviewerAudioLink)
                    {
                        model.SynthesizedVideoUrl = sampleQuestion.AIQuestionVideoLink;
                        model.CurrentQuestion = CleanMessage(sampleQuestion.Message);
                        model.CurrentQuestionId = sampleQuestion.id;
                        await _aiinterviewerService.LogTranscript(model.SessionId, "assistant", model.CurrentQuestion, sampleQuestion.id);

                    }

                    model.Message = "";

                    //model.IsAdaptive = true;
                    return View(model);
                }
            }
            else
            {
                string question = model.CurrentQuestion;
                string userMessage = model.Message;
                if (model.AudioFile != null && model.AudioFile.Length > 0)
                {
                    using var stream = model.AudioFile.OpenReadStream();
                    userMessage = await _speechToTextService.TranscribeAsync(stream, model.AudioFile.FileName);
                }

                var transcript = await _aiinterviewerService.GetAIInterviewTranscriptBySessionId(model.SessionId);
                var allQuestions = transcript.Where(x => x.Role.ToLower() == "assistant");
                //if (allQuestions?.Count() == 10) //&& !string.IsNullOrEmpty(model.CurrentQuestion)
                if (allQuestions?.Count() == 25) //&& !string.IsNullOrEmpty(model.CurrentQuestion)
                                                 //if (allQuestions?.Count() == 3)//&& !string.IsNullOrEmpty(model.CurrentQuestion)
                {
                    await _aiinterviewerService.LogTranscript(model.SessionId, "user", userMessage);
                    await UploadBotInterviewVideo(model, question, userMessage);
                    return Json("The interviewer is through with all the questions. This interview is now closed.");
                }
                else
                {
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

                    await UploadBotInterviewVideo(model, question, userMessage);

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
        private async Task UploadBotInterviewVideo(ChatMessageViewModel model, string question, string candidateResponse)
        {
            if (model.VideoFile != null && model.VideoFile.Length > 0)
            {
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.VideoFile);
                if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4" || fileMimeType == "video/quicktime" || fileMimeType == "video/x-matroska" || fileMimeType == "video/webm")
                {
                    var file = model.VideoFile;
                    var filename = model.SessionId.ToString().Replace(".", "_").Replace("@", "a").ToString() + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".mp4";
                    //await _azureStorageHelper.InsertBlob(filename, "botinterviewvideo", file);
                    await _azureStorageHelper.InsertImageBlob(filename, "botinterviewvideo", file, fileMimeType);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("botinterviewvideo");
                    var blob = containerRef.GetBlockBlobReference(filename);
                    string uploadedvideoFileURL = blob.Uri.ToString();
                    var sas = _sasUrlHelper.CreateSasUrl("botinterviewvideo");
                    var VideoLink = string.Empty;
                    if (!string.IsNullOrEmpty(uploadedvideoFileURL) && !string.IsNullOrEmpty(sas))
                    {
                        VideoLink = uploadedvideoFileURL + "?" + sas;
                    }
                    //Save in db with question.sessionid,videoLink,Datetime
                    var result = await _aiinterviewerService.UpdateBotInterviewVideoDetails(VideoLink, question, model.SessionId.ToString(), model.SynthesizedVideoUrl, candidateResponse, model.TimeLeftInSeconds, model.InterviewId, model.AIInterviewerAudioLink, model.CurrentQuestionId);
                }
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
        public async Task<IActionResult> Report(Guid seId, bool redo = false, bool isExit = false)//Guid seId
        {
            var meetingDetails = await _aiinterviewerService.GetAIInterviewDetailsBySessionId(seId);
            if (meetingDetails?.AIInterviewType == (int)AIInterviewTypeEnum.Preset)
            {
                InterviewReportResult aiReport = null;
                if (redo)
                {
                    aiReport = await _api.GenerateReport(seId, string.Empty, meetingDetails.AIInterviewType);
                    if (aiReport != null)
                    {
                        await _aiinterviewerService.SaveReportOfAIInterviewByAssistant(seId, JsonConvert.SerializeObject(aiReport));
                    }
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
                        aiReport = await _api.GenerateReport(seId, string.Empty, meetingDetails.AIInterviewType);
                        var list = new List<string>();
                        list.Add(aiReport.Replies.FirstOrDefault());
                        aiReport.Replies = list;
                        if (aiReport != null)
                        {
                            var result = await _aiinterviewerService.SaveReportOfAIInterviewByAssistant(seId, JsonConvert.SerializeObject(aiReport));
                            if (isExit)
                            {
                                if (result)
                                {
                                    return Json("Meeting closed successfully. Thankyou for your time!");
                                }
                                else
                                {
                                    return Json("Error while exiting the interview. Please contact support.");
                                }
                            }
                        }
                    }
                }

                if (isExit)
                {
                    return Json("Meeting closed successfully. Thankyou for your time!");
                }

                var reportModel = new AIInterviewReportWrapperClass();
                reportModel.videoQuestionDetails = await _aiinterviewerService.GetBotInterviewVideoDetails(seId);
                //reportModel.chatMessageViewModel = chatMessageModel; 
                reportModel.AIInterviewEvaluation = TryParseEvaluation(aiReport?.Replies?.FirstOrDefault());
                //reportModel.TotalQuestions = result.Replies.Count() - 1;
                ViewBag.IsAdaptive = false;
                reportModel.CandidateEmail = meetingDetails.candidateEmail;
                reportModel.CandidateName = meetingDetails.candidateName;
                reportModel.InterviewDate = meetingDetails.interviewDate;
                return View(reportModel);
            }
            else
            {
                string candidatePrompt = "Consider the interview finished. I want you to generate a report for this interview, as directed in the instructions. Format your response for readability as this response will be fed as it is into a Razor (.cshtml) page.";
                InterviewReportResult aiReport = null;
                if (redo)
                {
                    aiReport = await _api.GenerateReport(seId, candidatePrompt);
                    if (aiReport != null)
                    {
                        await _aiinterviewerService.SaveReportOfAIInterviewByAssistant(seId, JsonConvert.SerializeObject(aiReport));
                    }
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
                        aiReport = await _api.GenerateReport(seId, candidatePrompt);
                        var list = new List<string>();
                        list.Add(aiReport.Replies.FirstOrDefault());
                        aiReport.Replies = list;
                        if (aiReport != null)
                        {
                            var result = await _aiinterviewerService.SaveReportOfAIInterviewByAssistant(seId, JsonConvert.SerializeObject(aiReport));
                            if (isExit)
                            {
                                if (result)
                                {
                                    return Json("Meeting closed successfully. Thankyou for your time!");
                                }
                                else
                                {
                                    return Json("Error while exiting the interview. Please contact support.");
                                }
                            }
                        }
                    }
                }
                if (isExit)
                {
                    return Json("Meeting closed successfully. Thankyou for your time!");
                }

                AIInterviewReportWrapperClass reportModel = new AIInterviewReportWrapperClass();
                reportModel.videoQuestionDetails = await _aiinterviewerService.GetBotInterviewVideoDetails(seId);
                //reportModel.chatMessageViewModel = chatMessageModel;
                
                reportModel.AIInterviewEvaluation = TryParseEvaluation(aiReport?.Replies?.FirstOrDefault()); //JsonConvert.DeserializeObject<AIInterviewEvaluation>(cleaned);
                //reportModel.TotalQuestions = result.Replies.Count() - 1;
                ViewBag.IsAdaptive = true;
                reportModel.CandidateEmail = meetingDetails.candidateEmail;
                reportModel.CandidateName = meetingDetails.candidateName;
                reportModel.InterviewDate = meetingDetails.interviewDate;
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
                        var videoId = await _synthesiaTextToVideoService.CreateVideoAsync(questionDetails.Comment, "your_template_id_here");

                        // Wait/poll until video is ready (Synthesia usually takes 30–60s)
                        string videoUrl;
                        do
                        {
                            await Task.Delay(8000);
                            videoUrl = await _synthesiaTextToVideoService.GetVideoUrlAsync(videoId);
                        } while (string.IsNullOrWhiteSpace(videoUrl));


                        //save the video in our blob
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
                                    uploadedVideoUrl = _azureStorageHelper.InsertBlobCloudCompare(fileName, "videos", null, myStream).Result;

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
    }
}