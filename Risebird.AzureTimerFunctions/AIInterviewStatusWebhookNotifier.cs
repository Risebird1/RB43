using System.Net.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System.Text;

namespace Risebird.AzureTimerFunctions
{
    /// <summary>
    /// Timer function that drains the AIInterviewStatusWebHookData queue and pushes each AI interview
    /// status change (scheduled / completed / noshow / dropped / cancelled) to the MCQ AI Interview webhook.
    /// The receiving webhook fetches the report / video / transcript URLs on its side for completed rows.
    /// Mirrors the delivery pattern used by <see cref="CandidateStatusUpdateWebhookNotifier"/>.
    /// </summary>
    public class AIInterviewStatusWebhookNotifier
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ICandidateRepository _candidateRepository;
        private readonly ILogger log;

        private readonly IHttpClientFactory _httpClientFactory;

        public AIInterviewStatusWebhookNotifier(ILoggerFactory loggerFactory, IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            ICandidateRepository candidateRepository, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _candidateRepository = candidateRepository;
            log = loggerFactory.CreateLogger<AIInterviewStatusWebhookNotifier>();
        }

        [Function("AIInterviewStatusWebhookNotifier")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
        {
            log.LogInformation($"AIInterviewStatusWebhookNotifier started executing at: {DateTime.Now}");

            try
            {
                var webhookUrl = _configuration["AIInterviewWebhook:Url"];
                var serviceAccountToken = _configuration["AIInterviewWebhook:ServiceAccountToken"];

                if (string.IsNullOrWhiteSpace(webhookUrl) || webhookUrl.Contains("{your-mcq-host}"))
                {
                    log.LogWarning("AIInterviewStatusWebhookNotifier skipped: AIInterviewWebhook:Url is not configured.");
                    return;
                }

                var webHookData = await _candidateRepository.GetAIInterviewStatusForWebHook();

                if (webHookData != null && webHookData.Count() > 0)
                {
                    var successfullyDeliveredIds = new List<int>();

                    foreach (var data in webHookData)
                    {
                        try
                        {
                            using (HttpClient client = _httpClientFactory.CreateClient())
                            {
                                var status = data.Status?.ToLower();

                                // Only cancellations carry a reason; fall back to the cancellation source when the reason is blank.
                                string reason = string.Empty;
                                if (status == "cancelled")
                                {
                                    reason = string.IsNullOrWhiteSpace(data.Cancellationreason)
                                        ? data.Cancellationsource
                                        : data.Cancellationreason;
                                }

                                var requestBody = new
                                {
                                    skillChannelId = data.IRFId,
                                    candidateEmailId = data.CandidateEmailId,
                                    currentStatus = data.Status,
                                    jobId = data.jobId,
                                    reason = reason
                                };

                                var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl);
                                if (!string.IsNullOrWhiteSpace(serviceAccountToken))
                                {
                                    request.Headers.Add("service-account-token", serviceAccountToken);
                                }

                                var jsonContent = JsonConvert.SerializeObject(requestBody);
                                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                                var response = await client.SendAsync(request);

                                response.EnsureSuccessStatusCode();
                                successfullyDeliveredIds.Add(data.id);

                                var responseContent = await response.Content.ReadAsStringAsync();
                                log.LogInformation($"AI interview webhook response received for IRF {data.IRFId} / {data.CandidateEmailId} ({data.Status}): {responseContent}");
                            }
                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }
                    }

                    if (successfullyDeliveredIds != null && successfullyDeliveredIds.Count() > 0)
                    {
                        await _candidateRepository.DeleteDeliveredAIInterviewStatusForWebHook(successfullyDeliveredIds);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            log.LogInformation($"AIInterviewStatusWebhookNotifier executed at: {DateTime.Now}");
        }
    }
}
