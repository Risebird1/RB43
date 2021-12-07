using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.MicroService
{
    public class CandidateMeetingReminder
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly ICandidateService _candidateService;
        private readonly IMeetingService _meetingService;
        private readonly IIRFService _irfService;
        private readonly IIRFRepository _irfRepository;
        private readonly IWhatsAppHelper _whatsAppHelper;

        public CandidateMeetingReminder(IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, ICandidateService candidateService, IMeetingService meetingService, IIRFService irfService, IIRFRepository irfRepository, IWhatsAppHelper whatsAppHelper)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _candidateService = candidateService;
            _meetingService = meetingService;
            _irfService = irfService;
            _irfRepository = irfRepository;
            _whatsAppHelper = whatsAppHelper;
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
                    string yesbutton = "https://portal.risebird.io/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=1&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;
                    string nobutton = "https://portal.risebird.io/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=2&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;
                    if (item.Typefunction == 2)
                    {
                        //Send Notifications
                        Mail _mail = new Mail
                        {
                            TO = item.CandidateEmail,
                            CC = item.staffingAgency,
                            Subject = "Risebird: Interview Reminder"
                        };
                        var emailresult = _notificationHelper.SendInterviewRemindersecond(_mail, item.FirstName, item.interviewDate, yesbutton, nobutton);
                        //Send Call, WhatsApp and mail
                        WhatsAppVM _whatsAppVM = new WhatsAppVM();
                        _whatsAppVM.Emailid = item.CandidateEmail;
                        _whatsAppVM.InterviewDate = item.interviewDate.ToString();
                        _whatsAppVM.buttonlink = !String.IsNullOrEmpty(yesbutton) ? yesbutton.Replace("https://portal.risebird.io/Interview/CandidateMeetingsStatus", "") : "";
                        _whatsAppVM.Event = "Interview Reminder Second";
                        _whatsAppHelper.WhatsAppSendInterviewReminder(_whatsAppVM);
                        //With Yes/No Button
                    }
                    else if (item.Typefunction == 6)
                    {
                        //Send Notifications
                        Mail _mail = new Mail
                        {
                            TO = item.CandidateEmail,
                            Subject = "Risebird: Interview Reminder"
                        };
                        var emailresult = _notificationHelper.SendInterviewReminder(_mail, item.FirstName, item.interviewDate, yesbutton, nobutton);
                        //Send  WhatsApp and mail
                        WhatsAppVM _whatsAppVM = new WhatsAppVM();
                        _whatsAppVM.Emailid = item.CandidateEmail;
                        _whatsAppVM.InterviewDate = item.interviewDate.ToString();
                        _whatsAppVM.buttonlink = !String.IsNullOrEmpty(yesbutton) ? yesbutton.Replace("https://portal.risebird.io/Interview/CandidateMeetingsStatus", "") : "";
                        _whatsAppVM.Event = "Interview Reminder";
                        _whatsAppHelper.WhatsAppSendInterviewReminder(_whatsAppVM);
                        //With Yes/No Button
                    }
                    else if (item.Typefunction == 24)
                    {
                        //Send Notifications
                        Mail _mail = new Mail
                        {
                            TO = item.CandidateEmail,
                            Subject = "Risebird: Interview Reminder"
                        };
                        var emailresult = _notificationHelper.SendInterviewReminder(_mail, item.FirstName, item.interviewDate, yesbutton, nobutton);
                        //Send Call, WhatsApp and mail
                        WhatsAppVM _whatsAppVM = new WhatsAppVM();
                        _whatsAppVM.Emailid = item.CandidateEmail;
                        _whatsAppVM.InterviewDate = item.interviewDate.ToString();
                        _whatsAppVM.buttonlink = !String.IsNullOrEmpty(yesbutton) ? yesbutton.Replace("https://portal.risebird.io/Interview/CandidateMeetingsStatus", "") : "";
                        _whatsAppVM.Event = "Interview Reminder";
                        _whatsAppHelper.WhatsAppSendInterviewReminder(_whatsAppVM);
                        //With Yes/No Button
                    }
                    else if (item.Typefunction == 1)
                    {
                        if (_meetingService.PublicCancelInterview(item.Interviewid))
                        {
                            var setvalue = _meetingService.UpdateCancelInterviewDetails(item.Interviewid);
                            var candidateId = await _irfRepository.GetCandidateIdByEmail(item.CandidateEmail);
                            var result1 = await _irfService.AddCandidateStatus("System", candidateId, item.Irfid, "Candidate Cancelled", (int)CandidateStatusOptions.Cancelled);
                            var result = await _candidateService.FetchChannelOwnerandInterviewer(item.Interviewid);
                            var irfOwnersAndRestrictedIrfOwner = await _candidateService.GetCandidateStaffingAgencyAndClientHRByInterviewId(item.Interviewid);
                            List<string> interviewers = new List<string>();
                            if (!String.IsNullOrEmpty(result.interviewer))
                            {
                                interviewers.Add(result.interviewer);
                            }
                            if (!String.IsNullOrEmpty(result.EmailId))
                            {
                                interviewers.Add(result.EmailId);
                            }
                            var emailresult = _notificationHelper.SendInterviewCancelledMail(interviewers, result.interviewDate.ToString(), irfOwnersAndRestrictedIrfOwner?.ToList());
                            var emailresult1 = _notificationHelper.SendInterviewCancelledMailClient(result.interviewDate.ToString(), result.CandidateName, irfOwnersAndRestrictedIrfOwner?.ToList());
                            var emailresult2 = _notificationHelper.SendInterviewCancelledMailCandidateWithSchedulingLink(result.candidateEmail, result.interviewDate.ToString(), result.CandidateName, result.CalendlyLink);
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
}
