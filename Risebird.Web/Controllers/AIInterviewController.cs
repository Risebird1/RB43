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


        public AIInterviewController(IConfiguration configuration, IAzureStorageHelper azureStorageHelper,
            IApplicationErrorLogger applicationErrorLogger, IInterviewApiService api, IOpenAISpeechToTextService speechToTextService)
        {
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _api = api;
            _speechToTextService = speechToTextService;
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
        public async Task<IActionResult> Index(ChatMessageViewModel model)
        {

            string userMessage = model.Message;
            if (model.AudioFile != null && model.AudioFile.Length > 0)
            {
                using var stream = model.AudioFile.OpenReadStream();
                userMessage = await _speechToTextService.TranscribeAsync(stream, model.AudioFile.FileName);
            }
            var replies = await _api.SendReply(model.SessionId, userMessage);

            model.ChatLog.Add(new ChatLine { Role = "User", Message = userMessage });
            foreach (var reply in replies)
            {
                model.ChatLog.Add(new ChatLine { Role = "Bot", Message = reply });
            }

            model.Message = "";
            return View(model);
        }
    }
}
