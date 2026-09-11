using Microsoft.Extensions.Configuration;

namespace Risebird.Repository.Search
{
    /// <summary>
    /// Loads shared and per-listing Azure Search options from configuration.
    /// Reuse for every new listing type — only pass a different <paramref name="listingKey"/>.
    /// </summary>
    public static class ListingSearchOptionsLoader
    {
        /// <summary>
        /// Reads <c>AzureSearch</c> (endpoint/api key) and <c>AzureSearch:{listingKey}</c>.
        /// </summary>
        /// <param name="configuration">App configuration.</param>
        /// <param name="listingKey">Subsection name, e.g. <c>IrfListing</c>, <c>CandidateListing</c>.</param>
        /// <param name="defaultIndexName">Fallback index name when not set in config.</param>
        /// <param name="defaultPageSize">Fallback page size.</param>
        /// <param name="defaultBatchSize">Fallback crawl batch size.</param>
        public static (AzureSearchOptions Service, ListingSearchOptions Listing) Load(
            IConfiguration configuration,
            string listingKey,
            string defaultIndexName,
            int defaultPageSize = 9,
            int defaultBatchSize = 500)
        {
            var service = new AzureSearchOptions
            {
                Endpoint = configuration["AzureSearch:Endpoint"],
                ApiKey = configuration["AzureSearch:ApiKey"]
            };

            var section = configuration.GetSection($"AzureSearch:{listingKey}");
            var listing = new ListingSearchOptions
            {
                ListingKey = listingKey,
                // TryParse rather than GetValue<bool>: a blank or malformed flag in appsettings
                // should fall back to off, not throw on startup.
                Enabled = bool.TryParse(section["Enabled"], out bool enabled) && enabled,
                IndexName = section["IndexName"] ?? defaultIndexName,
                PageSize = int.TryParse(section["PageSize"], out int pageSize) ? pageSize : defaultPageSize,
                BatchSize = int.TryParse(section["BatchSize"], out int batchSize) ? batchSize : defaultBatchSize,
                ManageIndexSchema = bool.TryParse(section["ManageIndexSchema"], out bool manageSchema) && manageSchema
            };

            return (service, listing);
        }
    }
}
