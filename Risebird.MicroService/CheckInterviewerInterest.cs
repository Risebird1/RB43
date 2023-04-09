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
    public class CheckInterviewerInterest
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IUserService _userService;

        public CheckInterviewerInterest(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IUserService userService)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _userService = userService;
        }
        [FunctionName("CheckInterviewerInterest")]
        public async Task Run([TimerTrigger("0 0 9 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var _interviewsdetails = await _userService.GetInterviewerInterestNotification();
            foreach (var item in _interviewsdetails)
            {
                try
                {
                    Mail _mail = new Mail
                    {
                        TO = item.EmailId,
                        Subject = "Risebird Interviewer Interest Request Details"
                    };
                    var ReviewURL = "https://portal.risebird.io/Interviewer/ShowInterest?uniqueid=" + item.uniqueid;
                    var emailsend = await _notificationHelper.SendInterviewerIntrest(_mail, item.FirstName, ReviewURL);
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }
        }
    }
}
