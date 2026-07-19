using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
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
    /// Interviewer roster plugin for Azure AI Search (<c>/Interviewer/Index</c>).
    /// </summary>
    public class InterviewerListingSearchRepository
        : AzureListingIndexRepositoryBase<InterviewerIndexDocument>,
          IInterviewerListingSearchRepository
    {
        public const string ConfigListingKey = "InterviewerListing";

        private readonly string _connectionString;

        public InterviewerListingSearchRepository(
            IConfiguration configuration,
            IAzureSearchClientFactory clientFactory,
            IApplicationErrorLogger applicationErrorLogger)
            : base(
                clientFactory,
                applicationErrorLogger,
                ListingSearchOptionsLoader.Load(
                    configuration,
                    ConfigListingKey,
                    InterviewerIndexDefinition.DefaultIndexName,
                    defaultPageSize: InterviewerIndexDefinition.DefaultPageSize,
                    defaultBatchSize: InterviewerIndexDefinition.DefaultBatchSize).Listing)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _connectionString = configuration["ConnectionString:DBConnectionString"];
        }

        protected override string LogPrefix => "InterviewerListingSearch";

        protected override string DocumentKeyField => InterviewerIndexFields.Key;

        protected override SearchIndex CreateIndexDefinition(string indexName) =>
            InterviewerIndexDefinition.CreateIndex(indexName);

        protected override SearchDocument ToAzureDocument(InterviewerIndexDocument document) =>
            InterviewerIndexDocumentMapper.ToAzureDocument(document);

        protected override bool ShouldRemoveFromIndex(InterviewerIndexDocument document) =>
            document.IsDeleted;

        public Task<bool> UpsertInterviewerAsync(int interviewerProfileId) =>
            UpsertAsync(interviewerProfileId);

        public Task<bool> RemoveInterviewerAsync(int interviewerProfileId) =>
            RemoveAsync(interviewerProfileId);

        public async Task<bool> UpsertInterviewerByUserIdAsync(int userId)
        {
            if (!IsEnabled || userId <= 0)
                return false;

            try
            {
                var documents = await QueryDocumentsAsync(interviewerProfileId: null, userId: userId);
                var document = documents.FirstOrDefault();
                if (document == null)
                    return false;

                return await UpsertAsync(document.InterviewerProfileId);
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, $"UpsertInterviewerByUserIdAsync failed for userId={userId}.");
                return false;
            }
        }

        public async Task<bool> UpsertInterviewerByUniqueIdAsync(string uniqueId)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(uniqueId))
                return false;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var profileId = await connection.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT TOP 1 intp.Id
                      FROM dbo.InterviewerProfile intp
                      INNER JOIN dbo.UserList ul ON ul.id = intp.UserId
                      WHERE ul.uniqueid = @uniqueId",
                    new { uniqueId = uniqueId.Trim() });

                if (!profileId.HasValue || profileId.Value <= 0)
                    return false;

                return await UpsertAsync(profileId.Value);
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, $"UpsertInterviewerByUniqueIdAsync failed for uniqueId='{uniqueId}'.");
                return false;
            }
        }

        public async Task<bool> UpsertInterviewerByEmailAsync(string emailId)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(emailId))
                return false;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var profileId = await connection.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT TOP 1 intp.Id
                      FROM dbo.InterviewerProfile intp
                      INNER JOIN dbo.UserList ul ON ul.id = intp.UserId
                      WHERE ul.EmailId = @emailId",
                    new { emailId = emailId.Trim() });

                if (!profileId.HasValue || profileId.Value <= 0)
                    return false;

                return await UpsertAsync(profileId.Value);
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, $"UpsertInterviewerByEmailAsync failed for emailId='{emailId}'.");
                return false;
            }
        }

        public async Task<IEnumerable<InterviewerIndexDocument>> GetIndexDocumentsFromDatabaseAsync(
            int? interviewerProfileId = null,
            int? userId = null) =>
            await QueryDocumentsAsync(interviewerProfileId, userId);

        protected override async Task<IReadOnlyList<InterviewerIndexDocument>> LoadDocumentsFromDatabaseAsync(
            int? documentId) =>
            await QueryDocumentsAsync(documentId, userId: null);

        private async Task<IReadOnlyList<InterviewerIndexDocument>> QueryDocumentsAsync(
            int? interviewerProfileId,
            int? userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var rows = await connection.QueryAsync<InterviewerIndexDocumentRow>(
                    "usp_GetInterviewerIndexBatch",
                    new
                    {
                        InterviewerProfileId = interviewerProfileId,
                        UserId = userId
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 300);

                return rows.Select(InterviewerIndexDocumentMapper.FromDatabaseRow).ToList();
            }
            catch (SqlException ex)
            {
                await LogExceptionAsync(ex,
                    $"QueryDocumentsAsync SQL failed (profileId={interviewerProfileId?.ToString(CultureInfo.InvariantCulture) ?? "null"}, userId={userId?.ToString(CultureInfo.InvariantCulture) ?? "null"}).");
                throw;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex,
                    $"QueryDocumentsAsync failed (profileId={interviewerProfileId?.ToString(CultureInfo.InvariantCulture) ?? "null"}, userId={userId?.ToString(CultureInfo.InvariantCulture) ?? "null"}).");
                throw;
            }
        }

        /// <inheritdoc />
        public Task<IEnumerable<InterviewerProfileVM>> SearchAllAsync(string searchValue = null)
        {
            // Full-roster download is not viable (production index is 60k+ documents).
            // Index page must use SearchPageAsync; export/JSON paths use SQL GetInterviewers.
            return Task.FromResult<IEnumerable<InterviewerProfileVM>>(null);
        }

        /// <inheritdoc />
        public async Task<InterviewerSearchPageResult> SearchPageAsync(
            int pageNumber,
            int pageSize,
            string searchValue = null,
            InterviewFilter filter = null)
        {
            if (!IsEnabled)
                return null;

            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1)
                pageSize = PageSize > 0 ? PageSize : 10;
            if (pageSize > 1000)
                pageSize = 1000;

            try
            {
                var searchClient = CreateSearchClient();
                using var timeoutCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(20));

                var options = new SearchOptions
                {
                    Filter = BuildSearchFilter(filter),
                    Size = pageSize,
                    Skip = (pageNumber - 1) * pageSize,
                    IncludeTotalCount = true,
                    OrderBy =
                    {
                        $"{InterviewerIndexFields.FirstName} asc",
                        $"{InterviewerIndexFields.LastName} asc"
                    }
                };

                foreach (var field in InterviewerIndexFields.ListViewSearch)
                    options.SearchFields.Add(field);

                foreach (var field in InterviewerIndexFields.ListViewSelect)
                    options.Select.Add(field);

                var query = string.IsNullOrWhiteSpace(searchValue) ? "*" : searchValue.Trim();
                var response = await searchClient.SearchAsync<SearchDocument>(query, options, timeoutCts.Token);
                var items = new List<InterviewerProfileVM>();

                await foreach (var result in response.Value.GetResultsAsync().WithCancellation(timeoutCts.Token))
                {
                    if (result.Document != null)
                        items.Add(InterviewerIndexDocumentMapper.ToListViewModel(result.Document));
                }

                return new InterviewerSearchPageResult
                {
                    Items = items,
                    TotalCount = response.Value.TotalCount ?? items.Count,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            catch (OperationCanceledException ex)
            {
                await LogExceptionAsync(ex, "SearchPageAsync timed out after 20s — falling back to SQL.");
                return null;
            }
            catch (RequestFailedException ex)
            {
                await LogExceptionAsync(ex, "SearchPageAsync Azure request failed.");
                return null;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, "SearchPageAsync failed.");
                return null;
            }
        }

        private static string BuildSearchFilter(InterviewFilter filter)
        {
            var filters = new List<string> { $"{InterviewerIndexFields.IsDeleted} eq false" };

            if (filter == null)
            {
                filters.Add($"{InterviewerIndexFields.IsActive} eq true");
                return string.Join(" and ", filters);
            }

            AppendBoolInFilter(filters, InterviewerIndexFields.IsActive, filter.IsActive, defaultWhenEmpty: true);
            AppendBoolInFilter(filters, InterviewerIndexFields.IsInterested, filter.IsInterested, defaultWhenEmpty: null);
            AppendLeadInterviewerFilter(filters, filter.IsLeadInterviewer);

            AzureSearchOData.AppendInFilter(filters, InterviewerIndexFields.Senioritylevel, filter.Senioritylevel);
            AzureSearchOData.AppendInFilter(filters, InterviewerIndexFields.CompanyType, filter.CompanyType);
            AzureSearchOData.AppendInFilter(filters, InterviewerIndexFields.Country, filter.Domain);
            AzureSearchOData.AppendInFilter(
                filters,
                InterviewerIndexFields.PerHourRateIndia,
                MapEnumDescriptionsToIds<IndiaPrice>(filter.IndianRate));
            AzureSearchOData.AppendInFilter(
                filters,
                InterviewerIndexFields.PerHourRateOutside,
                MapEnumDescriptionsToIds<OutSideIndiaPrice>(filter.OutSideIndiaRate));

            // ProfileType / CompentencyLevel are stored as numeric strings; UI filters use descriptions.
            AzureSearchOData.AppendInFilter(
                filters,
                InterviewerIndexFields.ProfileType,
                MapEnumDescriptionsToIds<Heuristicsscorelevel>(filter.ProfileType));
            AzureSearchOData.AppendInFilter(
                filters,
                InterviewerIndexFields.CompentencyLevel,
                MapEnumDescriptionsToIds<CompetencyLevel>(filter.CompentencyLevel));

            AppendCollectionAnyFilter(filters, InterviewerIndexFields.AssessmentGroupNames, filter.AssessmentGroup);

            return string.Join(" and ", filters);
        }

        private static void AppendBoolInFilter(
            IList<string> filters,
            string fieldName,
            bool[] values,
            bool? defaultWhenEmpty)
        {
            if (values != null && values.Length > 0)
            {
                var distinct = values.Distinct().ToList();
                if (distinct.Count == 1)
                    filters.Add($"{fieldName} eq {distinct[0].ToString().ToLowerInvariant()}");
                else
                    filters.Add($"({string.Join(" or ", distinct.Select(v => $"{fieldName} eq {v.ToString().ToLowerInvariant()}"))})");
                return;
            }

            if (defaultWhenEmpty.HasValue)
                filters.Add($"{fieldName} eq {defaultWhenEmpty.Value.ToString().ToLowerInvariant()}");
        }

        private static void AppendLeadInterviewerFilter(IList<string> filters, string[] values)
        {
            if (values == null || values.Length == 0)
                return;

            var bools = new List<bool>();
            foreach (var value in values)
            {
                if (bool.TryParse(value, out var parsed))
                    bools.Add(parsed);
                else if (string.Equals(value, "Yes", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(value, "Lead Interviewer", StringComparison.OrdinalIgnoreCase))
                    bools.Add(true);
                else if (string.Equals(value, "No", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(value, "Not a Lead Interviewer", StringComparison.OrdinalIgnoreCase))
                    bools.Add(false);
            }

            if (bools.Count == 0)
                return;

            AppendBoolInFilter(filters, InterviewerIndexFields.IsLeadInterviewer, bools.Distinct().ToArray(), defaultWhenEmpty: null);
        }

        private static void AppendCollectionAnyFilter(IList<string> filters, string fieldName, IEnumerable<string> values)
        {
            if (filters == null || string.IsNullOrWhiteSpace(fieldName) || values == null)
                return;

            var clauses = values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => $"{fieldName}/any(a: a eq '{AzureSearchOData.Escape(v.Trim())}')")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (clauses.Count == 0)
                return;

            filters.Add(clauses.Count == 1 ? clauses[0] : $"({string.Join(" or ", clauses)})");
        }

        private static IEnumerable<string> MapEnumDescriptionsToIds<TEnum>(IEnumerable<string> descriptions)
            where TEnum : struct, Enum
        {
            if (descriptions == null)
                yield break;

            var map = Enum.GetValues(typeof(TEnum))
                .Cast<Enum>()
                .ToDictionary(
                    e => e.GetDescription(),
                    e => Convert.ToInt32(e, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var description in descriptions.Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                var trimmed = description.Trim();
                if (map.TryGetValue(trimmed, out var id))
                    yield return id;
                else if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    yield return trimmed; // already an id
            }
        }
    }
}
