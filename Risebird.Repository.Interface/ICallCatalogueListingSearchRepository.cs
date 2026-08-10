using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface.Search;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>
    /// Call catalogue plugin for Azure AI Search (<c>/HR/CallCatalogue</c>).
    /// </summary>
    public interface ICallCatalogueListingSearchRepository : IListingIndexRepository
    {
        Task<bool> UpsertCandidateCallCatalogueAsync(int irfIntId, string candidateEmail);

        Task<bool> UpsertCandidateCallCatalogueAsync(string irfIdOrNumeric, string candidateEmail);

        Task<bool> UpsertInterviewerCallCatalogueAsync(string interviewerEmail);

        /// <summary>
        /// Queries the call catalogue index. Returns null on disable/error so callers fall back to SQL.
        /// </summary>
        Task<IEnumerable<CallCatalogueVM>> SearchAsync(
            string profileName,
            string emailId,
            string catalogueType,
            InterviewFilter filter = null,
            string searchValue = null);
    }
}
