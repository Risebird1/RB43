using Dapper;
using Microsoft.Azure.WebJobs;
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

namespace Risebird.MicroService
{
    public class InterviewerFeedbackRemainder
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IDashboarRepository _dashboardRespository;
        private readonly IWhatsAppHelper _whatsAppHelper;

        private readonly string _connection;
        public InterviewerFeedbackRemainder(IConfiguration configuration, IDashboarRepository dashboardRespository, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IWhatsAppHelper whatsAppHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _dashboardRespository = dashboardRespository;
            _whatsAppHelper = whatsAppHelper;
            _connection = _configuration["ConnectionString:DBConnectionString"];
        }
        [FunctionName("InterviewerFeedbackRemainder")]
        public async void Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var Emails = new List<CandidateDetailsForWhatsApp>();
                var str = _connection;
                var query = "Select im.id,im.interviewer as ClientName,im.interviewDate,im.candidateEmail,RTRIM(IsNull(ul.FirstName,'')+' '+IsNull(ul.LastName,'')) as CandidateName From InterviewComments ic join InterviewMeetings im left join Userlist ul on ul.EmailId=im.candidateEmail on im.id=ic.InterviewId where ic.MeetingStatus=2 and im.confirmationStatus=1 and interviewDate<=@timeH and interviewDate>=@timeL";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@timeL", DateTime.Now.AddMinutes(-125));
                dynamicParameters.Add("@timeH", DateTime.Now.AddMinutes(-115));
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
                            WhatsAppVM _whatsAppVM = new WhatsAppVM();
                            _whatsAppVM.Emailid = item.ClientName;
                            _whatsAppVM.InterviewDate = item.interviewDate.ToString();
                            _whatsAppVM.Event = "Interviewer Feedback Remainder";
                            _whatsAppVM.MessageValue = item.CandidateName;
                            _whatsAppHelper.WhatsAppSendInterviewerFeedbackRemainder(_whatsAppVM);
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
