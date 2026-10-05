using Risebird.Entities.Search;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface.Search;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>
    /// IRF listing plugin for Azure AI Search (<c>/IRF/Index</c>).
    /// Extends <see cref="IListingIndexRepository"/> with IRF-specific list search.
    /// </summary>
    public interface IIrfListingSearchRepository : IListingIndexRepository
    {
        /// <summary>Whether this profile should use Azure Search (Recruiter falls back to SQL).</summary>
        bool SupportsProfile(string profileName);

        /// <summary>Convenience alias for <see cref="IListingIndexRepository.UpsertAsync"/>.</summary>
        Task<bool> UpsertIrfAsync(int irfIntId);

        /// <summary>Convenience alias for <see cref="IListingIndexRepository.RemoveAsync"/>.</summary>
        Task<bool> RemoveIrfAsync(int irfIntId);

        /// <summary>Queries the IRF index for the paginated list view.</summary>
        Task<IEnumerable<IRF>> SearchAsync(
            string emailId,
            string profileName,
            int? pageNumber,
            string searchValue = null,
            IRFFilter filterModel = null);

        /// <summary>Loads IRF documents from SQL for indexing (full crawl or single id).</summary>
        Task<IEnumerable<IrfIndexDocument>> GetIndexDocumentsFromDatabaseAsync(int? irfIntId = null);

        /// <summary>
        /// Ids of the active IRFs the given user takes part in, as channel owner, client user,
        /// recruiter, senior HR (via the client) or HR. Empty when the user is on none.
        /// </summary>
        Task<IReadOnlyList<int>> GetIrfIntIdsByUserIdAsync(int userId);

        /// <summary>
        /// Re-indexes every IRF the user takes part in, because <c>risebirdHrEmail</c> and the
        /// <c>allowedViewerEmails</c> ACL are snapshots of <c>UserList.EmailId</c> and go stale
        /// when the user row changes.
        /// <para>
        /// <paramref name="additionalIrfIntIds"/> is for ids captured *before* the edit: an edit
        /// that drops a client assignment leaves an IRF the user no longer belongs to, so it
        /// would not come back from the lookup, yet it still carries their email in its ACL.
        /// </para>
        /// </summary>
        /// <returns>The number of IRF documents pushed to the index.</returns>
        Task<int> UpsertIrfsByUserIdAsync(int userId, IEnumerable<int> additionalIrfIntIds = null);
    }
}
