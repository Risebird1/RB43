using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IRiskCategoryService
    {
        /// <summary>
        /// Returns all RiskCategory rows for interviews within the given date range.
        /// </summary>
        Task<List<RiskCategory>> GetRiskCategories(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Returns the RiskCategory record for the given InterviewId.
        /// </summary>
        Task<RiskCategory> GetRiskCategoryByInterviewId(int interviewId);

        /// <summary>
        /// Derives risk categories from PaymentSLADetails for the given
        /// InterviewId and saves (insert or update) one row in RiskCategory.
        /// </summary>
        Task<bool> AddUpdateRiskCategory(int interviewId);
    }
}
