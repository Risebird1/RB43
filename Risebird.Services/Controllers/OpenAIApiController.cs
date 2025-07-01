using Google.Apis.Drive.v3.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Newtonsoft.Json;
using RestSharp;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Risebird.Services.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OpenAIApiController : ControllerBase
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IOpenAIInterviewerAssistantHelper _ai;
        private readonly IAIInterviewerRepository _aiInterviewerRepository;
        private readonly IAIInterviewerService _aIInterviewerService;
        private readonly IConfiguration _config;
        private readonly string _assistantId;

        public OpenAIApiController(IApplicationErrorLogger applicationErrorLogger, IOpenAIInterviewerAssistantHelper ai,
            IAIInterviewerRepository aiInterviewerRepository, IConfiguration config, IAIInterviewerService aIInterviewerService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _aiInterviewerRepository = aiInterviewerRepository;
            _ai = ai;
            _config = config;
            _assistantId = _config["OpenAI:AIInterviewerAssistantId"];
            _aIInterviewerService = aIInterviewerService;
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] string candidateContext)
        {
            try
            {
                var sessionId = Guid.NewGuid();
                var threadId = await _ai.CreateThreadAsync();

                await _aiInterviewerRepository.CreateSession(new InterviewSession
                {
                    SessionId = sessionId,
                    ThreadId = threadId,
                    AssistantId = _assistantId,
                    CandidateEmail = ""
                });

                await _ai.AddMessageAsync(threadId, "user", candidateContext);

                var runId = await _ai.CreateRunAsync(threadId);
                await _ai.WaitForRunAsync(threadId, runId);

                var replies = await _ai.GetMessagesAsync(threadId);
                foreach (var reply in replies)
                {
                    await _aiInterviewerRepository.LogTranscript(sessionId, "assistant", reply);
                }

                return Ok(new { sessionId, threadId, replies });
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Something went wrong: {ex.Message}");
            }
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] InterviewMessageVM input)
        {
            var threadId = await _aiInterviewerRepository.GetThreadId(input.SessionId);
            await _ai.AddMessageAsync(threadId, "user", input.Message);
            await _aiInterviewerRepository.LogTranscript(input.SessionId, "user", input.Message);

            var runId = await _ai.CreateRunAsync(threadId);
            await _ai.WaitForRunAsync(threadId, runId);

            var replies = await _ai.GetMessagesAsync(threadId);
            foreach (var reply in replies)
            {
                await _aiInterviewerRepository.LogTranscript(input.SessionId, "assistant", reply);
            }

            return Ok(new { replies });
        }

        [HttpGet("transcript/{sessionId}")]
        public async Task<IActionResult> Transcript(Guid sessionId)
        {
            var threadId = await _aiInterviewerRepository.GetThreadId(sessionId);
            var messages = await _ai.GetFullConversation(sessionId, threadId);
            return Ok(messages);
        }

        [HttpPost("report")]
        public async Task<IActionResult> Report([FromBody] AIReportVM input)
        {
            var threadId = await _aIInterviewerService.GetThreadId(input.SessionId);
            await _ai.AddMessageAsync(threadId, "user", input.Message);
            //await _aiInterviewerRepository.LogTranscript(input.SessionId, "user", input.Message);

            var runId = await _ai.CreateRunAsync(threadId);
            await _ai.WaitForRunAsync(threadId, runId);

            var replies = await _ai.GetMessagesAsync(threadId); // all these replies here are in response to the prompt above. Therefore all consitute the report that we asked for in the prompt. Send them all back.

            //save the report in the database.


            //foreach (var reply in replies)
            //{
            //    await _aiInterviewerRepository.LogTranscript(input.SessionId, "assistant", reply);
            //}

            return Ok(new { input.SessionId, threadId, replies });
        }

    }
}
