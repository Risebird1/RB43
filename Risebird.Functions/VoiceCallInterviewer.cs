using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Twilio;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Newtonsoft.Json;

namespace Risebird.Functions
{
    public static class VoiceCallInterviewer
    {
        [FunctionName("VoiceCallInterviewer")]
        public static void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var phoneNumbers = new List<string>();
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                var query = "select UserList.MobileNumber from UserList inner join (select InterviewMeetings.interviewer from InterviewMeetings where interviewDate<=@timeH and interviewDate>=@timeL) a on a.interviewer=UserList.EmailId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@timeL", DateTime.Now.AddMinutes(30));
                dynamicParameters.Add("@timeH", DateTime.Now.AddMinutes(45));
                using (SqlConnection con = new SqlConnection(str))
                {
                    phoneNumbers = con.Query<string>(query, dynamicParameters).ToList();
                }
                
                if (phoneNumbers != null && phoneNumbers.Count() > 0)
                {
                    var twilioAuthToken = ConfigurationManager.AppSettings["TwilioSettings:AUTH_TOKEN"];
                    var twilioAccountSID = ConfigurationManager.AppSettings["TwilioSettings:ACCOUNT_SID"];
                    var twilioVoiceCallNumber = ConfigurationManager.AppSettings["TwilioSettings:CallerNumber"];


                    foreach (var phoneNumber in phoneNumbers)
                    {
                        TwilioClient.Init(twilioAccountSID, twilioAuthToken);
                        var to = new PhoneNumber(phoneNumber);
                        var from = new PhoneNumber(twilioVoiceCallNumber);
                        var call = CallResource.CreateAsync(
                            to: to,
                            from: from,
                            url: new Uri("https://beige-vulture-6005.twil.io/assets/phoneCallFile.xml"));
                    }
                }

            }
            catch (Exception ex)
            {
                log.Error($"Failed to send mail. Please check all filled and try again.");
            }
        }
    }
}
