using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task Run([TimerTrigger("0 0 8,16,21 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var _AllInterviewerInterview = await _interviewerService.FetchAllInterviewerallUpcomingInterview();
                foreach (var item in _AllInterviewerInterview)
                {
                    if (!string.IsNullOrEmpty(item.EmailId))
                    {
                        string sendreporturl = string.Empty;
                        if (string.IsNullOrEmpty(item.uniqueid))
                        {
                            Guid uniqueid = Guid.NewGuid();
                            var _uniqueid = await _clientService.PublicInterviewerUrlSave(item.EmailId, uniqueid.ToString());
                            sendreporturl = "https://portal.risebird.io/Interview/PublicOpenInterviewInvite?id=" + uniqueid;
                            if (!string.IsNullOrEmpty(_uniqueid))
                            {
                                sendreporturl = "https://portal.risebird.io/Interview/PublicOpenInterviewInvite?id=" + _uniqueid;
                            }
                        }
                        else
                        {
                            sendreporturl = "https://portal.risebird.io/Interview/PublicOpenInterviewInvite?id=" + item.uniqueid;
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
                            _whatsAppVM.buttonlink = !string.IsNullOrEmpty(sendreporturl) ? sendreporturl.Replace("https://portal.risebird.io/", "") : "";
                            _whatsAppVM.Event = "Interviewer Common Interview";
                            _whatsAppHelper.WhatsAppSendInterviewerCommonInterviewDetails(_whatsAppVM);
                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        }
                    }
                }
                //Start: Send Email to Clients
                log.LogInformation($"Send Emails To Clients");
                SendMailtoClient();
                //End: Send Email to Clients
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
        public async void SendMailtoClient()
        {
            //Start : create time with previous run and current
            DateTime _stdt = new DateTime();
            DateTime _sedt = DateTime.Now;
            if (_sedt.Hour == 8)
            {
                //DIfference b/w 8 to previous run 21
                _stdt = _sedt.AddHours(-11);
            }
            else if (_sedt.Hour == 16)
            {
                //Difference b/w 16 to previous run 8
                _stdt = _sedt.AddHours(-8);
            }
            else if (_sedt.Hour == 21)
            {
                //Difference b/w 21 to previous run 16
                _stdt = _sedt.AddHours(-5);
            }
            //End : create time with previous run and current
            //Start : Get All Clients Details
            var _AllClientAndStaffingAgency = await _interviewerService.FetchAllStaffingAgencyAndClientHRwithTime(_stdt, _sedt);
            //End : Get All Clients Details
            //Start: Filter the Data According to Emailids
            var _clientemailids = _AllClientAndStaffingAgency.Select(x => x.EmailId).Distinct().ToList();
            //End: Filter the Data According to Emailids
            //Start: Send email
            foreach (var item in _clientemailids)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(item) && item.Contains("@"))
                        {
                            var details = _AllClientAndStaffingAgency.Where(x => x.EmailId == item).ToList();
                            DataTable dt = new DataTable();
                            dt.Columns.AddRange(new DataColumn[3] { new DataColumn("IRF Name", typeof(string)),
                            new DataColumn("Primary Skill", typeof(string)),
                            new DataColumn("Interview Count",typeof(int))});
                            foreach (var _interview in details)
                            {
                                dt.Rows.Add(_interview.IRFId, _interview.PrimarySkill, _interview.Total);
                            }
                            Mail _mail1 = new Mail
                            {
                                TO = item,
                                CC = "talent@risebird.io",
                                Subject = "New Interview Scheduled"
                            };
                            var emailsend1 = _notificationHelper.SendClientCommonInterviewNotification(_mail1, dt);
                        }
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    }
                }
            }
            //End: Send email
        }
    }
}
