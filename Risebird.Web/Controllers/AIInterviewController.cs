using Google.Apis.Drive.v3.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rest;
using RestSharp;
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

        public AIInterviewController(IConfiguration configuration, IAzureStorageHelper azureStorageHelper,
            IApplicationErrorLogger applicationErrorLogger, IInterviewApiService api,
            IOpenAISpeechToTextService speechToTextService, IFileMimeTypeHelper fileMimeTypeHelper,
            ISASUrlHelper sasUrlHelper, IAIInterviewerService aiinterviewerService,
            ISynthesiaTextToVideoService synthesiaTextToVideoService, IPdfReaderHelper pdfReaderHelper)
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
            if(meetingDetails != null)
            {
                //not started yet
                if(string.IsNullOrEmpty(meetingDetails.ThreadId))
                {

                    string resumeStringContent = string.Empty;
                    string techSkillsJson = string.Empty;

                    if(!string.IsNullOrEmpty(meetingDetails.resumeLink))
                    {
                        resumeStringContent = await _pdfReaderHelper.ReadPdfFromUrlAsync(meetingDetails.resumeLink);
                    }
                    if (!string.IsNullOrEmpty(meetingDetails.TechSkillsInJson))
                    {
                        techSkillsJson = meetingDetails.TechSkillsInJson;
                    }
                    // For demo: tech stack prompt
                    string candidatePrompt = $"You will now interview the candidate for {meetingDetails.JDTiltle} according to the tech rubric in json here: {techSkillsJson}. The resume of the candidate in string format is as follows:{resumeStringContent ?? string.Empty}. Proceed as per the instructions fed to you.";
                    var result = await _api.StartConversation(sid, candidatePrompt);

                    var model = new ChatMessageViewModel
                    {
                        SessionId = sid,
                        ChatLog = result.Replies.Select(r => new ChatLine { Role = "Bot", Message = CleanMessage(r) }).ToList()
                    };


                    var latestReply = CleanMessage(result.Replies.FirstOrDefault());

                    if (!string.IsNullOrEmpty(latestReply))
                    {
                        var audioBytes = await _speechToTextService.ConvertTextToSpeechAsync(latestReply);
                        var base64Audio = Convert.ToBase64String(audioBytes);
                        ViewData["AssistantAudioBase64"] = base64Audio;
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
                    model.TimeLeftInSeconds = meetingDetails.TimeLeftInSeconds;
                    model.candidateName = meetingDetails.candidateName;
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

                        if (latestEntry.Role.ToLower() == "assistant")
                        {
                            var candidateLatestEntry = transcript.Where(x => x.Role.ToLower() == "user")?.FirstOrDefault();

                            if(candidateLatestEntry != null)
                            {
                                model.ChatLog.Add(new ChatLine { Role = "User", Message = candidateLatestEntry.Message });

                            }
                            foreach (var question in allQuestions)
                            {
                                model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(question.Message) });
                            }
                            var latestQuestion = latestEntry.Message;
                            if (!string.IsNullOrEmpty(latestQuestion))
                            {
                                var audioBytes = await _speechToTextService.ConvertTextToSpeechAsync(latestQuestion);
                                var base64Audio = Convert.ToBase64String(audioBytes);
                                ViewData["AssistantAudioBase64"] = base64Audio;
                            }
                        }
                        else if (latestEntry.Role.ToLower() == "user")
                        {
                            model.ChatLog.Add(new ChatLine { Role = "User", Message = latestEntry.Message });
                            var replies = await _api.SendReply(sid, latestEntry.Message);
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
                            }
                        }
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
                    model.candidateName = meetingDetails.candidateName;
                    //ViewBag.IsAdaptive = true;
                    model.IsAdaptive = meetingDetails.IsAdaptive;
                    return View(model);

                }
            }

            return View(null);
        }

        [HttpPost]
        public async Task<IActionResult> Index([FromForm] ChatMessageViewModel model)
        {

            string userMessage = model.Message;
            if (model.AudioFile != null && model.AudioFile.Length > 0)
            {
                using var stream = model.AudioFile.OpenReadStream();
                userMessage = await _speechToTextService.TranscribeAsync(stream, model.AudioFile.FileName);
            }
            var replies = await _api.SendReply(model.SessionId, userMessage);
            var question = replies?.Skip(1)?.Take(1);
            model.ChatLog.Add(new ChatLine { Role = "User", Message = userMessage });

            foreach (var reply in replies)
            {
                model.ChatLog.Add(new ChatLine { Role = "Bot", Message = CleanMessage(reply) });
            }

            UploadBotInterviewVideo(model, question.FirstOrDefault(), userMessage);

            var latestReply = CleanMessage(replies.FirstOrDefault());

            if (!string.IsNullOrEmpty(latestReply))
            {
                var audioBytes = await _speechToTextService.ConvertTextToSpeechAsync(latestReply);
                var base64Audio = Convert.ToBase64String(audioBytes);
                ViewData["AssistantAudioBase64"] = base64Audio;
            }


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

            model.IsAdaptive = true;
            return View(model);
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
                    var result = await _aiinterviewerService.UpdateBotInterviewVideoDetails(VideoLink, question, model.SessionId.ToString(), model.SynthesizedVideoUrl, candidateResponse, model.TimeLeftInSeconds, model.InterviewId);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> Report(Guid seId, bool redo = false)//Guid seId
        {
            // For demo: tech stack prompt
            //seId = Guid.Parse("85e5b7f6-a443-4a76-814b-f7c1c1e5aaf5");
            string candidatePrompt = "Consider the interview finished. I want you to generate a report for this interview, as directed in the instructions. Format your response for readability as this response will be fed as it is into a Razor (.cshtml) page.";
            //var aiReport = new InterviewReportResult();
            InterviewReportResult aiReport = null;
            if (redo)
            {
                aiReport = await _api.GenerateReport(seId, candidatePrompt);

                //save the report in db
                if (aiReport != null)
                {
                    await _aiinterviewerService.SaveReportOfAIInterviewByAssistant(seId, JsonConvert.SerializeObject(aiReport));

                }
            }
            else
            {
                //check if the report already exists and fetch if exists

                var aiReportJson = await _aiinterviewerService.GetReportOfAIInterviewByAssistant(seId);
                if (!string.IsNullOrEmpty(aiReportJson))
                {
                    aiReport = JsonConvert.DeserializeObject<InterviewReportResult>(aiReportJson);
                }
                if (aiReport == null)
                {
                    aiReport = await _api.GenerateReport(seId, candidatePrompt);

                    //aiReport.Replies = aiReport.Replies.FirstOrDefault().ToList();
                    var list = new List<string>();
                    list.Add(aiReport.Replies.FirstOrDefault());
                    aiReport.Replies = list;
                    //save the report in db
                    if (aiReport != null)
                    {
                        await _aiinterviewerService.SaveReportOfAIInterviewByAssistant(seId, JsonConvert.SerializeObject(aiReport));

                    }
                }
                
            }
                

            var chatMessageModel = new ChatMessageViewModel
            {
                SessionId = aiReport.SessionId,
                ChatLog = new List<ChatLine>
                {
                    new ChatLine
                    {
                        Role = "Bot",
                        Message = CleanMessage(aiReport.Replies.FirstOrDefault())
                    }
                }
            };

            AIInterviewReportWrapperClass reportModel = new AIInterviewReportWrapperClass();
            reportModel.videoQuestionDetails = await _aiinterviewerService.GetBotInterviewVideoDetails(seId);
            reportModel.chatMessageViewModel = chatMessageModel;
            //reportModel.TotalQuestions = result.Replies.Count() - 1;
            ViewBag.IsAdaptive = true;
            return View(reportModel);
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
    }
}
