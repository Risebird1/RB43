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

        public AIInterviewController(IConfiguration configuration, IAzureStorageHelper azureStorageHelper,
            IApplicationErrorLogger applicationErrorLogger, IInterviewApiService api, IOpenAISpeechToTextService speechToTextService, IFileMimeTypeHelper fileMimeTypeHelper, ISASUrlHelper sasUrlHelper)
        {
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _api = api;
            _speechToTextService = speechToTextService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _sasUrlHelper = sasUrlHelper;
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
                ChatLog = result.Replies.Select(r => new ChatLine { Role = "Bot", Message = r }).ToList()
            };

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
                model.ChatLog.Add(new ChatLine { Role = "Bot", Message = reply });
            }
            UploadBotInterviewVideo(model, question.FirstOrDefault());
            var latestReply = replies.FirstOrDefault();
            if (!string.IsNullOrEmpty(latestReply))
            {
                var audioBytes = await _speechToTextService.ConvertTextToSpeechAsync(latestReply);
                var base64Audio = Convert.ToBase64String(audioBytes);
                ViewData["AssistantAudioBase64"] = base64Audio;
            }

            model.Message = "";
            return View(model);
        }
        private async Task UploadBotInterviewVideo(ChatMessageViewModel model, string question)
        {
            if (model.VideoFile != null && model.VideoFile.Length > 0)
            {
                var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(Request.Form.Files[0]);
                if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4" || fileMimeType == "video/quicktime" || fileMimeType == "video/x-matroska")
                {
                    var file = Request.Form.Files[0];
                    var filename = model.SessionId.ToString().Replace(".", "_").Replace("@", "a").ToString() + DateTime.Now.ToLongTimeString().Replace(" ", "").Replace(".", "_") + ".mp4";
                    await _azureStorageHelper.InsertBlob(filename, "BotInterviewvideo", file);
                    var containerRef = _azureStorageHelper.GetCloudBlobContainer("BotInterviewvideo");
                    var blob = containerRef.GetBlockBlobReference(filename);
                    string uploadedvideoFileURL = blob.Uri.ToString();
                    var sas = _sasUrlHelper.CreateSasUrl("BotInterviewvideo");
                    var VideoLink = string.Empty;
                    if (!string.IsNullOrEmpty(uploadedvideoFileURL) && !string.IsNullOrEmpty(sas))
                    {
                        VideoLink = uploadedvideoFileURL + "?" + sas;
                    }
                    //Save in db with question.sessionid,videoLink,Datetime
                    //var result = await _interviewerService.UpdateVideoKYCDetails(VideoLink, emailId);
                }
            }
        }
    }
}
