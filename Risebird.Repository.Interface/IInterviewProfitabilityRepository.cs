using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInterviewProfitabilityRepository
    {
        /// <summary>Runs usp_GetInterviewProfitability. When the query fails the error is logged and LoadError says why.</summary>
        Task<InterviewProfitabilityRawData> GetRawDataAsync(
            DateTime startDate,
            DateTime endDate,
            int? clientId,
            int? irfIntId,
            string interviewerEmail,
            bool? isAIInterview,
            bool includeCancelled);

        Task<InterviewProfitabilityFilterOptions> GetFilterOptionsAsync(int? clientId);

        Task<List<OperationalCostFactor>> GetCostFactorsAsync();

        /// <summary>Inserts (Id = 0) or updates a factor and returns its Id, or 0 when nothing was saved.</summary>
        Task<int> SaveCostFactorAsync(OperationalCostFactor factor, string changedBy);
    }
}
