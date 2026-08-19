using Google.Apis.Drive.v3.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Azure;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.OpenAIModels;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IAIInterviewerRepository _aiInterviewerRepository;
        private readonly IAIInterviewerService _aIInterviewerService;
        private readonly IConfiguration _config;
        private string _assistantId;

        public OpenAIApiController(IApplicationErrorLogger applicationErrorLogger, IOpenAIInterviewerAssistantHelper ai,
            IAIInterviewerRepository aiInterviewerRepository, IConfiguration config, IAIInterviewerService aIInterviewerService,
            IAssistantServiceHelper assistantServiceHelper)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _aiInterviewerRepository = aiInterviewerRepository;
            _ai = ai;
            _config = config;
            //_assistantId = _config["OpenAI:AIInterviewerAssistantId"];
            _aIInterviewerService = aIInterviewerService;
            _assistantServiceHelper = assistantServiceHelper;
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] InterviewMessageVM input) //string candidateContext
        {
            try
            {
                _assistantId = input.AssistantId;
                if (UseAIInterviewerResponsesApi())
                {
                    var (runResult, responseId) = await _assistantServiceHelper.GenerateAIInterviewerMessage(input.Message);
                    if (runResult == null || string.IsNullOrWhiteSpace(responseId))
                        return StatusCode(500, "AI interviewer returned an empty response.");

                    await _aiInterviewerRepository.CreateSession(new InterviewSession
                    {
                        SessionId = input.SessionId,
                        ThreadId = responseId,
                        AssistantId = _assistantId,
                        CandidateEmail = ""
                    });

                    var responseReplies = ExtractRunResultReplies(runResult);
                    if (responseReplies != null && responseReplies.Count > 0)
                        await _aiInterviewerRepository.LogTranscript(input.SessionId, "assistant", responseReplies.FirstOrDefault());

                    return Ok(new { input.SessionId, threadId = responseId, replies = responseReplies });
                }

                //var sessionId = Guid.NewGuid();
                var threadId = await _ai.CreateThreadAsync();

                await _aiInterviewerRepository.CreateSession(new InterviewSession
                {
                    SessionId = input.SessionId,
                    ThreadId = threadId,
                    AssistantId = _assistantId,
                    CandidateEmail = ""
                });

                await _ai.AddMessageAsync(threadId, "user", input.Message);

                var runId = await _ai.CreateRunAsync(threadId, _assistantId);
                await _ai.WaitForRunAsync(threadId, runId);

                var replies = await _ai.GetMessagesAsync(threadId);

                if (replies != null && replies.Count() > 0)
                {
                    await _aiInterviewerRepository.LogTranscript(input.SessionId, "assistant", replies.FirstOrDefault());

                }
                //foreach (var reply in replies)
                //{
                //    await _aiInterviewerRepository.LogTranscript(input.SessionId, "assistant", reply);
                //}

                return Ok(new { input.SessionId, threadId, replies });
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Something went wrong: {ex.Message}");
            }
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] InterviewMessageVM input)
        {
            if (UseAIInterviewerResponsesApi())
            {
                var previousResponseId = await _aiInterviewerRepository.GetThreadId(input.SessionId);
                await _aiInterviewerRepository.LogTranscript(input.SessionId, "user", input.Message);

                var (runResult, responseId) = await _assistantServiceHelper.GenerateAIInterviewerMessage(input.Message, previousResponseId);
                if (runResult == null || string.IsNullOrWhiteSpace(responseId))
                    return StatusCode(500, "AI interviewer returned an empty response.");

                await _aiInterviewerRepository.CreateSession(new InterviewSession
                {
                    SessionId = input.SessionId,
                    ThreadId = responseId,
                    AssistantId = input.AssistantId,
                    CandidateEmail = ""
                });

                var responseReplies = ExtractRunResultReplies(runResult);
                if (responseReplies != null && responseReplies.Count > 0)
                    await _aiInterviewerRepository.LogTranscript(input.SessionId, "assistant", responseReplies.FirstOrDefault());

                return Ok(new { replies = responseReplies });
            }

            var threadId = await _aiInterviewerRepository.GetThreadId(input.SessionId);
            await _ai.AddMessageAsync(threadId, "user", input.Message);
            await _aiInterviewerRepository.LogTranscript(input.SessionId, "user", input.Message);

            var runId = await _ai.CreateRunAsync(threadId, input.AssistantId);
            await _ai.WaitForRunAsync(threadId, runId);

            var replies = await _ai.GetMessagesAsync(threadId);

            if (replies != null && replies.Count() > 0)
            {
                await _aiInterviewerRepository.LogTranscript(input.SessionId, "assistant", replies.FirstOrDefault());

            }
            //foreach (var reply in replies)
            //{
            //    await _aiInterviewerRepository.LogTranscript(input.SessionId, "assistant", reply);
            //}

            return Ok(new { replies });
        }

        /// <summary>
        /// Like Ask, but for internal/system-level prompts sent to OpenAI during recovery or initialisation.
        /// The system prompt itself is NOT logged to TransCriptsConv; only the assistant's reply is logged.
        /// This prevents internal restart/context prompts from polluting the candidate transcript.
        /// </summary>
        [HttpPost("asksystem")]
        public async Task<IActionResult> AskSystem([FromBody] InterviewMessageVM input)
        {
            if (UseAIInterviewerResponsesApi())
            {
                var previousResponseId = await _aiInterviewerRepository.GetThreadId(input.SessionId);
                var (runResult, responseId) = await _assistantServiceHelper.GenerateAIInterviewerMessage(input.Message, previousResponseId);
                if (runResult == null || string.IsNullOrWhiteSpace(responseId))
                    return StatusCode(500, "AI interviewer returned an empty response.");

                await _aiInterviewerRepository.CreateSession(new InterviewSession
                {
                    SessionId = input.SessionId,
                    ThreadId = responseId,
                    AssistantId = input.AssistantId,
                    CandidateEmail = ""
                });

                var responseReplies = ExtractRunResultReplies(runResult);
                if (responseReplies != null && responseReplies.Count > 0)
                    await _aiInterviewerRepository.LogTranscript(input.SessionId, "assistant", responseReplies.FirstOrDefault());

                return Ok(new { replies = responseReplies });
            }

            var threadId = await _aiInterviewerRepository.GetThreadId(input.SessionId);
            await _ai.AddMessageAsync(threadId, "user", input.Message);
            // Intentionally NOT logging the system prompt to TransCriptsConv.

            var runId = await _ai.CreateRunAsync(threadId, input.AssistantId);
            await _ai.WaitForRunAsync(threadId, runId);

            var replies = await _ai.GetMessagesAsync(threadId);

            if (replies != null && replies.Count() > 0)
            {
                await _aiInterviewerRepository.LogTranscript(input.SessionId, "assistant", replies.FirstOrDefault());
            }

            return Ok(new { replies });
        }

        [HttpGet("transcript/{sessionId}")]
        public async Task<IActionResult> Transcript(Guid sessionId)
        {
            if (UseAIInterviewerResponsesApi())
            {
                var dbMessages = await _aIInterviewerService.GetAIInterviewTranscriptBySessionId(sessionId);
                return Ok(dbMessages);
            }

            var threadId = await _aiInterviewerRepository.GetThreadId(sessionId);
            var messages = await _ai.GetFullConversation(sessionId, threadId);
            return Ok(messages);
        }

        [HttpPost("report")]
        public async Task<IActionResult> Report([FromBody] AIReportVM input)
        {
            bool isReportSavedToDb = false;
            //var threadId = await _aIInterviewerService.GetThreadId(input.SessionId);
            if (input?.AIInterviewType == (int)AIInterviewTypeEnum.Preset)
            {
                var meetingDetails = await _aIInterviewerService.GetAIInterviewDetailsBySessionId(input.SessionId);

                if (string.IsNullOrEmpty(meetingDetails.QuestionAnswerSetInJson) && input.isExit)
                {
                    //mark as no show
                    isReportSavedToDb = await _aIInterviewerService.SaveReportOfAIInterviewByAssistant(input.SessionId, null, input.isExit, null, null, 1);
                    return Ok(new InterviewReportResult { SessionId = input.SessionId, IsReportSavedToDB = true });
                }
                else if ((!string.IsNullOrEmpty(meetingDetails.QuestionAnswerSetInJson)))
                {
                    string threadId = null;
                    List<string> replies = null;
                    var evaluationPromptId = !string.IsNullOrWhiteSpace(meetingDetails.OpenAIPromptId)
                        ? meetingDetails.OpenAIPromptId.Trim()
                        : _config["OpenAI:AIInterviewPromptMap"];
                    var useResponsesApi = _config.GetValue("OpenAI:UseResponsesApi", false)
                        && !string.IsNullOrWhiteSpace(evaluationPromptId);

                    if (useResponsesApi)
                    {
                        var runResult = await _assistantServiceHelper.GenerateAIInterviewEvaluation(
                            meetingDetails.TechSkillsInJson,
                            meetingDetails.QuestionAnswerSetInJson,
                            evaluationPromptId);
                        replies = ExtractRunResultReplies(runResult);
                    }
                    else
                    {
                        threadId = await _ai.CreateThreadAsync();
                        input.Message = $"Generate an output in json based on the inputs. {AIInterviewVerdict.OutputSchemaInstruction} The techrubrics in json are as follows:{meetingDetails.TechSkillsInJson}. The mapped_rubric_parameters, question, candidate's answer and ideal answer set object in json are as follows: {meetingDetails.QuestionAnswerSetInJson}";
                        await _ai.AddMessageAsync(threadId, "user", input.Message);

                        var runId = await _ai.CreateRunAsync(threadId, meetingDetails.OpenAIAssistantId);
                        await _ai.WaitForRunAsync(threadId, runId);

                        replies = await _ai.GetMessagesAsync(threadId);

                        int counter = 0;
                        if (replies != null && replies.Count() == 0)
                        {
                            do
                            {
                                await Task.Delay(4000);
                                replies = await _ai.GetMessagesAsync(threadId);
                                counter++;
                            }
                            while (counter < 2 && replies != null && replies.Count() == 0);
                        }
                    }
                    if (replies != null)//&& replies.Count() > 0
                    {
                        var reportObject = new InterviewReportResult { SessionId = input.SessionId, ThreadId = threadId, Replies = replies };
                        //save the report in the database.
                        var aIInterviewEvaluation = TryParseEvaluation(replies?.FirstOrDefault());
                        int? interviewStatus = null;
                        string interviewerComment = string.Empty;
                        if (!string.IsNullOrWhiteSpace(meetingDetails.QuestionAnswerSetInJson))
                        {
                            if (aIInterviewEvaluation != null)
                            {
                                if (AIInterviewVerdict.IsSelectFamily(aIInterviewEvaluation.FinalVerdict))
                                {
                                    interviewStatus = 2;
                                }
                                else if (AIInterviewVerdict.IsRejectFamily(aIInterviewEvaluation.FinalVerdict))
                                {
                                    interviewStatus = 3;
                                }
                                interviewerComment = aIInterviewEvaluation.FinalComment;
                            }
                        }
                        int? meetingStatus = null;
                        if (input.isExit)
                        {
                            if (interviewStatus == 2 || interviewStatus == 3)
                            {
                                meetingStatus = 4;
                            }
                            else
                            {
                                meetingStatus = 3; //dropped
                            }
                        }
                        isReportSavedToDb = await _aIInterviewerService.SaveReportOfAIInterviewByAssistant(input.SessionId, JsonConvert.SerializeObject(reportObject), input.isExit, interviewStatus, interviewerComment, meetingStatus);

                    }


                    return Ok(new { input.SessionId, threadId, replies, isReportSavedToDb });
                }
                else
                {
                    // QuestionAnswerSetInJson is empty and isExit is false — no Q&A data to evaluate.
                    // Still save what we can (ensure InterviewComments row exists) and return empty result.
                    isReportSavedToDb = await _aIInterviewerService.SaveReportOfAIInterviewByAssistant(input.SessionId, null, input.isExit);
                    return Ok(new InterviewReportResult { SessionId = input.SessionId, IsReportSavedToDB = isReportSavedToDb });
                }



            }
            else
            {
                var aiInterviewDetails = await _aIInterviewerService.GetAIInterviewSpecificDetails(input.SessionId);

                var threadId = aiInterviewDetails?.ThreadId;
                List<string> replies;
                if (UseAIInterviewerResponsesApi())
                {
                    var (runResult, responseId) = await _assistantServiceHelper.GenerateAIInterviewerMessage(input.Message, threadId);
                    if (runResult == null || string.IsNullOrWhiteSpace(responseId))
                        return Ok(new { input.SessionId, threadId, replies = (List<string>)null, isReportSavedToDb = false });

                    await _aiInterviewerRepository.CreateSession(new InterviewSession
                    {
                        SessionId = input.SessionId,
                        ThreadId = responseId,
                        AssistantId = aiInterviewDetails?.AssistantId,
                        CandidateEmail = ""
                    });

                    threadId = responseId;
                    replies = ExtractRunResultReplies(runResult);
                }
                else
                {
                    await _ai.AddMessageAsync(threadId, "user", input.Message);

                    var runId = await _ai.CreateRunAsync(threadId, aiInterviewDetails?.AssistantId);
                    await _ai.WaitForRunAsync(threadId, runId);

                    replies = await _ai.GetMessagesAsync(threadId); // all these replies here are in response to the prompt above. Therefore all consitute the report that we asked for in the prompt. Send them all back.
                }

                //save the report in the database.
                if (replies != null)
                {
                    var rawReply = replies.FirstOrDefault();

                    int? interviewStatus = null;
                    string interviewerComment = null;
                    // Adaptive reports are ready-to-render HTML, so they can't be parsed into the structured
                    // AIInterviewEvaluation model. To still persist InterviewStatus/InterviewerComment, the assistant
                    // appends a small machine-readable verdict block (an HTML comment) at the end of the report. Extract
                    // it here to populate the columns, and strip it from the stored HTML so viewers never see it.
                    var verdictBlock = ExtractAdaptiveVerdictBlock(rawReply);
                    if (verdictBlock != null)
                    {
                        if (AIInterviewVerdict.IsSelectFamily(verdictBlock.Verdict))
                        {
                            interviewStatus = 2;
                        }
                        else if (AIInterviewVerdict.IsRejectFamily(verdictBlock.Verdict))
                        {
                            interviewStatus = 3;
                        }
                        interviewerComment = verdictBlock.Comment;
                        rawReply = verdictBlock.CleanedHtml;
                    }
                    else
                    {
                        // Fallback: if the assistant ever returns a fully-structured JSON report, use that verdict.
                        var aIInterviewEvaluation = TryParseEvaluation(rawReply);
                        if (aIInterviewEvaluation != null)
                        {
                            if (AIInterviewVerdict.IsSelectFamily(aIInterviewEvaluation.FinalVerdict))
                            {
                                interviewStatus = 2;
                            }
                            else if (AIInterviewVerdict.IsRejectFamily(aIInterviewEvaluation.FinalVerdict))
                            {
                                interviewStatus = 3;
                            }
                            interviewerComment = aIInterviewEvaluation.FinalComment;
                        }
                    }

                    var aiReport = new InterviewReportResult
                    {
                        Replies = new List<string> { rawReply },
                        SessionId = input.SessionId,
                        ThreadId = threadId
                    };

                    // Intentionally pass meetingStatus = null. The adaptive completion flow has already set
                    // MeetingStatus = 4 (completed) before this report is generated. Deriving a meetingStatus from
                    // isExit here (as the preset path does) would overwrite that completed status - e.g. to 3/dropped
                    // whenever no select/reject verdict is available.
                    isReportSavedToDb = await _aIInterviewerService.SaveReportOfAIInterviewByAssistant(input.SessionId, JsonConvert.SerializeObject(aiReport), input.isExit, interviewStatus, interviewerComment, null);

                }

                return Ok(new { input.SessionId, threadId, replies, isReportSavedToDb });
            }

        }

        private bool UseAIInterviewerResponsesApi()
        {
            return _config.GetValue("OpenAI:UseResponsesApi", false)
                && !string.IsNullOrWhiteSpace(_config["OpenAI:AIInterviewerPromptId"]);
        }

        private static List<string> ExtractRunResultReplies(RunResult result)
        {
            var text = result?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return new List<string> { text };
        }

        private AIInterviewEvaluation TryParseEvaluation(string raw)
            => AIInterviewEvaluation.TryParse(raw);

        /// <summary>
        /// Extracts the machine-readable verdict block the adaptive assistant appends to its HTML report
        /// (format: <![CDATA[<!--RB_EVAL {"final_verdict":"...","final_comment":"..."} RB_EVAL-->]]>), and returns the
        /// parsed verdict/comment along with the HTML stripped of that block. Returns null when no block is present.
        /// </summary>
        private static AdaptiveVerdictBlock ExtractAdaptiveVerdictBlock(string rawReply)
        {
            if (string.IsNullOrEmpty(rawReply)) return null;

            var match = Regex.Match(
                rawReply,
                @"<!--\s*RB_EVAL\s*(?<json>\{.*?\})\s*RB_EVAL\s*-->",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (!match.Success) return null;

            var cleanedHtml = rawReply.Remove(match.Index, match.Length).Trim();

            try
            {
                var json = JObject.Parse(match.Groups["json"].Value);
                var verdict = ((string)(json["final_verdict"] ?? json["finalVerdict"] ?? json["verdict"]))?.Trim();
                var comment = (string)(json["final_comment"] ?? json["finalComment"] ?? json["comment"]);

                return new AdaptiveVerdictBlock
                {
                    Verdict = AIInterviewVerdict.Normalize(verdict),
                    Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                    CleanedHtml = cleanedHtml
                };
            }
            catch
            {
                // Malformed block: still strip it from the HTML so it never renders, but leave the columns unset.
                return new AdaptiveVerdictBlock { CleanedHtml = cleanedHtml };
            }
        }

        private sealed class AdaptiveVerdictBlock
        {
            public string Verdict { get; set; }
            public string Comment { get; set; }
            public string CleanedHtml { get; set; }
        }
    }
}
