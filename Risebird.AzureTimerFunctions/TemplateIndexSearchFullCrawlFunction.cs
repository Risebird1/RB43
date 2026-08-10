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
    /// Indexes clonable skill channel (template) documents into Azure AI Search.
    ///
    /// Full crawl (all active templates; soft-deleted rows are removed on upsert):
    ///   POST/GET /api/TemplateIndexSearch/FullCrawl?code=&lt;function-key&gt;
    ///
    /// Single document upsert (create/edit/delete/restore sync):
    ///   POST/GET /api/TemplateIndexSearch/Upsert?clonableSkillChannelId=123&amp;code=&lt;function-key&gt;
    ///
    /// Generic route also available:
    ///   /api/ListingIndex/TemplateListing/FullCrawl
    /// </summary>
    public class TemplateIndexSearchFullCrawlFunction
    {
        private readonly ITemplateListingSearchRepository _templateListingSearchRepository;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger _log;

        public TemplateIndexSearchFullCrawlFunction(
            ITemplateListingSearchRepository templateListingSearchRepository,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            ILoggerFactory loggerFactory)
        {
            _templateListingSearchRepository = templateListingSearchRepository;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<TemplateIndexSearchFullCrawlFunction>();
        }

        [Function("TemplateIndexSearchFullCrawl")]
        public async Task<HttpResponseData> FullCrawl(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "TemplateIndexSearch/FullCrawl")]
            HttpRequestData req)
        {
            return await RunIndexingAsync(req, fullCrawl: true, clonableSkillChannelId: null);
        }

        [Function("TemplateIndexSearchUpsert")]
        public async Task<HttpResponseData> Upsert(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "TemplateIndexSearch/Upsert")]
            HttpRequestData req)
        {
            if (!TryParseClonableSkillChannelId(req, out int clonableSkillChannelId, out string parseError))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync(parseError);
                return badRequest;
            }

            return await RunIndexingAsync(req, fullCrawl: false, clonableSkillChannelId: clonableSkillChannelId);
        }

        private async Task<HttpResponseData> RunIndexingAsync(
            HttpRequestData req,
            bool fullCrawl,
            int? clonableSkillChannelId)
        {
            var response = req.CreateResponse();

            if (!_configuration.GetValue<bool>("AzureSearch:TemplateListing:Enabled"))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("AzureSearch:TemplateListing:Enabled is false.");
                return response;
            }

            if (!_templateListingSearchRepository.IsEnabled)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Azure Search endpoint or API key is not configured.");
                return response;
            }

            try
            {
                if (fullCrawl)
                {
                    _log.LogInformation("[TemplateIndexSearch] Full crawl started at {UtcNow:u}", DateTime.UtcNow);
                    var indexed = await _templateListingSearchRepository.FullCrawlAsync();
                    _log.LogInformation("[TemplateIndexSearch] Full crawl indexed {Count} documents.", indexed);

                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync($"Full crawl complete. Indexed {indexed} template documents.");
                    return response;
                }

                _log.LogInformation(
                    "[TemplateIndexSearch] Upsert started for clonableSkillChannelId={ClonableSkillChannelId} at {UtcNow:u}",
                    clonableSkillChannelId, DateTime.UtcNow);
                var success = await _templateListingSearchRepository.UpsertTemplateAsync(clonableSkillChannelId.Value);

                response.StatusCode = success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(success
                    ? $"Upsert complete for clonableSkillChannelId {clonableSkillChannelId.Value}."
                    : $"Upsert failed for clonableSkillChannelId {clonableSkillChannelId.Value}. See application logs.");
                return response;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[TemplateIndexSearch] Indexing failed.");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Indexing failed. See application logs for details.");
                return response;
            }
        }

        private static bool TryParseClonableSkillChannelId(HttpRequestData req, out int clonableSkillChannelId, out string error)
        {
            clonableSkillChannelId = 0;
            error = null;

            var query = req.Url.Query;
            if (string.IsNullOrWhiteSpace(query))
            {
                error = "Query parameter 'clonableSkillChannelId' is required. Example: /api/TemplateIndexSearch/Upsert?clonableSkillChannelId=123";
                return false;
            }

            var pairs = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            string raw = null;
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2
                    && (string.Equals(parts[0], "clonableSkillChannelId", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(parts[0], "documentId", StringComparison.OrdinalIgnoreCase)))
                {
                    raw = Uri.UnescapeDataString(parts[1]);
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "Query parameter 'clonableSkillChannelId' is required. Example: /api/TemplateIndexSearch/Upsert?clonableSkillChannelId=123";
                return false;
            }

            if (!int.TryParse(raw, out clonableSkillChannelId) || clonableSkillChannelId <= 0)
            {
                error = "Query parameter 'clonableSkillChannelId' must be a positive integer.";
                return false;
            }

            return true;
        }
    }
}
