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
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public async Task InsertAsync(int transInterviewerId, string eventType, string fromValue, string toValue,
            string reasonCategory, int? triggeringInterviewId, string changedBy)
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
                        ChangedBy = changedBy ?? "System"
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task UpdateAutoQcFieldsAsync(int transInterviewerId, string autoQcVerdict, int triggeringInterviewId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                await con.ExecuteAsync("usp_UpdateTransInterviewerAutoQcFields",
                    new
                    {
                        TransInterviewerId = transInterviewerId,
                        AutoQcVerdict = autoQcVerdict,
                        AutoQcTriggeringInterviewId = triggeringInterviewId
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
                      SET AutoQcVerdict = NULL, AutoQcTriggeringInterviewId = NULL, AutoQcRemovedAt = NULL
                      WHERE id = @TransInterviewerId",
                    new { TransInterviewerId = transInterviewerId },
                    commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<TransInterviewerInterviewContext> GetContextForInterviewAsync(int interviewId)
        {
            try
            {
                using var con = new SqlConnection(_connectionString);
                return await con.QueryFirstOrDefaultAsync<TransInterviewerInterviewContext>(
                    "usp_GetTransInterviewerContextForInterview",
                    new { InterviewId = interviewId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
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
    }
}
