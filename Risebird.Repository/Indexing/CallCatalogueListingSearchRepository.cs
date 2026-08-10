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
    /// Call catalogue plugin for Azure AI Search (<c>/HR/CallCatalogue</c>).
    /// </summary>
    public class CallCatalogueListingSearchRepository
        : AzureListingIndexRepositoryBase<CallCatalogueIndexDocument>,
          ICallCatalogueListingSearchRepository
    {
        public const string ConfigListingKey = "CallCatalogueListing";

        private const int SearchPageSize = 1000;
        private const int MaxSkip = 10000;

        private readonly string _connectionString;

        public CallCatalogueListingSearchRepository(
            IConfiguration configuration,
            IAzureSearchClientFactory clientFactory,
            IApplicationErrorLogger applicationErrorLogger)
            : base(
                clientFactory,
                applicationErrorLogger,
                ListingSearchOptionsLoader.Load(
                    configuration,
                    ConfigListingKey,
                    CallCatalogueIndexDefinition.DefaultIndexName,
                    defaultPageSize: CallCatalogueIndexDefinition.DefaultPageSize,
                    defaultBatchSize: CallCatalogueIndexDefinition.DefaultBatchSize).Listing)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _connectionString = configuration["ConnectionString:DBConnectionString"];
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionString:DBConnectionString is not configured. FullCrawl cannot load call catalogue rows from SQL.");
            }
        }

        protected override string LogPrefix => "CallCatalogueListingSearch";

        protected override string DocumentKeyField => CallCatalogueIndexFields.Key;

        protected override SearchIndex CreateIndexDefinition(string indexName) =>
            CallCatalogueIndexDefinition.CreateIndex(indexName);

        protected override SearchDocument ToAzureDocument(CallCatalogueIndexDocument document) =>
            CallCatalogueIndexDocumentMapper.ToAzureDocument(document);

        protected override bool ShouldRemoveFromIndex(CallCatalogueIndexDocument document) => false;

        /// <remarks>Composite string keys — use upsert helper methods instead.</remarks>
        public Task<bool> UpsertAsync(int documentId) => Task.FromResult(false);

        public Task<bool> UpsertCandidateCallCatalogueAsync(int irfIntId, string candidateEmail) =>
            UpsertDocumentAsync(irfIntId: irfIntId, candidateEmail: candidateEmail, interviewerEmail: null);

        public async Task<bool> UpsertCandidateCallCatalogueAsync(string irfIdOrNumeric, string candidateEmail)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(candidateEmail))
                return false;

            var irfIntId = await ResolveIrfIntIdAsync(irfIdOrNumeric);
            if (irfIntId <= 0)
                return false;

            return await UpsertDocumentAsync(irfIntId, candidateEmail, null);
        }

        public Task<bool> UpsertInterviewerCallCatalogueAsync(string interviewerEmail) =>
            UpsertDocumentAsync(irfIntId: null, candidateEmail: null, interviewerEmail: interviewerEmail);

        protected override async Task<IReadOnlyList<CallCatalogueIndexDocument>> LoadDocumentsFromDatabaseAsync(int? documentId)
        {
            try
            {
                var documents = await QueryBatchAsync(
                    documentKey: null,
                    irfIntId: null,
                    candidateEmail: null,
                    interviewerEmail: null);

                if (documents.Count == 0)
                {
                    await LogExceptionAsync(
                        new InvalidOperationException(
                            "usp_GetCallCatalogueIndexBatch returned 0 rows. Deploy the SP and verify CallRecordingDetails / CallRecordingInterviewer have data."),
                        "LoadDocumentsFromDatabaseAsync empty result.");
                }

                return documents;
            }
            catch (SqlException ex)
            {
                await LogExceptionAsync(ex, "LoadDocumentsFromDatabaseAsync SQL failed (is usp_GetCallCatalogueIndexBatch deployed?).");
                throw;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, "LoadDocumentsFromDatabaseAsync failed.");
                throw;
            }
        }

        public async Task<IEnumerable<CallCatalogueVM>> SearchAsync(
            string profileName,
            string emailId,
            string catalogueType,
            InterviewFilter filter = null,
            string searchValue = null)
        {
            if (!IsEnabled)
                return null;

            try
            {
                var searchClient = CreateSearchClient();
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

                var odataFilter = BuildSearchFilter(profileName, emailId, catalogueType, filter);
                var query = string.IsNullOrWhiteSpace(searchValue) ? "*" : searchValue.Trim();
                var results = new List<CallCatalogueVM>();
                var skip = 0;

                while (true)
                {
                    var options = new SearchOptions
                    {
                        Filter = odataFilter,
                        Size = SearchPageSize,
                        Skip = skip,
                        OrderBy = { $"{CallCatalogueIndexFields.CalledOn} desc" }
                    };

                    foreach (var field in CallCatalogueIndexFields.ListViewSelect)
                        options.Select.Add(field);

                    if (!string.IsNullOrWhiteSpace(searchValue))
                    {
                        foreach (var field in CallCatalogueIndexFields.ListViewSearch)
                            options.SearchFields.Add(field);
                    }

                    var response = await searchClient.SearchAsync<SearchDocument>(query, options, timeoutCts.Token);
                    var pageCount = 0;

                    await foreach (var result in response.Value.GetResultsAsync().WithCancellation(timeoutCts.Token))
                    {
                        if (result.Document != null)
                        {
                            results.Add(CallCatalogueIndexDocumentMapper.ToListViewModel(result.Document));
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
                await LogExceptionAsync(ex, $"SearchAsync Azure request failed (profile={profileName}, type={catalogueType}).");
                return null;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, $"SearchAsync failed (profile={profileName}, type={catalogueType}).");
                return null;
            }
        }

        private async Task<bool> UpsertDocumentAsync(int? irfIntId, string candidateEmail, string interviewerEmail)
        {
            if (!IsEnabled)
                return false;

            var hasCandidate = irfIntId.HasValue && irfIntId.Value > 0 && !string.IsNullOrWhiteSpace(candidateEmail);
            var hasInterviewer = !string.IsNullOrWhiteSpace(interviewerEmail);
            if (!hasCandidate && !hasInterviewer)
                return false;

            try
            {
                await EnsureIndexAsync();
                var documents = await QueryBatchAsync(
                    documentKey: null,
                    irfIntId: irfIntId,
                    candidateEmail: string.IsNullOrWhiteSpace(candidateEmail) ? null : candidateEmail.Trim(),
                    interviewerEmail: string.IsNullOrWhiteSpace(interviewerEmail) ? null : interviewerEmail.Trim());

                var document = documents.FirstOrDefault();
                if (document == null)
                {
                    await LogExceptionAsync(
                        new InvalidOperationException("No SQL row returned for call catalogue upsert."),
                        "UpsertDocumentAsync skipped (empty load).");
                    return false;
                }

                var searchClient = CreateSearchClient();
                var response = await searchClient.MergeOrUploadDocumentsAsync(new[] { ToAzureDocument(document) });

                if (response.Value.Results.All(r => r.Succeeded))
                    return true;

                await LogFailedIndexingResultsAsync(response.Value.Results, "UpsertDocumentAsync");
                return false;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, "UpsertDocumentAsync failed.");
                return false;
            }
        }

        private async Task<IReadOnlyList<CallCatalogueIndexDocument>> QueryBatchAsync(
            string documentKey,
            int? irfIntId,
            string candidateEmail,
            string interviewerEmail)
        {
            using var connection = new SqlConnection(_connectionString);
            var rows = await connection.QueryAsync<CallCatalogueIndexDocumentRow>(
                "usp_GetCallCatalogueIndexBatch",
                new
                {
                    DocumentKey = documentKey,
                    IrfIntId = irfIntId,
                    CandidateEmail = candidateEmail,
                    InterviewerEmail = interviewerEmail
                },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 300);

            return (rows ?? Array.Empty<CallCatalogueIndexDocumentRow>())
                .Select(CallCatalogueIndexDocumentMapper.FromDatabaseRow)
                .ToList();
        }

        private async Task<int> ResolveIrfIntIdAsync(string irfIdOrNumeric)
        {
            if (string.IsNullOrWhiteSpace(irfIdOrNumeric))
                return 0;

            if (int.TryParse(irfIdOrNumeric.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
                return numeric;

            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT TOP 1 id FROM dbo.IRF WHERE IRFId = @IrfId",
                new { IrfId = irfIdOrNumeric.Trim() }) ?? 0;
        }

        private static string BuildSearchFilter(
            string profileName,
            string emailId,
            string catalogueType,
            InterviewFilter filter)
        {
            var filters = new List<string>();
            var type = string.IsNullOrWhiteSpace(catalogueType) ? "Candidate" : catalogueType.Trim();
            filters.Add($"{CallCatalogueIndexFields.CatalogueType} eq '{AzureSearchOData.Escape(type)}'");

            if (string.Equals(type, "Candidate", StringComparison.OrdinalIgnoreCase))
                AppendCandidateVisibilityFilter(filters, profileName, emailId);

            AppendDateFilter(filters, filter);
            AppendMultiselectFilters(filters, filter);

            return string.Join(" and ", filters);
        }

        private static void AppendCandidateVisibilityFilter(IList<string> filters, string profileName, string emailId)
        {
            if (string.Equals(profileName, ProfileNames.Admin, StringComparison.OrdinalIgnoreCase))
            {
                filters.Add($"{CallCatalogueIndexFields.AllowAllInternal} eq true");
                return;
            }

            var email = (emailId ?? string.Empty).Trim().ToLowerInvariant();
            filters.Add(
                $"{CallCatalogueIndexFields.AllowedViewerEmails}/any(v: v eq '{AzureSearchOData.Escape(email)}')");
        }

        private static void AppendDateFilter(IList<string> filters, InterviewFilter filter)
        {
            DateTime? startDate;
            DateTime? endDate;

            if (filter?.StartDate != null || filter?.EndDate != null)
            {
                startDate = filter.StartDate;
                endDate = filter.EndDate.HasValue ? filter.EndDate.Value.AddDays(1) : filter.EndDate;
            }
            else
            {
                startDate = DateTime.Now.Date;
                endDate = DateTime.Now.Date.AddDays(1);
            }

            if (startDate.HasValue)
                filters.Add($"{CallCatalogueIndexFields.CalledOn} ge {AzureSearchOData.FormatDate(startDate.Value)}");

            if (endDate.HasValue)
                filters.Add($"{CallCatalogueIndexFields.CalledOn} le {AzureSearchOData.FormatDate(endDate.Value)}");
        }

        private static void AppendMultiselectFilters(IList<string> filters, InterviewFilter filter)
        {
            if (filter == null)
                return;

            AzureSearchOData.AppendInFilter(filters, CallCatalogueIndexFields.PrimarySkill, filter.PrimarySkill);
            AzureSearchOData.AppendInFilter(filters, CallCatalogueIndexFields.ClientName, filter.ClientName);
            AzureSearchOData.AppendInFilter(filters, CallCatalogueIndexFields.IRFId, filter.RFID);
        }
    }
}
