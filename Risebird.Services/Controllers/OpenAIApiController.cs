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

        /// <summary>
        /// Splits a skill channel's two AI-interview prompts into the two jobs they do.
        /// The Primary Prompt ID conducts the interview - for adaptive interviews it is the question
        /// generator. The Interview Evaluator Prompt ID generates the candidate's report, and is
        /// optional: when it is empty the Primary Prompt ID generates the report too.
        /// Either may come back null, in which case the helper layer falls back to the globally
        /// configured prompt, exactly as it did before this split.
        /// </summary>
        private static (string PrimaryPromptId, string ReportPromptId) ResolvePromptIds(string primaryPromptId, string evaluatorPromptId)
        {
            var primary = string.IsNullOrWhiteSpace(primaryPromptId) ? null : primaryPromptId.Trim();
            var evaluator = string.IsNullOrWhiteSpace(evaluatorPromptId) ? null : evaluatorPromptId.Trim();
            return (primary, evaluator ?? primary);
        }

        /// <summary>
        /// Looks up <see cref="ResolvePromptIds"/> for the skill channel behind an interview session.
        /// </summary>
        private async Task<(string PrimaryPromptId, string ReportPromptId)> ResolveSessionPromptIds(Guid sessionId)
        {
            var details = await _aIInterviewerService.GetAIInterviewSpecificDetails(sessionId);
            return ResolvePromptIds(details?.OpenAIPromptId, details?.OpenAIEvaluatorPromptId);
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] InterviewMessageVM input) //string candidateContext
        {
            // InterviewMeetings.SessionId is this session, which is how the cost is tied to the interview.
            using var aiCostScope = OpenAIApiLogScope.Begin(sessionId: input?.SessionId);
            try
            {
                _assistantId = input.AssistantId;
                if (await UseAIInterviewerResponsesApiAsync())
                {
                    var (primaryPromptId, _) = await ResolveSessionPromptIds(input.SessionId);
                    var (runResult, responseId) = await _assistantServiceHelper.GenerateAIInterviewerMessage(input.Message, promptId: primaryPromptId);
                    if (runResult == null || string.IsNullOrWhiteSpace(responseId))
                        return StatusCode(500, "AI interviewer returned an empty response.");

                    await _aiInterviewerRepository.CreateSession(new InterviewSession
                    {
                        SessionId = input.SessionId,
                        ThreadId = responseId,
                        AssistantId = _assistantId,
                        CandidateEmail = ""
                    });

                    var responseReplies = await SanitizeSpokenRepliesAsync(ExtractRunResultReplies(runResult), input.SessionId);
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
            using var aiCostScope = OpenAIApiLogScope.Begin(sessionId: input?.SessionId);
            if (await UseAIInterviewerResponsesApiAsync())
            {
                var previousResponseId = await _aiInterviewerRepository.GetThreadId(input.SessionId);
                await _aiInterviewerRepository.LogTranscript(input.SessionId, "user", input.Message);

                var (primaryPromptId, _) = await ResolveSessionPromptIds(input.SessionId);
                var (runResult, responseId) = await _assistantServiceHelper.GenerateAIInterviewerMessage(input.Message, previousResponseId, primaryPromptId);
                if (runResult == null || string.IsNullOrWhiteSpace(responseId))
                    return StatusCode(500, "AI interviewer returned an empty response.");

                await _aiInterviewerRepository.CreateSession(new InterviewSession
                {
                    SessionId = input.SessionId,
                    ThreadId = responseId,
                    AssistantId = input.AssistantId,
                    CandidateEmail = ""
                });

                var responseReplies = await SanitizeSpokenRepliesAsync(ExtractRunResultReplies(runResult), input.SessionId);
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
            using var aiCostScope = OpenAIApiLogScope.Begin(sessionId: input?.SessionId);
            if (await UseAIInterviewerResponsesApiAsync())
            {
                var previousResponseId = await _aiInterviewerRepository.GetThreadId(input.SessionId);
                var (primaryPromptId, _) = await ResolveSessionPromptIds(input.SessionId);
                var (runResult, responseId) = await _assistantServiceHelper.GenerateAIInterviewerMessage(input.Message, previousResponseId, primaryPromptId);
                if (runResult == null || string.IsNullOrWhiteSpace(responseId))
                    return StatusCode(500, "AI interviewer returned an empty response.");

                await _aiInterviewerRepository.CreateSession(new InterviewSession
                {
                    SessionId = input.SessionId,
                    ThreadId = responseId,
                    AssistantId = input.AssistantId,
                    CandidateEmail = ""
                });

                var responseReplies = await SanitizeSpokenRepliesAsync(ExtractRunResultReplies(runResult), input.SessionId);
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
            if (await UseAIInterviewerResponsesApiAsync())
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
            using var aiCostScope = OpenAIApiLogScope.Begin(sessionId: input?.SessionId);
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
                    // The report is written by the Interview Evaluator Prompt ID. It is an optional
                    // field, so an empty one hands the job back to the Primary Prompt ID; only when
                    // the channel carries neither does the globally configured prompt take over.
                    var (_, reportPromptId) = ResolvePromptIds(meetingDetails.OpenAIPromptId, meetingDetails.OpenAIEvaluatorPromptId);
                    var evaluationPromptId = reportPromptId ?? _config["OpenAI:AIInterviewPromptMap"];
                    var useResponsesApi = true//_config.GetValue("OpenAI:UseResponsesApi", false)
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
                // Hoisted out of the branch below so the diagnostics that run after the report is read
                // can name the prompt that produced it. On the legacy path it stays null.
                string reportPromptId = null;
                if (await UseAIInterviewerResponsesApiAsync())
                {
                    // Closing turn of an adaptive interview: the transcript is already chained onto
                    // threadId, and it is the Interview Evaluator Prompt ID that reads it and writes the
                    // report. With no evaluator configured, the Primary Prompt ID that conducted the
                    // interview reports on it too - the behaviour adaptive channels had before the split.
                    (_, reportPromptId) = ResolvePromptIds(aiInterviewDetails?.OpenAIPromptId, aiInterviewDetails?.OpenAIEvaluatorPromptId);
                    var (runResult, responseId) = await _assistantServiceHelper.GenerateAIInterviewerMessage(
                        input.Message, threadId, reportPromptId, expectEvaluationReport: true);
                    if (runResult == null || string.IsNullOrWhiteSpace(responseId))
                    {
                        // Used to return here with no trace. The candidate has finished the interview at
                        // this point, so a report that never arrives looks to everyone downstream like an
                        // interview that was simply never evaluated.
                        await _applicationErrorLogger.LogMessage(
                            LogLevelInfo.Error,
                            $"AI interview session {input.SessionId}: the evaluator returned nothing, so no " +
                            $"report was stored. Evaluator prompt id used: {reportPromptId ?? "(none - fell back to the global prompt)"}.");
                        return Ok(new { input.SessionId, threadId, replies = (List<string>)null, isReportSavedToDb = false });
                    }

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

                            // The report is stored either way, but a screening report scores its two
                            // sections by matching rubric_score_summary entries to the channel's
                            // parameters. With none present it renders as empty sections and reads as
                            // "the interview was never evaluated" rather than "the evaluator drifted".
                            if (aIInterviewEvaluation.RubricScoreSummary == null
                                || aIInterviewEvaluation.RubricScoreSummary.Count == 0)
                            {
                                await _applicationErrorLogger.LogMessage(
                                    LogLevelInfo.Error,
                                    $"AI interview session {input.SessionId}: the evaluator returned a report " +
                                    "with no rubric_score_summary, so no parameter can be scored and the " +
                                    "report's section blocks will be empty. Check that the Interview " +
                                    "Evaluator Prompt Id emits one entry per rubric parameter.");
                            }
                        }
                        else
                        {
                            // Neither shape parsed. Nothing downstream can read this report, and until
                            // now that happened without a word anywhere.
                            await _applicationErrorLogger.LogMessage(
                                LogLevelInfo.Error,
                                $"AI interview session {input.SessionId}: the evaluator's reply is neither an " +
                                "adaptive verdict block nor a parseable evaluation, so InterviewStatus and " +
                                "the report's scores are both unset. The raw reply is stored on " +
                                $"InterviewComments.ReportOfAIInterviewByAssistant. Evaluator prompt id used: {reportPromptId ?? "(none - fell back to the global prompt)"}.");
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

        // Logged once per reason per process. This is consulted on every turn of every interview,
        // so logging unconditionally would bury the log; logging never at all is how an environment
        // ends up silently running every adaptive interview on the deprecated assistants path with
        // nobody aware the Responses path was switched off.
        private static int _responsesApiDisabledLogged;
        private static int _responsesApiNoGlobalPromptLogged;

        /// <summary>
        /// Whether the adaptive interview runs on the Responses API. Both conditions must hold, and
        /// either one failing silently reroutes the whole environment onto the legacy assistants
        /// path - so each reason is reported once rather than not at all.
        /// </summary>
        private async Task<bool> UseAIInterviewerResponsesApiAsync()
        {
            if (!_config.GetValue("OpenAI:UseResponsesApi", false))
            {
                if (Interlocked.Exchange(ref _responsesApiDisabledLogged, 1) == 0)
                {
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Warning,
                        "AI interviewer is running on the legacy assistants path: OpenAI:UseResponsesApi " +
                        "is false. Adaptive interviews and their reports will not use channel-level " +
                        "Primary / Interview Evaluator Prompt Ids, so screening reports cannot produce " +
                        "section scores. This is logged once per process.");
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config["OpenAI:AIInterviewerPromptId"]))
            {
                if (Interlocked.Exchange(ref _responsesApiNoGlobalPromptLogged, 1) == 0)
                {
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Warning,
                        "AI interviewer is running on the legacy assistants path: OpenAI:UseResponsesApi " +
                        "is true but OpenAI:AIInterviewerPromptId is empty, which switches the Responses " +
                        "path off even for channels that carry their own prompt ids. This is logged once " +
                        "per process.");
                }
                return false;
            }

            return true;
        }

        private static List<string> ExtractRunResultReplies(RunResult result)
        {
            var text = result?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return new List<string> { text };
        }

        /// <summary>
        /// Keeps report JSON out of the turns a candidate hears.
        ///
        /// An adaptive interview and its report run on ONE chained conversation, and a channel with
        /// no Interview Evaluator Prompt Id uses the same prompt for both jobs. If that prompt emits
        /// its evaluation JSON part-way through, the raw blob is spoken aloud by text-to-speech and
        /// stored as the question text on the report — which is what candidates were hearing.
        ///
        /// Applied ONLY to conversational turns; the report endpoints need the JSON intact. In a
        /// healthy interview no turn contains these keys, so this does nothing — but it warns when
        /// it fires, so a misbehaving prompt stays visible rather than quietly papered over.
        /// </summary>
        private async Task<List<string>> SanitizeSpokenRepliesAsync(List<string> replies, Guid sessionId)
        {
            if (replies == null || replies.Count == 0) return replies;

            var sanitized = new List<string>(replies.Count);
            var warned = false;

            foreach (var reply in replies)
            {
                var cleaned = AIInterviewReplySanitizer.StripOrFallback(reply, out var stripped);

                if (stripped && !warned)
                {
                    warned = true;
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Warning,
                        $"AI interview session {sessionId}: the interviewing prompt returned report JSON " +
                        "in a conversational turn. It was removed before reaching the candidate. Give this " +
                        "channel a Primary Prompt Id that only asks questions, or a separate Interview " +
                        "Evaluator Prompt Id, so the interview and the report stop sharing one prompt.");
                }

                if (!string.IsNullOrWhiteSpace(cleaned))
                    sanitized.Add(cleaned);
            }

            return sanitized;
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
