using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class UpdateIRFCalendlyResourceUri
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ICalendlyHelper _calendlyHelper;



        public UpdateIRFCalendlyResourceUri(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, ICalendlyHelper calendlyHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _calendlyHelper = calendlyHelper;

        }
        [FunctionName("UpdateIRFCalendlyResourceUri")]
        public async Task Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer, ILogger log)//runs every 2 minutes
        {
            try
            {
                log.LogInformation($"C# Timer trigger function: UpdateIRFCalendlyResourceUri  started executing at: {DateTime.Now}");

                await _calendlyHelper.UpdatePendingCalendlyResourceUriForIRF();

                log.LogInformation($"C# Timer trigger function: UpdateIRFCalendlyResourceUri executed at: {DateTime.Now}");

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
