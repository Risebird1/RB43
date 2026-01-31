using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IOpenAIApiLogRepository
    {
        Task<long> LogApiCallAsync(OpenAIApiLog log);
        Task<OpenAIApiLog> GetLogByIdAsync(long id);
        Task<List<OpenAIApiLog>> GetLogsBySessionIdAsync(Guid sessionId);
        Task<List<OpenAIApiLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<(List<OpenAIApiLog> logs, int totalCount)> GetLogsByDateRangePagedAsync(DateTime startDate, DateTime endDate, int pageNumber, int pageSize);
        Task<List<OpenAIApiLog>> GetLogsByOperationTypeAsync(string operationType, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<OpenAIApiLogSummary>> GetDailySummaryAsync(DateTime startDate, DateTime endDate);
        Task<List<OpenAIApiLogSummary>> GetSummaryByOperationTypeAsync(DateTime startDate, DateTime endDate);
        Task<List<OpenAIApiLogSummary>> GetSummaryByModelAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalCostAsync(DateTime startDate, DateTime endDate);
        Task<int> GetTotalCallsAsync(DateTime startDate, DateTime endDate);
        Task<List<OpenAIApiLog>> GetFailedCallsAsync(DateTime startDate, DateTime endDate);
        Task<List<OpenAIApiLog>> GetLogsByCandidateIdAsync(int candidateId);
        Task<List<OpenAIApiLog>> GetLogsByInterviewerIdAsync(int interviewerId);
        Task<List<OpenAIApiLog>> GetLogsByIRFIdAsync(int irfId);
        Task<List<OpenAIApiLog>> GetLogsByMeetingIdAsync(int meetingId);
        
        // Assistant cost tracking methods
        Task RefreshAssistantCostSummaryAsync();
        Task<List<OpenAIAssistantCostSummary>> GetAssistantCostRankingAsync(int? topN = null, decimal? minCost = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<AssistantCostDetails> GetAssistantCostDetailsAsync(string assistantId, DateTime? startDate = null, DateTime? endDate = null);
    }
}
