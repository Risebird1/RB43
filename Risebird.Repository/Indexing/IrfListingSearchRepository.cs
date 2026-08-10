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
using System.Threading.Tasks;

namespace Risebird.Repository
{
    /// <summary>
    /// IRF listing plugin for Azure AI Search.
    /// <para>
    /// Indexing (FullCrawl / Upsert / Remove) is inherited from
    /// <see cref="AzureListingIndexRepositoryBase{TDocument}"/>.
    /// This class only adds IRF-specific SQL loading, mapping, ACL filters, and list SearchAsync.
    /// </para>
    /// <para>
    /// Copy this pattern for Candidate / Interviewer / User: subclass the base, set ListingKey,
    /// implement the abstract hooks, then add a SearchAsync for that list UI.
    /// </para>
    /// </summary>
    public class IrfListingSearchRepository : AzureListingIndexRepositoryBase<IrfIndexDocument>, IIrfListingSearchRepository
    {
        public const string ConfigListingKey = "IrfListing";

        private static readonly HashSet<string> InternalWideAccessProfiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Admin", "Finance", "SupportAdmin", "SeniorInterviewerManagement"
        };

        private static readonly HashSet<string> SqlFallbackProfiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Recruiter"
        };

        private readonly string _connectionString;

        public IrfListingSearchRepository(
            IConfiguration configuration,
            IAzureSearchClientFactory clientFactory,
            IApplicationErrorLogger applicationErrorLogger)
            : base(
                clientFactory,
                applicationErrorLogger,
                ListingSearchOptionsLoader.Load(
                    configuration,
                    ConfigListingKey,
                    IrfIndexDefinition.DefaultIndexName,
                    defaultPageSize: 9,
                    defaultBatchSize: IrfIndexDefinition.DefaultBatchSize).Listing)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _connectionString = configuration["ConnectionString:DBConnectionString"];
        }

        /// <inheritdoc />
        protected override string LogPrefix => "IrfListingSearch";

        /// <inheritdoc />
        protected override string DocumentKeyField => IrfIndexFields.Key;

        /// <inheritdoc />
        protected override SearchIndex CreateIndexDefinition(string indexName) =>
            IrfIndexDefinition.CreateIndex(indexName);

        /// <inheritdoc />
        protected override SearchDocument ToAzureDocument(IrfIndexDocument document) =>
            IrfIndexDocumentMapper.ToAzureDocument(document);

        /// <inheritdoc />
        protected override bool ShouldRemoveFromIndex(IrfIndexDocument document) =>
            document.IsDeleted;

        /// <summary>Whether Azure Search can serve the IRF list for the given profile.</summary>
        public bool SupportsProfile(string profileName) => CanUseSearchForProfile(profileName);

        /// <summary>Recruiter always uses SQL; other profiles may use search.</summary>
        public static bool CanUseSearchForProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return false;

            return !SqlFallbackProfiles.Contains(profileName.Trim());
        }

        /// <inheritdoc cref="IIrfListingSearchRepository.UpsertIrfAsync"/>
        public Task<bool> UpsertIrfAsync(int irfIntId) => UpsertAsync(irfIntId);

        /// <inheritdoc cref="IIrfListingSearchRepository.RemoveIrfAsync"/>
        public Task<bool> RemoveIrfAsync(int irfIntId) => RemoveAsync(irfIntId);

        /// <inheritdoc />
        public async Task<IEnumerable<IrfIndexDocument>> GetIndexDocumentsFromDatabaseAsync(int? irfIntId = null) =>
            await LoadDocumentsFromDatabaseAsync(irfIntId);

        /// <inheritdoc />
        protected override async Task<IReadOnlyList<IrfIndexDocument>> LoadDocumentsFromDatabaseAsync(int? documentId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var rows = await connection.QueryAsync<IrfIndexDocumentRow>(
                    "usp_GetIrfIndexBatch",
                    new { IrfIntId = documentId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300);

                return rows.Select(IrfIndexDocumentMapper.FromDatabaseRow).ToList();
            }
            catch (SqlException ex)
            {
                await LogExceptionAsync(ex, $"LoadDocumentsFromDatabaseAsync SQL failed (irfIntId={documentId?.ToString(CultureInfo.InvariantCulture) ?? "all"}).");
                throw;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, $"LoadDocumentsFromDatabaseAsync failed (irfIntId={documentId?.ToString(CultureInfo.InvariantCulture) ?? "all"}).");
                throw;
            }
        }

        /// <summary>
        /// Queries Azure Search for the paginated IRF list on <c>/IRF/Index</c>.
        /// Returns <see langword="null"/> so callers fall back to SQL when disabled or on error.
        /// </summary>
        public async Task<IEnumerable<IRF>> SearchAsync(
            string emailId,
            string profileName,
            int? pageNumber,
            string searchValue = null,
            IRFFilter filterModel = null)
        {
            if (!IsEnabled || !CanUseSearchForProfile(profileName))
                return null;

            try
            {
                var searchClient = CreateSearchClient();
                var page = pageNumber.GetValueOrDefault(1);
                if (page < 1)
                    page = 1;

                var options = new SearchOptions
                {
                    Filter = BuildSearchFilter(emailId, profileName, filterModel),
                    Size = PageSize,
                    Skip = (page - 1) * PageSize,
                    IncludeTotalCount = true,
                    OrderBy = { $"{IrfIndexFields.CreatedOn} desc" }
                };

                foreach (var field in IrfIndexFields.ListViewSearch)
                    options.SearchFields.Add(field);

                var query = string.IsNullOrWhiteSpace(searchValue) ? "*" : searchValue.Trim();
                var response = await searchClient.SearchAsync<SearchDocument>(query, options);
                var results = new List<IRF>();

                await foreach (var result in response.Value.GetResultsAsync())
                {
                    if (result.Document != null)
                        results.Add(IrfIndexDocumentMapper.ToListViewModel(result.Document, profileName));
                }

                return results;
            }
            catch (RequestFailedException ex)
            {
                await LogExceptionAsync(ex, $"SearchAsync Azure request failed (profile={profileName}, page={pageNumber}).");
                return null;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, $"SearchAsync failed (profile={profileName}, page={pageNumber}).");
                return null;
            }
        }

        /// <summary>Builds OData filter for status, ACL, dates, and UI facets.</summary>
        private static string BuildSearchFilter(string emailId, string profileName, IRFFilter filterModel)
        {
            var filters = new List<string> { $"{IrfIndexFields.IsDeleted} eq false" };
            filters.Add(BuildVisibilityFilter(emailId, profileName));

            var isFilterApplied = filterModel != null;
            if (!isFilterApplied)
                filters.Add($"{IrfIndexFields.EndDate} gt {AzureSearchOData.FormatDate(DateTime.Now.Date)}");

            if (filterModel != null)
            {
                if (filterModel.StartDate.HasValue)
                    filters.Add($"{IrfIndexFields.StartDate} ge {AzureSearchOData.FormatDate(filterModel.StartDate.Value.Date)}");

                if (filterModel.EndDate.HasValue)
                    filters.Add($"{IrfIndexFields.EndDate} le {AzureSearchOData.FormatDate(filterModel.EndDate.Value.Date)}");

                AzureSearchOData.AppendInFilter(filters, IrfIndexFields.IrfId, filterModel.RFID);
                AzureSearchOData.AppendInFilter(filters, IrfIndexFields.ClientCompName, filterModel.ClientName);
                AzureSearchOData.AppendInFilter(filters, IrfIndexFields.ClientName, filterModel.EndClientName);
                AzureSearchOData.AppendInFilter(filters, IrfIndexFields.PrimarySkill, filterModel.PrimarySkill);
            }

            return string.Join(" and ", filters);
        }

        private static string BuildVisibilityFilter(string emailId, string profileName)
        {
            if (string.Equals(profileName, "CuratedUser", StringComparison.OrdinalIgnoreCase))
                return $"{IrfIndexFields.CuratedLink} eq true";

            if (InternalWideAccessProfiles.Contains(profileName))
                return $"{IrfIndexFields.AllowAllInternal} eq true";

            return $"{IrfIndexFields.AllowedViewerEmails}/any(v: v eq '{AzureSearchOData.Escape((emailId ?? string.Empty).Trim().ToLowerInvariant())}')";
        }
    }
}
