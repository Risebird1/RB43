using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IRiskCategoryRepository
    {
        /// <summary>
        /// Returns all RiskCategory rows joined with InterviewMeetings
        /// where interviewDate falls within the given date range.
        /// </summary>
        Task<IEnumerable<RiskCategory>> GetRiskCategories(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Returns the RiskCategory row for the given InterviewId, joined with
        /// basic interview info from InterviewMeetings.
        /// </summary>
        Task<RiskCategory> GetRiskCategoryByInterviewId(int interviewId);

        /// <summary>
        /// Reads all risk violation columns from PaymentSLADetails for the given
        /// InterviewId, builds a comma-separated Name and UPSERTs one row into RiskCategory.
        /// </summary>
        Task<bool> AddUpdateRiskCategory(int interviewId);

        /// <summary>
        /// Evaluates risk checks for the interview without writing (usp_AddUpdateRiskCategory @DryRun=1).
        /// </summary>
        Task<RiskCategory> EvaluateRiskCategoryDryRun(int interviewId);
    }
}
