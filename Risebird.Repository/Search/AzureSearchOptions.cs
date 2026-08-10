namespace Risebird.Repository.Search
{
    /// <summary>
    /// Shared Azure AI Search service connection (one Search service, many indexes).
    /// Bound from configuration section <c>AzureSearch</c>.
    /// </summary>
    public sealed class AzureSearchOptions
    {
        public const string SectionName = "AzureSearch";

        /// <summary>Search service endpoint, e.g. https://myservice.search.windows.net</summary>
        public string Endpoint { get; set; }

        /// <summary>Admin or query API key for the Search service.</summary>
        public string ApiKey { get; set; }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey);
    }
}
