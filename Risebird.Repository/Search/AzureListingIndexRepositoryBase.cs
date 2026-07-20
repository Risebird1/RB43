using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface.Search;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Repository.Search
{
    /// <summary>
    /// Reusable Azure AI Search indexing pipeline for any listing document type.
    /// <para>
    /// <b>To add a new listing (Candidate, Interviewer, User, …):</b>
    /// </para>
    /// <list type="number">
    ///   <item>Add <c>AzureSearch:{ListingKey}</c> in appsettings (Enabled, IndexName, PageSize, BatchSize).</item>
    ///   <item>Create Fields / Definition / Row / Document / Mapper under <c>Indexing/{Name}/</c>.</item>
    ///   <item>Subclass this base; implement abstract hooks; add listing-specific <c>SearchAsync</c>.</item>
    ///   <item>Register the repository in DI; call <see cref="UpsertAsync"/> after create/edit/delete.</item>
    ///   <item>Add <c>Create-{Name}Index.ps1</c> and an HTTP FullCrawl/Upsert function (or reuse a generic one).</item>
    /// </list>
    /// </summary>
    /// <typeparam name="TDocument">In-memory index document (e.g. IrfIndexDocument).</typeparam>
    public abstract class AzureListingIndexRepositoryBase<TDocument> : IListingIndexRepository
        where TDocument : class
    {
        private readonly IAzureSearchClientFactory _clientFactory;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ListingSearchOptions _listingOptions;

        protected AzureListingIndexRepositoryBase(
            IAzureSearchClientFactory clientFactory,
            IApplicationErrorLogger applicationErrorLogger,
            ListingSearchOptions listingOptions)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _applicationErrorLogger = applicationErrorLogger ?? throw new ArgumentNullException(nameof(applicationErrorLogger));
            _listingOptions = listingOptions ?? throw new ArgumentNullException(nameof(listingOptions));
        }

        /// <inheritdoc />
        public string ListingKey => _listingOptions.ListingKey;

        /// <inheritdoc />
        public bool IsEnabled =>
            _listingOptions.Enabled && _clientFactory.IsConfigured;

        /// <summary>Configured index name for this listing.</summary>
        protected string IndexName => _listingOptions.IndexName;

        /// <summary>Page size for list queries.</summary>
        protected int PageSize => _listingOptions.PageSize;

        /// <summary>Batch size for FullCrawl uploads.</summary>
        protected int BatchSize => _listingOptions.BatchSize;

        /// <summary>Log prefix, e.g. <c>IrfListingSearch</c>, <c>CandidateListingSearch</c>.</summary>
        protected abstract string LogPrefix { get; }

        /// <summary>Azure document key field name (usually the SQL primary key as string).</summary>
        protected abstract string DocumentKeyField { get; }

        /// <summary>Builds the SearchIndex model (used only when ManageIndexSchema is true).</summary>
        protected abstract SearchIndex CreateIndexDefinition(string indexName);

        /// <summary>
        /// Loads documents from SQL. Pass <paramref name="documentId"/> for single upsert; null for full crawl.
        /// </summary>
        protected abstract Task<IReadOnlyList<TDocument>> LoadDocumentsFromDatabaseAsync(int? documentId);

        /// <summary>Maps domain document to Azure Search upload payload.</summary>
        protected abstract SearchDocument ToAzureDocument(TDocument document);

        /// <summary>
        /// When true, <see cref="UpsertAsync"/> deletes the document instead of uploading
        /// (e.g. soft-deleted rows).
        /// </summary>
        protected abstract bool ShouldRemoveFromIndex(TDocument document);

        /// <inheritdoc />
        public async Task EnsureIndexAsync()
        {
            if (!IsEnabled || !_listingOptions.ManageIndexSchema)
                return;

            try
            {
                var indexClient = _clientFactory.CreateIndexClient();
                await indexClient.CreateOrUpdateIndexAsync(CreateIndexDefinition(IndexName));
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, $"EnsureIndexAsync failed for index '{IndexName}'.");
            }
        }

        /// <inheritdoc />
        public async Task<int> FullCrawlAsync()
        {
            if (!IsEnabled)
                return 0;

            try
            {
                await EnsureIndexAsync();
                var documents = await LoadDocumentsFromDatabaseAsync(documentId: null);
                // Active rows only (SP filters IsDeleted=0). Soft-deleted orphans from before
                // search was enabled: recreate the index via Create-IrfIndex.ps1 -Recreate, then crawl.
                return await MergeOrUploadDocumentsAsync(documents);
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, "FullCrawlAsync failed.");
                return 0;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpsertAsync(int documentId)
        {
            if (!IsEnabled || documentId <= 0)
                return false;

            try
            {
                await EnsureIndexAsync();

                var documents = await LoadDocumentsFromDatabaseAsync(documentId);
                var document = documents.FirstOrDefault();

                // Only remove when we successfully loaded a soft-deleted row.
                // An empty load usually means the SP missed the row (deploy lag, wrong id) —
                // deleting would "succeed" without uploading and leave new IRFs out of the index.
                if (document == null)
                {
                    await LogExceptionAsync(
                        new InvalidOperationException($"No SQL row returned for documentId={documentId}."),
                        "UpsertAsync skipped (empty load).");
                    return false;
                }

                if (ShouldRemoveFromIndex(document))
                    return await RemoveAsync(documentId);

                var searchClient = _clientFactory.CreateSearchClient(IndexName);
                var response = await searchClient.MergeOrUploadDocumentsAsync(
                    new[] { ToAzureDocument(document) });

                if (response.Value.Results.All(r => r.Succeeded))
                    return true;

                await LogFailedIndexingResultsAsync(response.Value.Results, "UpsertAsync");
                return false;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, $"UpsertAsync failed for documentId={documentId}.");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> RemoveAsync(int documentId)
        {
            if (!IsEnabled || documentId <= 0)
                return false;

            try
            {
                var searchClient = _clientFactory.CreateSearchClient(IndexName);
                var response = await searchClient.DeleteDocumentsAsync(
                    DocumentKeyField,
                    new[] { documentId.ToString(CultureInfo.InvariantCulture) });

                if (response.Value.Results.All(r => r.Succeeded))
                    return true;

                await LogFailedIndexingResultsAsync(response.Value.Results, "RemoveAsync");
                return false;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, $"RemoveAsync failed for documentId={documentId}.");
                return false;
            }
        }

        /// <summary>Uploads documents in batches; continues after partial batch failures.</summary>
        protected async Task<int> MergeOrUploadDocumentsAsync(IReadOnlyList<TDocument> documents)
        {
            if (documents == null || documents.Count == 0)
                return 0;

            var searchClient = _clientFactory.CreateSearchClient(IndexName);
            var indexed = 0;

            foreach (var batch in documents.Chunk(BatchSize))
            {
                try
                {
                    var payload = batch.Select(ToAzureDocument).ToList();
                    var response = await searchClient.MergeOrUploadDocumentsAsync(payload);
                    var results = response?.Value?.Results;
                    if (results == null || results.Count == 0)
                    {
                        await LogExceptionAsync(
                            new InvalidOperationException("Azure Search returned no indexing results for batch upload."),
                            "MergeOrUploadDocumentsAsync empty response.");
                        continue;
                    }

                    await LogFailedIndexingResultsAsync(results, "MergeOrUploadDocumentsAsync");
                    indexed += results.Count(r => r.Succeeded);
                }
                catch (Exception ex)
                {
                    await LogExceptionAsync(ex, $"MergeOrUploadDocumentsAsync failed for batch of {batch.Count()} documents.");
                }
            }

            return indexed;
        }

        /// <summary>Creates a document client for the configured listing index (search / upload).</summary>
        protected Azure.Search.Documents.SearchClient CreateSearchClient() =>
            _clientFactory.CreateSearchClient(IndexName);

        /// <summary>Writes an exception with listing context to the application error log.</summary>
        protected async Task LogExceptionAsync(Exception ex, string context)
        {
            await _applicationErrorLogger.LogMessage(
                LogLevelInfo.Exception,
                new InvalidOperationException($"[{LogPrefix}] {context}", ex));
        }

        /// <summary>Logs per-document Azure indexing failures.</summary>
        protected async Task LogFailedIndexingResultsAsync(IReadOnlyList<IndexingResult> results, string operation)
        {
            if (results == null || results.Count == 0)
                return;

            foreach (var result in results.Where(r => !r.Succeeded))
            {
                await _applicationErrorLogger.LogMessage(
                    LogLevelInfo.Exception,
                    new InvalidOperationException(
                        $"[{LogPrefix}] {operation} failed for key '{result.Key}': {result.ErrorMessage}"));
            }
        }
    }
}
