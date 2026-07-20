using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Search;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Indexing;
using Risebird.Repository.Interface;
using Risebird.Repository.Search;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Repository
{
    /// <summary>
    /// Raised-issue support ticket plugin for Azure AI Search (<c>/Support/RaisedRequests</c>).
    /// </summary>
    public class RaisedIssueListingSearchRepository
        : AzureListingIndexRepositoryBase<RaisedIssueIndexDocument>,
          IRaisedIssueListingSearchRepository
    {
        public const string ConfigListingKey = "RaisedIssueListing";

        private const int SearchPageSize = 1000;
        private const int MaxSkip = 10000;

        private readonly string _connectionString;

        public RaisedIssueListingSearchRepository(
            IConfiguration configuration,
            IAzureSearchClientFactory clientFactory,
            IApplicationErrorLogger applicationErrorLogger)
            : base(
                clientFactory,
                applicationErrorLogger,
                ListingSearchOptionsLoader.Load(
                    configuration,
                    ConfigListingKey,
                    RaisedIssueIndexDefinition.DefaultIndexName,
                    defaultPageSize: RaisedIssueIndexDefinition.DefaultPageSize,
                    defaultBatchSize: RaisedIssueIndexDefinition.DefaultBatchSize).Listing)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _connectionString = configuration["ConnectionString:DBConnectionString"];
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionString:DBConnectionString is not configured. FullCrawl cannot load raised issues from SQL.");
            }
        }

        protected override string LogPrefix => "RaisedIssueListingSearch";

        protected override string DocumentKeyField => RaisedIssueIndexFields.Key;

        protected override SearchIndex CreateIndexDefinition(string indexName) =>
            RaisedIssueIndexDefinition.CreateIndex(indexName);

        protected override SearchDocument ToAzureDocument(RaisedIssueIndexDocument document) =>
            RaisedIssueIndexDocumentMapper.ToAzureDocument(document);

        /// <summary>Raised issues are never soft-deleted from the index; closed tickets stay indexed.</summary>
        protected override bool ShouldRemoveFromIndex(RaisedIssueIndexDocument document) => false;

        public Task<bool> UpsertRaisedIssueAsync(int raisedIssueId) => UpsertAsync(raisedIssueId);

        public Task<bool> RemoveRaisedIssueAsync(int raisedIssueId) => RemoveAsync(raisedIssueId);

        protected override async Task<IReadOnlyList<RaisedIssueIndexDocument>> LoadDocumentsFromDatabaseAsync(
            int? documentId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var rows = await connection.QueryAsync<RaisedIssueIndexDocumentRow>(
                    "usp_GetRaisedIssueIndexBatch",
                    new { RaisedIssueId = documentId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300);

                return (rows ?? Array.Empty<RaisedIssueIndexDocumentRow>())
                    .Select(RaisedIssueIndexDocumentMapper.FromDatabaseRow)
                    .ToList();
            }
            catch (SqlException ex)
            {
                await LogExceptionAsync(ex,
                    $"LoadDocumentsFromDatabaseAsync SQL failed (raisedIssueId={documentId?.ToString(CultureInfo.InvariantCulture) ?? "all"}).");
                throw;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex,
                    $"LoadDocumentsFromDatabaseAsync failed (raisedIssueId={documentId?.ToString(CultureInfo.InvariantCulture) ?? "all"}).");
                throw;
            }
        }

        /// <summary>
        /// Queries Azure Search for the ticket list on <c>/Support/RaisedRequests</c>.
        /// Returns an empty list on a successful zero-hit query and <see langword="null"/> so callers
        /// fall back to SQL when disabled or on error.
        /// </summary>
        public async Task<IEnumerable<RaiseBugsVM>> SearchAsync(string profileName, string type, string searchValue = null)
        {
            if (!IsEnabled)
                return null;

            try
            {
                var searchClient = CreateSearchClient();
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

                var filter = BuildSearchFilter(profileName, type);
                var query = string.IsNullOrWhiteSpace(searchValue) ? "*" : searchValue.Trim();
                var results = new List<RaiseBugsVM>();
                var skip = 0;

                while (true)
                {
                    var options = new SearchOptions
                    {
                        Filter = filter,
                        Size = SearchPageSize,
                        Skip = skip,
                        OrderBy = { $"{RaisedIssueIndexFields.RequestedOn} desc" }
                    };

                    foreach (var field in RaisedIssueIndexFields.ListViewSelect)
                        options.Select.Add(field);

                    if (!string.IsNullOrWhiteSpace(searchValue))
                    {
                        foreach (var field in RaisedIssueIndexFields.ListViewSearch)
                            options.SearchFields.Add(field);
                    }

                    var response = await searchClient.SearchAsync<SearchDocument>(query, options, timeoutCts.Token);
                    var pageCount = 0;

                    await foreach (var result in response.Value.GetResultsAsync().WithCancellation(timeoutCts.Token))
                    {
                        if (result.Document != null)
                        {
                            results.Add(RaisedIssueIndexDocumentMapper.ToListViewModel(result.Document));
                            pageCount++;
                        }
                    }

                    if (pageCount < SearchPageSize)
                        break;

                    skip += SearchPageSize;
                    if (skip >= MaxSkip)
                        break;
                }

                return results;
            }
            catch (OperationCanceledException ex)
            {
                await LogExceptionAsync(ex, "SearchAsync timed out after 20s — falling back to SQL.");
                return null;
            }
            catch (RequestFailedException ex)
            {
                await LogExceptionAsync(ex, $"SearchAsync Azure request failed (profile={profileName}, type={type}).");
                return null;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, $"SearchAsync failed (profile={profileName}, type={type}).");
                return null;
            }
        }

        /// <summary>Builds the OData filter for role visibility (profileName) and list type.</summary>
        private static string BuildSearchFilter(string profileName, string type)
        {
            var filters = new List<string>();

            if (string.Equals(profileName, "TechSupport", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add($"({RaisedIssueIndexFields.Requester} ne 'Interviewer' and {RaisedIssueIndexFields.Requester} ne 'Client')");
            }
            else if (!string.Equals(profileName, ProfileNames.Admin, StringComparison.OrdinalIgnoreCase))
            {
                filters.Add($"({RaisedIssueIndexFields.Requester} eq 'Interviewer' or {RaisedIssueIndexFields.Requester} eq 'Client')");
            }

            AppendTypeFilter(filters, type);

            return filters.Count == 0 ? null : string.Join(" and ", filters);
        }

        private static void AppendTypeFilter(IList<string> filters, string type)
        {
            if (string.Equals(type, "Open", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add($"{RaisedIssueIndexFields.Status} eq 'Open'");
                filters.Add($"{RaisedIssueIndexFields.RequestType} ne 'Payment'");
            }
            else if (string.Equals(type, "OpenPayment", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add($"{RaisedIssueIndexFields.Status} eq 'Open'");
                filters.Add($"{RaisedIssueIndexFields.RequestType} eq 'Payment'");
                filters.Add($"{RaisedIssueIndexFields.IsReopened} eq false");
            }
            else if (string.Equals(type, "ReOpenPayment", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add($"{RaisedIssueIndexFields.Status} eq 'Open'");
                filters.Add($"{RaisedIssueIndexFields.RequestType} eq 'Payment'");
                filters.Add($"{RaisedIssueIndexFields.IsReopened} eq true");
            }
            else if (string.Equals(type, "All", StringComparison.OrdinalIgnoreCase))
            {
                // Role filter only — no status/type restriction.
            }
            else
            {
                filters.Add($"{RaisedIssueIndexFields.Status} ne 'Open'");
            }
        }
    }
}
