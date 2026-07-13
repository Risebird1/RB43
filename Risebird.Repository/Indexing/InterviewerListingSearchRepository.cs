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
        public async Task<IEnumerable<InterviewerProfileVM>> SearchAllAsync(string searchValue = null)
        {
            if (!IsEnabled)
                return null;

            try
            {
                var searchClient = CreateSearchClient();
                var results = new List<InterviewerProfileVM>();
                var pageSize = Math.Max(PageSize, 100);
                var skip = 0;

                while (true)
                {
                    var options = new SearchOptions
                    {
                        Filter = $"{InterviewerIndexFields.IsDeleted} eq false",
                        Size = pageSize,
                        Skip = skip,
                        IncludeTotalCount = true,
                        OrderBy = { $"{InterviewerIndexFields.FirstName} asc", $"{InterviewerIndexFields.LastName} asc" }
                    };

                    foreach (var field in InterviewerIndexFields.ListViewSearch)
                        options.SearchFields.Add(field);

                    var query = string.IsNullOrWhiteSpace(searchValue) ? "*" : searchValue.Trim();
                    var response = await searchClient.SearchAsync<SearchDocument>(query, options);
                    var batchCount = 0;

                    await foreach (var result in response.Value.GetResultsAsync())
                    {
                        if (result.Document != null)
                        {
                            results.Add(InterviewerIndexDocumentMapper.ToListViewModel(result.Document));
                            batchCount++;
                        }
                    }

                    if (batchCount < pageSize)
                        break;

                    skip += pageSize;
                    if (skip >= 100000)
                        break;
                }

                return results;
            }
            catch (RequestFailedException ex)
            {
                await LogExceptionAsync(ex, "SearchAllAsync Azure request failed.");
                return null;
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(ex, "SearchAllAsync failed.");
                return null;
            }
        }
    }
}
