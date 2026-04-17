using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.AzureTimerFunctions
{
    public class AIQualityCheckBatchRun
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IDashboarRepository _dashboardRepository;
        private readonly ILogger log;

        public AIQualityCheckBatchRun(ILoggerFactory loggerFactory, 
            IApplicationErrorLogger applicationErrorLogger,
            IDashboarRepository dashboardRepository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _dashboardRepository = dashboardRepository;
            log = loggerFactory.CreateLogger<AIQualityCheckBatchRun>();
        }

        [Function("AIQualityCheckBatchRun")]
        public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer) // Every 10 minutes (was: 5 min - reduced OpenAI costs)
        {
            log.LogInformation($"AIQualityCheckBatchRun : C# Timer trigger function started executing  at: {DateTime.Now}");

            try
            {
                var qcPendingReports = await _dashboardRepository.GetQCPendingReportsForAIQC();
                if (qcPendingReports != null && qcPendingReports.Count() > 0)
                {
                    foreach (var item in qcPendingReports)
                    {
                        try
                        {
                            using (HttpClient client = new HttpClient())
                            {
                                client.Timeout = TimeSpan.FromMinutes(3);
                                var request = new HttpRequestMessage(HttpMethod.Get, $"https://portal.risebird.io/Interview/AIClientReport?interviewId={item.id}&irfId={item.irfIntId}&isOpenAIQC={true}&isBackgroudJobRequest={true}");
                                //var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:44329//Interview/AIClientReport?interviewId={item.id}&irfId={item.irfIntId}&isOpenAIQC={true}&isBackgroudJobRequest={true}");


                                var response = await client.SendAsync(request);

                                response.EnsureSuccessStatusCode();
                                //var responseContent = await response.Content.ReadAsStringAsync();

                            }
                        }
                        catch (Exception ex)
                        {
                            log.LogInformation($"AIQualityCheckBatchRun : C# Timer trigger {ex.Message}");
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }
                        try
                        {
                            using (HttpClient client = new HttpClient())
                            {
                                client.Timeout = TimeSpan.FromMinutes(3);
                                var request = new HttpRequestMessage(HttpMethod.Get, $"https://portal.risebird.io/Interview/AIClientReport?interviewId={item.id}&irfId={item.irfIntId}&IsContradictionAI={true}");
                                var response = await client.SendAsync(request);
                                response.EnsureSuccessStatusCode();
                            }
                        }
                        catch (Exception ex)
                        {
                            log.LogInformation($"AIQualityCheckBatchRun : API Call For transcriptSummaryCleanedJson {ex.Message}");
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }

                    }


                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }

            log.LogInformation($"AIQualityCheckBatchRun : C# Timer trigger function executed at: {DateTime.Now}");

        }
    }
}
