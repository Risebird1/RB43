using Risebird.Entities.Data;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IRiskCategoryService
    {
        /// <summary>
        /// Derives risk categories from PaymentSLADetails for the given
        /// InterviewId and saves (insert or update) one row in RiskCategory.
        /// </summary>
        Task<bool> AddUpdateRiskCategory(int interviewId);

        /// <summary>
        /// Returns the RiskCategory record for the given InterviewId.
        /// </summary>
        Task<RiskCategory> GetRiskCategoryByInterviewId(int interviewId);
    }
}
