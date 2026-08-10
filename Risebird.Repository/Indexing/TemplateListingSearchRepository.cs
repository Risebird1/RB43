using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
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
    /// Clonable skill channel (template) plugin for Azure AI Search (<c>/Template/Index</c>, <c>/Template/List</c>).
    /// </summary>
    public class TemplateListingSearchRepository
        : AzureListingIndexRepositoryBase<TemplateIndexDocument>,
          ITemplateListingSearchRepository
    {
        public const string ConfigListingKey = "TemplateListing";

        private const int SearchPageSize = 1000;
        private const int MaxSkip = 10000;

        private readonly string _connectionString;

        public TemplateListingSearchRepository(
            IConfiguration configuration,
            IAzureSearchClientFactory clientFactory,
            IApplicationErrorLogger applicationErrorLogger)
            : base(
                clientFactory,
                applicationErrorLogger,
                ListingSearchOptionsLoader.Load(
                    configuration,
                    ConfigListingKey,
                    TemplateIndexDefinition.DefaultIndexName,
                    defaultPageSize: TemplateIndexDefinition.DefaultPageSize,
                    defaultBatchSize: TemplateIndexDefinition.DefaultBatchSize).Listing)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _connectionString = configuration["ConnectionString:DBConnectionString"];
        }

        protected override string LogPrefix => "TemplateListingSearch";

        protected override string DocumentKeyField => TemplateIndexFields.Key;

        protected override SearchIndex CreateIndexDefinition(string indexName) =>
            TemplateIndexDefinition.CreateIndex(indexName);

        protected override SearchDocument ToAzureDocument(TemplateIndexDocument document) =>
            TemplateIndexDocumentMapper.ToAzureDocument(document);

        /// <summary>Soft-deleted templates are removed from the index; they are re-added if restored.</summary>
        protected override bool ShouldRemoveFromIndex(TemplateIndexDocument document) => document.IsDeleted;

        public Task<bool> UpsertTemplateAsync(int clonableSkillChannelId) => UpsertAsync(clonableSkillChannelId);

        public Task<bool> RemoveTemplateAsync(int clonableSkillChannelId) => RemoveAsync(clonableSkillChannelId);

        protected override async Task<IReadOnlyList<TemplateIndexDocument>> LoadDocumentsFromDatabaseAsync(
            int? documentId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var rows = await connection.QueryAsync<TemplateIndexDocumentRow>(
                    "usp_GetTemplateIndexBatch",
                    new { ClonableSkillChannelId = documentId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300);

                return rows.Select(TemplateIndexDocumentMapper.FromDatabaseRow).ToList();
            }
            catch (SqlException ex)
            {
                await LogExceptionAsync(ex,
                    $"LoadDocumentsFromDatabaseAsync SQL failed (clonableSkillChannelId={documentId?.ToString(CultureInfo.InvariantCulture) ?? "all"}).");
                throw;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex,
                    $"LoadDocumentsFromDatabaseAsync failed (clonableSkillChannelId={documentId?.ToString(CultureInfo.InvariantCulture) ?? "all"}).");
                throw;
            }
        }

        /// <summary>
        /// Queries Azure Search for the Templates list views (<c>/Template/Index</c>, <c>/Template/List</c>).
        /// Returns an empty list on a successful zero-hit query and <see langword="null"/> so callers
        /// fall back to SQL when disabled or on error.
        /// </summary>
        public async Task<IEnumerable<ClonableSkillChannelVM>> SearchAsync(ClonableTemplatesFilter filter = null, string searchValue = null)
        {
            if (!IsEnabled)
                return null;

            try
            {
                var searchClient = CreateSearchClient();
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

                var odataFilter = BuildSearchFilter(filter);
                var query = string.IsNullOrWhiteSpace(searchValue) ? "*" : searchValue.Trim();
                var results = new List<ClonableSkillChannelVM>();
                var skip = 0;

                while (true)
                {
                    var options = new SearchOptions
                    {
                        Filter = odataFilter,
                        Size = SearchPageSize,
                        Skip = skip,
                        OrderBy =
                        {
                            $"{TemplateIndexFields.SkillChannelCounts} desc",
                            $"{TemplateIndexFields.CreatedOn} desc"
                        }
                    };

                    foreach (var field in TemplateIndexFields.ListViewSelect)
                        options.Select.Add(field);

                    if (!string.IsNullOrWhiteSpace(searchValue))
                    {
                        foreach (var field in TemplateIndexFields.ListViewSearch)
                            options.SearchFields.Add(field);
                    }

                    var response = await searchClient.SearchAsync<SearchDocument>(query, options, timeoutCts.Token);
                    var pageCount = 0;

                    await foreach (var result in response.Value.GetResultsAsync().WithCancellation(timeoutCts.Token))
                    {
                        if (result.Document != null)
                        {
                            results.Add(TemplateIndexDocumentMapper.ToListViewModel(result.Document));
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
                await LogExceptionAsync(ex, "SearchAsync Azure request failed.");
                return null;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, "SearchAsync failed.");
                return null;
            }
        }

        /// <summary>Builds the OData filter for Archive (soft-delete) visibility and optional field filters.</summary>
        private static string BuildSearchFilter(ClonableTemplatesFilter filter)
        {
            var filters = new List<string>();

            var isArchiveView = filter != null && string.Equals(filter.Archive, "Yes", StringComparison.OrdinalIgnoreCase);
            filters.Add($"{TemplateIndexFields.IsDeleted} eq {(isArchiveView ? "true" : "false")}");

            if (filter == null)
                return string.Join(" and ", filters);

            if (!string.IsNullOrWhiteSpace(filter.Category))
                filters.Add($"{TemplateIndexFields.CategoryName} eq '{AzureSearchOData.Escape(filter.Category.Trim())}'");

            if (!string.IsNullOrWhiteSpace(filter.AssessmentGroupTemplate))
                filters.Add($"{TemplateIndexFields.AssessmentGroupName} eq '{AzureSearchOData.Escape(filter.AssessmentGroupTemplate.Trim())}'");

            if (int.TryParse(filter.CountryType, NumberStyles.Integer, CultureInfo.InvariantCulture, out var countryType))
                filters.Add($"{TemplateIndexFields.CountryType} eq {countryType.ToString(CultureInfo.InvariantCulture)}");

            if (int.TryParse(filter.CompanyType, NumberStyles.Integer, CultureInfo.InvariantCulture, out var companyType))
                filters.Add($"{TemplateIndexFields.CompanyTypeId} eq {companyType.ToString(CultureInfo.InvariantCulture)}");

            if (!string.IsNullOrWhiteSpace(filter.DifficultyLevel))
                filters.Add($"{TemplateIndexFields.DifficultyLevel} eq '{AzureSearchOData.Escape(filter.DifficultyLevel.Trim())}'");

            if (!string.IsNullOrWhiteSpace(filter.EmploymentType))
                filters.Add($"{TemplateIndexFields.EmploymentType} eq '{AzureSearchOData.Escape(filter.EmploymentType.Trim())}'");

            if (!string.IsNullOrWhiteSpace(filter.SeniorityLevel))
                filters.Add($"{TemplateIndexFields.SeniorityLevelId} eq '{AzureSearchOData.Escape(filter.SeniorityLevel.Trim())}'");

            // PrimarySkills uses ';'-delimited free text on both sides; matched in-memory by the caller
            // (TemplateController already applies this filter on the returned VMs).

            return string.Join(" and ", filters);
        }
    }
}
