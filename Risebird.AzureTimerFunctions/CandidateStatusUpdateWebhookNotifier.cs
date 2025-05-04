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
    public class CandidateStatusUpdateWebhookNotifier
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ICandidateRepository _candidateRepository;
        private readonly ILogger log;
        public CandidateStatusUpdateWebhookNotifier(ILoggerFactory loggerFactory, IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger,
            ICandidateRepository candidateRepository)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _candidateRepository = candidateRepository;
            log = loggerFactory.CreateLogger<CandidateStatusUpdateWebhookNotifier>();
        }

        [Function("CandidateStatusUpdateWebhookNotifier")]
        public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)//0 */5 * * * *
        {
            log.LogInformation($"C# Timer trigger function started executing  at: {DateTime.Now}");

            try
            {
                var webHookData = await _candidateRepository.GetCandidateStatusForWebHook();

                if (webHookData != null && webHookData.Count() > 0)
                {
                    var successfullyDeliveredCandidates = new List<int>();

                    foreach (var data in webHookData.Where(x => x.Status.ToLower() == "scheduled"))
                    {

                        try
                        {
                            using (HttpClient client = new HttpClient())
                            {

                                var requestBody = new
                                {
                                    skillChannelId = data.IRFId,
                                    candidateEmailId = data.CandidateEmailId,
                                    currentStatus = data.Status,
                                    jobId = data.jobId
                                };

                                //staging request
                                //var request = new HttpRequestMessage(HttpMethod.Post, $"https://matching-staging.turing.com/api_/service-accounts-actions/webhooks/risebird/interview-scheduled")
                                //{
                                //    Headers = { { "service-account-token", "2|S9XfGcUNPwmgkbdmlrKa2CehENFDy0Ax" } }
                                //};

                                //prod request
                                var request = new HttpRequestMessage(HttpMethod.Post, $"https://matching.turing.com/api_/service-accounts-actions/webhooks/risebird/interview-scheduled")
                                {
                                    Headers = { { "service-account-token", "52|bhH2cNYKI33AgokiZYOKV62djVrchn1E" } }
                                };

                                var jsonContent = JsonConvert.SerializeObject(requestBody);
                                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                                request.Content = content;

                                var response = await client.SendAsync(request);

                                response.EnsureSuccessStatusCode();
                                successfullyDeliveredCandidates.Add(data.id);
                                var responseContent = await response.Content.ReadAsStringAsync();
                                log.LogInformation($"Response recieved : {responseContent}");

                            }
                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }

                    }
                    foreach (var data in webHookData.Where(x => x.Status.ToLower() == "completed" || x.Status.ToLower() == "noshow" || x.Status.ToLower() == "dropped" || x.Status.ToLower() == "cancelled" || x.Status.ToLower() == "coldcandidate"))
                    {

                        try
                        {
                            using (HttpClient client = new HttpClient())
                            {
                                if (data.Status.ToLower() != "cancelled")
                                {
                                    data.Cancellationreason = "";
                                }
                                var requestBody = new
                                {
                                    skillChannelId = data.IRFId,
                                    candidateEmailId = data.CandidateEmailId,
                                    currentStatus = data.Status,
                                    jobId = data.jobId,
                                    Cancellationreason = data.Cancellationreason
                                };
                                //staging request
                                //var request = new HttpRequestMessage(HttpMethod.Post, $"https://matching-staging.turing.com/api_/service-accounts-actions/webhooks/risebird/interview-result")
                                //{
                                //    Headers = {  { "service-account-token", "2|S9XfGcUNPwmgkbdmlrKa2CehENFDy0Ax" } }//{ "Content-Type", "application/json" },
                                //};

                                //prod request
                                var request = new HttpRequestMessage(HttpMethod.Post, $"https://matching.turing.com/api_/service-accounts-actions/webhooks/risebird/interview-result")
                                {
                                    Headers = { { "service-account-token", "52|bhH2cNYKI33AgokiZYOKV62djVrchn1E" } }//{ "Content-Type", "application/json" },
                                };

                                var jsonContent = JsonConvert.SerializeObject(requestBody);
                                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                                request.Content = content;

                                var response = await client.SendAsync(request);

                                response.EnsureSuccessStatusCode();
                                successfullyDeliveredCandidates.Add(data.id);
                                var responseContent = await response.Content.ReadAsStringAsync();

                                log.LogInformation($"Response recieved : {responseContent}");

                            }
                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }

                    }

                    if (successfullyDeliveredCandidates != null && successfullyDeliveredCandidates.Count() > 0)
                    {
                        await _candidateRepository.DeleteDeliveredCandidateStatusForWebHook(successfullyDeliveredCandidates);
                    }
                }


            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        }
    }
}
