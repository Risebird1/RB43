using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections;
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
            var sql = "INSERT INTO InterviewSessions (SessionId, ThreadId, AssistantId, CandidateEmail, CreatedAt) VALUES (@SessionId, @ThreadId, @AssistantId, @CandidateEmail, @CreatedAt)";
            using var conn = new SqlConnection(connectionString);
            await conn.ExecuteAsync(sql, session);
        }

        public async Task<string> GetThreadId(Guid sessionId)
        {
            var sql = "SELECT ThreadId FROM InterviewSessions WHERE SessionId = @SessionId";
            using var conn = new SqlConnection(connectionString);
            return await conn.ExecuteScalarAsync<string>(sql, new { SessionId = sessionId });
        }

        public async Task LogTranscript(Guid sessionId, string role, string message, int? deliveryHeadQuestionId = null)
        {
            var sql = "INSERT INTO TransCriptsConv (SessionId, Role, Message, Timestamp, DeliveryHeadQuestionId) VALUES (@SessionId, @Role, @Message, @Timestamp, @DeliveryHeadQuestionId)";
            using var conn = new SqlConnection(connectionString);
            await conn.ExecuteAsync(sql, new { SessionId = sessionId, Role = role, Message = CleanMessage(message), Timestamp = DateTime.UtcNow, DeliveryHeadQuestionId = deliveryHeadQuestionId });
        }
        public async Task<bool> UpdateBotInterviewVideoDetails(string VideoLink, string question, string SessionId, string botSynthesizedVideoLink, string candidateResponse, int secondsLeft, int interviewId, string BotAudioLink = null)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@VideoLink", VideoLink);
                    parameters.Add("@question", CleanMessage(question));
                    parameters.Add("@SessionId", SessionId);
                    parameters.Add("@BotSynthesizedVideoLink", botSynthesizedVideoLink);
                    parameters.Add("@CandidateResponse", candidateResponse);
                    parameters.Add("@secondsLeft", secondsLeft);
                    parameters.Add("@interviewId", interviewId);
                    parameters.Add("@CreatedAt", DateTime.Now);
                    parameters.Add("@BotAudioLink", BotAudioLink);
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
                        if (dt.Rows != null)
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
                            _resultVM.ThreadId = !string.IsNullOrEmpty(dt.Rows[0]["ThreadId"].ToString()) ? dt.Rows[0]["ThreadId"].ToString() : null;
                            _resultVM.resumeLink = !string.IsNullOrEmpty(dt.Rows[0]["resumeLink"].ToString()) ? dt.Rows[0]["resumeLink"].ToString() : null;
                            _resultVM.candidateName = !string.IsNullOrEmpty(dt.Rows[0]["candidateName"].ToString()) ? dt.Rows[0]["candidateName"].ToString() : null;
                            _resultVM.IsAdaptive = !string.IsNullOrEmpty(dt.Rows[0]["IsAdaptive"].ToString()) ? Convert.ToBoolean(dt.Rows[0]["IsAdaptive"]) : true;
                            _resultVM.AIInterviewType = !string.IsNullOrEmpty(dt.Rows[0]["AIInterviewType"].ToString()) ? Convert.ToInt16(dt.Rows[0]["AIInterviewType"]) : 0;
                            _resultVM.irfIntId = !string.IsNullOrEmpty(dt.Rows[0]["irfIntId"].ToString()) ? Convert.ToInt32(dt.Rows[0]["irfIntId"]) : 0;
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

        public async Task<bool> SaveReportOfAIInterviewByAssistant(Guid sessionId, string serializedReportOfAIInterviewByAssistant)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SessionId", sessionId);
                    parameters.Add("@SerializedReportOfAIInterviewByAssistant", serializedReportOfAIInterviewByAssistant);
                    var result = await SqlMapper.QueryAsync(con, "usp_SaveReportOfAIInterviewByAssistant", parameters,
                         commandType: CommandType.StoredProcedure, commandTimeout:150);
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
                    if(result != null)
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

    }
}
