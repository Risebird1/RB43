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
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class TransInterviewerAuditRepository : ITransInterviewerAuditRepository
    {
        private readonly string _connectionString;
        private readonly IApplicationErrorLogger _logger;

        public TransInterviewerAuditRepository(IConfiguration configuration, IApplicationErrorLogger logger)
        {
            _connectionString = configuration["ConnectionString:DBConnectionString"];
            _logger = logger;
        }

        public async Task InsertAsync(int transInterviewerId, string eventType, string fromValue, string toValue,
            string reasonCategory, int? triggeringInterviewId, string changedBy,
            string auditTag = null, string auditSource = null, Guid? backfillBatchId = null, string extraMetadata = null)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync("usp_InsertTransInterviewerAuditLog",
                    new
                    {
                        TransInterviewerId = transInterviewerId,
                        EventType = eventType,
                        FromValue = fromValue,
                        ToValue = toValue,
                        ReasonCategory = reasonCategory,
                        TriggeringInterviewId = triggeringInterviewId,
                        ChangedBy = changedBy ?? "System",
                        AuditTag = auditTag,
                        AuditSource = auditSource,
                        BackfillBatchId = backfillBatchId,
                        ExtraMetadata = extraMetadata
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task UpdateAutoQcFieldsAsync(int transInterviewerId, string autoQcVerdict, int triggeringInterviewId, string flagSource = null)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync("usp_UpdateTransInterviewerAutoQcFields",
                    new
                    {
                        TransInterviewerId = transInterviewerId,
                        AutoQcVerdict = autoQcVerdict,
                        AutoQcTriggeringInterviewId = triggeringInterviewId,
                        AutoQcFlagSource = flagSource
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task ClearAutoQcFieldsAsync(int transInterviewerId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync(
                    @"UPDATE dbo.TransInterviewer
                      SET AutoQcVerdict = NULL, AutoQcTriggeringInterviewId = NULL, AutoQcRemovedAt = NULL, AutoQcFlagSource = NULL
                      WHERE id = @TransInterviewerId",
                    new { TransInterviewerId = transInterviewerId },
                    commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<TransInterviewerInterviewContext> GetContextForInterviewAsync(int interviewId, int irfId = 0)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);

                TransInterviewerInterviewContext ctx = null;
                try
                {
                    ctx = await con.QueryFirstOrDefaultAsync<TransInterviewerInterviewContext>(
                        "usp_GetTransInterviewerContextForInterview",
                        new { InterviewId = interviewId, IrfId = irfId },
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 30);
                }
                catch (Exception spEx)
                {
                    await _logger.LogMessage(LogLevelInfo.Exception, spEx);
                }

                if (ctx != null)
                    return ctx;

                return await QueryTransInterviewerContextFallbackAsync(con, interviewId, irfId);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        private static async Task<TransInterviewerInterviewContext> QueryTransInterviewerContextFallbackAsync(
            SqlConnection con, int interviewId, int irfId)
        {
            const string sql = @";WITH InterviewRow AS (
    SELECT im.id, LTRIM(RTRIM(im.interviewer)) AS InterviewerRaw, im.IRFId
    FROM dbo.InterviewMeetings im WHERE im.id = @InterviewId
),
ResolvedUser AS (
    SELECT TOP 1 u.UserId, u.EmailId, u.Priority
    FROM InterviewRow ir
    CROSS APPLY (
        SELECT ul.id AS UserId, ul.EmailId, 1 AS Priority FROM dbo.UserList ul
        WHERE LTRIM(RTRIM(ul.EmailId)) = ir.InterviewerRaw
        UNION ALL
        SELECT ul.id, ul.EmailId, 2 FROM dbo.UserList ul WHERE ul.EmailId = ir.InterviewerRaw
        UNION ALL
        SELECT ip.UserId, ul.EmailId, 3 FROM dbo.InterviewerProfile ip
        INNER JOIN dbo.UserList ul ON ul.id = ip.UserId
        WHERE LTRIM(RTRIM(ip.NickName)) = ir.InterviewerRaw
    ) u
    WHERE ir.InterviewerRaw IS NOT NULL AND ir.InterviewerRaw <> ''
    ORDER BY u.Priority
)
SELECT TOP 1
    ti.id AS TransInterviewerId, ru.UserId AS UserId, ti.IRFId AS IrfId,
    ti.IsActive AS IsActive, ti.ClientAction AS ClientAction, ti.AutoQcVerdict AS AutoQcVerdict,
    ru.EmailId AS InterviewerEmail, ip.NickName AS InterviewerCodedId,
    CASE WHEN ti.IsSMEVerified = 1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsRbVetYes,
    CASE WHEN ti.IsSMEVerified = 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS RbVetAlreadyNo
FROM InterviewRow ir
INNER JOIN ResolvedUser ru ON 1 = 1
INNER JOIN dbo.IRF irf ON (@IrfId > 0 AND irf.id = @IrfId) OR (@IrfId <= 0 AND irf.IRFId = ir.IRFId)
INNER JOIN dbo.TransInterviewer ti ON ti.IRFId = irf.id
   AND (ti.UserId = ru.UserId
        OR EXISTS (SELECT 1 FROM dbo.Interviewer iv WHERE iv.id = ti.InterviewerId AND iv.UserId = ru.UserId)
        OR EXISTS (SELECT 1 FROM dbo.InterviewerProfile ipr WHERE ipr.id = ti.InterviewerId AND ipr.UserId = ru.UserId))
LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = ru.UserId
ORDER BY ti.id;";

            return await con.QueryFirstOrDefaultAsync<TransInterviewerInterviewContext>(
                sql, new { InterviewId = interviewId, IrfId = irfId }, commandTimeout: 30);
        }

        public async Task<int> GetFutureAssignedCountForInterviewAsync(int interviewId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<int>(
                    "usp_GetFutureAssignedInterviewsForInterview",
                    new { InterviewId = interviewId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<IReadOnlyList<TransInterviewerInactiveAuditSummary>> GetInactiveAuditSummariesByIrfIdAsync(int irfId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var results = await con.QueryAsync<TransInterviewerInactiveAuditSummary>(
                    "usp_GetTransInterviewerInactiveAuditSummariesByIrfId",
                    new { IrfId = irfId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
                return results?.ToList() ?? new List<TransInterviewerInactiveAuditSummary>();
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return Array.Empty<TransInterviewerInactiveAuditSummary>();
            }
        }

        public async Task<bool> HasAutoQcRemovalAuditForInterviewAsync(int transInterviewerId, int triggeringInterviewId, string auditSource = null)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var row = await con.QueryFirstOrDefaultAsync<dynamic>(
                    "usp_HasAutoQcRemovalAuditForInterview",
                    new
                    {
                        TransInterviewerId = transInterviewerId,
                        TriggeringInterviewId = triggeringInterviewId,
                        AuditSource = auditSource
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
                return row != null && (bool)row.AlreadyRecorded;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<bool> HasAutoQcFlagAuditForInterviewAsync(int transInterviewerId, int triggeringInterviewId, string auditSource = null)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                var row = await con.QueryFirstOrDefaultAsync<dynamic>(
                    "usp_HasAutoQcFlagAuditForInterview",
                    new
                    {
                        TransInterviewerId = transInterviewerId,
                        TriggeringInterviewId = triggeringInterviewId,
                        AuditSource = auditSource
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
                return row != null && (bool)row.AlreadyRecorded;
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<TransInterviewerInterviewContext> GetContextByTransInterviewerIdAsync(int transInterviewerId)
        {
            try
            {
                const string sql = @"
SELECT TOP 1
    ti.id AS TransInterviewerId,
    ti.UserId,
    ti.IRFId AS IrfId,
    ti.IsActive AS IsActive,
    ti.ClientAction AS ClientAction,
    ti.AutoQcVerdict AS AutoQcVerdict,
    ul.EmailId AS InterviewerEmail,
    ip.NickName AS InterviewerCodedId,
    CASE WHEN ti.IsSMEVerified = 1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsRbVetYes,
    CASE WHEN ti.IsSMEVerified = 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS RbVetAlreadyNo
FROM dbo.TransInterviewer ti
LEFT JOIN dbo.UserList ul ON ul.id = ti.UserId
LEFT JOIN dbo.InterviewerProfile ip ON ip.UserId = ti.UserId
WHERE ti.id = @TransInterviewerId;";

                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<TransInterviewerInterviewContext>(
                    sql, new { TransInterviewerId = transInterviewerId }, commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
