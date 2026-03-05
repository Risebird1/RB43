using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.PlivoModels;
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
    /// <summary>
    /// Repository for Plivo call log operations (SQL Server via Dapper)
    /// </summary>
    public class PlivoCallRepository : IPlivoCallRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public PlivoCallRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
        }

        public async Task<int> SaveCallLogAsync(PlivoCallLog callLog)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    INSERT INTO PlivoCallLogs (
                        PlivoCallUUID, PlivoRequestUUID, InterviewId, IRFId, RecipientId,
                        RecipientPhone, RecipientName, RecipientEmail, FromNumber,
                        CallStatus, ScheduledDateTime, CallStartedAt, CallEndedAt, HangupCause,
                        DurationSeconds, Transcript, RecordingUrl, InterviewBooked,
                        BookedInterviewDateTime, BookingConfirmationDetails, CallCost,
                        AIModel, Summary, StructuredData, CreatedAt, CreatedBy
                    ) VALUES (
                        @PlivoCallUUID, @PlivoRequestUUID, @InterviewId, @IRFId, @RecipientId,
                        @RecipientPhone, @RecipientName, @RecipientEmail, @FromNumber,
                        @CallStatus, @ScheduledDateTime, @CallStartedAt, @CallEndedAt, @HangupCause,
                        @DurationSeconds, @Transcript, @RecordingUrl, @InterviewBooked,
                        @BookedInterviewDateTime, @BookingConfirmationDetails, @CallCost,
                        @AIModel, @Summary, @StructuredData, @CreatedAt, @CreatedBy
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                return await conn.ExecuteScalarAsync<int>(sql, callLog);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
            }
        }

        public async Task<bool> UpdateCallLogAsync(PlivoCallLog callLog)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE PlivoCallLogs SET
                        PlivoCallUUID           = @PlivoCallUUID,
                        IRFId                   = @IRFId,
                        CallStatus              = @CallStatus,
                        CallStartedAt           = @CallStartedAt,
                        CallEndedAt             = @CallEndedAt,
                        HangupCause             = @HangupCause,
                        DurationSeconds         = @DurationSeconds,
                        Transcript              = @Transcript,
                        RecordingUrl            = @RecordingUrl,
                        InterviewBooked         = @InterviewBooked,
                        BookedInterviewDateTime = @BookedInterviewDateTime,
                        BookingConfirmationDetails = @BookingConfirmationDetails,
                        CallCost                = @CallCost,
                        Summary                 = @Summary,
                        StructuredData          = @StructuredData,
                        UpdatedAt               = @UpdatedAt,
                        UpdatedBy               = @UpdatedBy
                    WHERE Id = @Id";

                await conn.ExecuteAsync(sql, callLog);
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<PlivoCallLog> GetCallLogByIdAsync(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<PlivoCallLog>(
                    "SELECT * FROM PlivoCallLogs WHERE Id = @Id", new { Id = id });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<PlivoCallLog> GetCallLogByCallUUIDAsync(string callUUID)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<PlivoCallLog>(
                    "SELECT * FROM PlivoCallLogs WHERE PlivoCallUUID = @CallUUID",
                    new { CallUUID = callUUID });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<PlivoCallLog> GetCallLogByRequestUUIDAsync(string requestUUID)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<PlivoCallLog>(
                    "SELECT * FROM PlivoCallLogs WHERE PlivoRequestUUID = @RequestUUID",
                    new { RequestUUID = requestUUID });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<PlivoCallLog>> GetCallLogsByInterviewIdAsync(int interviewId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<PlivoCallLog>(
                    "SELECT * FROM PlivoCallLogs WHERE InterviewId = @InterviewId ORDER BY CreatedAt DESC",
                    new { InterviewId = interviewId });
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCallLog>();
            }
        }

        public async Task<List<PlivoCallLog>> GetCallLogsByRecipientIdAsync(int recipientId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<PlivoCallLog>(
                    "SELECT * FROM PlivoCallLogs WHERE RecipientId = @RecipientId ORDER BY CreatedAt DESC",
                    new { RecipientId = recipientId });
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCallLog>();
            }
        }

        public async Task<List<PlivoCallLog>> GetRecentCallLogsAsync(int count = 50)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<PlivoCallLog>(
                    "SELECT TOP (@Count) * FROM PlivoCallLogs ORDER BY CreatedAt DESC",
                    new { Count = count });
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCallLog>();
            }
        }

        public async Task<bool> UpdateBookingStatusAsync(
            string callUUID, bool isBooked, DateTime? bookedDateTime, string confirmationDetails)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE PlivoCallLogs SET
                        InterviewBooked            = @IsBooked,
                        BookedInterviewDateTime    = @BookedDateTime,
                        BookingConfirmationDetails = @ConfirmationDetails,
                        UpdatedAt                  = @UpdatedAt
                    WHERE PlivoCallUUID = @CallUUID";

                await conn.ExecuteAsync(sql, new
                {
                    CallUUID            = callUUID,
                    IsBooked            = isBooked,
                    BookedDateTime      = bookedDateTime,
                    ConfirmationDetails = confirmationDetails,
                    UpdatedAt           = DateTime.UtcNow
                });
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        /// <summary>
        /// Reuses the same eligible-candidates SP as Vapi (same eligibility criteria)
        /// </summary>
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

        /// <summary>
        /// Increments the shared VapiCallAttempts counter to prevent over-calling
        /// (the column name pre-dates Plivo; it now tracks all automated call attempts)
        /// </summary>
        public async Task IncrementCallAttemptsAsync(int candidateId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@CandidateId", candidateId);
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
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@IrfId", irfId);
                parameters.Add("@CandidateEmail", candidateEmail);
                return await conn.ExecuteScalarAsync<bool>(
                    "usp_HasConfirmedInterview",
                    parameters,
                    commandType: CommandType.StoredProcedure);
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
                var parameters = new DynamicParameters();
                parameters.Add("@irfid", irfId);
                parameters.Add("@CandidateEmail", candidateEmail);
                var result = await conn.QueryAsync<CandidateDetailsForSchdeule>(
                    "usp_GetCandidateDetailsForSchedule",
                    parameters,
                    commandType: CommandType.StoredProcedure);
                return result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        // ─── Assistant config ─────────────────────────────────────────────────────

        public async Task<PlivoAssistantConfig> GetActiveAssistantConfigAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<PlivoAssistantConfig>(
                    "SELECT TOP 1 * FROM PlivoAssistantConfigs WHERE IsActive = 1 ORDER BY Id DESC");
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<PlivoAssistantConfig> GetAssistantConfigByNameAsync(string name)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<PlivoAssistantConfig>(
                    "SELECT TOP 1 * FROM PlivoAssistantConfigs WHERE AssistantName = @Name",
                    new { Name = name });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<PlivoAssistantConfig>> GetAllAssistantConfigsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<PlivoAssistantConfig>(
                    "SELECT * FROM PlivoAssistantConfigs ORDER BY IsActive DESC, Id DESC");
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoAssistantConfig>();
            }
        }

        public async Task<int> SaveAssistantConfigAsync(PlivoAssistantConfig config)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    INSERT INTO PlivoAssistantConfigs
                        (AssistantName, Instructions, Voice, Model, Temperature,
                         MaxResponseTokens, VadThreshold, SilenceDurationMs,
                         ExtraToolsJson, IsActive, CreatedAt, CreatedBy)
                    VALUES
                        (@AssistantName, @Instructions, @Voice, @Model, @Temperature,
                         @MaxResponseTokens, @VadThreshold, @SilenceDurationMs,
                         @ExtraToolsJson, @IsActive, @CreatedAt, @CreatedBy);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                config.CreatedAt = DateTime.UtcNow;
                return await conn.ExecuteScalarAsync<int>(sql, config);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<bool> UpdateAssistantConfigAsync(PlivoAssistantConfig config)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    UPDATE PlivoAssistantConfigs SET
                        AssistantName     = @AssistantName,
                        Instructions      = @Instructions,
                        Voice             = @Voice,
                        Model             = @Model,
                        Temperature       = @Temperature,
                        MaxResponseTokens = @MaxResponseTokens,
                        VadThreshold      = @VadThreshold,
                        SilenceDurationMs = @SilenceDurationMs,
                        ExtraToolsJson    = @ExtraToolsJson,
                        IsActive          = @IsActive,
                        UpdatedAt         = @UpdatedAt,
                        UpdatedBy         = @UpdatedBy
                    WHERE Id = @Id";

                config.UpdatedAt = DateTime.UtcNow;
                int rows = await conn.ExecuteAsync(sql, config);
                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        /// <summary>
        /// Deactivates all configs and sets the given one as active.
        /// </summary>
        public async Task<bool> SetActiveAssistantAsync(int id)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.ExecuteAsync(
                    "UPDATE PlivoAssistantConfigs SET IsActive = 0");
                await conn.ExecuteAsync(
                    "UPDATE PlivoAssistantConfigs SET IsActive = 1, UpdatedAt = @Now WHERE Id = @Id",
                    new { Id = id, Now = DateTime.UtcNow });
                return true;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}
