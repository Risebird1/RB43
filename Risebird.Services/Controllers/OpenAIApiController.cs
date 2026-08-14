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
                    var evaluationPromptId = _config["OpenAI:AIInterviewPromptMap"];
                    var useResponsesApi = _config.GetValue("OpenAI:UseResponsesApi", true)
                        && !string.IsNullOrWhiteSpace(evaluationPromptId);

                    if (useResponsesApi)
                    {
                        var runResult = await _assistantServiceHelper.GenerateAIInterviewEvaluation(
                            meetingDetails.TechSkillsInJson,
                            meetingDetails.QuestionAnswerSetInJson,
                            meetingDetails.OpenAIAssistantId);
                        replies = ExtractRunResultReplies(runResult);
                    }
                    else
                    {
                        threadId = await _ai.CreateThreadAsync();
                        input.Message = $"Generate an output in json based on the inputs. The techrubrics in json are as follows:{meetingDetails.TechSkillsInJson}. The mapped_rubric_parameters, question, candidate's answer and ideal answer set object in json are as follows: {meetingDetails.QuestionAnswerSetInJson}";
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
                                if (aIInterviewEvaluation.FinalVerdict?.ToLower() == "select")
                                {
                                    interviewStatus = 2;
                                }
                                else if (aIInterviewEvaluation.FinalVerdict?.ToLower() == "reject")
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
                await _ai.AddMessageAsync(threadId, "user", input.Message);

                var runId = await _ai.CreateRunAsync(threadId, aiInterviewDetails?.AssistantId);
                await _ai.WaitForRunAsync(threadId, runId);

                var replies = await _ai.GetMessagesAsync(threadId); // all these replies here are in response to the prompt above. Therefore all consitute the report that we asked for in the prompt. Send them all back.

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
                        if (verdictBlock.Verdict == "select")
                        {
                            interviewStatus = 2;
                        }
                        else if (verdictBlock.Verdict == "reject")
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
                            if (aIInterviewEvaluation.FinalVerdict?.ToLower() == "select")
                            {
                                interviewStatus = 2;
                            }
                            else if (aIInterviewEvaluation.FinalVerdict?.ToLower() == "reject")
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

        private static List<string> ExtractRunResultReplies(RunResult result)
        {
            var text = result?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return new List<string> { text };
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
            catch (Exception ex) { return null; }

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
                var verdict = ((string)(json["final_verdict"] ?? json["finalVerdict"] ?? json["verdict"]))?.Trim().ToLowerInvariant();
                var comment = (string)(json["final_comment"] ?? json["finalComment"] ?? json["comment"]);

                return new AdaptiveVerdictBlock
                {
                    Verdict = verdict,
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
