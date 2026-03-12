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

        // ── Rich call log view (for the Index page) ───────────────────────────────

        public async Task<List<PlivoCXCallLogDetailVM>> GetCallLogDetailsAsync(int count = 200)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                // The LEFT JOIN uses BookedInterviewDateTime (the date the AI extracted and
                // posted to the Schedule API) rather than CreatedAt.  This ensures that only
                // a meeting whose interviewDate falls on the same calendar day as the slot the
                // Plivo agent actually booked is considered "created by this call".
                // Meetings that exist for unrelated reasons are excluded.
                // AIReportedBooked is also derived from whether that matched meeting row exists,
                // replacing the stored pcl.InterviewBooked flag which may lag or be inaccurate.
                const string sql = @"
                    SELECT TOP (@Count)
                        pcl.Id                          AS CallLogId,
                        pcl.RecipientEmail              AS CandidateEmail,
                        pcl.RecipientName               AS CandidateName,
                        pcl.RecipientPhone,
                        irf.IRFId,
                        irf.JDTiltle,
                        pcl.ClientName,
                        pcl.TriggeredBy,
                        pcl.PlivoCXConversationId,
                        pcl.CallPurpose,
                        pcl.CandidateDisposition,

                        pcl.CallStatus,
                        pcl.DurationSeconds,
                        pcl.HangupCause,
                        pcl.CreatedAt                   AS CallInitiatedAt,
                        pcl.CallEndedAt,

                        -- Derived from meeting existence (not the stored flag)
                        CASE WHEN im.id IS NOT NULL THEN CAST(1 AS BIT)
                             ELSE CAST(0 AS BIT) END    AS AIReportedBooked,
                        pcl.BookedInterviewDate,
                        pcl.BookedInterviewTime,
                        pcl.BookedInterviewDateTime,
                        pcl.BookingApiSuccess,
                        pcl.BookingApiResponse,
                        pcl.CallTranscript,
                        pcl.ConversationSummary,

                        CASE WHEN im.id IS NOT NULL THEN 'YES' ELSE 'NO' END
                                                        AS MeetingCreated,
                        im.id                           AS MeetingId,
                        im.interviewDate                AS MeetingScheduledAt,
                        im.interviewSlot,
                        im.interviewDuration,
                        im.confirmationStatus           AS MeetingConfirmationStatus,
                        im.interviewer,
                        im.risebirdHR,
                        im.ClientHrContact,
                        im.isAutoCancelled,
                        im.CancellationReason,
                        im.IsCandidateResponsed,
                        im.CandidateResponse,
                        im.dtCreatedOn                  AS MeetingCreatedOn

                    FROM      PlivoCXCallLogs   pcl
                    JOIN      IRF               irf ON irf.id            = pcl.IRFId
                    LEFT JOIN InterviewMeetings  im
                           ON im.candidateEmail    = pcl.RecipientEmail
                          AND im.IRFId             = irf.IRFId
                          -- Only match a meeting whose date+time equals the slot the AI booked.
                          -- SMALLDATETIME truncates to the minute, matching the slot boundary
                          -- without being tripped by sub-second storage differences.
                          -- Requires BookedInterviewDateTime to be non-NULL (i.e. booking happened).
                          AND pcl.BookedInterviewDateTime IS NOT NULL
                          AND CAST(im.interviewDate AS SMALLDATETIME)
                            = CAST(pcl.BookedInterviewDateTime AS SMALLDATETIME)
                    ORDER BY pcl.CreatedAt DESC";

                var results = await conn.QueryAsync<PlivoCXCallLogDetailVM>(sql, new { Count = count });
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

        public async Task<int> SaveWebhookDebugLogAsync(PlivoCXWebhookDebugLog log)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                const string sql = @"
                    INSERT INTO PlivoCXWebhookDebugLogs
                        (CreatedAt, LogType, HttpStatusCode, PlivoCXId, CallLogId, RawPayload, ProcessingStatus, ErrorMessage)
                    VALUES
                        (@CreatedAt, @LogType, @HttpStatusCode, @PlivoCXId, @CallLogId, @RawPayload, @ProcessingStatus, @ErrorMessage);
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

        public async Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForCallsAsync(int count = 10)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var results = await conn.QueryAsync<VapiEligibleCandidateVM>(
                    "usp_GetEligibleCandidatesForPlivoCXCalls",
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
                // Candidate.id → CandidatesMaster → UserList.EmailId
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
    }
}
