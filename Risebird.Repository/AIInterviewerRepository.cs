using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class AIInterviewerRepository : IAIInterviewerRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private IApplicationErrorLogger _applicationErrorLogger;
        public AIInterviewerRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        //public async Task<bool> SaveSentWhatsappMessageDetails(SentWhatsAppMessageDetails model)
        //{
        //    try
        //    {
        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {
        //            var parameters = new DynamicParameters();
        //            parameters.Add("@Recipient", model.Recipient);
        //            parameters.Add("@CountryCode", model.CountryCode);
        //            parameters.Add("@WhatsappNumber", model.WhatsappNumber);
        //            parameters.Add("@Message", model.Message);
        //            parameters.Add("@ButtonLink", model.ButtonLink);
        //            parameters.Add("@Event", model.Event);
        //            parameters.Add("@IRFId", model.IRFId);
        //            parameters.Add("@InterviewId", model.InterviewId);
        //            parameters.Add("@InterviewDate", model.InterviewDate);
        //            parameters.Add("@Header", model.Header);
        //            parameters.Add("@CreatedAt", DateTime.Now);
        //            parameters.Add("@SentBy", model.SentBy);

        //            await SqlMapper.QueryAsync(con, "usp_SaveSentWhatsappMessageDetails", parameters,
        //                 commandType: CommandType.StoredProcedure);
        //            return true;
        //        }
        //    }
        //    catch (SqlException ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //    return false;
        //}

        public async Task CreateSession(InterviewSession session)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", session.SessionId);
                    parameters.Add("@ThreadId", session.ThreadId);
                    parameters.Add("@AssistantId", session.AssistantId);
                    parameters.Add("@CandidateEmail", session.CandidateEmail);
                    parameters.Add("@CreatedAt", session.CreatedAt == default ? DateTime.UtcNow : session.CreatedAt);
                    await SqlMapper.QueryAsync(con, "usp_CreateOrUpdateInterviewSession", parameters,
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<string> GetThreadId(Guid sessionId)
        {
            var sql = "SELECT ThreadId FROM InterviewSessions WHERE SessionId = @SessionId";
            using var conn = new SqlConnection(connectionString);
            return await conn.ExecuteScalarAsync<string>(sql, new { SessionId = sessionId });
        }

        public async Task LogTranscript(Guid sessionId, string role, string message, int? deliveryHeadQuestionId = null)
        {
            if (sessionId == Guid.Empty) return;

            var cleanedRole = NormalizeForDb(role);
            if (cleanedRole is null) return;

            // Markdown scrubbing belongs to generated assistant text only. Running it over a
            // candidate answer corrupts submitted code, stripping '#' comments and directives
            // and turning Python's '**' operator into whitespace.
            var isCandidateMessage = cleanedRole.Equals("user", StringComparison.OrdinalIgnoreCase);
            var cleanedMessage = NormalizeForDb(isCandidateMessage ? message : CleanMessage(message));
            if (cleanedMessage is null) return;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);
                    parameters.Add("@Role", cleanedRole);
                    parameters.Add("@Message", cleanedMessage);
                    parameters.Add("@Timestamp", DateTime.UtcNow);
                    parameters.Add("@DeliveryHeadQuestionId", deliveryHeadQuestionId);

                    await SqlMapper.QueryAsync(con, "usp_LogTranscript", parameters,
                         commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                // Silently fail to not block the interview flow
            }
        }
        public async Task<bool> UpdateBotInterviewVideoDetails(string VideoLink, string question, string SessionId, string botSynthesizedVideoLink, string candidateResponse, int secondsLeft, int interviewId, string BotAudioLink = null, int? deliveryHeadQuestionId = null, bool isTypingSpeedQuestion = false, bool isListeningComprehension = false, string candidateAnswerAudioLink = null, bool allowMissingVideo = false)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    // Normalize all strings before sending to the stored proc.
                    // This avoids inserting whitespace-only values ("   ") which are not caught by SQL checks like != ''.
                    var normalizedSessionId = NormalizeForDb(SessionId);
                    var normalizedVideoLink = NormalizeForDb(VideoLink);
                    // The candidate response can be code, so it is stored verbatim; only the
                    // generated question text is scrubbed of markdown.
                    var normalizedCandidateResponse = NormalizeForDb(candidateResponse);
                    var normalizedQuestion = NormalizeForDb(CleanMessage(question));
                    var normalizedBotSynthVideoLink = NormalizeForDb(botSynthesizedVideoLink);
                    var normalizedBotAudioLink = NormalizeForDb(BotAudioLink);
                    var normalizedCandidateAnswerAudioLink = NormalizeForDb(candidateAnswerAudioLink);

                    // Required fields for a meaningful row in BotInterviewQuestionVideos.
                    //if(isTypingSpeedQuestion || isListeningComprehension)
                    //{
                    //    if (normalizedSessionId is null ||
                    //    normalizedVideoLink is null ||
                    //    //normalizedCandidateResponse is null ||
                    //    normalizedQuestion is null ||
                    //    interviewId <= 0)
                    //    {
                    //        return false;
                    //    }
                    //    normalizedCandidateResponse = "Response Typed Out";
                    //}
                    //else
                    //{
                    //    if (normalizedSessionId is null ||
                    //    normalizedVideoLink is null ||
                    //    normalizedCandidateResponse is null ||
                    //    normalizedQuestion is null ||
                    //    interviewId <= 0)
                    //    {
                    //        return false;
                    //    }
                    //}
                    // allowMissingVideo covers question types that record nothing at all - the MCQ
                    // assessment section, where the candidate answers inside an embedded test.
                    // Everything else still requires a video link, so a genuinely lost upload is
                    // still reported as a failed save rather than silently accepted.
                    if (normalizedSessionId is null ||
                        (normalizedVideoLink is null && !allowMissingVideo) ||
                        normalizedCandidateResponse is null ||
                        normalizedQuestion is null ||
                        interviewId <= 0)
                    {
                        return false;
                    }


                    var parameters = new DynamicParameters();
                    parameters.Add("@VideoLink", normalizedVideoLink);
                    parameters.Add("@question", normalizedQuestion);
                    parameters.Add("@SessionId", normalizedSessionId);
                    parameters.Add("@BotSynthesizedVideoLink", normalizedBotSynthVideoLink);
                    parameters.Add("@CandidateResponse", normalizedCandidateResponse);
                    parameters.Add("@secondsLeft", secondsLeft);
                    parameters.Add("@interviewId", interviewId);
                    parameters.Add("@CreatedAt", DateTime.Now);
                    parameters.Add("@BotAudioLink", normalizedBotAudioLink);
                    parameters.Add("@DeliveryHeadQuestionId", deliveryHeadQuestionId);
                    parameters.Add("@CandidateAnswerAudioLink", normalizedCandidateAnswerAudioLink);
                    parameters.Add("@AllowMissingVideo", allowMissingVideo);
                    await SqlMapper.QueryAsync(con, "usp_SaveBotInterviewQuestionVideos", parameters,
                         commandType: CommandType.StoredProcedure);
                    //await SqlMapper.QueryAsync(con, "usp_SaveBotInterviewQuestionVideosTestVersion", parameters,
                    //     commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<List<BotInterviewQuestionVideos>> GetBotInterviewVideoDetails(Guid sessionId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);
                    var result = await SqlMapper.QueryAsync<BotInterviewQuestionVideos>(con, "usp_GetBotInterviewVideoDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result?.ToList();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> UpdateTimeLeft(Guid sessionId, int secondsLeft, int interviewId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);
                    parameters.Add("@interviewId", interviewId);
                    parameters.Add("@secondsLeft", secondsLeft);
                    parameters.Add("@UpdatedAt", DateTime.Now);
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateTimeLeft", parameters,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        //public async Task<MeetingVM> GetAIInterviewDetailsBySessionId(Guid sessionId)
        //{
        //    try
        //    {
        //        using (SqlConnection con = new SqlConnection(connectionString))
        //        {
        //            var parameters = new DynamicParameters();
        //            parameters.Add("@SessionId", sessionId);
        //            var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetAIInterviewDetailsBySessionId", parameters,
        //                 commandType: CommandType.StoredProcedure);
        //            return result?.FirstOrDefault();
        //        }
        //    }
        //    catch (SqlException ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //    }
        //    return null;
        //}


        public async Task<MeetingVM> GetAIInterviewDetailsBySessionId(Guid sessionId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    MeetingVM _resultVM = new MeetingVM();
                    DataSet dataset = new DataSet();
                    SqlDataAdapter adapter = new SqlDataAdapter();
                    adapter.SelectCommand = new SqlCommand("usp_GetAIInterviewDetailsBySessionId", con);
                    adapter.SelectCommand.CommandType = CommandType.StoredProcedure;
                    adapter.SelectCommand.Parameters.AddWithValue("@SessionId", sessionId);
                    adapter.Fill(dataset);
                    DataTable dt = new DataTable();
                    if (dataset.Tables.Count > 0)
                    {
                        dt = dataset.Tables[0];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {

                            _resultVM.InterviewId = !string.IsNullOrEmpty(dt.Rows[0]["InterviewId"].ToString()) ? Convert.ToInt32(dt.Rows[0]["InterviewId"]) : 0;
                            _resultVM.interviewDate = !string.IsNullOrEmpty(dt.Rows[0]["interviewDate"].ToString()) ? Convert.ToDateTime(dt.Rows[0]["interviewDate"]) : new DateTime();
                            _resultVM.AssistantId = !string.IsNullOrEmpty(dt.Rows[0]["AssistantId"].ToString()) ? dt.Rows[0]["AssistantId"].ToString() : null;
                            _resultVM.candidateEmail = !string.IsNullOrEmpty(dt.Rows[0]["candidateEmail"].ToString()) ? dt.Rows[0]["candidateEmail"].ToString() : null;
                            _resultVM.MobileNumber = !string.IsNullOrEmpty(dt.Rows[0]["MobileNumber"].ToString()) ? dt.Rows[0]["MobileNumber"].ToString() : null;
                            _resultVM.interviewer = !string.IsNullOrEmpty(dt.Rows[0]["interviewer"].ToString()) ? dt.Rows[0]["interviewer"].ToString() : null;
                            _resultVM.TimeLeftInSeconds = !string.IsNullOrEmpty(dt.Rows[0]["TimeLeftInSeconds"].ToString()) ? Convert.ToInt32(dt.Rows[0]["TimeLeftInSeconds"]) : 0;

                            _resultVM.TimeZonesValue = !string.IsNullOrEmpty(dt.Rows[0]["TimeZonesValue"].ToString()) ? dt.Rows[0]["TimeZonesValue"].ToString() : null;
                            _resultVM.TimeZoneoffset = !string.IsNullOrEmpty(dt.Rows[0]["TimeZoneoffset"].ToString()) ? Convert.ToInt32(dt.Rows[0]["TimeZoneoffset"]) : 0;

                            _resultVM.JDTiltle = !string.IsNullOrEmpty(dt.Rows[0]["JDTiltle"].ToString()) ? dt.Rows[0]["JDTiltle"].ToString() : null;
                            _resultVM.JDLink = !string.IsNullOrEmpty(dt.Rows[0]["JDLink"].ToString()) ? dt.Rows[0]["JDLink"].ToString() : null;
                            _resultVM.ThreadId = !string.IsNullOrEmpty(dt.Rows[0]["ThreadId"].ToString()) ? dt.Rows[0]["ThreadId"].ToString() : null;
                            _resultVM.resumeLink = !string.IsNullOrEmpty(dt.Rows[0]["resumeLink"].ToString()) ? dt.Rows[0]["resumeLink"].ToString() : null;
                            _resultVM.candidateName = !string.IsNullOrEmpty(dt.Rows[0]["candidateName"].ToString()) ? dt.Rows[0]["candidateName"].ToString() : null;
                            _resultVM.IsAdaptive = !string.IsNullOrEmpty(dt.Rows[0]["IsAdaptive"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsAdaptive"]) : true;
                            _resultVM.AIInterviewType = !string.IsNullOrEmpty(dt.Rows[0]["AIInterviewType"].ToString()) ? Convert.ToInt16(dt.Rows[0]["AIInterviewType"]) : 0;
                            _resultVM.irfIntId = !string.IsNullOrEmpty(dt.Rows[0]["irfIntId"].ToString()) ? Convert.ToInt32(dt.Rows[0]["irfIntId"]) : 0;

                            _resultVM.MeetingStatus = !string.IsNullOrEmpty(dt.Rows[0]["MeetingStatus"].ToString()) ? Convert.ToInt32(dt.Rows[0]["MeetingStatus"]) : 0;
                            _resultVM.InterviewStatus = !string.IsNullOrEmpty(dt.Rows[0]["InterviewStatus"].ToString()) ? Convert.ToInt32(dt.Rows[0]["InterviewStatus"]) : 0;
                            _resultVM.CandidateAnswerVideoIdAI = !string.IsNullOrEmpty(dt.Rows[0]["CandidateAnswerVideoIdAI"].ToString()) ? Convert.ToInt32(dt.Rows[0]["CandidateAnswerVideoIdAI"]) : 0;
                            _resultVM.OpenAIAssistantId = !string.IsNullOrEmpty(dt.Rows[0]["OpenAIAssistantId"].ToString()) ? dt.Rows[0]["OpenAIAssistantId"].ToString() : null;
                            _resultVM.OpenAIPromptId = dt.Columns.Contains("OpenAIPromptId") && !string.IsNullOrEmpty(dt.Rows[0]["OpenAIPromptId"].ToString()) ? dt.Rows[0]["OpenAIPromptId"].ToString() : null;
                            _resultVM.OpenAIEvaluatorPromptId = dt.Columns.Contains("OpenAIEvaluatorPromptId") && !string.IsNullOrEmpty(dt.Rows[0]["OpenAIEvaluatorPromptId"].ToString()) ? dt.Rows[0]["OpenAIEvaluatorPromptId"].ToString() : null;
                            _resultVM.showChatBox = !string.IsNullOrEmpty(dt.Rows[0]["showChatBox"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["showChatBox"]) : false;
                            _resultVM.isMultiLingual = !string.IsNullOrEmpty(dt.Rows[0]["isMultiLingual"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["isMultiLingual"]) : false;
                            _resultVM.IsProctoring = !dt.Columns.Contains("IsProctoring") || string.IsNullOrEmpty(dt.Rows[0]["IsProctoring"].ToString()) || Convert.ToBoolean(dt.Rows[0]["IsProctoring"]);
                            _resultVM.IsAadhaarVerificationRequired = dt.Columns.Contains("IsAadhaarVerificationRequired") && !string.IsNullOrEmpty(dt.Rows[0]["IsAadhaarVerificationRequired"].ToString()) && Convert.ToBoolean(dt.Rows[0]["IsAadhaarVerificationRequired"]);
                            _resultVM.AIResumeScreeningVerdict = !string.IsNullOrEmpty(dt.Rows[0]["AIResumeScreeningVerdict"].ToString()) ? dt.Rows[0]["AIResumeScreeningVerdict"].ToString() : null;
                            _resultVM.AIResumeScreeningScore = !string.IsNullOrEmpty(dt.Rows[0]["AIResumeScreeningScore"].ToString()) ? Convert.ToInt32(dt.Rows[0]["AIResumeScreeningScore"]) : 0;

                            // Guarded with Columns.Contains, like the MCQ columns above: code deployed
                            // ahead of usp_GetAIInterviewDetailsBySessionId then degrades to "no resume
                            // evaluation" instead of throwing on every report load.
                            _resultVM.AIScreeningResultJsonV2 = dt.Columns.Contains("AIScreeningResultJsonV2")
                                && !string.IsNullOrEmpty(dt.Rows[0]["AIScreeningResultJsonV2"].ToString())
                                ? dt.Rows[0]["AIScreeningResultJsonV2"].ToString() : null;
                            // MCQ assessment section. All four are guarded with Columns.Contains so a
                            // deployment that ships this code ahead of the updated stored procedure
                            // degrades to "no MCQ section" instead of throwing on every page load.
                            //
                            // IRFId matters most: it is the value MCQIRF.PortalIRFId holds on
                            // screen.risebird.io, so it is what addresses the MCQ API. Leaving it
                            // unmapped makes the portal post skillChannelId: null, which the MCQ side
                            // rejects as a validation error - and the provision call comes back null
                            // with nothing on screen to say why.
                            _resultVM.IRFId = dt.Columns.Contains("IRFId") && !string.IsNullOrEmpty(dt.Rows[0]["IRFId"].ToString())
                                ? dt.Rows[0]["IRFId"].ToString()
                                : null;
                            _resultVM.HasMcqAssessmentSection = dt.Columns.Contains("HasMcqAssessmentSection")
                                && !string.IsNullOrEmpty(dt.Rows[0]["HasMcqAssessmentSection"].ToString())
                                && Convert.ToBoolean(dt.Rows[0]["HasMcqAssessmentSection"]);
                            _resultVM.McqAssessmentSections = dt.Columns.Contains("McqAssessmentSections")
                                && !string.IsNullOrEmpty(dt.Rows[0]["McqAssessmentSections"].ToString())
                                ? dt.Rows[0]["McqAssessmentSections"].ToString()
                                : null;
                            _resultVM.McqAssessmentMinutes = dt.Columns.Contains("McqAssessmentMinutes")
                                && !string.IsNullOrEmpty(dt.Rows[0]["McqAssessmentMinutes"].ToString())
                                ? Convert.ToInt32(dt.Rows[0]["McqAssessmentMinutes"])
                                : (int?)null;
                            if (dt.Columns.Contains("PrimarySkill") && !string.IsNullOrEmpty(dt.Rows[0]["PrimarySkill"].ToString()))
                                _resultVM.PrimarySkill = dt.Rows[0]["PrimarySkill"].ToString();

                            try
                            {
                                if (dt.Rows[0]["MatchScorePercentage"] != DBNull.Value)
                                {
                                    var matchPercentage = dt.Rows[0]["MatchScorePercentage"];
                                    if (decimal.TryParse(matchPercentage.ToString(), out decimal result))
                                    {
                                        _resultVM.MatchScorePercentage = (int)Math.Round(result);
                                    }
                                    else
                                    {
                                        _resultVM.MatchScorePercentage = 0;
                                    }
                                }
                                else
                                {
                                    _resultVM.MatchScorePercentage = 0;
                                }
                            }
                            catch (Exception ex)
                            {
                                _resultVM.MatchScorePercentage = 0;

                            }
                            _resultVM.AadharVerificationId = !string.IsNullOrEmpty(dt.Rows[0]["AadharVerificationId"].ToString()) ? Convert.ToInt32(dt.Rows[0]["AadharVerificationId"]) : 0;
                        }
                    }
                    if (dataset.Tables.Count > 1)
                    {
                        dt = dataset.Tables[1];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {
                            //List<TransSkill> techSkills = new List<TransSkill>();
                            ////var listStaffingAgencyMeetings = new List<TransSkill>();
                            //for (int i = 0; i < dt.Rows.Count; i++)
                            //{
                            //    techSkills.Add(new TransSkill
                            //    {
                            //        //id = !string.IsNullOrEmpty(dt.Rows[i]["id"].ToString()) ? Convert.ToInt32(dt.Rows[i]["id"]) : 0,
                            //        IRFId = !string.IsNullOrEmpty(dt.Rows[i]["IRFId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["IRFId"]) : 0,
                            //        //SkillId = !string.IsNullOrEmpty(dt.Rows[i]["SkillId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["SkillId"]) : 0,
                            //        LevelId = !string.IsNullOrEmpty(dt.Rows[i]["LevelId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["LevelId"]) : 0,
                            //        Weightage = !string.IsNullOrEmpty(dt.Rows[i]["Weightage"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Weightage"]) : 0,
                            //        Concepts = !string.IsNullOrEmpty(dt.Rows[i]["Concepts"].ToString()) ? dt.Rows[i]["Concepts"].ToString() : null,
                            //        Natureofquestion = !string.IsNullOrEmpty(dt.Rows[i]["Natureofquestion"].ToString()) ? dt.Rows[i]["Natureofquestion"].ToString() : null,
                            //        Timelimit = !string.IsNullOrEmpty(dt.Rows[i]["Timelimit"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Timelimit"]) : 0,
                            //        SkillName = !string.IsNullOrEmpty(dt.Rows[i]["SkillName"].ToString()) ? dt.Rows[i]["SkillName"].ToString() : null,
                            //        //isEliminationskill = !string.IsNullOrEmpty(dt.Rows[i]["isEliminationskill"].ToString()) ? Convert.ToBoolean(dt.Rows[i]["isEliminationskill"]) : false,
                            //        //SkillSet = !string.IsNullOrEmpty(dt.Rows[i]["SkillSet"].ToString()) ? Convert.ToInt32(dt.Rows[i]["SkillSet"]) : 1,
                            //    });
                            //}


                            var techSkills = new List<ExpandoObject>();

                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                dynamic obj = new ExpandoObject();

                                //obj.IRFId = !string.IsNullOrEmpty(dt.Rows[i]["IRFId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["IRFId"]) : 0;
                                obj.LevelId = !string.IsNullOrEmpty(dt.Rows[i]["LevelId"].ToString()) ? Convert.ToInt32(dt.Rows[i]["LevelId"]) : 0;
                                obj.Weightage = !string.IsNullOrEmpty(dt.Rows[i]["Weightage"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Weightage"]) : 0;
                                obj.Concepts = !string.IsNullOrEmpty(dt.Rows[i]["Concepts"].ToString()) ? dt.Rows[i]["Concepts"].ToString() : null;
                                obj.Natureofquestion = !string.IsNullOrEmpty(dt.Rows[i]["Natureofquestion"].ToString()) ? dt.Rows[i]["Natureofquestion"].ToString() : null;
                                //obj.Timelimit = !string.IsNullOrEmpty(dt.Rows[i]["Timelimit"].ToString()) ? Convert.ToInt32(dt.Rows[i]["Timelimit"]) : 0;
                                obj.SkillName = !string.IsNullOrEmpty(dt.Rows[i]["SkillName"].ToString()) ? dt.Rows[i]["SkillName"].ToString() : null;

                                techSkills.Add(obj);
                            }

                            // Serialize to JSON
                            string jsonOutput = JsonConvert.SerializeObject(techSkills, Formatting.Indented);
                            _resultVM.TechSkillsInJson = jsonOutput;
                        }
                    }
                    if (dataset.Tables.Count > 2)
                    {
                        dt = dataset.Tables[2];
                        if (dt.Rows != null && dt.Rows.Count > 0)
                        {

                            var qaSet = new List<ExpandoObject>();

                            for (int i = 0; i < dt.Rows.Count; i++)
                            {
                                dynamic obj = new ExpandoObject();

                                obj.question = !string.IsNullOrEmpty(dt.Rows[i]["question"].ToString()) ? dt.Rows[i]["question"].ToString() : null;
                                obj.CandidateAnswer = !string.IsNullOrEmpty(dt.Rows[i]["CandidateAnswer"].ToString()) ? dt.Rows[i]["CandidateAnswer"].ToString() : null;
                                obj.ExpectedAnswer = !string.IsNullOrEmpty(dt.Rows[i]["ExpectedAnswer"].ToString()) ? dt.Rows[i]["ExpectedAnswer"].ToString() : null;
                                obj.mapped_rubric_parameters = !string.IsNullOrEmpty(dt.Rows[i]["SkillName"].ToString()) ? dt.Rows[i]["SkillName"].ToString() : null;

                                qaSet.Add(obj);
                            }

                            // Serialize to JSON
                            string jsonOutput = JsonConvert.SerializeObject(qaSet, Formatting.Indented);
                            _resultVM.QuestionAnswerSetInJson = jsonOutput;
                        }
                    }

                    return _resultVM;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        public async Task<IEnumerable<TranscriptConv>> GetAIInterviewTranscriptBySessionId(Guid sessionId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);
                    var result = await SqlMapper.QueryAsync<TranscriptConv>(con, "usp_GetAIInterviewTranscriptBySessionId", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> SaveReportOfAIInterviewByAssistant(Guid sessionId, string serializedReportOfAIInterviewByAssistant = null, bool? isExit = null, int? interviewStatus = null, string interviewerComment = null, int? meetingStatus = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);
                    parameters.Add("@SerializedReportOfAIInterviewByAssistant", serializedReportOfAIInterviewByAssistant);
                    parameters.Add("@isExit", isExit);
                    parameters.Add("@interviewStatus", interviewStatus);
                    parameters.Add("@interviewerComment", interviewerComment);
                    parameters.Add("@meetingStatus", meetingStatus);
                    parameters.Add("@timeNow", (isExit.HasValue && isExit.Value == true) ? DateTime.Now : null);
                    var result = await SqlMapper.QueryAsync(con, "usp_SaveReportOfAIInterviewByAssistant", parameters,
                         commandType: CommandType.StoredProcedure, commandTimeout: 150);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<string> GetReportOfAIInterviewByAssistant(Guid sessionId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);
                    var result = await SqlMapper.QueryAsync<string>(con, "usp_GetReportOfAIInterviewByAssistant", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
        private string CleanMessage(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            return input
                .Replace("#", "")        // Remove Markdown headers
                .Replace("**", "")       // Remove bold formatting if present
                .Trim();                 // Trim whitespace
        }

        /// <summary>
        /// Standard DB normalization for optional text fields:
        /// trims whitespace and converts empty strings to null.
        /// </summary>
        private static string NormalizeForDb(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var trimmed = input.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        public async Task<AddUpdateMeetingVM> GetAIInterviewSpecificDetails(Guid sessionId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);
                    var result = await SqlMapper.QueryAsync<AddUpdateMeetingVM>(con, "usp_GetAIInterviewSpecificDetails", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result?.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<TechSkillComments> GetRandomQuestionForSession(int interviewId, int irfId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@interviewId", interviewId);
                    parameters.Add("@irfId", irfId);
                    var result = await SqlMapper.QueryAsync<TechSkillComments>(con, "usp_GetRandomQuestionForSession", parameters,
                         commandType: CommandType.StoredProcedure);
                    if (result != null)
                    {
                        return result?.FirstOrDefault();

                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> UpdateCandidateAnswerById(int questionId, string updatedAnswer, string updatedBy)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@questionId", questionId);
                    parameters.Add("@updatedAnswer", updatedAnswer);
                    parameters.Add("@updatedBy", updatedBy);
                    parameters.Add("@timeNow", DateTime.Now);
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateCandidateAnswerById", parameters,
                         commandType: CommandType.StoredProcedure, commandTimeout: 150);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<bool> UpdateMeetingCompletedStatusForAIInterview(string sessionId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);
                    parameters.Add("@timeNow", DateTime.Now);
                    parameters.Add("@ModifiedBy", "AI_Interview_System");
                    var result = await SqlMapper.QueryAsync(con, "usp_UpdateMeetingCompletedStatusForAIInterview", parameters,
                         commandType: CommandType.StoredProcedure, commandTimeout: 150);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<bool> SaveSentimentAnalysis(VideoSentimentAnalysis obj)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@biqvId", obj.biqvId);
                    parameters.Add("@FEAR", obj.FEAR);
                    parameters.Add("@CONFUSED", obj.CONFUSED);
                    parameters.Add("@SAD", obj.SAD);
                    parameters.Add("@CALM", obj.CALM);
                    parameters.Add("@SURPRISED", obj.SURPRISED);
                    parameters.Add("@DISGUSTED", obj.DISGUSTED);
                    parameters.Add("@ANGRY", obj.ANGRY);
                    parameters.Add("@HAPPY", obj.HAPPY);
                    parameters.Add("@CreatedAt", DateTime.Now);
                    parameters.Add("@CreatedBy", obj.CreatedBy);
                    var result = await SqlMapper.QueryAsync(con, "usp_SaveSentimentAnalysis", parameters,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<VideoSentimentAnalysis> GetSentimentAnalysisById(int videoId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@biqvId", videoId);
                    var result = await SqlMapper.QueryAsync<VideoSentimentAnalysis>(con, "usp_GetSentimentAnalysisById", parameters,
                         commandType: CommandType.StoredProcedure, commandTimeout: 150);
                    return result?.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<MeetingVM> GetAIInterviewSpecificDetailsByCandidateVideoId(int videoId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@biqvId", videoId);
                    var result = await SqlMapper.QueryAsync<MeetingVM>(con, "usp_GetAIInterviewSpecificDetailsByCandidateVideoId", parameters,
                         commandType: CommandType.StoredProcedure, commandTimeout: 150);
                    return result?.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> SaveTypingSpeedAssessment(TypingSpeedAssessment obj)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", obj.SessionId);
                    parameters.Add("@QuestionId", obj.QuestionId);
                    parameters.Add("@SampleText", obj.SampleText);
                    parameters.Add("@TypedText", obj.TypedText);
                    parameters.Add("@TimeStarted", obj.TimeStarted);
                    parameters.Add("@TimeCompleted", obj.TimeCompleted);
                    parameters.Add("@TimeTakenInSeconds", obj.TimeTakenInSeconds);
                    parameters.Add("@TotalCharacters", obj.TotalCharacters);
                    parameters.Add("@CorrectCharacters", obj.CorrectCharacters);
                    parameters.Add("@AccuracyPercentage", obj.AccuracyPercentage);
                    parameters.Add("@WordsPerMinute", obj.WordsPerMinute);
                    parameters.Add("@PassingWPM", obj.PassingWPM);
                    parameters.Add("@PassingAccuracyPercentage", obj.PassingAccuracyPercentage);
                    parameters.Add("@IsPassed", obj.IsPassed);
                    parameters.Add("@OpenAIAnalysisJson", obj.OpenAIAnalysisJson);
                    parameters.Add("@OpenAIThreadId", obj.OpenAIThreadId);
                    parameters.Add("@OpenAIRunId", obj.OpenAIRunId);
                    parameters.Add("@CreatedBy", obj.CreatedBy);

                    await SqlMapper.QueryAsync(con, "usp_SaveTypingSpeedAssessment", parameters,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<List<TypingSpeedAssessment>> GetTypingSpeedAssessmentBySessionId(string sessionId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);
                    var result = await SqlMapper.QueryAsync<TypingSpeedAssessment>(con, "usp_GetTypingSpeedAssessmentBySessionId", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result?.ToList() ?? new List<TypingSpeedAssessment>();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return new List<TypingSpeedAssessment>();
        }

        // Looked up server-side at scoring time so the expected answer never reaches the
        // candidate's browser, where it would be visible in the page source.
        public async Task<TechSkillComments> GetListeningComprehensionQuestionDetails(int questionId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@QuestionId", questionId);

                    var result = await SqlMapper.QueryAsync<TechSkillComments>(con,
                        "usp_GetListeningComprehensionQuestionDetails", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task<bool> SaveListeningComprehensionAssessment(ListeningComprehensionAssessment obj)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", obj.SessionId);
                    parameters.Add("@QuestionId", obj.QuestionId);
                    parameters.Add("@AudioUrl", obj.AudioUrl);
                    parameters.Add("@AudioTranscription", obj.AudioTranscription);
                    parameters.Add("@CandidateSummary", obj.CandidateSummary);
                    parameters.Add("@TimeStarted", obj.TimeStarted);
                    parameters.Add("@TimeCompleted", obj.TimeCompleted);
                    parameters.Add("@TimeTakenInSeconds", obj.TimeTakenInSeconds);
                    parameters.Add("@CreatedBy", obj.CreatedBy);
                    parameters.Add("@ExpectedAnswer", obj.ExpectedAnswer);
                    parameters.Add("@AudioPlayCount", obj.AudioPlayCount);
                    parameters.Add("@IsTimeExpired", obj.IsTimeExpired);
                    parameters.Add("@Score", obj.Score);
                    parameters.Add("@MaxScore", obj.MaxScore);
                    parameters.Add("@IsPassed", obj.IsPassed);
                    parameters.Add("@OpenAIAnalysisJson", obj.OpenAIAnalysisJson);
                    parameters.Add("@OpenAIResponseId", obj.OpenAIResponseId);

                    await SqlMapper.QueryAsync(con, "usp_SaveListeningComprehensionAssessment", parameters,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<bool> SaveCodingAssessment(CodingAssessment obj)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", obj.SessionId);
                    parameters.Add("@QuestionId", obj.QuestionId);
                    parameters.Add("@QuestionText", obj.QuestionText);
                    parameters.Add("@CodeLanguage", obj.CodeLanguage);
                    parameters.Add("@SubmittedCode", obj.SubmittedCode);
                    parameters.Add("@TimeLimitInMinutes", obj.TimeLimitInMinutes);
                    parameters.Add("@TimeStarted", obj.TimeStarted);
                    parameters.Add("@TimeCompleted", obj.TimeCompleted);
                    parameters.Add("@TimeTakenInSeconds", obj.TimeTakenInSeconds);
                    parameters.Add("@IsTimeExpired", obj.IsTimeExpired);
                    parameters.Add("@StdInput", obj.StdInput);
                    parameters.Add("@LastRunOutput", obj.LastRunOutput);
                    parameters.Add("@LastRunStatus", obj.LastRunStatus);
                    parameters.Add("@RunCount", obj.RunCount);
                    parameters.Add("@CreatedBy", obj.CreatedBy);

                    await SqlMapper.QueryAsync(con, "usp_SaveCodingAssessment", parameters,
                         commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<TypingSpeedSampleText> GetRandomTypingSpeedSampleText()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<TypingSpeedSampleText>(con, "usp_GetRandomTypingSpeedSampleText",
                         commandType: CommandType.StoredProcedure);
                    return result?.FirstOrDefault();
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }

        public async Task LogAIInterviewException(string sessionId, string httpMethod, Exception ex, LogLevelInfo logLevel = LogLevelInfo.Exception)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", string.IsNullOrWhiteSpace(sessionId) ? (object)DBNull.Value : sessionId);
                    parameters.Add("@HttpMethod", httpMethod ?? "UNKNOWN");
                    parameters.Add("@ExceptionMessage", ex?.Message);
                    parameters.Add("@InnerExceptionMessage", ex?.InnerException?.Message);
                    parameters.Add("@StackTrace", ex?.StackTrace);
                    parameters.Add("@LogLevel", (int)logLevel);
                    parameters.Add("@OccurredAt", DateTime.UtcNow);
                    await SqlMapper.QueryAsync(con, "usp_LogAIInterviewException", parameters,
                        commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception innerEx)
            {
                // Silently fail — DB logging must never cascade into the interview flow
                Console.WriteLine($"[AIInterviewExceptionLog] Failed to persist exception to DB: {innerEx.Message}");
            }
        }

        
        public async Task<bool> SaveAIInterviewMcqAssessment(McqAssessmentCacheRow row)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", row.SessionId);
                    parameters.Add("@InterviewId", row.InterviewId);
                    parameters.Add("@IRFId", row.IRFId);
                    parameters.Add("@McqId", row.McqId);
                    parameters.Add("@Status", row.Status);
                    parameters.Add("@TotalQuestions", row.TotalQuestions);
                    parameters.Add("@CorrectAnswers", row.CorrectAnswers);
                    parameters.Add("@ScorePercent", row.ScorePercent);
                    parameters.Add("@RedFlags", row.RedFlags);
                    parameters.Add("@AllottedMinutes", row.AllottedMinutes);
                    parameters.Add("@ElapsedSeconds", row.ElapsedSeconds);
                    parameters.Add("@TabSwitchCount", row.TabSwitchCount);
                    parameters.Add("@StartedOn", row.StartedOn);
                    parameters.Add("@CompletedOn", row.CompletedOn);
                    parameters.Add("@ReportJson", row.ReportJson);
                    await SqlMapper.ExecuteAsync(con, "usp_SaveAIInterviewMcqAssessment", parameters,
                        commandType: CommandType.StoredProcedure);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return false;
        }

        public async Task<McqAssessmentCacheRow> GetAIInterviewMcqAssessment(string sessionId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);
                    var result = await SqlMapper.QueryAsync<McqAssessmentCacheRow>(con, "usp_GetAIInterviewMcqAssessment", parameters,
                        commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            return null;
        }
    }
}
