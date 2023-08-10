using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly ITwilioHelper _twilioHelper;
        private readonly IConfiguration _configuration;
        private readonly string candidateDashboard;

        public CandidateMeetingReminder(IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, ICandidateService candidateService, IMeetingService meetingService, IIRFService irfService, IIRFRepository irfRepository, IWhatsAppHelper whatsAppHelper, ITwilioHelper twilioHelper, IConfiguration configuration)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _candidateService = candidateService;
            _meetingService = meetingService;
            _irfService = irfService;
            _irfRepository = irfRepository;
            _whatsAppHelper = whatsAppHelper;
            _twilioHelper = twilioHelper;
            _configuration = configuration;
            candidateDashboard = _configuration["CandidateDashBoard:UseDashboard"];
        }
        [FunctionName("CandidateMeetingReminder")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            string timeZoneValue = "IST";
            //Fetch Interview 24 hours,6 hours and 2 hours before
            var candidatemeetinglist = await _meetingService.FetchCandidateMeetingsForReminder();
            foreach (var item in candidatemeetinglist)
            {
                try
                {

                    log.LogInformation($"Actual candidate data used : {item.CandidateEmail},{item.interviewDate},{item.TimeZonesValue},,{item.TimeZoneoffset}");


                    var primaryskill = item.PrimarySkillInvite;
                    string yesbutton = "https://portal.risebird.io/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=1&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;
                    string nobutton = "https://portal.risebird.io/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=2&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;
                    string rescheulebutton = "https://portal.risebird.io/Interview/CandidateRescheduleMeetingsStatus?meetingid=" + item.Interviewid + "&status=3&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;
                    string needcleritybutton = "https://portal.risebird.io/Interview/CandidateMeetingsStatus?meetingid=" + item.Interviewid + "&status=4&uniqueid=" + item.uniqueid + "&Irfid=" + item.Irfid + "&candidateId=" + item.candidateid;

                    if (!string.IsNullOrWhiteSpace(item.TimeZonesValue)
                        && item.TimeZoneoffset.HasValue
                        && !item.TimeZonesValue.Contains("Chennai"))
                    {
                        timeZoneValue = item.TimeZonesValue.Split(")").ElementAtOrDefault(1);
                        if (item.TimeZoneoffset.Value >= 0)
                        {
                            item.interviewDate = item.interviewDate.AddMinutes(-(330 + item.TimeZoneoffset.Value));
                        }
                        else
                        {
                            item.interviewDate = item.interviewDate.AddMinutes(item.TimeZoneoffset.Value);
                        }
                    }



                    string dashboardLink = "https://portal.risebird.io/Candidate/InterviewDetails/" + item.uniqueid + "?Irfid=" + item.Irfid;
                    if (item.Typefunction == 2)
                    {
                        //Send Notifications
                        Mail _mail = new Mail
                        {
                            TO = item.CandidateEmail,
                            Subject = item.ClientName + ": For Skill :" + primaryskill + ": Interview Reminder Confirm Your Availability"
                        };
                        var emailresult = await _notificationHelper.SendInterviewRemindersecond(_mail, item.FirstName, item.interviewDate, item.ClientName, dashboardLink, yesbutton, nobutton, rescheulebutton, needcleritybutton, timeZoneValue, item.MeetingLink, item.Irfid, item.Interviewid, item.CompanyLink, item.LogoLink);
                        if (candidateDashboard == "1")
                        {
                            WhatsAppVM _whatsAppVM = new WhatsAppVM();
                            _whatsAppVM.Emailid = item.CandidateEmail;
                            _whatsAppVM.InterviewDate = item.interviewDate.ToString() + "(" + item.TimeZonesValue + ")";
                            _whatsAppVM.buttonlink = !string.IsNullOrEmpty(dashboardLink) ? dashboardLink.Replace("https://portal.risebird.io/Candidate/InterviewDetails", "") : "";
                            _whatsAppVM.Event = "Candidate Dashboard Reminder";
                            await _whatsAppHelper.WhatsAppSendCandidateDashboardReminder(_whatsAppVM);
                        }
                        else
                        {
                            WhatsAppVM _whatsAppVM = new WhatsAppVM();
                            _whatsAppVM.Emailid = item.CandidateEmail;
                            _whatsAppVM.InterviewDate = item.interviewDate.ToString() + "(" + item.TimeZonesValue + ")";
                            _whatsAppVM.buttonlink = !string.IsNullOrEmpty(yesbutton) ? yesbutton.Replace("https://portal.risebird.io/Interview/CandidateMeetingsStatus", "") : "";
                            _whatsAppVM.Event = "Interview Reminder Second";
                            _whatsAppVM.PrimarySkill = item.PrimarySkillInvite;
                            _whatsAppVM.MessageValue = item.ClientName;
                            _whatsAppVM.timezone = timeZoneValue;
                            await _whatsAppHelper.WhatsAppSendInterviewReminder(_whatsAppVM);
                        }
                        //Send Call, WhatsApp and mail

                        //With Yes/No Button
                        await _twilioHelper.CandidateReminderCall(item.MobileNumber, item.FirstName, item.interviewDate.ToString());
                    }
                    else if (item.Typefunction == 6)
                    {
                        //Send Notifications
                        Mail _mail = new Mail
                        {
                            TO = item.CandidateEmail,
                            Subject = item.ClientName + ": For Skill :" + primaryskill + ": Interview Reminder Confirm Your Availability"
                        };
                        var emailresult = await _notificationHelper.SendInterviewReminder(_mail, item.FirstName, item.interviewDate, item.ClientName, dashboardLink, yesbutton, nobutton, rescheulebutton, needcleritybutton, timeZoneValue, item.MeetingLink, item.Irfid, item.Interviewid, item.CompanyLink, item.LogoLink);
                        if (candidateDashboard == "1")
                        {
                            WhatsAppVM _whatsAppVM = new WhatsAppVM();
                            _whatsAppVM.Emailid = item.CandidateEmail;
                            _whatsAppVM.InterviewDate = item.interviewDate.ToString() + "(" + item.TimeZonesValue + ")";
                            _whatsAppVM.buttonlink = !string.IsNullOrEmpty(dashboardLink) ? dashboardLink.Replace("https://portal.risebird.io/Candidate/InterviewDetails", "") : "";
                            _whatsAppVM.Event = "Candidate Dashboard Reminder";
                            await _whatsAppHelper.WhatsAppSendCandidateDashboardReminder(_whatsAppVM);
                        }
                        else
                        {
                            //Send  WhatsApp and mail
                            WhatsAppVM _whatsAppVM = new WhatsAppVM();
                            _whatsAppVM.Emailid = item.CandidateEmail;
                            _whatsAppVM.InterviewDate = item.interviewDate.ToString();
                            _whatsAppVM.buttonlink = !string.IsNullOrEmpty(yesbutton) ? yesbutton.Replace("https://portal.risebird.io/Interview/CandidateMeetingsStatus", "") : "";
                            _whatsAppVM.Event = "Interview Reminder";
                            _whatsAppVM.PrimarySkill = item.PrimarySkillInvite;
                            _whatsAppVM.MessageValue = item.ClientName;
                            _whatsAppVM.timezone = timeZoneValue;
                            await _whatsAppHelper.WhatsAppSendInterviewReminder(_whatsAppVM);
                            //With Yes/No Button
                        }
                    }
                    else if (item.Typefunction == 24)
                    {
                        //Send Notifications
                        Mail _mail = new Mail
                        {
                            TO = item.CandidateEmail,
                            Subject = item.ClientName + ": For Skill :" + primaryskill + ": Interview Reminder Confirm Your Availability"
                        };
                        var emailresult = await _notificationHelper.SendInterviewReminder(_mail, item.FirstName, item.interviewDate, item.ClientName, dashboardLink, yesbutton, nobutton, rescheulebutton, needcleritybutton, timeZoneValue, item.MeetingLink, item.Irfid, item.Interviewid, item.CompanyLink, item.LogoLink);
                        if (candidateDashboard == "1")
                        {
                            WhatsAppVM _whatsAppVM = new WhatsAppVM();
                            _whatsAppVM.Emailid = item.CandidateEmail;
                            _whatsAppVM.InterviewDate = item.interviewDate.ToString() + "(" + item.TimeZonesValue + ")";
                            _whatsAppVM.buttonlink = !string.IsNullOrEmpty(dashboardLink) ? dashboardLink.Replace("https://portal.risebird.io/Candidate/InterviewDetails", "") : "";
                            _whatsAppVM.Event = "Candidate Dashboard Reminder";
                            await _whatsAppHelper.WhatsAppSendCandidateDashboardReminder(_whatsAppVM);
                        }
                        else
                        {
                            //Send Call, WhatsApp and mail
                            WhatsAppVM _whatsAppVM = new WhatsAppVM();
                            _whatsAppVM.Emailid = item.CandidateEmail;
                            _whatsAppVM.InterviewDate = item.interviewDate.ToString();
                            _whatsAppVM.buttonlink = !string.IsNullOrEmpty(yesbutton) ? yesbutton.Replace("https://portal.risebird.io/Interview/CandidateMeetingsStatus", "") : "";
                            _whatsAppVM.Event = "Interview Reminder";
                            _whatsAppVM.PrimarySkill = item.PrimarySkillInvite;
                            _whatsAppVM.MessageValue = item.ClientName;
                            _whatsAppVM.timezone = timeZoneValue;
                            await _whatsAppHelper.WhatsAppSendInterviewReminder(_whatsAppVM);
                            //With Yes/No Button
                        }
                    }
                    else if (item.Typefunction == 1)
                    {
                        if (await _meetingService.PublicCancelInterview(item.Interviewid))
                        {
                            var setvalue = _meetingService.UpdateCancelInterviewDetails(item.Interviewid);
                            var candidateId = await _irfRepository.GetCandidateIdByEmail(item.CandidateEmail);
                            var result1 = await _irfService.AddCandidateStatus("System", candidateId, item.Irfid, "Candidate Cancelled", (int)CandidateStatusOptions.Cancelled);
                            var result = await _candidateService.FetchChannelOwnerandInterviewer(item.Interviewid);
                            var irfOwnersAndRestrictedIrfOwner = await _candidateService.GetCandidateStaffingAgencyAndClientHRByInterviewId(item.Interviewid);
                            List<string> interviewers = new List<string>();
                            if (!string.IsNullOrEmpty(result.interviewer))
                            {
                                interviewers.Add(result.interviewer);
                            }
                            if (!string.IsNullOrEmpty(result.EmailId))
                            {
                                interviewers.Add(result.EmailId);
                            }
                            var emailresult = await _notificationHelper.SendInterviewCancelledMail(interviewers, result.interviewDate.ToString(), irfOwnersAndRestrictedIrfOwner?.ToList(), item.Irfid, item.Interviewid);
                            var emailresult1 = await _notificationHelper.SendInterviewCancelledMailClient(result.interviewDate.ToString(), result.CandidateName, irfOwnersAndRestrictedIrfOwner?.ToList(), item.Irfid, item.Interviewid);
                            var emailresult2 = await _notificationHelper.SendInterviewCancelledMailCandidateWithSchedulingLink(result.candidateEmail, result.interviewDate.ToString(), result.CandidateName, result.CalendlyLink, item.Irfid, item.Interviewid);
                        }
                    }
                    else if (item.Typefunction == 31)
                    {
                        //Send Notifications
                        Mail _mail = new Mail
                        {
                            TO = item.CandidateEmail,

                            Subject = item.ClientName + ": For Skill :" + primaryskill + ": Interview Reminder"
                        };
                        var emailresult = await _notificationHelper.SendInterviewNoReminder(_mail, item.FirstName, item.interviewDate, item.ClientName, item.PrimarySkillInvite, timeZoneValue, item.MeetingLink, item.Irfid, item.Interviewid, item.LogoLink);
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
