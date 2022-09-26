using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.MicroService
{
    public class ClientCompleteInterviewNotification
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IClientService _clientService;
        private readonly IIRFService _irfService;

        public ClientCompleteInterviewNotification(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IClientService clientService, IIRFService irfService)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _clientService = clientService;
            _irfService = irfService;
        }
        [FunctionName("ClientCompleteInterviewNotification")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, ILogger log)//Every day
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var _interviewsdetails = (await _irfService.GetCompletedInterviewCountMappedClient(DateTime.Now.AddDays(-1), DateTime.Now))?.ToList();
            foreach (var item in _interviewsdetails)
            {
                try
                {
                    if (item.TotalCompletedInterview > 0)
                    {
                        Guid uniqueid = Guid.NewGuid();
                        var _uniqueid = await _clientService.PublicClientUrlSave(item.EmailId, uniqueid.ToString());
                        string sendreporturl = "https://portal.risebird.io/Candidate/PublicAllMeetings?id=" + uniqueid;
                        if (!String.IsNullOrEmpty(_uniqueid))
                        {
                            sendreporturl = "https://portal.risebird.io/Candidate/PublicAllMeetings?id=" + _uniqueid;
                        }
                        Mail _mail = new Mail
                        {
                            TO = item.EmailId,
                            CC = "talent@risebird.io",
                            Subject = "Public URL for completed interview details"
                        };
                        var emailsend = _notificationHelper.SendClientInterviewPublicURL(_mail, item.ClientName, sendreporturl);
                    }
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }
        }
    }
}
