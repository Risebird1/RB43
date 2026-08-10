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
    /// Indexes interviewer roster documents into Azure AI Search.
    ///
    /// Full crawl (all non-deleted interviewers):
    ///   POST/GET /api/InterviewerIndexSearch/FullCrawl?code=&lt;function-key&gt;
    ///
    /// Single document upsert (create/edit/delete sync):
    ///   POST/GET /api/InterviewerIndexSearch/Upsert?interviewerProfileId=123&amp;code=&lt;function-key&gt;
    ///   POST/GET /api/InterviewerIndexSearch/Upsert?userId=456&amp;code=&lt;function-key&gt;
    /// </summary>
    public class InterviewerIndexSearchFullCrawlFunction
    {
        private readonly IInterviewerListingSearchRepository _interviewerListingSearchRepository;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ILogger _log;

        public InterviewerIndexSearchFullCrawlFunction(
            IInterviewerListingSearchRepository interviewerListingSearchRepository,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            ILoggerFactory loggerFactory)
        {
            _interviewerListingSearchRepository = interviewerListingSearchRepository;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _log = loggerFactory.CreateLogger<InterviewerIndexSearchFullCrawlFunction>();
        }

        [Function("InterviewerIndexSearchFullCrawl")]
        public async Task<HttpResponseData> FullCrawl(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "InterviewerIndexSearch/FullCrawl")] HttpRequestData req)
        {
            return await RunIndexingAsync(req, fullCrawl: true, interviewerProfileId: null, userId: null);
        }

        [Function("InterviewerIndexSearchUpsert")]
        public async Task<HttpResponseData> Upsert(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "InterviewerIndexSearch/Upsert")] HttpRequestData req)
        {
            if (!TryParseIds(req, out int? interviewerProfileId, out int? userId, out string parseError))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync(parseError);
                return badRequest;
            }

            return await RunIndexingAsync(req, fullCrawl: false, interviewerProfileId: interviewerProfileId, userId: userId);
        }

        private async Task<HttpResponseData> RunIndexingAsync(
            HttpRequestData req,
            bool fullCrawl,
            int? interviewerProfileId,
            int? userId)
        {
            var response = req.CreateResponse();

            if (!_configuration.GetValue<bool>("AzureSearch:InterviewerListing:Enabled"))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("AzureSearch:InterviewerListing:Enabled is false.");
                return response;
            }

            if (!_interviewerListingSearchRepository.IsEnabled)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("Azure Search endpoint or API key is not configured.");
                return response;
            }

            try
            {
                if (fullCrawl)
                {
                    _log.LogInformation("[InterviewerIndexSearch] Full crawl started at {UtcNow:u}", DateTime.UtcNow);
                    var indexed = await _interviewerListingSearchRepository.FullCrawlAsync();
                    _log.LogInformation("[InterviewerIndexSearch] Full crawl indexed {Count} documents.", indexed);

                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync($"Full crawl complete. Indexed {indexed} interviewer documents.");
                    return response;
                }

                bool success;
                string label;
                if (interviewerProfileId.HasValue)
                {
                    _log.LogInformation(
                        "[InterviewerIndexSearch] Upsert started for interviewerProfileId={ProfileId} at {UtcNow:u}",
                        interviewerProfileId, DateTime.UtcNow);
                    success = await _interviewerListingSearchRepository.UpsertInterviewerAsync(interviewerProfileId.Value);
                    label = $"interviewerProfileId {interviewerProfileId.Value}";
                }
                else
                {
                    _log.LogInformation(
                        "[InterviewerIndexSearch] Upsert started for userId={UserId} at {UtcNow:u}",
                        userId, DateTime.UtcNow);
                    success = await _interviewerListingSearchRepository.UpsertInterviewerByUserIdAsync(userId.Value);
                    label = $"userId {userId.Value}";
                }

                response.StatusCode = success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(success
                    ? $"Upsert complete for {label}."
                    : $"Upsert failed for {label}. See application logs.");
                return response;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[InterviewerIndexSearch] Indexing failed.");
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Indexing failed. See application logs for details.");
                return response;
            }
        }

        private static bool TryParseIds(
            HttpRequestData req,
            out int? interviewerProfileId,
            out int? userId,
            out string error)
        {
            interviewerProfileId = null;
            userId = null;
            error = null;

            var query = req.Url.Query;
            if (string.IsNullOrWhiteSpace(query))
            {
                error = "Provide 'interviewerProfileId' or 'userId'. Example: /api/InterviewerIndexSearch/Upsert?interviewerProfileId=123";
                return false;
            }

            var pairs = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            string profileRaw = null;
            string userRaw = null;
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length != 2)
                    continue;

                if (string.Equals(parts[0], "interviewerProfileId", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(parts[0], "documentId", StringComparison.OrdinalIgnoreCase))
                {
                    profileRaw = Uri.UnescapeDataString(parts[1]);
                }
                else if (string.Equals(parts[0], "userId", StringComparison.OrdinalIgnoreCase))
                {
                    userRaw = Uri.UnescapeDataString(parts[1]);
                }
            }

            if (!string.IsNullOrWhiteSpace(profileRaw))
            {
                if (!int.TryParse(profileRaw, out int profileId) || profileId <= 0)
                {
                    error = "Query parameter 'interviewerProfileId' must be a positive integer.";
                    return false;
                }

                interviewerProfileId = profileId;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(userRaw))
            {
                if (!int.TryParse(userRaw, out int parsedUserId) || parsedUserId <= 0)
                {
                    error = "Query parameter 'userId' must be a positive integer.";
                    return false;
                }

                userId = parsedUserId;
                return true;
            }

            error = "Provide 'interviewerProfileId' or 'userId'. Example: /api/InterviewerIndexSearch/Upsert?interviewerProfileId=123";
            return false;
        }
    }
}
