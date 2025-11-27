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
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Collections.Generic;
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
        private string _assistantId;

        public OpenAIApiController(IApplicationErrorLogger applicationErrorLogger, IOpenAIInterviewerAssistantHelper ai,
            IAIInterviewerRepository aiInterviewerRepository, IConfiguration config, IAIInterviewerService aIInterviewerService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _aiInterviewerRepository = aiInterviewerRepository;
            _ai = ai;
            _config = config;
            //_assistantId = _config["OpenAI:AIInterviewerAssistantId"];
            _aIInterviewerService = aIInterviewerService;
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

            if(replies != null && replies.Count()>0)
            {
                await _aiInterviewerRepository.LogTranscript(input.SessionId, "assistant", replies.FirstOrDefault());

            }
            //foreach (var reply in replies)
            //{
            //    await _aiInterviewerRepository.LogTranscript(input.SessionId, "assistant", reply);
            //}

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
            if(input?.AIInterviewType == (int)AIInterviewTypeEnum.Preset)
            {
                var meetingDetails = await _aIInterviewerService.GetAIInterviewDetailsBySessionId(input.SessionId);

                if(string.IsNullOrEmpty(meetingDetails.QuestionAnswerSetInJson) && input.isExit)
                {
                    //mark as no show
                    isReportSavedToDb = await _aIInterviewerService.SaveReportOfAIInterviewByAssistant(input.SessionId, null, input.isExit, null, null, 1);
                    return Ok(new InterviewReportResult { SessionId = input.SessionId, IsReportSavedToDB = true });
                }
                else if((!string.IsNullOrEmpty(meetingDetails.QuestionAnswerSetInJson)))
                {
                    var threadId = await _ai.CreateThreadAsync();
                    //the prompt will be different here. Fetch techrubrics and the table of questions, answers and expected answers. Serialize them, add them to a 
                    input.Message = $"Generate an output in json based on the inputs. The techrubrics in json are as follows:{meetingDetails.TechSkillsInJson}. The mapped_rubric_parameters, question, candidate's answer and ideal answer set object in json are as follows: {meetingDetails.QuestionAnswerSetInJson}";
                    await _ai.AddMessageAsync(threadId, "user", input.Message);

                    var runId = await _ai.CreateRunAsync(threadId, meetingDetails.OpenAIAssistantId);//meetingDetails.AssistantId
                    await _ai.WaitForRunAsync(threadId, runId);

                    var replies = await _ai.GetMessagesAsync(threadId); // all these replies here are in response to the prompt above. Therefore all consitute the report that we asked for in the prompt. Send them all back.

                    int counter = 0;
                    if(replies != null && replies.Count() == 0)
                    {
                        do
                        {
                            await Task.Delay(4000);
                            replies = await _ai.GetMessagesAsync(threadId);
                            counter++;
                        }
                        while (counter < 2 && replies != null && replies.Count() == 0);
                        
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
                        if(input.isExit)
                        {
                            if(interviewStatus == 2 || interviewStatus == 3)
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
                    return null;
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
                    var aiReport = new InterviewReportResult();
                    var list = new List<string>();
                    list.Add(replies.FirstOrDefault());
                    aiReport.Replies = list;
                    aiReport.SessionId = input.SessionId;
                    aiReport.ThreadId = threadId;
                    //save the report in the database.

                    isReportSavedToDb = await _aIInterviewerService.SaveReportOfAIInterviewByAssistant(input.SessionId, JsonConvert.SerializeObject(aiReport), input.isExit);

                }

                return Ok(new { input.SessionId, threadId, replies, isReportSavedToDb });
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
            catch(Exception ex) { return null; }

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
