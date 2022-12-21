using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Risebird.MicroService
{
    public class InterviewWindUpCall
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;


        private readonly string connectionString;
        //private readonly string SASConnectionString;

        public InterviewWindUpCall(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        [FunctionName("InterviewWindUpCall")]
        public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"InterviewWindUpCall : C# Timer trigger function started executing at: {DateTime.Now}");

            try
            {
                var interviewList = new List<InterviewTimeClass>();
                int windUpsetting;
                var query = "select im.interviewDate, im.interviewDuration, ul.MobileNumber as InterviewerMobile from InterviewMeetings im left join UserList ul on im.interviewer=ul.EmailId left join InterviewComments ic on im.id =ic.InterviewId where im.confirmationStatus=1 and (ic.MeetingStatus is null)";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    interviewList = con.Query<InterviewTimeClass>(query).ToList();
                }
                var querySettings = "select InterviewWindupReminderMinutes from Settings";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    windUpsetting = con.Query<int>(querySettings).FirstOrDefault();
                }
                interviewList = interviewList.Where(x => (x.interviewDate < DateTime.Now) && (DateTime.Now.AddMinutes(windUpsetting - 1) < x.interviewDate.AddMinutes(x.interviewDuration)) && (DateTime.Now.AddMinutes(windUpsetting) > x.interviewDate.AddMinutes(x.interviewDuration))).ToList();
                var phoneNumbers = interviewList.Select(x => x.InterviewerMobile);
                phoneNumbers = phoneNumbers.Distinct();

                if (phoneNumbers != null && phoneNumbers.Count() > 0)
                {
                    var twilioAuthToken = "3f15940e73243adb8e75f9689adaa0e2";
                    var twilioAccountSID = "ACcad9f6682d4a76df9af7cde78dd0867e";
                    var twilioVoiceCallNumber = "+17274782239";

                    Parallel.ForEach(phoneNumbers, phoneNumber =>
                    {
                        if (!string.IsNullOrEmpty(phoneNumber))
                        {
                            TwilioClient.Init(twilioAccountSID, twilioAuthToken);
                            var to = new PhoneNumber(phoneNumber);
                            var from = new PhoneNumber(twilioVoiceCallNumber);
                            var call = CallResource.CreateAsync(
                                to: to,
                                from: from,
                                url: new Uri("https://beige-vulture-6005.twil.io/assets/WindupMessage.xml"));
                            log.LogInformation($"Voice call Timer trigger function executed for: {phoneNumber}");
                        }

                    }
                    );

                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

            }

            log.LogInformation($"InterviewWindUpCall : C# Timer trigger function finished executing at: {DateTime.Now}");
        }
    }
    public class InterviewTimeClass
    {
        public DateTime interviewDate { get; set; }
        public int interviewDuration { get; set; }
        public string InterviewerMobile { get; set; }
    }
}
