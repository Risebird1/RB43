using Risebird.Entities.Search;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface.Search;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>
    /// Interviewer roster plugin for Azure AI Search (<c>/Interviewer/Index</c>).
    /// </summary>
    public interface IInterviewerListingSearchRepository : IListingIndexRepository
    {
        Task<bool> UpsertInterviewerAsync(int interviewerProfileId);

        Task<bool> UpsertInterviewerByUserIdAsync(int userId);

        Task<bool> UpsertInterviewerByUniqueIdAsync(string uniqueId);

        Task<bool> UpsertInterviewerByEmailAsync(string emailId);

        Task<bool> RemoveInterviewerAsync(int interviewerProfileId);

        /// <summary>
        /// Loads all non-deleted interviewers for the Index page (controller still applies
        /// in-memory filters/paging). Returns null on disable/error so callers fall back to SQL.
        /// </summary>
        Task<IEnumerable<InterviewerProfileVM>> SearchAllAsync(string searchValue = null);

        Task<IEnumerable<InterviewerIndexDocument>> GetIndexDocumentsFromDatabaseAsync(
            int? interviewerProfileId = null,
            int? userId = null);
    }
}
