using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface.Search;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>
    /// Raised-issue support ticket plugin for Azure AI Search (<c>/Support/RaisedRequests</c>).
    /// </summary>
    public interface IRaisedIssueListingSearchRepository : IListingIndexRepository
    {
        Task<bool> UpsertRaisedIssueAsync(int raisedIssueId);

        Task<bool> RemoveRaisedIssueAsync(int raisedIssueId);

        /// <summary>
        /// Queries the raised-issue index for the ticket list view, filtered by profile role and list type.
        /// Returns null on disable/error so callers fall back to SQL.
        /// </summary>
        Task<IEnumerable<RaiseBugsVM>> SearchAsync(string profileName, string type, string searchValue = null);
    }
}
