using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class PhoneScreenRepository : IPhoneScreenRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public PhoneScreenRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _applicationErrorLogger = applicationErrorLogger;
        }

        // ────────────────────────────────────────────────────────────────────
        // Skill channel (IRF) -> Plivo assistant URL mapping
        // ────────────────────────────────────────────────────────────────────

        public async Task<string> GetIRFPlivoAssistantUrlAsync(int irfId)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var result = await con.QueryFirstOrDefaultAsync<string>(
                        "SELECT PlivoAssistantUrl FROM IRF WHERE id = @id",
                        new { id = irfId });
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> SetIRFPlivoAssistantUrlAsync(int? irfId, string irfCode, string url)
        {
            try
            {
                // Match by internal id when known (edit), else by business IRFId (create).
                const string sql = @"
UPDATE IRF
   SET PlivoAssistantUrl = @url
 WHERE (@id IS NOT NULL AND id = @id)
    OR (@id IS NULL AND @irfCode IS NOT NULL AND IRFId = @irfCode);";

                using (var con = new SqlConnection(_connectionString))
                {
                    var rows = await con.ExecuteAsync(sql, new
                    {
                        id = irfId,
                        irfCode,
                        url = string.IsNullOrWhiteSpace(url) ? null : url.Trim()
                    });
                    return rows > 0;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<string> GetIRFPlivoWhatsAppAssistantUrlAsync(int irfId)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    var result = await con.QueryFirstOrDefaultAsync<string>(
                        "SELECT PlivoWhatsAppAssistantUrl FROM IRF WHERE id = @id",
                        new { id = irfId });
                    return result;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> SetIRFPlivoWhatsAppAssistantUrlAsync(int? irfId, string irfCode, string url)
        {
            try
            {
                // Match by internal id when known (edit), else by business IRFId (create).
                const string sql = @"
UPDATE IRF
   SET PlivoWhatsAppAssistantUrl = @url
 WHERE (@id IS NOT NULL AND id = @id)
    OR (@id IS NULL AND @irfCode IS NOT NULL AND IRFId = @irfCode);";

                using (var con = new SqlConnection(_connectionString))
                {
                    var rows = await con.ExecuteAsync(sql, new
                    {
                        id = irfId,
                        irfCode,
                        url = string.IsNullOrWhiteSpace(url) ? null : url.Trim()
                    });
                    return rows > 0;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<PhoneScreenIRFHeader> GetIRFHeaderAsync(int irfId)
        {
            try
            {
                const string sql = @"
SELECT TOP 1
       id                AS Id,
       IRFId             AS IRFCode,
       JDTiltle          AS IRFName,
       ClientName        AS ClientName,
       PlivoAssistantUrl AS PlivoAssistantUrl,
       PlivoWhatsAppAssistantUrl AS PlivoWhatsAppAssistantUrl,
       StartDate         AS StartDate
  FROM IRF
 WHERE id = @id;";

                using (var con = new SqlConnection(_connectionString))
                {
                    return await con.QueryFirstOrDefaultAsync<PhoneScreenIRFHeader>(sql, new { id = irfId });
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // Phone screen call records
        // ────────────────────────────────────────────────────────────────────

        public async Task<int> InsertCallAsync(PhoneScreenCall call)
        {
            try
            {
                const string sql = @"
INSERT INTO PhoneScreenCalls
    (Provider, IRFId, CandidateId, CandidateMasterId, CandidateName, CandidateEmail, Phone,
     ExternalCallId, ConversationId, Status, StartedAt, CreatedAt, CreatedBy)
VALUES
    (@Provider, @IRFId, @CandidateId, @CandidateMasterId, @CandidateName, @CandidateEmail, @Phone,
     @ExternalCallId, @ConversationId, @Status, @StartedAt, GETUTCDATE(), @CreatedBy);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                using (var con = new SqlConnection(_connectionString))
                {
                    return await con.ExecuteScalarAsync<int>(sql, new
                    {
                        call.Provider,
                        call.IRFId,
                        call.CandidateId,
                        call.CandidateMasterId,
                        call.CandidateName,
                        call.CandidateEmail,
                        call.Phone,
                        call.ExternalCallId,
                        call.ConversationId,
                        call.Status,
                        call.StartedAt,
                        call.CreatedBy
                    });
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<PhoneScreenCall> GetCallByIdAsync(int id)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    return await con.QueryFirstOrDefaultAsync<PhoneScreenCall>(
                        "SELECT * FROM PhoneScreenCalls WHERE Id = @id", new { id });
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<PhoneScreenCall> GetCallByConversationIdAsync(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId)) return null;
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    return await con.QueryFirstOrDefaultAsync<PhoneScreenCall>(
                        "SELECT TOP 1 * FROM PhoneScreenCalls WHERE ConversationId = @conversationId ORDER BY Id DESC",
                        new { conversationId });
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<PhoneScreenCall> GetCallByExternalCallIdAsync(string externalCallId)
        {
            if (string.IsNullOrWhiteSpace(externalCallId)) return null;
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    return await con.QueryFirstOrDefaultAsync<PhoneScreenCall>(
                        "SELECT TOP 1 * FROM PhoneScreenCalls WHERE ExternalCallId = @externalCallId ORDER BY Id DESC",
                        new { externalCallId });
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<IEnumerable<PhoneScreenCall>> GetCallsByIRFAsync(int irfId)
        {
            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    return await con.QueryAsync<PhoneScreenCall>(
                        "SELECT * FROM PhoneScreenCalls WHERE IRFId = @irfId ORDER BY Id DESC",
                        new { irfId });
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Enumerable.Empty<PhoneScreenCall>();
            }
        }

        public async Task<bool> UpdateCallTriggerResultAsync(int id, string status, string conversationId, string externalCallId)
        {
            try
            {
                const string sql = @"
UPDATE PhoneScreenCalls
   SET Status         = @status,
       ConversationId = COALESCE(@conversationId, ConversationId),
       ExternalCallId = COALESCE(@externalCallId, ExternalCallId),
       UpdatedAt      = GETUTCDATE()
 WHERE Id = @id;";

                using (var con = new SqlConnection(_connectionString))
                {
                    var rows = await con.ExecuteAsync(sql, new
                    {
                        id,
                        status,
                        conversationId = string.IsNullOrWhiteSpace(conversationId) ? null : conversationId,
                        externalCallId = string.IsNullOrWhiteSpace(externalCallId) ? null : externalCallId
                    });
                    return rows > 0;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        /// <summary>
        /// Merge-update a call row from a webhook payload. Only non-null incoming
        /// values overwrite existing columns (COALESCE), so partial webhooks and
        /// out-of-order delivery never wipe previously stored data.
        /// </summary>
        public async Task<bool> UpdateCallFromWebhookAsync(PhoneScreenCall data)
        {
            try
            {
                const string sql = @"
UPDATE PhoneScreenCalls
   SET Status            = COALESCE(@Status, Status),
       EndedReason       = COALESCE(@EndedReason, EndedReason),
       ConversationId    = COALESCE(@ConversationId, ConversationId),
       ExternalCallId    = COALESCE(@ExternalCallId, ExternalCallId),
       EndedAt           = COALESCE(@EndedAt, EndedAt),
       DurationSeconds   = COALESCE(@DurationSeconds, DurationSeconds),
       RecordingUrl      = COALESCE(@RecordingUrl, RecordingUrl),
       Transcript        = COALESCE(@Transcript, Transcript),
       Summary           = COALESCE(@Summary, Summary),
       SuccessEvaluation = COALESCE(@SuccessEvaluation, SuccessEvaluation),
       Disposition       = COALESCE(@Disposition, Disposition),
       Qualified         = COALESCE(@Qualified, Qualified),
       FailedCriteria    = COALESCE(@FailedCriteria, FailedCriteria),
       Availability      = COALESCE(@Availability, Availability),
       StructuredData    = COALESCE(@StructuredData, StructuredData),
       RawPayload        = COALESCE(@RawPayload, RawPayload),
       Cost              = COALESCE(@Cost, Cost),
       UpdatedAt         = GETUTCDATE()
 WHERE Id = @Id;";

                using (var con = new SqlConnection(_connectionString))
                {
                    var rows = await con.ExecuteAsync(sql, new
                    {
                        data.Id,
                        data.Status,
                        data.EndedReason,
                        data.ConversationId,
                        data.ExternalCallId,
                        data.EndedAt,
                        data.DurationSeconds,
                        data.RecordingUrl,
                        data.Transcript,
                        data.Summary,
                        data.SuccessEvaluation,
                        data.Disposition,
                        data.Qualified,
                        data.FailedCriteria,
                        data.Availability,
                        data.StructuredData,
                        data.RawPayload,
                        data.Cost
                    });
                    return rows > 0;
                }
            }
            catch (SqlException ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
    }
}
