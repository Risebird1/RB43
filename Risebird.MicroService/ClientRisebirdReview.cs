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
    public class ClientRisebirdReview
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IClientService _clientService;
        private readonly IIRFService _irfService;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly IAadhaarService _aadhaarService;
        private readonly IUserService _userService;

        public ClientRisebirdReview(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IClientService clientService, IIRFService irfService, IWhatsAppHelper whatsAppHelper, IAadhaarService aadhaarService, IUserService userService)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _clientService = clientService;
            _irfService = irfService;
            _whatsAppHelper = whatsAppHelper;
            _aadhaarService = aadhaarService;
            _userService = userService;
        }
        [FunctionName("ClientRisebirdReview")]
        public async Task Run([TimerTrigger("0 0 10 * * 2")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var _interviewsdetails = await _userService.GetClientUsersForReviewNotification();
            foreach (var item in _interviewsdetails)
            {
                try
                {
                    Mail _mail = new Mail
                    {
                        TO = item.EmailId,
                        Subject = "Feedback Risebird Services"
                    };
                    var ReviewURL = "https://portal.risebird.io/Survey/InterviewsReview?uniqueid=" + item.uniqueid;
                    var emailsend = await _notificationHelper.SendClientRisebirdReview(_mail, item.ClientName, ReviewURL, item.logolink);
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }
        }
    }
}
