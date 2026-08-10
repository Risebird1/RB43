using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Search;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Reusable HTTP endpoints for Azure AI Search indexing of any registered listing type.
    /// <para>
    /// Full crawl:
    ///   POST/GET /api/ListingIndex/{listingKey}/FullCrawl?code=&lt;key&gt;
    ///   listingKey examples: IrfListing, InterviewerListing, RaisedIssueListing, TemplateListing
    /// </para>
    /// <para>
    /// Single upsert:
    ///   POST/GET /api/ListingIndex/{listingKey}/Upsert?documentId=123&amp;code=&lt;key&gt;
    /// </para>
    /// Legacy IRF routes remain on <see cref="IrfIndexSearchFullCrawlFunction"/>.
    /// </summary>
    public class ListingIndexHttpFunction
    {
        private readonly IListingIndexRepositoryResolver _resolver;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger _log;

        public ListingIndexHttpFunction(
            IListingIndexRepositoryResolver resolver,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            ILoggerFactory loggerFactory)
        {
            _resolver = resolver;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<ListingIndexHttpFunction>();
        }

        [Function("ListingIndexFullCrawl")]
        public async Task<HttpResponseData> FullCrawl(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "ListingIndex/{listingKey}/FullCrawl")]
            HttpRequestData req,
            string listingKey)
        {
            return await RunAsync(req, listingKey, fullCrawl: true, documentId: null);
        }

        [Function("ListingIndexUpsert")]
        public async Task<HttpResponseData> Upsert(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "ListingIndex/{listingKey}/Upsert")]
            HttpRequestData req,
            string listingKey)
        {
            if (!TryParseDocumentId(req, out int documentId, out string parseError))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync(parseError);
                return badRequest;
            }

            return await RunAsync(req, listingKey, fullCrawl: false, documentId: documentId);
        }

        private async Task<HttpResponseData> RunAsync(
            HttpRequestData req,
            string listingKey,
            bool fullCrawl,
            int? documentId)
        {
            var response = req.CreateResponse();
            var repository = _resolver.Resolve(listingKey);

            if (repository == null)
            {
                var known = string.Join(", ", _resolver.GetAll().Select(r => r.ListingKey));
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync(
                    $"Unknown listingKey '{listingKey}'. Registered: {(string.IsNullOrEmpty(known) ? "(none)" : known)}");
                return response;
            }

            if (!_configuration.GetValue<bool>($"AzureSearch:{repository.ListingKey}:Enabled"))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync($"AzureSearch:{repository.ListingKey}:Enabled is false.");
                return response;
            }

            if (!repository.IsEnabled)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Azure Search endpoint or API key is not configured.");
                return response;
            }

            try
            {
                if (fullCrawl)
                {
                    _log.LogInformation("[{ListingKey}] Full crawl started at {UtcNow:u}", repository.ListingKey, DateTime.UtcNow);
                    var indexed = await repository.FullCrawlAsync();
                    _log.LogInformation("[{ListingKey}] Full crawl indexed {Count} documents.", repository.ListingKey, indexed);

                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync($"Full crawl complete for {repository.ListingKey}. Indexed {indexed} documents.");
                    return response;
                }

                _log.LogInformation("[{ListingKey}] Upsert started for documentId={DocumentId}", repository.ListingKey, documentId);
                var success = await repository.UpsertAsync(documentId.Value);

                response.StatusCode = success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(success
                    ? $"Upsert complete for {repository.ListingKey} id {documentId.Value}."
                    : $"Upsert failed for {repository.ListingKey} id {documentId.Value}. See application logs.");
                return response;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[{ListingKey}] Indexing failed.", repository.ListingKey);
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Indexing failed. See application logs for details.");
                return response;
            }
        }

        private static bool TryParseDocumentId(HttpRequestData req, out int documentId, out string error)
        {
            documentId = 0;
            error = null;

            var query = req.Url.Query;
            if (string.IsNullOrWhiteSpace(query))
            {
                error = "Query parameter 'documentId' is required. Example: /api/ListingIndex/IrfListing/Upsert?documentId=123";
                return false;
            }

            var pairs = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            string raw = null;
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2
                    && (string.Equals(parts[0], "documentId", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(parts[0], "irfIntId", StringComparison.OrdinalIgnoreCase)))
                {
                    raw = Uri.UnescapeDataString(parts[1]);
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "Query parameter 'documentId' is required. Example: /api/ListingIndex/IrfListing/Upsert?documentId=123";
                return false;
            }

            if (!int.TryParse(raw, out documentId) || documentId <= 0)
            {
                error = "Query parameter 'documentId' must be a positive integer.";
                return false;
            }

            return true;
        }
    }
}
