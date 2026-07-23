using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ITrialFlagReviewRepository
    {
        Task<(bool TableExists, int TotalRows)> GetDiagnosticsAsync();

        Task<(int Pending, int Kept, int Removed)> GetStatusCountsAsync();

        /// <summary>Paginated light grid — usp_GetTrialFlagsForReview_List.</summary>
        Task<(IReadOnlyList<TrialFlagReviewRowVm> Rows, int TotalRows)> GetTrialFlagsListAsync(
            string reviewStatus, int offset, int pageSize, string search = null, string severity = null, string channelOwner = null);

        /// <summary>Per-owner P0 + total counts for active tab.</summary>
        Task<IReadOnlyList<TrialFlagOwnerSummaryVm>> GetOwnerSummariesAsync(string reviewStatus);

        /// <summary>Heavy per-row detail — usp_GetTrialFlagDetail.</summary>
        Task<TrialFlagReviewRowVm> GetTrialFlagDetailAsync(int trialFlagId);

        Task<int> SyncTrialFlagsVerdictFromRiskCategoryAsync();

        Task<IReadOnlyList<InterviewAiRiskSummaryRowVm>> GetInterviewAiRiskSummaryAsync(int interviewId);

        Task<TrialFlagReviewRowVm> GetTrialFlagByIdAsync(int trialFlagId);

        Task<bool> UpdateReviewAsync(int trialFlagId, string reviewStatus, string reviewedBy, string reviewerComment);
    }
}
