using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ITrialFlagReviewRepository
    {
        Task<(bool TableExists, int TotalRows)> GetDiagnosticsAsync();

        Task<(int Pending, int Kept, int Removed)> GetStatusCountsAsync();

        Task<IReadOnlyList<TrialFlagReviewRowVm>> GetTrialFlagsAsync(string reviewStatus);

        Task<IReadOnlyList<InterviewAiRiskSummaryRowVm>> GetInterviewAiRiskSummaryAsync(int interviewId);

        Task<TrialFlagReviewRowVm> GetTrialFlagByIdAsync(int trialFlagId);

        Task<bool> UpdateReviewAsync(int trialFlagId, string reviewStatus, string reviewedBy, string reviewerComment);
    }
}
