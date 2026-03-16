using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoWhatsAppModels;
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
    public class PlivoWhatsAppRepository : IPlivoWhatsAppRepository
    {
        private readonly string                  _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public PlivoWhatsAppRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger           = logger;
        }

        // ── Log CRUD ──────────────────────────────────────────────────────────

        public async Task<int> SaveLogAsync(PlivoWhatsAppLog log)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    INSERT INTO PlivoWhatsAppLogs (
                        RecipientName, RecipientPhone, RecipientEmail,
                        IRFId, CandidateId, JobTitle, ClientName,
                        MessagePurpose, OriginalInterviewDate,
                        OutboundMessageUUID, MessageStatus,
                        Option1DateTime, Option2DateTime,
                        Status, TriggeredBy, CreatedAt
                    ) VALUES (
                        @RecipientName, @RecipientPhone, @RecipientEmail,
                        @IRFId, @CandidateId, @JobTitle, @ClientName,
                        @MessagePurpose, @OriginalInterviewDate,
                        @OutboundMessageUUID, @MessageStatus,
                        @Option1DateTime, @Option2DateTime,
                        @Status, @TriggeredBy, @CreatedAt
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                log.CreatedAt = DateTime.UtcNow;
                return await conn.ExecuteScalarAsync<int>(sql, log);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<bool> UpdateLogAsync(PlivoWhatsAppLog log)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE PlivoWhatsAppLogs SET
                        PlivoCXConversationId   = @PlivoCXConversationId,
                        PlivoCXRequestId        = @PlivoCXRequestId,
                        MessageStatus           = @MessageStatus,
                        ConversationTranscript  = @ConversationTranscript,
                        ConversationSummary     = @ConversationSummary,
                        AIReportedInterviewDate = @AIReportedInterviewDate,
                        AIReportedInterviewTime = @AIReportedInterviewTime,
                        AIReportedBooked        = @AIReportedBooked,
                        InterviewBooked         = @InterviewBooked,
                        BookingApiSuccess       = @BookingApiSuccess,
                        BookingApiResponse      = @BookingApiResponse,
                        BookedInterviewDate     = @BookedInterviewDate,
                        BookedInterviewTime     = @BookedInterviewTime,
                        BookedInterviewDateTime = @BookedInterviewDateTime,
                        Status                  = @Status,
                        CandidateDisposition    = @CandidateDisposition,
                        ConversationEndedAt     = @ConversationEndedAt,
                        UpdatedAt               = @UpdatedAt
                    WHERE Id = @Id";

                log.UpdatedAt = DateTime.UtcNow;
                int rows = await conn.ExecuteAsync(sql, log);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<PlivoWhatsAppLog> GetLogByIdAsync(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<PlivoWhatsAppLog>(
                    "SELECT * FROM PlivoWhatsAppLogs WHERE Id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<PlivoWhatsAppLog> GetLogByConversationIdAsync(string conversationId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<PlivoWhatsAppLog>(
                    "SELECT TOP 1 * FROM PlivoWhatsAppLogs WHERE PlivoCXConversationId = @ConversationId ORDER BY CreatedAt DESC",
                    new { ConversationId = conversationId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<PlivoWhatsAppLog>> GetRecentLogsAsync(int count = 50)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<PlivoWhatsAppLog>(
                    "SELECT TOP (@Count) * FROM PlivoWhatsAppLogs ORDER BY CreatedAt DESC",
                    new { Count = count });
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoWhatsAppLog>();
            }
        }

        // ── Dashboard view ────────────────────────────────────────────────────

        public async Task<List<PlivoWhatsAppLogDetailVM>> GetLogDetailsAsync(
            int page = 1, int pageSize = 50,
            string status = null, string purpose = null,
            string meeting = null, string disposition = null)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<PlivoWhatsAppLogDetailVM>(
                    "usp_GetPlivoWhatsAppLogDetails",
                    new
                    {
                        Page        = page,
                        PageSize    = pageSize,
                        Status      = string.IsNullOrWhiteSpace(status)      ? null : status,
                        Purpose     = string.IsNullOrWhiteSpace(purpose)     ? null : purpose,
                        Meeting     = string.IsNullOrWhiteSpace(meeting)     ? null : meeting,
                        Disposition = string.IsNullOrWhiteSpace(disposition) ? null : disposition
                    },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoWhatsAppLogDetailVM>();
            }
        }

        // ── Candidate eligibility ─────────────────────────────────────────────

        public async Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForScheduleAsync(int count = 10)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<VapiEligibleCandidateVM>(
                    "usp_GetEligibleCandidatesForPlivoWhatsApp",
                    new { Count = count, NowIST = DateTime.Now },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<VapiEligibleCandidateVM>();
            }
        }

        public async Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForRescheduleAsync(int count = 10)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<VapiEligibleCandidateVM>(
                    "usp_GetEligibleCandidatesForPlivoWhatsAppReschedule",
                    new { Count = count, NowIST = DateTime.Now },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<VapiEligibleCandidateVM>();
            }
        }

        // ── Shared helpers (delegate to existing SPs used by voice channel) ───

        public async Task<CandidateDetailsForSchdeule> GetCandidateDetailsForScheduleAsync(int irfId, string candidateEmail)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<CandidateDetailsForSchdeule>(
                    "usp_GetCandidateDetailsForPlivoCXSchedule",
                    new { IRFId = irfId, CandidateEmail = candidateEmail },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
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
                await conn.ExecuteAsync("usp_HasConfirmedInterview", parameters, commandType: CommandType.StoredProcedure);
                return parameters.Get<bool>("@HasConfirmed");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<string> GetCandidateEmailByIdAsync(int candidateId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    SELECT TOP 1 ul.EmailId
                    FROM   Candidate        ca
                    JOIN   CandidatesMaster cm ON cm.id = ca.CandidateMasterId
                    JOIN   UserList         ul ON ul.id = cm.UserId
                    WHERE  ca.id = @CandidateId";
                return await conn.ExecuteScalarAsync<string>(sql, new { CandidateId = candidateId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task UpdateCandidateDispositionAsync(int candidateId, string disposition)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.ExecuteAsync(
                    "UPDATE Candidate SET PlivoCXDisposition = @Disposition WHERE id = @CandidateId",
                    new { CandidateId = candidateId, Disposition = disposition });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task IncrementCallAttemptsAsync(int candidateId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@CandidateId", candidateId, DbType.Int32);
                await conn.ExecuteAsync(
                    "usp_IncrementCandidateVapiCallAttempts", parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
