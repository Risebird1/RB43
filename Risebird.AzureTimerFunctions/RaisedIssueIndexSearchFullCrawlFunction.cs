using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Indexes raised-issue support tickets into Azure AI Search.
    ///
    /// Full crawl (all tickets):
    ///   POST/GET /api/RaisedIssueIndexSearch/FullCrawl?code=&lt;function-key&gt;
    ///
    /// Single document upsert (create/close/reopen sync):
    ///   POST/GET /api/RaisedIssueIndexSearch/Upsert?raisedIssueId=123&amp;code=&lt;function-key&gt;
    ///
    /// Generic route also available:
    ///   /api/ListingIndex/RaisedIssueListing/FullCrawl
    /// </summary>
    public class RaisedIssueIndexSearchFullCrawlFunction
    {
        private readonly IRaisedIssueListingSearchRepository _raisedIssueListingSearchRepository;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger _log;

        public RaisedIssueIndexSearchFullCrawlFunction(
            IRaisedIssueListingSearchRepository raisedIssueListingSearchRepository,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            ILoggerFactory loggerFactory)
        {
            _raisedIssueListingSearchRepository = raisedIssueListingSearchRepository;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<RaisedIssueIndexSearchFullCrawlFunction>();
        }

        [Function("RaisedIssueIndexSearchFullCrawl")]
        public async Task<HttpResponseData> FullCrawl(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "RaisedIssueIndexSearch/FullCrawl")]
            HttpRequestData req)
        {
            return await RunIndexingAsync(req, fullCrawl: true, raisedIssueId: null);
        }

        [Function("RaisedIssueIndexSearchUpsert")]
        public async Task<HttpResponseData> Upsert(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "RaisedIssueIndexSearch/Upsert")]
            HttpRequestData req)
        {
            if (!TryParseRaisedIssueId(req, out int raisedIssueId, out string parseError))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync(parseError);
                return badRequest;
            }

            return await RunIndexingAsync(req, fullCrawl: false, raisedIssueId: raisedIssueId);
        }

        private async Task<HttpResponseData> RunIndexingAsync(
            HttpRequestData req,
            bool fullCrawl,
            int? raisedIssueId)
        {
            var response = req.CreateResponse();

            if (!_configuration.GetValue<bool>("AzureSearch:RaisedIssueListing:Enabled"))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("AzureSearch:RaisedIssueListing:Enabled is false.");
                return response;
            }

            if (!_raisedIssueListingSearchRepository.IsEnabled)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Azure Search endpoint or API key is not configured.");
                return response;
            }

            try
            {
                if (fullCrawl)
                {
                    _log.LogInformation("[RaisedIssueIndexSearch] Full crawl started at {UtcNow:u}", DateTime.UtcNow);
                    var indexed = await _raisedIssueListingSearchRepository.FullCrawlAsync();
                    _log.LogInformation("[RaisedIssueIndexSearch] Full crawl indexed {Count} documents.", indexed);

                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync($"Full crawl complete. Indexed {indexed} raised-issue documents.");
                    return response;
                }

                _log.LogInformation(
                    "[RaisedIssueIndexSearch] Upsert started for raisedIssueId={RaisedIssueId} at {UtcNow:u}",
                    raisedIssueId, DateTime.UtcNow);
                var success = await _raisedIssueListingSearchRepository.UpsertRaisedIssueAsync(raisedIssueId.Value);

                response.StatusCode = success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(success
                    ? $"Upsert complete for raisedIssueId {raisedIssueId.Value}."
                    : $"Upsert failed for raisedIssueId {raisedIssueId.Value}. See application logs.");
                return response;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[RaisedIssueIndexSearch] Indexing failed.");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(
                    $"Indexing failed: {ex.Message}. See application logs for details.");
                return response;
            }
        }

        private static bool TryParseRaisedIssueId(HttpRequestData req, out int raisedIssueId, out string error)
        {
            raisedIssueId = 0;
            error = null;

            var query = req.Url.Query;
            if (string.IsNullOrWhiteSpace(query))
            {
                error = "Query parameter 'raisedIssueId' is required. Example: /api/RaisedIssueIndexSearch/Upsert?raisedIssueId=123";
                return false;
            }

            var pairs = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            string raw = null;
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2
                    && (string.Equals(parts[0], "raisedIssueId", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(parts[0], "documentId", StringComparison.OrdinalIgnoreCase)))
                {
                    raw = Uri.UnescapeDataString(parts[1]);
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "Query parameter 'raisedIssueId' is required. Example: /api/RaisedIssueIndexSearch/Upsert?raisedIssueId=123";
                return false;
            }

            if (!int.TryParse(raw, out raisedIssueId) || raisedIssueId <= 0)
            {
                error = "Query parameter 'raisedIssueId' must be a positive integer.";
                return false;
            }

            return true;
        }
    }
}
