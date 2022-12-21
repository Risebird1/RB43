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
    public class VoiceCallInterviewer
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;


        private readonly string connectionString;
        //private readonly string SASConnectionString;

        public VoiceCallInterviewer(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        [FunctionName("VoiceCallInterviewer")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"VoiceCallInterviewer : C# Timer trigger function started executing at: {DateTime.Now}");

            try
            {
                var phoneNumbers = new List<string>();
                var query = "select UserList.MobileNumber from UserList inner join (select InterviewMeetings.interviewer from InterviewMeetings where interviewDate<=@timeH and interviewDate>=@timeL and confirmationStatus=1) a on a.interviewer=UserList.EmailId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@timeL", DateTime.Now);
                dynamicParameters.Add("@timeH", DateTime.Now.AddMinutes(35));
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    phoneNumbers = con.Query<string>(query, dynamicParameters).ToList();
                }

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
                                url: new Uri("https://beige-vulture-6005.twil.io/assets/phoneCallFile.xml"));
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

            log.LogInformation($"VoiceCallInterviewer : C# Timer trigger function finished executing at: {DateTime.Now}");
        }
    }
}
