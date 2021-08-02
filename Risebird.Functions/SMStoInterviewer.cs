using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;

namespace Risebird.Functions
{
    public static class SMStoInterviewer
    {
        [FunctionName("SMStoInterviewer")]
        public static void Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            try
            {
                var msg = "Dear Interviewer! Greetings from RiseBird. This is a gentle reminder for your upcoming video based interview meeting scheduled today. Kindly be available on time.";
                var phoneNumbers = new List<string>();
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                var query = "select UserList.MobileNumber from UserList inner join (select InterviewMeetings.interviewer from InterviewMeetings where interviewDate<=@timeH and interviewDate>=@timeL and confirmationStatus=1) a on a.interviewer=UserList.EmailId";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@timeL", DateTime.Now);
                dynamicParameters.Add("@timeH", DateTime.Now.AddMinutes(35));
                using (SqlConnection con = new SqlConnection(str))
                {
                    phoneNumbers = con.Query<string>(query, dynamicParameters).ToList();
                }

                if (phoneNumbers != null && phoneNumbers.Count() > 0)
                {
                    //send an sms to all the interviewers with the message
                    //var twilioAuthToken = "3f15940e73243adb8e75f9689adaa0e2";
                    //var twilioAccountSID = "ACcad9f6682d4a76df9af7cde78dd0867e";
                    //var twilioSMSNumber = "+12055768041";
                    //var _twilioClient = new TwilioRestClient(twilioAccountSID, twilioAuthToken);
                    //Parallel.ForEach(phoneNumbers, phoneNumber =>
                    //{
                    //    if (!string.IsNullOrEmpty(phoneNumber))
                    //    {
                    //        if (!string.IsNullOrEmpty(phoneNumber))
                    //            MessageResource.CreateAsync(to: new PhoneNumber(phoneNumber), from: new PhoneNumber(twilioSMSNumber), body: msg, client: _twilioClient);
                    //        log.Info($"SMS Timer trigger function executed for: {phoneNumber}");
                    //    }

                    //}
                    //);
                    //MessageResource.CreateAsync(to: new PhoneNumber("+919284487005"), from: new PhoneNumber(twilioSMSNumber), body: msg, client: _twilioClient);
                    //foreach (var phoneNumber in phoneNumbers)
                    //{
                    //    if(!string.IsNullOrEmpty(phoneNumber))
                    //    MessageResource.CreateAsync(to: new PhoneNumber(phoneNumber), from: new PhoneNumber(twilioSMSNumber), body: msg, client: _twilioClient);
                    //}
                }

            }
            catch (Exception)
            {
                log.Error($"Failed to send sms. Please check all filled and try again.");
            }



        }
    }
}
