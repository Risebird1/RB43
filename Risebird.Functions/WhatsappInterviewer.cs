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
        //[FunctionName("WhatsappInterviewer")]
        //public static void Run([TimerTrigger("0 */30 * * * *")]TimerInfo myTimer, TraceWriter log)
        //{
        //    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
        //    try
        //    {
        //        var msg = "Dear Interviewer! Greetings from RiseBird. This is a gentle reminder for your video based interview meeting scheduled in the next 30 minutes.";
        //        var phoneNumbers = new List<string>();
        //        var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
        //        var query = "select UserList.MobileNumber from UserList inner join (select InterviewMeetings.interviewer from InterviewMeetings where interviewDate<=@timeH and interviewDate>=@timeL and confirmationStatus=1) a on a.interviewer=UserList.EmailId";
        //        var dynamicParameters = new DynamicParameters();
        //        dynamicParameters.Add("@timeL",  DateTime.Now.AddHours(6));
        //        dynamicParameters.Add("@timeH",DateTime.Now.AddHours(6).AddMinutes(30));
        //        using (SqlConnection con = new SqlConnection(str))
        //        {
        //            phoneNumbers = con.Query<string>(query, dynamicParameters).ToList();
        //        }
        //        //var twilioAuthToken = "3f15940e73243adb8e75f9689adaa0e2";
        //        //var twilioAccountSID = "ACcad9f6682d4a76df9af7cde78dd0867e";
        //        //var twilioWhatsappNumber = "+14155238886";
        //        //var _twilioClient = new TwilioRestClient(twilioAccountSID, twilioAuthToken);
        //        //var response= MessageResource.CreateAsync(to: new PhoneNumber("whatsapp:+918888571923" ), from: new PhoneNumber("whatsapp:" + twilioWhatsappNumber), body: msg, client: _twilioClient);

        //        if (phoneNumbers != null && phoneNumbers.Count() > 0)
        //        {
        //            var twilioAuthToken = "3f15940e73243adb8e75f9689adaa0e2";
        //            var twilioAccountSID = "ACcad9f6682d4a76df9af7cde78dd0867e";
        //            var twilioWhatsappNumber = "+14155238886";
        //            var _twilioClient = new TwilioRestClient(twilioAccountSID, twilioAuthToken);
        //            //send an sms to all the interviewers with the message

        //            foreach (var phoneNumber in phoneNumbers)
        //            {
        //               if(!string.IsNullOrEmpty(phoneNumber))
        //                MessageResource.CreateAsync(to: new PhoneNumber("whatsapp:" + phoneNumber), from: new PhoneNumber("whatsapp:" + twilioWhatsappNumber), body: msg, client: _twilioClient);
        //            }
        //        }

        //    }
        //    catch (Exception)
        //    {
        //        log.Error($"Failed to send mail. Please check all filled and try again.");
        //    }

        //}
    }
}
