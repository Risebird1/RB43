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
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public PlivoCXCallRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
        }

        // ── Rich call log view (for the Index page) ───────────────────────────────

        public async Task<List<PlivoCXCallLogDetailVM>> GetCallLogDetailsAsync(
            int page = 1, int pageSize = 50,
            string status = null, string purpose = null,
            string meeting = null, string disposition = null,
            DateTime? dateFrom = null, DateTime? dateTo = null,
            string clientName = null)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<PlivoCXCallLogDetailVM>(
                    "usp_GetPlivoCXCallLogDetails",
                    new
                    {
                        Page = page,
                        PageSize = pageSize,
                        Status = string.IsNullOrWhiteSpace(status) ? null : status,
                        Purpose = string.IsNullOrWhiteSpace(purpose) ? null : purpose,
                        Meeting = string.IsNullOrWhiteSpace(meeting) ? null : meeting,
                        Disposition = string.IsNullOrWhiteSpace(disposition) ? null : disposition,
                        DateFrom = dateFrom,
                        DateTo = dateTo,
                        ClientName = string.IsNullOrWhiteSpace(clientName) ? null : clientName
                    },
                    commandType: CommandType.StoredProcedure);
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCXCallLogDetailVM>();
            }
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
                        CallStatus, InterviewBooked, CallPurpose,
                        CreatedAt, TriggeredBy
                    ) VALUES (
                        @PlivoCXConversationId, @PlivoCXRequestId,
                        @RecipientName, @RecipientPhone, @RecipientEmail,
                        @IRFId, @CandidateId, @JobTitle, @ClientName,
                        @CallStatus, @InterviewBooked, @CallPurpose,
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
                        CandidateDisposition     = @CandidateDisposition,
                        CallPurpose              = @CallPurpose,
                        CallStartedAt            = @CallStartedAt,
                        CallEndedAt              = @CallEndedAt,
                        UpdatedAt                = @UpdatedAt
                    WHERE Id = @Id";

                callLog.UpdatedAt = DateTime.UtcNow;
                int rows = await conn.ExecuteAsync(sql, callLog);

                // Recording capture (PlivoCallUuid / RecordingUrl) is written separately so that a
                // database without the new columns yet can never block the main call-log update.
                // COALESCE keeps any stored value when a later update does not carry it.
                if (rows > 0 && (!string.IsNullOrWhiteSpace(callLog.PlivoCallUuid) || !string.IsNullOrWhiteSpace(callLog.RecordingUrl)))
                {
                    try
                    {
                        const string recordingSql = @"
                            UPDATE PlivoCXCallLogs SET
                                PlivoCallUuid = COALESCE(@PlivoCallUuid, PlivoCallUuid),
                                RecordingUrl  = COALESCE(@RecordingUrl,  RecordingUrl)
                            WHERE Id = @Id";
                        await conn.ExecuteAsync(recordingSql, new
                        {
                            callLog.Id,
                            PlivoCallUuid = string.IsNullOrWhiteSpace(callLog.PlivoCallUuid) ? null : callLog.PlivoCallUuid,
                            RecordingUrl  = string.IsNullOrWhiteSpace(callLog.RecordingUrl)  ? null : callLog.RecordingUrl
                        });
                    }
                    catch (Exception recEx)
                    {
                        await _logger.LogMessage(LogLevelInfo.Exception, recEx);
                    }
                }

                return rows > 0;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<List<PlivoCXCallLog>> GetCallLogsMissingRecordingAsync(int maxAgeHours = 24, int count = 50)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    SELECT TOP (@Count) *
                    FROM   PlivoCXCallLogs
                    WHERE  PlivoCallUuid IS NOT NULL
                      AND  RecordingUrl  IS NULL
                      AND  CallEndedAt   >= DATEADD(HOUR, -@MaxAgeHours, GETUTCDATE())
                    ORDER BY CallEndedAt DESC";
                var results = await conn.QueryAsync<PlivoCXCallLog>(sql, new { Count = count, MaxAgeHours = maxAgeHours });
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCXCallLog>();
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

        public async Task<PlivoCXCallLog> GetCallLogByRequestIdAsync(string requestId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryFirstOrDefaultAsync<PlivoCXCallLog>(
                    "SELECT * FROM PlivoCXCallLogs WHERE PlivoCXRequestId = @RequestId",
                    new { RequestId = requestId });
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

        // ── Debug / checkpoint logs ───────────────────────────────────────────────

        // Debug logging disabled — testing phase complete, no longer writing to PlivoCXWebhookDebugLogs
        public Task<int> SaveWebhookDebugLogAsync(PlivoCXWebhookDebugLog log)
        {
            // try
            // {
            //     using var conn = new SqlConnection(_connectionString);
            //     const string sql = @"
            //         INSERT INTO PlivoCXWebhookDebugLogs
            //             (CreatedAt, LogType, HttpStatusCode, PlivoCXId, CallLogId, RawPayload, ProcessingStatus, ErrorMessage)
            //         VALUES
            //             (@CreatedAt, @LogType, @HttpStatusCode, @PlivoCXId, @CallLogId, @RawPayload, @ProcessingStatus, @ErrorMessage);
            //         SELECT CAST(SCOPE_IDENTITY() AS INT);";
            //
            //     log.CreatedAt = DateTime.UtcNow;
            //     return await conn.ExecuteScalarAsync<int>(sql, log);
            // }
            // catch (Exception ex)
            // {
            //     await _logger.LogMessage(LogLevelInfo.Exception, ex);
            //     return 0;
            // }
            return Task.FromResult(0);
        }

        public async Task<List<PlivoCXWebhookDebugLog>> GetRecentDebugLogsAsync(int count = 50)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<PlivoCXWebhookDebugLog>(
                    "SELECT TOP (@Count) * FROM PlivoCXWebhookDebugLogs ORDER BY CreatedAt DESC",
                    new { Count = count });
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCXWebhookDebugLog>();
            }
        }

        // ── Webhook checkpoints ───────────────────────────────────────────────────

        // Checkpoint logging disabled — testing phase complete, no longer writing to PlivoCXWebhookCheckpoints
        public async Task<int> SaveCheckpointAsync(PlivoCXWebhookCheckpoint checkpoint)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    INSERT INTO PlivoCXWebhookCheckpoints
                        (Timestamp, CheckpointName, Message, ConversationId, CallLogId, AdditionalData)
                    VALUES
                        (@Timestamp, @CheckpointName, @Message, @ConversationId, @CallLogId, @AdditionalData);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                checkpoint.Timestamp = DateTime.UtcNow;
                return await conn.ExecuteScalarAsync<int>(sql, checkpoint);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<List<PlivoCXWebhookCheckpoint>> GetRecentCheckpointsAsync(int count = 100)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<PlivoCXWebhookCheckpoint>(
                    "SELECT TOP (@Count) * FROM PlivoCXWebhookCheckpoints ORDER BY Timestamp DESC",
                    new { Count = count });
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<PlivoCXWebhookCheckpoint>();
            }
        }

        // ── Candidate helpers ─────────────────────────────────────────────────────

        public async Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForRescheduleAsync(int count = 10)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<VapiEligibleCandidateVM>(
                    "usp_GetEligibleCandidatesForPlivoCXReschedule",
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

        public async Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForCallsAsync(int count = 10)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<VapiEligibleCandidateVM>(
                    "usp_GetEligibleCandidatesForPlivoCXCalls",
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

        public async Task IncrementCallAttemptsAsync(int candidateId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
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
                using var conn = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@CandidateEmail", candidateEmail, DbType.String);
                parameters.Add("@IRFId", irfId, DbType.Int32);
                parameters.Add("@HasConfirmed", dbType: DbType.Boolean, direction: ParameterDirection.Output);
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

        public async Task<string> GetCandidateEmailByIdAsync(int candidateId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    SELECT TOP 1 ul.EmailId
                    FROM   Candidate        ca
                    JOIN   CandidatesMaster cm ON cm.id    = ca.CandidateMasterId
                    JOIN   UserList         ul ON ul.id    = cm.UserId
                    WHERE  ca.id = @CandidateId";
                return await conn.ExecuteScalarAsync<string>(sql, new { CandidateId = candidateId });
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<string>> GetAllClientNamesAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<string>(
                    "SELECT Name FROM Client WHERE Name IS NOT NULL ORDER BY Name");
                return results.ToList();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<string>();
            }
        }
    }
}
