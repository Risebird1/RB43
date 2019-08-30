using System;
using System.Configuration;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Risebird.Functions
{
    public static class SMStoInterviewer
    {
        [FunctionName("SMStoInterviewer")]
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

                if(phoneNumbers!=null && phoneNumbers.Count()>0)
                {
                    //send an sms to all the interviewers with the message
                    var twilioAuthToken = ConfigurationManager.AppSettings["TwilioSettings:AUTH_TOKEN"];
                    var twilioAccountSID = ConfigurationManager.AppSettings["TwilioSettings:ACCOUNT_SID"];
                    var twilioSMSNumber = ConfigurationManager.AppSettings["TwilioSettings:SmsSENDER"];
                    var _twilioClient = new TwilioRestClient(twilioAccountSID, twilioAuthToken);
                    foreach (var phoneNumber in phoneNumbers)
                    {

                        //HttpClient client = new HttpClient();
                        //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                        //client.BaseAddress = new Uri(ConfigurationManager.AppSettings["WebApiBaseUri"].ToString());

                        //client.DefaultRequestHeaders.ExpectContinue = false;

                        //log.Info("Calling update status api.");
                        //HttpResponseMessage response = client.PostAsync("/api/Scheduler/UpdateAllVMsStatus", null).Result;
                       
                        MessageResource.CreateAsync(to: new PhoneNumber(phoneNumber), from: new PhoneNumber(twilioSMSNumber), body: msg, client: _twilioClient);

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
