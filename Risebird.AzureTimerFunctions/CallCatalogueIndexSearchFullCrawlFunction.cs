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
    /// Indexes HR call catalogue rows into Azure AI Search.
    /// </summary>
    public class CallCatalogueIndexSearchFullCrawlFunction
    {
        private readonly ICallCatalogueListingSearchRepository _callCatalogueListingSearchRepository;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger _log;

        public CallCatalogueIndexSearchFullCrawlFunction(
            ICallCatalogueListingSearchRepository callCatalogueListingSearchRepository,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            ILoggerFactory loggerFactory)
        {
            _callCatalogueListingSearchRepository = callCatalogueListingSearchRepository;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<CallCatalogueIndexSearchFullCrawlFunction>();
        }

        [Function("CallCatalogueIndexSearchFullCrawl")]
        public async Task<HttpResponseData> FullCrawl(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "CallCatalogueIndexSearch/FullCrawl")]
            HttpRequestData req)
        {
            return await RunFullCrawlAsync(req);
        }

        [Function("CallCatalogueIndexSearchUpsertCandidate")]
        public async Task<HttpResponseData> UpsertCandidate(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "CallCatalogueIndexSearch/UpsertCandidate")]
            HttpRequestData req)
        {
            if (!TryParseCandidateUpsert(req, out int irfIntId, out string candidateEmail, out string parseError))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync(parseError);
                return badRequest;
            }

            return await RunCandidateUpsertAsync(req, irfIntId, candidateEmail);
        }

        [Function("CallCatalogueIndexSearchUpsertInterviewer")]
        public async Task<HttpResponseData> UpsertInterviewer(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "CallCatalogueIndexSearch/UpsertInterviewer")]
            HttpRequestData req)
        {
            if (!TryParseInterviewerUpsert(req, out string interviewerEmail, out string parseError))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync(parseError);
                return badRequest;
            }

            return await RunInterviewerUpsertAsync(req, interviewerEmail);
        }

        private async Task<HttpResponseData> RunFullCrawlAsync(HttpRequestData req)
        {
            var response = req.CreateResponse();

            if (!_configuration.GetValue<bool>("AzureSearch:CallCatalogueListing:Enabled"))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("AzureSearch:CallCatalogueListing:Enabled is false.");
                return response;
            }

            if (!_callCatalogueListingSearchRepository.IsEnabled)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Azure Search endpoint or API key is not configured.");
                return response;
            }

            try
            {
                _log.LogInformation("[CallCatalogueIndexSearch] Full crawl started at {UtcNow:u}", DateTime.UtcNow);
                var indexed = await _callCatalogueListingSearchRepository.FullCrawlAsync();
                _log.LogInformation("[CallCatalogueIndexSearch] Full crawl indexed {Count} documents.", indexed);

                response.StatusCode = HttpStatusCode.OK;
                var message = indexed > 0
                    ? $"Full crawl complete. Indexed {indexed} call catalogue documents."
                    : "Full crawl finished with 0 documents indexed. Check application error logs: "
                      + "deploy usp_GetCallCatalogueIndexBatch, confirm SQL call-recording data exists, "
                      + "and ensure document keys use Azure-safe characters (redeploy app + SP).";
                await response.WriteStringAsync(message);
                return response;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[CallCatalogueIndexSearch] Full crawl failed.");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Indexing failed: {ex.Message}. See application logs for details.");
                return response;
            }
        }

        private async Task<HttpResponseData> RunCandidateUpsertAsync(
            HttpRequestData req,
            int irfIntId,
            string candidateEmail)
        {
            var response = req.CreateResponse();

            if (!_configuration.GetValue<bool>("AzureSearch:CallCatalogueListing:Enabled")
                || !_callCatalogueListingSearchRepository.IsEnabled)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Call catalogue Azure Search is not enabled or configured.");
                return response;
            }

            try
            {
                var success = await _callCatalogueListingSearchRepository.UpsertCandidateCallCatalogueAsync(
                    irfIntId, candidateEmail);

                response.StatusCode = success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(success
                    ? $"Upsert complete for candidate {candidateEmail} on IRF {irfIntId}."
                    : $"Upsert failed for candidate {candidateEmail} on IRF {irfIntId}. See application logs.");
                return response;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Indexing failed: {ex.Message}.");
                return response;
            }
        }

        private async Task<HttpResponseData> RunInterviewerUpsertAsync(HttpRequestData req, string interviewerEmail)
        {
            var response = req.CreateResponse();

            if (!_configuration.GetValue<bool>("AzureSearch:CallCatalogueListing:Enabled")
                || !_callCatalogueListingSearchRepository.IsEnabled)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Call catalogue Azure Search is not enabled or configured.");
                return response;
            }

            try
            {
                var success = await _callCatalogueListingSearchRepository.UpsertInterviewerCallCatalogueAsync(
                    interviewerEmail);

                response.StatusCode = success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(success
                    ? $"Upsert complete for interviewer {interviewerEmail}."
                    : $"Upsert failed for interviewer {interviewerEmail}. See application logs.");
                return response;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"Indexing failed: {ex.Message}.");
                return response;
            }
        }

        private static bool TryParseCandidateUpsert(
            HttpRequestData req,
            out int irfIntId,
            out string candidateEmail,
            out string error)
        {
            irfIntId = 0;
            candidateEmail = null;
            error = null;

            var query = req.Url.Query?.TrimStart('?') ?? string.Empty;
            string rawIrf = null;
            string rawEmail = null;

            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length != 2)
                    continue;

                if (string.Equals(parts[0], "irfIntId", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(parts[0], "documentId", StringComparison.OrdinalIgnoreCase))
                    rawIrf = Uri.UnescapeDataString(parts[1]);
                else if (string.Equals(parts[0], "candidateEmail", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(parts[0], "email", StringComparison.OrdinalIgnoreCase))
                    rawEmail = Uri.UnescapeDataString(parts[1]);
            }

            if (string.IsNullOrWhiteSpace(rawIrf) || string.IsNullOrWhiteSpace(rawEmail))
            {
                error = "Query parameters 'irfIntId' and 'candidateEmail' are required.";
                return false;
            }

            if (!int.TryParse(rawIrf, out irfIntId) || irfIntId <= 0)
            {
                error = "Query parameter 'irfIntId' must be a positive integer.";
                return false;
            }

            candidateEmail = rawEmail.Trim();
            return true;
        }

        private static bool TryParseInterviewerUpsert(
            HttpRequestData req,
            out string interviewerEmail,
            out string error)
        {
            interviewerEmail = null;
            error = null;

            var query = req.Url.Query?.TrimStart('?') ?? string.Empty;
            string rawEmail = null;

            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2
                    && (string.Equals(parts[0], "interviewerEmail", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(parts[0], "email", StringComparison.OrdinalIgnoreCase)))
                {
                    rawEmail = Uri.UnescapeDataString(parts[1]);
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(rawEmail))
            {
                error = "Query parameter 'interviewerEmail' is required.";
                return false;
            }

            interviewerEmail = rawEmail.Trim();
            return true;
        }
    }
}
