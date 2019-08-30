using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Risebird.Functions
{
    public static class WhatsappInterviewer
    {
        [FunctionName("WhatsappInterviewer")]
        public static void Run([TimerTrigger("0 */15 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var msg = "Hi! Greetings from RiseBird. This is a gentle reminder for your interview meeting scheduled within the next 30 minutes.";
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
                    var twilioWhatsappNumber = ConfigurationManager.AppSettings["TwilioSettings:WhatsappSENDER"];
                    var _twilioClient = new TwilioRestClient(twilioAccountSID, twilioAuthToken);
                    //send an sms to all the interviewers with the message

                    foreach (var phoneNumber in phoneNumbers)
                    {
                        MessageResource.CreateAsync(to: new PhoneNumber("whatsapp:" + phoneNumber), from: new PhoneNumber("whatsapp:" + twilioWhatsappNumber), body: msg, client: _twilioClient);
                    }
                }

            }
            catch (Exception)
            {
                log.Error($"Failed to send mail. Please check all filled and try again.");
            }

        }
    }
}
