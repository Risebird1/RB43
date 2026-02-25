using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.Repository
{
    /// <summary>
    /// Repository implementation for Vapi call operations
    /// </summary>
    public class VapiCallRepository : IVapiCallRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public VapiCallRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        /// <summary>
        /// Saves a new Vapi call log
        /// </summary>
        public async Task<int> SaveCallLogAsync(VapiCallLog callLog)
        {
            try
            {
                await LogRepositoryCheckpoint("REPO-A", $"SaveCallLogAsync called - VapiCallId: {callLog.VapiCallId}, RecipientPhone: {callLog.RecipientPhone}, RecipientName: {callLog.RecipientName}", callLog.VapiCallId);

                using (var conn = new SqlConnection(_connectionString))
                {
                    await LogRepositoryCheckpoint("REPO-B", "Connection created, about to execute INSERT", callLog.VapiCallId);

                    var sql = @"
                        INSERT INTO VapiCallLogs (
                            VapiCallId, InterviewId, IRFId, RecipientId, RecipientPhone, RecipientName, RecipientEmail,
                            CallStatus, ScheduledDateTime, CallStartedAt, CallEndedAt, EndedReason, DurationSeconds,
                            Transcript, RecordingUrl, InterviewBooked, BookedInterviewDateTime, BookingConfirmationDetails,
                            CallCost, AssistantId, Summary, StructuredData, CreatedAt, CreatedBy
                        )
                        VALUES (
                            @VapiCallId, @InterviewId, @IRFId, @RecipientId, @RecipientPhone, @RecipientName, @RecipientEmail,
                            @CallStatus, @ScheduledDateTime, @CallStartedAt, @CallEndedAt, @EndedReason, @DurationSeconds,
                            @Transcript, @RecordingUrl, @InterviewBooked, @BookedInterviewDateTime, @BookingConfirmationDetails,
                            @CallCost, @AssistantId, @Summary, @StructuredData, @CreatedAt, @CreatedBy
                        );
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                    var id = await conn.ExecuteScalarAsync<int>(sql, callLog);

                    await LogRepositoryCheckpoint("REPO-C-SUCCESS", $"INSERT executed successfully - New ID: {id}", callLog.VapiCallId);

                    return id;
                }
            }
            catch (Exception ex)
            {
                await LogRepositoryCheckpoint("REPO-ERROR", $"SaveCallLogAsync failed - {ex.Message}", callLog?.VapiCallId, $"InnerException: {ex.InnerException?.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Logs repository checkpoint to database
        /// </summary>
        private async Task LogRepositoryCheckpoint(string checkpointName, string message, string callId = null, string additionalData = null)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var sql = @"INSERT INTO VapiWebhookCheckpoints 
                        (Timestamp, CheckpointName, Message, CallId, ToolCallName, AdditionalData)
                        VALUES (@Timestamp, @CheckpointName, @Message, @CallId, @ToolCallName, @AdditionalData)";

                    await conn.ExecuteAsync(sql, new
                    {
                        Timestamp = DateTime.UtcNow,
                        CheckpointName = checkpointName,
                        Message = message,
                        CallId = callId,
                        ToolCallName = "Repository",
                        AdditionalData = additionalData?.Length > 5000 ? additionalData.Substring(0, 5000) : additionalData
                    });
                }
            }
            catch
            {
                // Silently fail
            }
        }

        /// <summary>
        /// Updates an existing Vapi call log
        /// </summary>
        public async Task<bool> UpdateCallLogAsync(VapiCallLog callLog)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var sql = @"
                        UPDATE VapiCallLogs
                        SET IRFId = @IRFId,
                            CallStatus = @CallStatus,
                            CallStartedAt = @CallStartedAt,
                            CallEndedAt = @CallEndedAt,
                            EndedReason = @EndedReason,
                            DurationSeconds = @DurationSeconds,
                            Transcript = @Transcript,
                            RecordingUrl = @RecordingUrl,
                            InterviewBooked = @InterviewBooked,
                            BookedInterviewDateTime = @BookedInterviewDateTime,
                            BookingConfirmationDetails = @BookingConfirmationDetails,
                            CallCost = @CallCost,
                            Summary = @Summary,
                            StructuredData = @StructuredData,
                            UpdatedAt = @UpdatedAt,
                            UpdatedBy = @UpdatedBy
                        WHERE VapiCallId = @VapiCallId";

                    await conn.ExecuteAsync(sql, callLog);
                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        /// <summary>
        /// Gets call log by Vapi call ID
        /// </summary>
        public async Task<VapiCallLog> GetCallLogByVapiCallIdAsync(string vapiCallId)
        {
            try
            {
                await LogRepositoryCheckpoint("REPO-D", $"GetCallLogByVapiCallIdAsync called - Looking for VapiCallId: {vapiCallId}", vapiCallId);

                using (var conn = new SqlConnection(_connectionString))
                {
                    var sql = "SELECT * FROM VapiCallLogs WHERE VapiCallId = @VapiCallId";
                    var result = await conn.QueryFirstOrDefaultAsync<VapiCallLog>(sql, new { VapiCallId = vapiCallId });

                    await LogRepositoryCheckpoint("REPO-E", $"Query executed - Result found: {result != null}, Result ID: {result?.Id ?? 0}", vapiCallId);

                    return result;
                }
            }
            catch (Exception ex)
            {
                await LogRepositoryCheckpoint("REPO-ERROR", $"GetCallLogByVapiCallIdAsync failed - {ex.Message}", vapiCallId, ex.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Gets call log by internal ID
        /// </summary>
        public async Task<VapiCallLog> GetCallLogByIdAsync(int id)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var sql = "SELECT * FROM VapiCallLogs WHERE Id = @Id";
                    return await conn.QueryFirstOrDefaultAsync<VapiCallLog>(sql, new { Id = id });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// Gets all call logs for an interview
        /// </summary>
        public async Task<List<VapiCallLog>> GetCallLogsByInterviewIdAsync(int interviewId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var sql = "SELECT * FROM VapiCallLogs WHERE InterviewId = @InterviewId ORDER BY CreatedAt DESC";
                    var results = await conn.QueryAsync<VapiCallLog>(sql, new { InterviewId = interviewId });
                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<VapiCallLog>();
            }
        }

        /// <summary>
        /// Gets all call logs for a recipient
        /// </summary>
        public async Task<List<VapiCallLog>> GetCallLogsByRecipientIdAsync(int recipientId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var sql = "SELECT * FROM VapiCallLogs WHERE RecipientId = @RecipientId ORDER BY CreatedAt DESC";
                    var results = await conn.QueryAsync<VapiCallLog>(sql, new { RecipientId = recipientId });
                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<VapiCallLog>();
            }
        }

        /// <summary>
        /// Gets recent call logs
        /// </summary>
        public async Task<List<VapiCallLog>> GetRecentCallLogsAsync(int count = 50)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var sql = "SELECT TOP (@Count) * FROM VapiCallLogs ORDER BY CreatedAt DESC";
                    var results = await conn.QueryAsync<VapiCallLog>(sql, new { Count = count });
                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<VapiCallLog>();
            }
        }

        /// <summary>
        /// Updates call booking status
        /// </summary>
        public async Task<bool> UpdateBookingStatusAsync(string vapiCallId, bool isBooked, DateTime? bookedDateTime, string confirmationDetails)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var sql = @"
                        UPDATE VapiCallLogs
                        SET InterviewBooked = @IsBooked,
                            BookedInterviewDateTime = @BookedDateTime,
                            BookingConfirmationDetails = @ConfirmationDetails,
                            UpdatedAt = @UpdatedAt
                        WHERE VapiCallId = @VapiCallId";

                    await conn.ExecuteAsync(sql, new
                    {
                        VapiCallId = vapiCallId,
                        IsBooked = isBooked,
                        BookedDateTime = bookedDateTime,
                        ConfirmationDetails = confirmationDetails,
                        UpdatedAt = DateTime.UtcNow
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<CandidateDetailsForSchdeule> GetCandidateDetailsForSchedule(int irfid, string CandidateEmail)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();

                    parameters.Add("@irfid", irfid);
                    parameters.Add("@CandidateEmail", CandidateEmail);
                    var result = await SqlMapper.QueryAsync<CandidateDetailsForSchdeule>(conn, "usp_GetCandidateDetailsForSchedule", parameters,
                         commandType: CommandType.StoredProcedure);
                    return result.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// Returns true if a confirmed interview already exists in InterviewMeetings
        /// for the given candidate email and IRF (identified by IRF.id).
        /// Mirrors the eligibility check in usp_GetEligibleCandidatesForVapiCalls.
        /// </summary>
        public async Task<bool> HasConfirmedInterviewAsync(string candidateEmail, int irfId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IrfId", irfId);
                    parameters.Add("@CandidateEmail", candidateEmail);

                    var result = await conn.ExecuteScalarAsync<bool>(
                        "usp_HasConfirmedInterview",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return result;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        /// <summary>
        /// Gets eligible candidates for Vapi automated interview calls
        /// Returns candidates who:
        /// - Don't have a confirmed interview (confirmationStatus = 1 in InterviewMeetings)
        /// - Haven't been called more than 2 times
        /// - Have valid contact information
        /// </summary>
        public async Task<List<VapiEligibleCandidateVM>> GetEligibleCandidatesForVapiCallsAsync(int count = 10)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var results = await conn.QueryAsync<VapiEligibleCandidateVM>(
                        "usp_GetEligibleCandidatesForVapiCalls",
                        new { Count = count },
                        commandType: CommandType.StoredProcedure);

                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<VapiEligibleCandidateVM>();
            }
        }
    }
}
