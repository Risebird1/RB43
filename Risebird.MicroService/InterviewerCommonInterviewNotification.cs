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
    public class InterviewerCommonInterviewNotification
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IInterviewerService _interviewerService;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private IClientService _clientService;

        public InterviewerCommonInterviewNotification(IConfiguration configuration, IInterviewerService interviewerService, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IWhatsAppHelper whatsAppHelper, IClientService clientService)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _interviewerService = interviewerService;
            _whatsAppHelper = whatsAppHelper;
            _clientService = clientService;
        }
        [FunctionName("InterviewerCommonInterviewNotification")]
        //public async void Run([TimerTrigger("0 0 */6 * * *")] TimerInfo myTimer, ILogger log)
        public async void Run([TimerTrigger("0 0 8,16,21 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var _AllInterviewerInterview = await _interviewerService.FetchAllInterviewerallUpcomingInterview();
                foreach (var item in _AllInterviewerInterview)
                {
                    if (!String.IsNullOrEmpty(item.EmailId))
                    {
                        string sendreporturl = String.Empty;
                        if (String.IsNullOrEmpty(item.uniqueid))
                        {
                            Guid uniqueid = Guid.NewGuid();
                            var _uniqueid = await _clientService.PublicInterviewerUrlSave(item.EmailId, uniqueid.ToString());
                            sendreporturl = "https://portal.risebird.io/Interview/PublicOpenInterviewInvite/" + uniqueid;
                            if (!String.IsNullOrEmpty(_uniqueid))
                            {
                                sendreporturl = "https://portal.risebird.io/Interview/PublicOpenInterviewInvite/" + _uniqueid;
                            }
                        }
                        try
                        {
                            //Email Message Send
                            Mail _mail1 = new Mail
                            {
                                TO = item.EmailId,
                                CC = "talent@risebird.io",
                                Subject = "Open interviews are available for you, Please assign"
                            };
                            var emailsend1 = _notificationHelper.SendInterviewerCommonInterviewNotification(_mail1, sendreporturl, item.Total);
                            //What's App Message Send
                            WhatsAppVM _whatsAppVM = new WhatsAppVM();
                            _whatsAppVM.Emailid = item.EmailId;
                            _whatsAppVM.MessageValue = item.Total.ToString();
                            _whatsAppVM.buttonlink = !String.IsNullOrEmpty(sendreporturl) ? sendreporturl.Replace("https://portal.risebird.io/", "") : "";
                            _whatsAppVM.Event = "Interviewer Common Interview";
                            _whatsAppHelper.WhatsAppSendInterviewerCommonInterviewDetails(_whatsAppVM);
                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }
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
