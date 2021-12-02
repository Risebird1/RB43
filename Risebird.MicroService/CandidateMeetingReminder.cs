using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;

namespace Risebird.MicroService
{
    public class CandidateMeetingReminder
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly ICandidateService _candidateService;
        private readonly IMeetingService _meetingService;

        public CandidateMeetingReminder(IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, ICandidateService candidateService, IMeetingService meetingService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _candidateService = candidateService;
            _meetingService = meetingService;
        }
        [FunctionName("CandidateMeetingReminder")]
        public async void Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            //Fetch Interview 24 hours,6 hours and 2 hours before
            var candidatemeetinglist = await _meetingService.FetchCandidateMeetingsForReminder();
            foreach (var item in candidatemeetinglist)
            {
                try
                {
                    if (item.Typefunction == 2)
                    {
                        //Send Notifications
                        //Send Call, WhatsApp and mail
                        //With Yes/No Button
                    }
                    else if (item.Typefunction == 6)
                    {
                        //Send Notifications
                        //Send  WhatsApp and mail
                        //With Yes/No Button
                    }
                    else if (item.Typefunction == 24)
                    {
                        //Send Notifications
                        //Send Call, WhatsApp and mail
                        //With Yes/No Button
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
