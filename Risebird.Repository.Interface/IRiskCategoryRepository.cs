using Risebird.Entities.Data;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IRiskCategoryRepository
    {
        /// <summary>
        /// Reads all risk violation columns from PaymentSLADetails for the given
        /// InterviewId, builds a comma-separated Name and UPSERTs one row into RiskCategory.
        /// </summary>
        Task<bool> AddUpdateRiskCategory(int interviewId);

        /// <summary>
        /// Returns the RiskCategory row for the given InterviewId, joined with
        /// basic interview info from InterviewMeetings.
        /// </summary>
        Task<RiskCategory> GetRiskCategoryByInterviewId(int interviewId);
    }
}
