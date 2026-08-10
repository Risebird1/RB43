using System.Threading.Tasks;

namespace Risebird.Repository.Interface.Search
{
    /// <summary>
    /// Shared write/index contract for any Azure AI Search listing (IRF, Candidate, Interviewer, User, …).
    /// Implement once per document type; reuse FullCrawl / Upsert / Remove from HTTP functions or domain services.
    /// </summary>
    public interface IListingIndexRepository
    {
        /// <summary>Config key under <c>AzureSearch</c>, e.g. <c>IrfListing</c>, <c>CandidateListing</c>.</summary>
        string ListingKey { get; }

        /// <summary>True when the listing feature flag is on and Azure Search is configured.</summary>
        bool IsEnabled { get; }

        /// <summary>Creates/updates index schema when ManageIndexSchema is enabled (dev only).</summary>
        Task EnsureIndexAsync();

        /// <summary>Loads all source rows from SQL and uploads them to the index.</summary>
        /// <returns>Count of successfully indexed documents.</returns>
        Task<int> FullCrawlAsync();

        /// <summary>Upserts one document by SQL primary key; removes it when missing or soft-deleted.</summary>
        Task<bool> UpsertAsync(int documentId);

        /// <summary>Deletes one document from the index by key.</summary>
        Task<bool> RemoveAsync(int documentId);
    }
}
