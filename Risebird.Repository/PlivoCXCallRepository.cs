using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoCXModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class PlivoCXCallRepository : IPlivoCXCallRepository
    {
        private readonly string                  _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public PlivoCXCallRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger           = logger;
        }

        // ── Call logs ─────────────────────────────────────────────────────────────

        public async Task<int> SaveCallLogAsync(PlivoCXCallLog callLog)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    INSERT INTO PlivoCXCallLogs (
                        PlivoCXConversationId, PlivoCXRequestId,
                        RecipientName, RecipientPhone, RecipientEmail,
                        IRFId, CandidateId, JobTitle, ClientName,
                        CallStatus, InterviewBooked,
                        CreatedAt, TriggeredBy
                    ) VALUES (
                        @PlivoCXConversationId, @PlivoCXRequestId,
                        @RecipientName, @RecipientPhone, @RecipientEmail,
                        @IRFId, @CandidateId, @JobTitle, @ClientName,
                        @CallStatus, @InterviewBooked,
                        @CreatedAt, @TriggeredBy
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                callLog.CreatedAt = DateTime.UtcNow;
                return await conn.ExecuteScalarAsync<int>(sql, callLog);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<bool> UpdateCallLogAsync(PlivoCXCallLog callLog)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE PlivoCXCallLogs SET
                        PlivoCXConversationId    = @PlivoCXConversationId,
                        PlivoCXRequestId         = @PlivoCXRequestId,
                        CallStatus               = @CallStatus,
                        DurationSeconds          = @DurationSeconds,
                        HangupCause              = @HangupCause,
                        CallTranscript           = @CallTranscript,
                        ConversationSummary      = @ConversationSummary,
                        InterviewBooked          = @InterviewBooked,
                        BookedInterviewDate      = @BookedInterviewDate,
                        BookedInterviewTime      = @BookedInterviewTime,
                        BookedInterviewDateTime  = @BookedInterviewDateTime,
                        BookingApiResponse       = @BookingApiResponse,
                        BookingApiSuccess        = @BookingApiSuccess,
                        CallStartedAt            = @CallStartedAt,
                        CallEndedAt              = @CallEndedAt,
                        UpdatedAt                = @UpdatedAt
                    WHERE Id = @Id";

                callLog.UpdatedAt = DateTime.UtcNow;
                int rows = await conn.ExecuteAsync(sql, callLog);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<PlivoCXCallLog> GetCallLogByIdAsync(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<PlivoCXCallLog>(
                    "SELECT * FROM PlivoCXCallLogs WHERE Id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<PlivoCXCallLog> GetCallLogByConversationIdAsync(string conversationId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<PlivoCXCallLog>(
                    "SELECT * FROM PlivoCXCallLogs WHERE PlivoCXConversationId = @ConversationId",
                    new { ConversationId = conversationId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<PlivoCXCallLog>> GetRecentCallLogsAsync(int count = 50)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<PlivoCXCallLog>(
                    "SELECT TOP (@Count) * FROM PlivoCXCallLogs ORDER BY CreatedAt DESC",
                    new { Count = count });
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCXCallLog>();
            }
        }

        // ── Candidate helpers — reuse existing Vapi stored procedures ────────────

        public async Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForCallsAsync(int count = 10)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<VapiEligibleCandidateVM>(
                    "usp_GetEligibleCandidatesForVapiCalls",
                    new { Count = count },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<VapiEligibleCandidateVM>();
            }
        }

        public async Task IncrementCallAttemptsAsync(int candidateId)
        {
            try
            {
                using var conn   = new SqlConnection(_connectionString);
                var parameters   = new DynamicParameters();
                parameters.Add("@CandidateId", candidateId, DbType.Int32);
                await conn.ExecuteAsync(
                    "usp_IncrementCandidateVapiCallAttempts",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<bool> HasConfirmedInterviewAsync(string candidateEmail, int irfId)
        {
            try
            {
                using var conn   = new SqlConnection(_connectionString);
                var parameters   = new DynamicParameters();
                parameters.Add("@CandidateEmail", candidateEmail, DbType.String);
                parameters.Add("@IRFId",          irfId,          DbType.Int32);
                parameters.Add("@HasConfirmed",   dbType: DbType.Boolean, direction: ParameterDirection.Output);
                await conn.ExecuteAsync(
                    "usp_HasConfirmedInterview",
                    parameters,
                    commandType: CommandType.StoredProcedure);
                return parameters.Get<bool>("@HasConfirmed");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<CandidateDetailsForSchdeule> GetCandidateDetailsForScheduleAsync(int irfId, string candidateEmail)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<CandidateDetailsForSchdeule>(
                    "usp_GetCandidateDetailsForVapiSchedule",
                    new { IRFId = irfId, CandidateEmail = candidateEmail },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
