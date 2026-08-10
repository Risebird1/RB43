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
    /// Indexes IRF list-view documents into Azure AI Search.
    ///
    /// Full crawl (all active IRFs):
    ///   POST/GET /api/IrfIndexSearch/FullCrawl?code=&lt;function-key&gt;
    ///
    /// Single document upsert (create/edit/archive sync):
    ///   POST/GET /api/IrfIndexSearch/Upsert?irfIntId=123&amp;code=&lt;function-key&gt;
    /// </summary>
    public class IrfIndexSearchFullCrawlFunction
    {
        private readonly IIrfListingSearchRepository _irfListingSearchRepository;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger _log;

        public IrfIndexSearchFullCrawlFunction(
            IIrfListingSearchRepository irfListingSearchRepository,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            ILoggerFactory loggerFactory)
        {
            _irfListingSearchRepository = irfListingSearchRepository;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<IrfIndexSearchFullCrawlFunction>();
        }

        [Function("IrfIndexSearchFullCrawl")]
        public async Task<HttpResponseData> FullCrawl(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "IrfIndexSearch/FullCrawl")] HttpRequestData req)
        {
            return await RunIndexingAsync(req, fullCrawl: true, irfIntId: null);
        }

        [Function("IrfIndexSearchUpsert")]
        public async Task<HttpResponseData> Upsert(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "IrfIndexSearch/Upsert")] HttpRequestData req)
        {
            if (!TryParseIrfIntId(req, out int irfIntId, out string parseError))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync(parseError);
                return badRequest;
            }

            return await RunIndexingAsync(req, fullCrawl: false, irfIntId: irfIntId);
        }

        private async Task<HttpResponseData> RunIndexingAsync(HttpRequestData req, bool fullCrawl, int? irfIntId)
        {
            var response = req.CreateResponse();

            if (!_configuration.GetValue<bool>("AzureSearch:IrfListing:Enabled"))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("AzureSearch:IrfListing:Enabled is false.");
                return response;
            }

            if (!_irfListingSearchRepository.IsEnabled)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Azure Search endpoint or API key is not configured.");
                return response;
            }

            try
            {
                if (fullCrawl)
                {
                    _log.LogInformation("[IrfIndexSearch] Full crawl started at {UtcNow:u}", DateTime.UtcNow);
                    var indexed = await _irfListingSearchRepository.FullCrawlAsync();
                    _log.LogInformation("[IrfIndexSearch] Full crawl indexed {Count} documents.", indexed);

                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync($"Full crawl complete. Indexed {indexed} IRF documents.");
                    return response;
                }

                _log.LogInformation("[IrfIndexSearch] Upsert started for irfIntId={IrfIntId} at {UtcNow:u}", irfIntId, DateTime.UtcNow);
                var success = await _irfListingSearchRepository.UpsertIrfAsync(irfIntId.Value);

                response.StatusCode = success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(success
                    ? $"Upsert complete for IRF id {irfIntId.Value}."
                    : $"Upsert failed for IRF id {irfIntId.Value}. See application logs.");
                return response;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[IrfIndexSearch] Indexing failed.");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Indexing failed. See application logs for details.");
                return response;
            }
        }

        private static bool TryParseIrfIntId(HttpRequestData req, out int irfIntId, out string error)
        {
            irfIntId = 0;
            error = null;

            var query = req.Url.Query;
            if (string.IsNullOrWhiteSpace(query))
            {
                error = "Query parameter 'irfIntId' is required. Example: /api/IrfIndexSearch/Upsert?irfIntId=123";
                return false;
            }

            var pairs = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            string raw = null;
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2 && string.Equals(parts[0], "irfIntId", StringComparison.OrdinalIgnoreCase))
                {
                    raw = Uri.UnescapeDataString(parts[1]);
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "Query parameter 'irfIntId' is required. Example: /api/IrfIndexSearch/Upsert?irfIntId=123";
                return false;
            }

            if (!int.TryParse(raw, out irfIntId) || irfIntId <= 0)
            {
                error = "Query parameter 'irfIntId' must be a positive integer.";
                return false;
            }

            return true;
        }
    }
}
