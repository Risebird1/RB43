using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Risebird.Functions
{
    public static class InterviewWindUpCall
    {
        [FunctionName("InterviewWindUpCall")]//25-59/30 * * * *
        public static void Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var interviewList = new List<InterviewTimeClass>();
                int windUpsetting;
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                var query = "select im.interviewDate, im.interviewDuration, ul.MobileNumber as InterviewerMobile from InterviewMeetings im left join UserList ul on im.interviewer=ul.EmailId left join InterviewComments ic on im.id =ic.InterviewId where im.confirmationStatus=1 and (ic.MeetingStatus is null)";

                using (SqlConnection con = new SqlConnection(str))
                {
                    interviewList = con.Query<InterviewTimeClass>(query).ToList();
                }
                var querySettings = "select InterviewWindupReminderMinutes from Settings";
                using (SqlConnection con = new SqlConnection(str))
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

                    // COMMENTED OUT: Twilio IVR call disabled
                    //Parallel.ForEach(phoneNumbers, phoneNumber =>
                    //{
                    //    if (!string.IsNullOrEmpty(phoneNumber))
                    //    {
                    //        TwilioClient.Init(twilioAccountSID, twilioAuthToken);
                    //        var to = new PhoneNumber(phoneNumber);
                    //        var from = new PhoneNumber(twilioVoiceCallNumber);
                    //        var call = CallResource.CreateAsync(
                    //            to: to,
                    //            from: from,
                    //            url: new Uri("https://beige-vulture-6005.twil.io/assets/WindupMessage.xml"));
                    //        log.Info($"Voice call Timer trigger function executed for: {phoneNumber}");
                    //    }

                    //}
                    //);

                }

            }
            catch (Exception)
            {
                log.Error($"Failed to make call. Please check all filled and try again.");
            }

        }
    }

    public class InterviewTimeClass
    {
        public DateTime interviewDate { get; set; }
        public int interviewDuration { get; set; }
        public string InterviewerMobile { get; set; }
    }
}
