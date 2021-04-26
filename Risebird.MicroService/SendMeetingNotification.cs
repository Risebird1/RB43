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
                            if (!String.IsNullOrEmpty(item.ClientHrContact))
                            {
                                if (!CCEmails.Contains(item.ClientHrContact))
                                {
                                    CCEmails = CCEmails + ";" + item.ClientHrContact;
                                }
                            }
                            Mail _mail = new Mail
                            {
                                TO = item.candidateEmail,
                                CC = CCEmails,
                                Subject = item.ClientName + " " + item.PrimarySkill + " Interview-  Interviewer is waiting for you. Please join immediately."
                            };
                            var resultset = _notificationHelper.InterviewerWaitingNotification(_mail, item.zoomlink, "Interviewer", item.candidateName, item.interviewDate.ToString());
                        }
                        else
                        {
                            Mail _mail = new Mail
                            {
                                TO = item.interviewer,
                                CC = CCEmails,
                                Subject = "Risebird Interview: Are you late? Candidate (" + item.candidateName + " ) joined the meeting and waiting. Please join immediately."
                            };
                            var resultset = _notificationHelper.InterviewerWaitingNotification(_mail, item.zoomlink, "Candidate", item.candidateName, "");
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
                            Subject = "Risebird Emergency Interview-Candidate(" + item.candidateName + ") is waiting for the interviewer."
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
                            Subject = $"Risebird: Apology for ongoing delay in the interview- Please wait for 5 more mins"
                        };
                        var resultset1 = _notificationHelper.AppologyNotificationToCandidateSecond(_mail1, item.candidateName);
                    }
                    else if (item.EmailType == "Reschedule")
                    {
                        Mail _mail = new Mail
                        {
                            TO = item.candidateEmail,
                            CC = CCEmails,
                            Subject = $"Risebird: Due to some emergency, the interviewer is unable to join the interview. Apology for inconvenience, please reschedule."
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
