namespace Risebird.Repository.Search
{
    /// <summary>
    /// Per-listing Azure AI Search settings (one index per listing type).
    /// Bound from <c>AzureSearch:{ListingKey}</c>, e.g. <c>AzureSearch:IrfListing</c>,
    /// <c>AzureSearch:CandidateListing</c>, <c>AzureSearch:InterviewerListing</c>.
    /// </summary>
    public sealed class ListingSearchOptions
    {
        /// <summary>Config subsection name under <c>AzureSearch</c> (e.g. IrfListing).</summary>
        public string ListingKey { get; set; }

        /// <summary>Feature flag. When false, listing uses SQL fallback.</summary>
        public bool Enabled { get; set; }

        /// <summary>Azure index name (e.g. irfindex, candidateindex).</summary>
        public string IndexName { get; set; }

        /// <summary>Page size for list SearchAsync calls.</summary>
        public int PageSize { get; set; } = 9;

        /// <summary>Documents per MergeOrUpload batch during FullCrawl.</summary>
        public int BatchSize { get; set; } = 500;

        /// <summary>
        /// When true, app may CreateOrUpdateIndex at runtime (dev only).
        /// Production should use the listing's Create-*-Index.ps1 script.
        /// </summary>
        public bool ManageIndexSchema { get; set; }
    }
}
