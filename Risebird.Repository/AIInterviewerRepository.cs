using Dapper;
using Microsoft.Extensions.Configuration;
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

        public async Task LogTranscript(Guid sessionId, string role, string message)
        {
            var sql = "INSERT INTO TransCriptsConv (SessionId, Role, Message, Timestamp) VALUES (@SessionId, @Role, @Message, @Timestamp)";
            using var conn = new SqlConnection(connectionString);
            await conn.ExecuteAsync(sql, new { SessionId = sessionId, Role = role, Message = message, Timestamp = DateTime.UtcNow });
        }
        public async Task<bool> UpdateBotInterviewVideoDetails(string VideoLink, string question, string SessionId, string botSynthesizedVideoLink)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@VideoLink", VideoLink);
                    parameters.Add("@question", question);
                    parameters.Add("@SessionId", SessionId);
                    parameters.Add("@BotSynthesizedVideoLink", botSynthesizedVideoLink);
                    parameters.Add("@CreatedAt", DateTime.Now);
                    await SqlMapper.QueryAsync(con, "usp_SaveBotInterviewQuestionVideos", parameters,
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
    }
}
