using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace Risebird.MicroService
{
    public class AIQualityCheckBatchRun
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IDashboarRepository _dashboardRepository;

        public AIQualityCheckBatchRun(IApplicationErrorLogger applicationErrorLogger,
            IDashboarRepository dashboardRepository)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _dashboardRepository = dashboardRepository;
        }

        [FunctionName("AIQualityCheckBatchRun")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)//0 */5 * * * *
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
