using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;

namespace Risebird.Repository.Search
{
    /// <summary>
    /// Creates Azure Search clients for any listing index. Register once in DI; reuse across IRF, Candidate, etc.
    /// </summary>
    public interface IAzureSearchClientFactory
    {
        /// <summary>True when Endpoint and ApiKey are configured.</summary>
        bool IsConfigured { get; }

        /// <summary>Client for document search / upload / delete against <paramref name="indexName"/>.</summary>
        SearchClient CreateSearchClient(string indexName);

        /// <summary>Client for CreateOrUpdateIndex (schema management).</summary>
        SearchIndexClient CreateIndexClient();
    }

    /// <inheritdoc />
    public sealed class AzureSearchClientFactory : IAzureSearchClientFactory
    {
        private readonly AzureSearchOptions _options;

        public AzureSearchClientFactory(AzureSearchOptions options)
        {
            _options = options ?? throw new System.ArgumentNullException(nameof(options));
        }

        public bool IsConfigured => _options.IsConfigured;

        public SearchClient CreateSearchClient(string indexName)
        {
            EnsureConfigured();
            return new SearchClient(new System.Uri(_options.Endpoint), indexName, new AzureKeyCredential(_options.ApiKey));
        }

        public SearchIndexClient CreateIndexClient()
        {
            EnsureConfigured();
            return new SearchIndexClient(new System.Uri(_options.Endpoint), new AzureKeyCredential(_options.ApiKey));
        }

        private void EnsureConfigured()
        {
            if (!IsConfigured)
                throw new System.InvalidOperationException(
                    "Azure Search is not configured. Set AzureSearch:Endpoint and AzureSearch:ApiKey.");
        }
    }
}
