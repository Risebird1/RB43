using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    /// <summary>
    /// Revenue, every cost and margin per interview (Interview → Profitability tab), and maintenance of
    /// the running costs that are not recorded per interview.
    /// </summary>
    public interface IInterviewProfitabilityService
    {
        InterviewProfitabilitySettings GetSettings();

        Task<InterviewProfitabilityReport> GetReportAsync(InterviewProfitabilityFilter filter);

        Task<InterviewProfitabilityFilterOptions> GetFilterOptionsAsync(int? clientId);

        Task<List<OperationalCostFactor>> GetCostFactorsAsync();

        /// <summary>Validates and saves a factor. Returns the error to show, or null when saved.</summary>
        Task<string> SaveCostFactorAsync(OperationalCostFactor factor, string changedBy);
    }
}
