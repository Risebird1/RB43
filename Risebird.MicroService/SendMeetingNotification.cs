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
    public class SendMeetingNotification
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IDashboardService _dashboardService;
        public SendMeetingNotification(IConfiguration configuration, IDashboardService dashboardService, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _dashboardService = dashboardService;
        }
        [FunctionName("SendMeetingNotification")]
        public async void Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer, ILogger log)
        {

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var Ribbonmodel = await _dashboardService.SendNotoificationForParticipantWaiting();
                foreach (var item in Ribbonmodel)
                {
                    string CCEmails = string.Empty;
                    if (item.HRCoordinatorEmailId == item.risebirdHR)
                    {
                        CCEmails = item.risebirdHR;
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(item.HRCoordinatorEmailId))
                        {
                            CCEmails = item.risebirdHR + ";" + item.HRCoordinatorEmailId;
                        }
                        else
                        {
                            CCEmails = item.risebirdHR;
                        }
                    }
                    if (item.EmailType == "reaminder")
                    {
                        if (item.isInterviewerWaiting)
                        {
                            Mail _mail = new Mail
                            {
                                TO = item.candidateEmail,
                                CC = CCEmails,
                                Subject = $"Interviewer Waiting for you to Start the interview"
                            };
                            var resultset = _notificationHelper.InterviewerWaitingNotification(_mail, item.zoomlink, "Interviewer", item.candidateName);
                        }
                        else
                        {
                            Mail _mail = new Mail
                            {
                                TO = item.interviewer,
                                CC = CCEmails,
                                Subject = $"Candidate join the meeting and waiting for interviewer"
                            };
                            var resultset = _notificationHelper.InterviewerWaitingNotification(_mail, item.zoomlink, "Candidate", item.candidateName);
                        }
                    }
                    else if (item.EmailType == "EI")
                    {
                        //reassign Interviewer link- openinterviewer
                        string linkurl = "https://portal.risebird.io/Interview/ReassignMeetingByEI/" + item.Meetingid;
                        Mail _mail = new Mail
                        {
                            TO = item.emergencyinterviewer,
                            CC = CCEmails,
                            Subject = $"Candidate joined the meeting and waiting for the interviewer"
                        };
                        var resultset = _notificationHelper.CandidateWaitingEINotification(_mail, linkurl);
                        Mail _mail1 = new Mail
                        {
                            TO = item.candidateEmail,
                            CC = CCEmails,
                            Subject = $"Risebird: Apology for the delay in the interview"
                        };
                        var resultset1 = _notificationHelper.AppologyNotificationToCandidate(_mail1, item.candidateName);
                    }
                    else if (item.EmailType == "CApology")
                    {
                        Mail _mail1 = new Mail
                        {
                            TO = item.candidateEmail,
                            CC = CCEmails,
                            Subject = $"Risebird: Apology for the delay in the interview"
                        };
                        var resultset1 = _notificationHelper.AppologyNotificationToCandidateSecond(_mail1, item.candidateName);
                    }
                    else if (item.EmailType == "Reschedule")
                    {
                        Mail _mail = new Mail
                        {
                            TO = item.candidateEmail,
                            CC = CCEmails,
                            Subject = $"Apology for inconvience please reschedule the interview"
                        };
                        var resultset = _notificationHelper.ReScheduleNotificationToCandidate(_mail, item.CalendlyLink, item.candidateName);
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
