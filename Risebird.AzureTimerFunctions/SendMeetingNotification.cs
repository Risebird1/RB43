using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.AzureTimerFunctions
{
    public class SendMeetingNotification
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IDashboardService _dashboardService;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly ICalendlyHelper _calendlyHelper;
        private readonly IInterviewSchedulerRepository _interviewSchedulerRepository;
        private readonly ILogger log;

        public SendMeetingNotification(ILoggerFactory loggerFactory, IConfiguration configuration, IDashboardService dashboardService, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IWhatsAppHelper whatsAppHelper, ICalendlyHelper calendlyHelper, IInterviewSchedulerRepository interviewSchedulerRepository)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _dashboardService = dashboardService;
            _whatsAppHelper = whatsAppHelper;
            _calendlyHelper = calendlyHelper;
            _interviewSchedulerRepository = interviewSchedulerRepository;
            log = loggerFactory.CreateLogger<SendMeetingNotification>();
        }
        [Function("SendMeetingNotification")]
        public async Task Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer)
        {


            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var Ribbonmodel = await _dashboardService.SendNotoificationForParticipantWaiting();
                foreach (var item in Ribbonmodel)
                {
                    string CCEmails = string.Empty;
                    //if (item.HRCoordinatorEmailId == item.risebirdHR)
                    //{
                    CCEmails = item.risebirdHR;
                    //}
                    //else
                    //{
                    //    if (!string.IsNullOrEmpty(item.HRCoordinatorEmailId))
                    //    {
                    //        CCEmails = item.risebirdHR + ";" + item.HRCoordinatorEmailId;
                    //    }
                    //    else
                    //    {
                    //        CCEmails = item.risebirdHR;
                    //    }
                    //}
                    if (item.EmailType == "WaitingFinal")
                    {
                        // Second waiting alert, 5 minutes after start: WhatsApp only, same event as the first.
                        await SendWaitingWhatsApp(item);
                    }
                    else if (item.EmailType == "reaminder")
                    {
                        if (item.isInterviewerWaiting)
                        {
                            // WhatsApp goes to the person who must act only; staff stay on the email CC.
                            await SendWaitingWhatsApp(item);
                            if (!string.IsNullOrEmpty(item.ClientHrContact))
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

                            string dashboardLink = "https://portal.risebird.io/Candidate/InterviewDetails?id=" + item.candidateUniqueId + "&IRFId=" + item.irfId;

                            //var resultset = await _notificationHelper.InterviewerWaitingNotification(_mail, item.zoomlink, "Interviewer", item.candidateName, item.interviewDate.ToString(), item.interviewMeetingId);
                            var resultset = await _notificationHelper.InterviewerWaitingNotification(_mail, dashboardLink, "Interviewer", item.candidateName, item.interviewDate.ToString(), item.interviewMeetingId, item.uniqueid);

                        }
                        else if (!string.IsNullOrEmpty(item.interviewer))
                        {
                            Mail _mail = new Mail
                            {
                                TO = item.interviewer,
                                CC = CCEmails,
                                Subject = "Risebird Interview: Are you late? Candidate (" + item.candidateName + " ) has joined the meeting and is waiting. Please join immediately."
                            };
                            string dashboardLink = "https://portal.risebird.io/Interview/InterviewDetails?id=" + item.irfId + "&interviewId=" + item.interviewMeetingId;
                            //var resultset = await _notificationHelper.InterviewerWaitingNotification(_mail, item.zoomlink, "Candidate", item.candidateName, "", item.interviewMeetingId);
                            var resultset = await _notificationHelper.InterviewerWaitingNotification(_mail, dashboardLink, "Candidate", item.candidateName, "", item.interviewMeetingId, item.uniqueid);
                            await SendWaitingWhatsApp(item);
                        }
                    }
                    else if (item.EmailType == "EI")
                    {
                        //reassign Interviewer link- openinterviewer
                        string linkurl = "https://portal.risebird.io/Interview/ReassignMeetingByEI?id=" + item.Meetingid;
                        Mail _mail = new Mail
                        {
                            TO = item.emergencyinterviewer,
                            CC = CCEmails,
                            Subject = "Risebird Urgent Interview-Candidate(" + item.candidateName + ") is waiting for the interviewer."
                        };
                        var resultset = await _notificationHelper.CandidateWaitingEINotification(_mail, linkurl, item.zoomlink, item.interviewMeetingId, item.JDLink, item.uniqueid);
                        WhatsAppVM _whatsAppVM = new WhatsAppVM();
                        _whatsAppVM.Emailid = item.emergencyinterviewer;
                        _whatsAppVM.buttonlink = !string.IsNullOrEmpty(linkurl) ? linkurl.Replace("https://portal.risebird.io/", "") : "";
                        _whatsAppVM.UserName = item.candidateName;
                        _whatsAppVM.CandidateName = item.candidateName;
                        _whatsAppVM.PrimarySkill = item.PrimarySkill;
                        _whatsAppVM.Event = "Emergency Notification";
                        _whatsAppVM.InterviewId = item.interviewMeetingId;
                        _whatsAppVM.irfId = item.irfId;
                        _ = await _whatsAppHelper.WhatsAppSendEInterviewer(_whatsAppVM);

                        Mail _mail1 = new Mail
                        {
                            TO = item.candidateEmail,
                            CC = CCEmails,
                            Subject = $"Risebird: We apologise for the delay in the interview"
                        };
                        var resultset1 = await _notificationHelper.AppologyNotificationToCandidate(_mail1, item.candidateName, item.interviewMeetingId);

                        WhatsAppVM _whatsAppVMC = new WhatsAppVM();
                        _whatsAppVMC.Emailid = item.candidateEmail;
                        _whatsAppVMC.UserName = item.candidateName;
                        _whatsAppVMC.Event = "Apology Notification";
                        _whatsAppVMC.InterviewId = item.interviewMeetingId;
                        _whatsAppVMC.irfId = item.irfId;
                        _ = await _whatsAppHelper.WhatsAppSendApology(_whatsAppVMC);
                    }
                    else if (item.EmailType == "CApology")
                    {
                        Mail _mail1 = new Mail
                        {
                            TO = item.candidateEmail,
                            CC = CCEmails,
                            Subject = $"Risebird: We apologise for the delay - Please wait for 5 more mins"
                        };
                        var resultset1 = await _notificationHelper.AppologyNotificationToCandidateSecond(_mail1, item.candidateName, item.interviewMeetingId);
                    }
                    else if (item.EmailType == "Reschedule")
                    {
                        string oneTimeSchedulingLink = string.Empty;
                        string stepfirst = item.CalendlyLink.Split("?").LastOrDefault();
                        var olduniqueid = stepfirst.Split("=").LastOrDefault();
                        oneTimeSchedulingLink = _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid, item.candidateEmail).Result;
                        if (string.IsNullOrEmpty(oneTimeSchedulingLink))
                        {
                            oneTimeSchedulingLink = _interviewSchedulerRepository.GenerateOneTimeEvent(olduniqueid, item.candidateEmail).Result;
                        }
                        //oneTimeSchedulingLink = item.CalendlyLink.Split("?").FirstOrDefault() + "?id=" + oneTimeSchedulingLink;
                        oneTimeSchedulingLink = "https://portal.risebird.io/InterviewScheduler/CandidateScheduleEvent?id=" + oneTimeSchedulingLink + "&CandidateEmail=" + item.candidateEmail;

                        Mail _mail = new Mail
                        {
                            TO = item.candidateEmail,
                            CC = CCEmails,
                            Subject = $"Risebird: Due to unforeseen circumstances, If the interviewer was unable to join the interview. We apologize for the inconvenience. Kindly reschedule."
                        };
                        var resultset = await _notificationHelper.ReScheduleNotificationToCandidate(_mail, oneTimeSchedulingLink, item.candidateName, item.LogoLink, item.interviewMeetingId);
                        //WhatsAppVM _whatsapp = new WhatsAppVM();
                        //_whatsapp.Emailid = item.candidateEmail;
                        ////if (item.CalendlyLink.Contains("InterviewScheduler/ScheduleEvent"))
                        ////{
                        //_whatsapp.Event = "Interview Reschedule Risebird";
                        ////}
                        ////else
                        ////{
                        ////    _whatsapp.Event = "Interview Reschedule";
                        ////}
                        //oneTimeSchedulingLink = oneTimeSchedulingLink.Replace("https://portal.risebird.io/", "").ToString();
                        //_whatsapp.buttonlink = oneTimeSchedulingLink;
                        //_whatsapp.InterviewId = item.interviewMeetingId;
                        ////_whatsapp.buttonlink = oneTimeSchedulingLink.Split("=").LastOrDefault().ToString();
                        //_whatsapp.irfId = item.irfId;
                        //_ = await _whatsAppHelper.WhatsAppSendReScheduling(_whatsapp);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }

        // C4 Interviewer Waiting (to the candidate, with the meeting link) or I4 Candidate Waiting (to the interviewer).
        private async Task SendWaitingWhatsApp(MeetingDetailsForParticipant item)
        {
            WhatsAppVM _whatsAppVM = new WhatsAppVM();
            _whatsAppVM.InterviewDate = item.interviewDate.ToString();
            _whatsAppVM.CandidateName = item.candidateName;
            _whatsAppVM.InterviewId = item.interviewMeetingId;
            _whatsAppVM.irfId = item.irfId;
            if (item.isInterviewerWaiting)
            {
                _whatsAppVM.Emailid = item.candidateEmail;
                _whatsAppVM.Event = "Interviewer Waiting";
                _whatsAppVM.MeetingLink = item.zoomlink;
                _whatsAppVM.buttonlink = "Candidate/InterviewDetails?id=" + item.candidateUniqueId + "&IRFId=" + item.irfId;
            }
            else
            {
                if (string.IsNullOrEmpty(item.interviewer))
                    return;
                _whatsAppVM.Emailid = item.interviewer;
                _whatsAppVM.Event = "Candidate Waiting";
                _whatsAppVM.buttonlink = "Interview/InterviewDetails?id=" + item.irfId + "&interviewId=" + item.interviewMeetingId;
            }
            _ = await _whatsAppHelper.WhatsAppSendInterviewWaiting(_whatsAppVM);
        }
    }
}
