using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class InterviewerFeedbackRemainder
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IDashboarRepository _dashboardRespository;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly ILogger log;
        private readonly string _connection;
        public InterviewerFeedbackRemainder(ILoggerFactory loggerFactory, IConfiguration configuration, IDashboarRepository dashboardRespository, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IWhatsAppHelper whatsAppHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _dashboardRespository = dashboardRespository;
            _whatsAppHelper = whatsAppHelper;
            _connection = _configuration["ConnectionString:DBConnectionString"];
            log = loggerFactory.CreateLogger<InterviewerFeedbackRemainder>();
        }
        [Function("InterviewerFeedbackRemainder")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var Emails = new List<CandidateDetailsForWhatsApp>();
                var str = _connection;
                // I6: 5–15 minutes after the interview ends (start + duration) while the report is still pending
                // (MeetingStatus 2 or no report row; 4, 6, 7, 8 = done). Once per interview (IsReportReminderSent).
                var query = @"Select im.id,im.interviewer as ClientName,im.interviewDate,im.candidateEmail, RTRIM(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')) as CandidateName, irf.id as IRFId
                    From InterviewMeetings im
                    left join Userlist ul on ul.EmailId=im.candidateEmail
                    join IRF on irf.irfId = im.IRFId
                    join UserList uli on uli.EmailId=im.interviewer and uli.IsActive=1
                    where im.confirmationStatus=1 and IsNull(im.IsReportReminderSent,0)=0
                    and not exists (Select 1 From InterviewComments ic where ic.InterviewId=im.id and ic.MeetingStatus is not null and ic.MeetingStatus<>2)
                    and dateadd(minute, IsNull(im.interviewDuration,60), im.interviewDate)<=@timeH
                    and dateadd(minute, IsNull(im.interviewDuration,60), im.interviewDate)>=@timeL";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@timeL", DateTime.Now.AddMinutes(-15));
                dynamicParameters.Add("@timeH", DateTime.Now.AddMinutes(-5));
                using (SqlConnection con = new SqlConnection(str))
                {
                    Emails = (List<CandidateDetailsForWhatsApp>)await con.QueryAsync<CandidateDetailsForWhatsApp>(query, dynamicParameters);
                }
                if (Emails != null && Emails.Count() > 0)
                {
                    try
                    {
                        foreach (var item in Emails)
                        {
                            // Mark first so an Interakt failure never turns into a reminder every 5 minutes.
                            using (SqlConnection con = new SqlConnection(str))
                            {
                                await con.ExecuteAsync("Update InterviewMeetings set IsReportReminderSent=1 where id=@id", new { id = item.id });
                            }
                            WhatsAppVM _whatsAppVM = new WhatsAppVM();
                            _whatsAppVM.Emailid = item.ClientName;
                            _whatsAppVM.InterviewDate = item.interviewDate.ToString();
                            _whatsAppVM.Event = "Interviewer Feedback Remainder";
                            _whatsAppVM.MessageValue = item.CandidateName;
                            _whatsAppVM.CandidateName = item.CandidateName;
                            // "Edit Report" opens the report form itself, as the interviewer's My Interview page does.
                            _whatsAppVM.ReportLink = "Interview/AddReport?interviewId=" + item.id + "&irfId=" + item.IRFId;
                            _whatsAppVM.InterviewId = item.id;
                            _whatsAppVM.irfId = item.IRFId;
                            await _whatsAppHelper.WhatsAppSendInterviewerFeedbackRemainder(_whatsAppVM);
                        }
                    }
                    catch (Exception)
                    {
                        log.LogError($"Failed to send WhatsApp. Please check all filled and try again.");
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                log.LogError($"Failed to send WhatsApp. Please check all filled and try again.");
            }
        }
    }
}
