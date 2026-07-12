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
    }
}
