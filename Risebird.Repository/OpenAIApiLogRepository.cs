using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    public class OpenAIApiLogRepository : IOpenAIApiLogRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public OpenAIApiLogRepository(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        public async Task<long> LogApiCallAsync(OpenAIApiLog log)
        {
            try
            {
                var sql = @"
                    INSERT INTO OpenAIApiLogs 
                    (LogDate, ApiEndpoint, ApiMethod, Model, AssistantId, ThreadId, RunId, FileId, SessionId, 
                     RequestPayload, ResponsePayload, PromptTokens, CompletionTokens, TotalTokens, EstimatedCostUSD, 
                     ResponseTimeMs, StatusCode, IsSuccess, ErrorMessage, UserId, CandidateId, InterviewerId, 
                     IRFId, MeetingId, OperationType, CreatedBy, CreatedAt)
                    VALUES 
                    (@LogDate, @ApiEndpoint, @ApiMethod, @Model, @AssistantId, @ThreadId, @RunId, @FileId, @SessionId, 
                     @RequestPayload, @ResponsePayload, @PromptTokens, @CompletionTokens, @TotalTokens, @EstimatedCostUSD, 
                     @ResponseTimeMs, @StatusCode, @IsSuccess, @ErrorMessage, @UserId, @CandidateId, @InterviewerId, 
                     @IRFId, @MeetingId, @OperationType, @CreatedBy, @CreatedAt);
                    SELECT CAST(SCOPE_IDENTITY() as bigint);";

                using var conn = new SqlConnection(connectionString);
                return await conn.ExecuteScalarAsync<long>(sql, log);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<OpenAIApiLog> GetLogByIdAsync(long id)
        {
            try
            {
                var sql = "SELECT * FROM OpenAIApiLogs WHERE Id = @Id";
                using var conn = new SqlConnection(connectionString);
                return await conn.QueryFirstOrDefaultAsync<OpenAIApiLog>(sql, new { Id = id });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<List<OpenAIApiLog>> GetLogsBySessionIdAsync(Guid sessionId)
        {
            try
            {
                var sql = "SELECT * FROM OpenAIApiLogs WHERE SessionId = @SessionId ORDER BY LogDate DESC";
                using var conn = new SqlConnection(connectionString);
                var result = await conn.QueryAsync<OpenAIApiLog>(sql, new { SessionId = sessionId });
                return result?.ToList() ?? new List<OpenAIApiLog>();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpenAIApiLog>();
            }
        }

        public async Task<List<OpenAIApiLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sql = "SELECT * FROM OpenAIApiLogs WHERE LogDate >= @StartDate AND LogDate <= @EndDate ORDER BY LogDate DESC";
                using var conn = new SqlConnection(connectionString);
                var result = await conn.QueryAsync<OpenAIApiLog>(sql, new { StartDate = startDate, EndDate = endDate });
                return result?.ToList() ?? new List<OpenAIApiLog>();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpenAIApiLog>();
            }
        }

        public async Task<(List<OpenAIApiLog> logs, int totalCount)> GetLogsByDateRangePagedAsync(DateTime startDate, DateTime endDate, int pageNumber, int pageSize, string sortBy = "LogDate", string sortOrder = "desc")
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                
                // Get total count
                var countSql = "SELECT COUNT(*) FROM OpenAIApiLogs WHERE LogDate >= @StartDate AND LogDate <= @EndDate";
                var totalCount = await conn.ExecuteScalarAsync<int>(countSql, new { StartDate = startDate, EndDate = endDate });
                
                // Validate sort column to prevent SQL injection
                var validSortColumns = new[] { "LogDate", "OperationType", "Model", "IsSuccess", "ResponseTimeMs", "TotalTokens", "PromptTokens", "CompletionTokens", "EstimatedCostUSD" };
                if (!validSortColumns.Contains(sortBy))
                {
                    sortBy = "LogDate";
                }
                
                // Validate sort order
                sortOrder = sortOrder.ToLower() == "asc" ? "ASC" : "DESC";
                
                // Build ORDER BY clause
                var orderByClause = $"ORDER BY {sortBy} {sortOrder}";
                
                // Get paged data
                var sql = $@"
                    SELECT * FROM OpenAIApiLogs 
                    WHERE LogDate >= @StartDate AND LogDate <= @EndDate 
                    {orderByClause}
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                
                var offset = (pageNumber - 1) * pageSize;
                var result = await conn.QueryAsync<OpenAIApiLog>(sql, new { StartDate = startDate, EndDate = endDate, Offset = offset, PageSize = pageSize });
                
                return (result?.ToList() ?? new List<OpenAIApiLog>(), totalCount);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return (new List<OpenAIApiLog>(), 0);
            }
        }

        public async Task<List<OpenAIApiLog>> GetLogsByOperationTypeAsync(string operationType, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var sql = @"SELECT * FROM OpenAIApiLogs 
                           WHERE OperationType = @OperationType 
                           AND (@StartDate IS NULL OR LogDate >= @StartDate)
                           AND (@EndDate IS NULL OR LogDate <= @EndDate)
                           ORDER BY LogDate DESC";
                using var conn = new SqlConnection(connectionString);
                var result = await conn.QueryAsync<OpenAIApiLog>(sql, new { OperationType = operationType, StartDate = startDate, EndDate = endDate });
                return result?.ToList() ?? new List<OpenAIApiLog>();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpenAIApiLog>();
            }
        }

        public async Task<List<OpenAIApiLogSummary>> GetDailySummaryAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sql = @"
                    SELECT 
                        CAST(LogDate AS DATE) AS Date,
                        COUNT(*) AS TotalCalls,
                        SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessfulCalls,
                        SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) AS FailedCalls,
                        ISNULL(SUM(TotalTokens), 0) AS TotalTokens,
                        ISNULL(SUM(PromptTokens), 0) AS TotalPromptTokens,
                        ISNULL(SUM(CompletionTokens), 0) AS TotalCompletionTokens,
                        ISNULL(SUM(EstimatedCostUSD), 0) AS TotalCostUSD,
                        ISNULL(AVG(ResponseTimeMs), 0) AS AvgResponseTimeMs
                    FROM OpenAIApiLogs
                    WHERE LogDate >= @StartDate AND LogDate <= @EndDate
                    GROUP BY CAST(LogDate AS DATE)
                    ORDER BY Date DESC";

                using var conn = new SqlConnection(connectionString);
                var result = await conn.QueryAsync<OpenAIApiLogSummary>(sql, new { StartDate = startDate, EndDate = endDate });
                return result?.ToList() ?? new List<OpenAIApiLogSummary>();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpenAIApiLogSummary>();
            }
        }

        public async Task<List<OpenAIApiLogSummary>> GetSummaryByOperationTypeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sql = @"
                    SELECT 
                        OperationType,
                        COUNT(*) AS TotalCalls,
                        SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessfulCalls,
                        SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) AS FailedCalls,
                        ISNULL(SUM(TotalTokens), 0) AS TotalTokens,
                        ISNULL(SUM(PromptTokens), 0) AS TotalPromptTokens,
                        ISNULL(SUM(CompletionTokens), 0) AS TotalCompletionTokens,
                        ISNULL(SUM(EstimatedCostUSD), 0) AS TotalCostUSD,
                        ISNULL(AVG(ResponseTimeMs), 0) AS AvgResponseTimeMs
                    FROM OpenAIApiLogs
                    WHERE LogDate >= @StartDate AND LogDate <= @EndDate
                    GROUP BY OperationType
                    ORDER BY TotalCostUSD DESC";

                using var conn = new SqlConnection(connectionString);
                var result = await conn.QueryAsync<OpenAIApiLogSummary>(sql, new { StartDate = startDate, EndDate = endDate });
                return result?.ToList() ?? new List<OpenAIApiLogSummary>();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpenAIApiLogSummary>();
            }
        }

        public async Task<List<OpenAIApiLogSummary>> GetSummaryByModelAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sql = @"
                    SELECT 
                        Model,
                        COUNT(*) AS TotalCalls,
                        SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessfulCalls,
                        SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) AS FailedCalls,
                        ISNULL(SUM(TotalTokens), 0) AS TotalTokens,
                        ISNULL(SUM(PromptTokens), 0) AS TotalPromptTokens,
                        ISNULL(SUM(CompletionTokens), 0) AS TotalCompletionTokens,
                        ISNULL(SUM(EstimatedCostUSD), 0) AS TotalCostUSD,
                        ISNULL(AVG(ResponseTimeMs), 0) AS AvgResponseTimeMs
                    FROM OpenAIApiLogs
                    WHERE LogDate >= @StartDate AND LogDate <= @EndDate
                    GROUP BY Model
                    ORDER BY TotalCostUSD DESC";

                using var conn = new SqlConnection(connectionString);
                var result = await conn.QueryAsync<OpenAIApiLogSummary>(sql, new { StartDate = startDate, EndDate = endDate });
                return result?.ToList() ?? new List<OpenAIApiLogSummary>();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpenAIApiLogSummary>();
            }
        }

        public async Task<decimal> GetTotalCostAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sql = "SELECT ISNULL(SUM(EstimatedCostUSD), 0) FROM OpenAIApiLogs WHERE LogDate >= @StartDate AND LogDate <= @EndDate";
                using var conn = new SqlConnection(connectionString);
                return await conn.ExecuteScalarAsync<decimal>(sql, new { StartDate = startDate, EndDate = endDate });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<int> GetTotalCallsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sql = "SELECT COUNT(*) FROM OpenAIApiLogs WHERE LogDate >= @StartDate AND LogDate <= @EndDate";
                using var conn = new SqlConnection(connectionString);
                return await conn.ExecuteScalarAsync<int>(sql, new { StartDate = startDate, EndDate = endDate });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return 0;
            }
        }

        public async Task<List<OpenAIApiLog>> GetFailedCallsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sql = "SELECT * FROM OpenAIApiLogs WHERE IsSuccess = 0 AND LogDate >= @StartDate AND LogDate <= @EndDate ORDER BY LogDate DESC";
                using var conn = new SqlConnection(connectionString);
                var result = await conn.QueryAsync<OpenAIApiLog>(sql, new { StartDate = startDate, EndDate = endDate });
                return result?.ToList() ?? new List<OpenAIApiLog>();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpenAIApiLog>();
            }
        }

        public async Task<List<OpenAIApiLog>> GetLogsByCandidateIdAsync(int candidateId)
        {
            try
            {
                var sql = "SELECT * FROM OpenAIApiLogs WHERE CandidateId = @CandidateId ORDER BY LogDate DESC";
                using var conn = new SqlConnection(connectionString);
                var result = await conn.QueryAsync<OpenAIApiLog>(sql, new { CandidateId = candidateId });
                return result?.ToList() ?? new List<OpenAIApiLog>();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpenAIApiLog>();
            }
        }

        public async Task<List<OpenAIApiLog>> GetLogsByInterviewerIdAsync(int interviewerId)
        {
            try
            {
                var sql = "SELECT * FROM OpenAIApiLogs WHERE InterviewerId = @InterviewerId ORDER BY LogDate DESC";
                using var conn = new SqlConnection(connectionString);
                var result = await conn.QueryAsync<OpenAIApiLog>(sql, new { InterviewerId = interviewerId });
                return result?.ToList() ?? new List<OpenAIApiLog>();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpenAIApiLog>();
            }
        }

        public async Task<List<OpenAIApiLog>> GetLogsByIRFIdAsync(int irfId)
        {
            try
            {
                var sql = "SELECT * FROM OpenAIApiLogs WHERE IRFId = @IRFId ORDER BY LogDate DESC";
                using var conn = new SqlConnection(connectionString);
                var result = await conn.QueryAsync<OpenAIApiLog>(sql, new { IRFId = irfId });
                return result?.ToList() ?? new List<OpenAIApiLog>();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpenAIApiLog>();
            }
        }

        public async Task<List<OpenAIApiLog>> GetLogsByMeetingIdAsync(int meetingId)
        {
            try
            {
                var sql = "SELECT * FROM OpenAIApiLogs WHERE MeetingId = @MeetingId ORDER BY LogDate DESC";
                using var conn = new SqlConnection(connectionString);
                var result = await conn.QueryAsync<OpenAIApiLog>(sql, new { MeetingId = meetingId });
                return result?.ToList() ?? new List<OpenAIApiLog>();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpenAIApiLog>();
            }
        }

        public async Task RefreshAssistantCostSummaryAsync()
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.ExecuteAsync("usp_RefreshAssistantCostSummary", commandType: System.Data.CommandType.StoredProcedure, commandTimeout: 120);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        public async Task<List<OpenAIAssistantCostSummary>> GetAssistantCostRankingAsync(int? topN = null, decimal? minCost = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                var result = await conn.QueryAsync<OpenAIAssistantCostSummary>(
                    "usp_GetAssistantCostRanking",
                    new { TopN = topN, MinCost = minCost, StartDate = startDate, EndDate = endDate },
                    commandType: System.Data.CommandType.StoredProcedure
                );
                return result?.ToList() ?? new List<OpenAIAssistantCostSummary>();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new List<OpenAIAssistantCostSummary>();
            }
        }

        public async Task<AssistantCostDetails> GetAssistantCostDetailsAsync(string assistantId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                
                using var multi = await conn.QueryMultipleAsync(
                    "usp_GetAssistantCostDetails",
                    new { AssistantId = assistantId, StartDate = startDate, EndDate = endDate },
                    commandType: System.Data.CommandType.StoredProcedure
                );
                
                var summary = await multi.ReadFirstOrDefaultAsync<OpenAIAssistantCostSummary>();
                var costByOperation = (await multi.ReadAsync<AssistantCostByOperation>()).ToList();
                var costByModel = (await multi.ReadAsync<AssistantCostByModel>()).ToList();
                var dailyCosts = (await multi.ReadAsync<AssistantDailyCost>()).ToList();
                
                return new AssistantCostDetails
                {
                    Summary = summary,
                    CostByOperation = costByOperation,
                    CostByModel = costByModel,
                    DailyCosts = dailyCosts
                };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
