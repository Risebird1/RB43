using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
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
                using (var conn = new SqlConnection(_connectionString))
                {
                    var sql = @"
                        INSERT INTO VapiCallLogs (
                            VapiCallId, InterviewId, RecipientId, RecipientPhone, RecipientName, RecipientEmail,
                            CallStatus, ScheduledDateTime, CallStartedAt, CallEndedAt, EndedReason, DurationSeconds,
                            Transcript, RecordingUrl, InterviewBooked, BookedInterviewDateTime, BookingConfirmationDetails,
                            CallCost, AssistantId, Summary, StructuredData, CreatedAt, CreatedBy
                        )
                        VALUES (
                            @VapiCallId, @InterviewId, @RecipientId, @RecipientPhone, @RecipientName, @RecipientEmail,
                            @CallStatus, @ScheduledDateTime, @CallStartedAt, @CallEndedAt, @EndedReason, @DurationSeconds,
                            @Transcript, @RecordingUrl, @InterviewBooked, @BookedInterviewDateTime, @BookingConfirmationDetails,
                            @CallCost, @AssistantId, @Summary, @StructuredData, @CreatedAt, @CreatedBy
                        );
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                    var id = await conn.ExecuteScalarAsync<int>(sql, callLog);
                    return id;
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                throw;
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
                        SET CallStatus = @CallStatus,
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
                using (var conn = new SqlConnection(_connectionString))
                {
                    var sql = "SELECT * FROM VapiCallLogs WHERE VapiCallId = @VapiCallId";
                    return await conn.QueryFirstOrDefaultAsync<VapiCallLog>(sql, new { VapiCallId = vapiCallId });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
    }
}
