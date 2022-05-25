using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;

namespace Risebird.MicroService
{
    public class UnassignedInterviewsNotification
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IIRFService _irfService;
        private readonly ICandidateService _candidateService;
        private readonly IIRFRepository _irfRepository;
        private readonly IMeetingService _meetingService;

        public UnassignedInterviewsNotification(IIRFService irfService, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, ICandidateService candidateService, IIRFRepository irfRepository, IMeetingService meetingService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _irfService = irfService;
            _candidateService = candidateService;
            _irfRepository = irfRepository;
            _meetingService = meetingService;
        }
        [FunctionName("UnassignedInterviewsNotification")]
        public async void Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var _unassignedmeetinglist = await _meetingService.FetchAllUnassignedInterviewesToShare();
                foreach (var item in _unassignedmeetinglist)
                {
                    try
                    {
                        var transInterviewer = _irfRepository.GetTransInterviewer(item.Irfid);
                        if (transInterviewer != null)
                        {
                            var interviewers = transInterviewer.Select(x => x.EmailId).ToList();
                            var emailresult = _notificationHelper.SendUnassignedInterviewsNotifications(interviewers, item.interviewDate.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
