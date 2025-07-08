using Google.Apis.Drive.v3.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public AIInterviewController(IConfiguration configuration, IAzureStorageHelper azureStorageHelper,
            IApplicationErrorLogger applicationErrorLogger, IInterviewApiService api,
            IOpenAISpeechToTextService speechToTextService, IFileMimeTypeHelper fileMimeTypeHelper,
            ISASUrlHelper sasUrlHelper, IAIInterviewerService aiinterviewerService,
            ISynthesiaTextToVideoService synthesiaTextToVideoService)
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
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // For demo: tech stack prompt
            string candidatePrompt = "You will now interview the candidate. Proceed as per the instructions fed to you.";
            var result = await _api.StartConversation(candidatePrompt);

            var model = new ChatMessageViewModel
            {
                SessionId = result.SessionId,
                ChatLog = result.Replies.Select(r => new ChatLine { Role = "Bot", Message = CleanMessage(r) }).ToList()
            };

            var videoId = await _synthesiaTextToVideoService.CreateVideoAsync(result.Replies.FirstOrDefault(), "your_template_id_here");

            // Wait/poll until video is ready (Synthesia usually takes 30–60s)
            string videoUrl;
            do
            {
                await Task.Delay(8000);
                videoUrl = await _synthesiaTextToVideoService.GetVideoUrlAsync(videoId);
            } while (string.IsNullOrWhiteSpace(videoUrl));

            model.SynthesizedVideoUrl = videoUrl;

            return View(model);
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
            var latestReply = replies.FirstOrDefault();
            //if (!string.IsNullOrEmpty(latestReply))
            //{
            //    var audioBytes = await _speechToTextService.ConvertTextToSpeechAsync(latestReply);
            //    var base64Audio = Convert.ToBase64String(audioBytes);
            //    ViewData["AssistantAudioBase64"] = base64Audio;
            //}


            var videoId = await _synthesiaTextToVideoService.CreateVideoAsync(latestReply, "your_template_id_here");

            // Wait/poll until video is ready (Synthesia usually takes 30–60s)
            string videoUrl;
            do
            {
                await Task.Delay(8000);
                videoUrl = await _synthesiaTextToVideoService.GetVideoUrlAsync(videoId);
            } while (string.IsNullOrWhiteSpace(videoUrl));
            model.SynthesizedVideoUrl = videoUrl;
            model.Message = "";
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
                    var result = await _aiinterviewerService.UpdateBotInterviewVideoDetails(VideoLink, question, model.SessionId.ToString(), model.SynthesizedVideoUrl, candidateResponse);
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> Report(Guid seId)//Guid seId
        {
            // For demo: tech stack prompt
            //seId = Guid.Parse("85e5b7f6-a443-4a76-814b-f7c1c1e5aaf5");
            string candidatePrompt = "Consider the interview finished. I want you to generate a report for this interview, as directed in the instructions. Format your response for readability as this response will be fed as it is into a Razor (.cshtml) page.";
            var result = await _api.GenerateReport(seId, candidatePrompt);

            //var chatMessageModel = new ChatMessageViewModel
            //{
            //    SessionId = result.SessionId,
            //    ChatLog = result.Replies.Select(r => new ChatLine { Role = "Bot", Message = CleanMessage(r) }).ToList()
            //};

            var chatMessageModel = new ChatMessageViewModel
            {
                SessionId = result.SessionId,
                ChatLog = new List<ChatLine>
                {
                    new ChatLine
                    {
                        Role = "Bot",
                        Message = CleanMessage(result.Replies.FirstOrDefault())
                    }
                }
            };

            AIInterviewReportWrapperClass reportModel = new AIInterviewReportWrapperClass();
            reportModel.videoQuestionDetails = await _aiinterviewerService.GetBotInterviewVideoDetails(seId);
            reportModel.chatMessageViewModel = chatMessageModel;
            //reportModel.TotalQuestions = result.Replies.Count() - 1;
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

    }
}
