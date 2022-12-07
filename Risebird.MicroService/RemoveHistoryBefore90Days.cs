using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    class RemoveHistoryBefore90Days
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly string PasswordTimePeriod;
        private readonly IInterviewerService _interviewerService;


        public RemoveHistoryBefore90Days(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IWhatsAppHelper whatsAppHelper, IInterviewerService interviewerService)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _whatsAppHelper = whatsAppHelper;
            PasswordTimePeriod = _configuration["PasswordAging:TimePeriod"];
            _interviewerService = interviewerService;
        }
        [FunctionName("RemoveHistoryBefore90Days")]
        public async Task Run([TimerTrigger("0 0 1 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger(RemoveHistoryBefore90Days) function started executing at: {DateTime.Now}");
            try
            {
                //Remove All 90 days data from History table
              
                var value = await _interviewerService.DeleteAllHistorybefore90days();

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
            log.LogInformation($"C# Timer trigger(RemoveHistoryBefore90Days) function executed at: {DateTime.Now}");
        }
    }
}
