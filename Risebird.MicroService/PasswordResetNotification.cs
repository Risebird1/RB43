using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;

namespace Risebird.MicroService
{
    public class PasswordResetNotification
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly string PasswordTimePeriod;
        private readonly IInterviewerService _interviewerService;

        public PasswordResetNotification(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IWhatsAppHelper whatsAppHelper, IInterviewerService interviewerService)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _whatsAppHelper = whatsAppHelper;
            PasswordTimePeriod = _configuration["PasswordAging:TimePeriod"];
            _interviewerService = interviewerService;
        }
        [FunctionName("PasswordResetNotification")]
        public async void Run([TimerTrigger("0 0 8 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger(PasswordResetNotification) function executed at: {DateTime.Now}");
            try
            {
                //Fetch All 90 days data for expire records and Customize from appsetting
                var _list = _interviewerService.FetchAllPasswordResetDetails(Convert.ToInt32(PasswordTimePeriod));
                if (_list != null)
                {
                    var clientlist = String.Join(";", _list);
                    //Send Email Notification For reset Password
                    Mail _mail1 = new Mail
                    {
                        TO = clientlist,
                        Subject = "Risebird: Password is expired"
                    };
                    var emailsend1 = _notificationHelper.SendPasswordresetNotification(_mail1);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
