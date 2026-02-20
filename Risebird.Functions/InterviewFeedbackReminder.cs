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
    public static class InterviewFeedbackReminder
    {
        [FunctionName("InterviewFeedbackReminder")]
        public static void Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var interviewList = new List<InterviewTimeClass>();
                int feedbackMinutes;
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;

                //get feedback time from settings
                var querySettings = "select InterviewReportReminderMinutes from Settings";
                using (SqlConnection con = new SqlConnection(str))
                {
                    feedbackMinutes = con.Query<int>(querySettings).FirstOrDefault();
                }

                var query = "select ul.MobileNumber as InterviewerMobile from InterviewMeetings im inner join UserList ul on im.interviewer=ul.EmailId left join InterviewComments ic on im.id = ic.InterviewId where DATEADD(MINUTE,interviewDuration,interviewDate) <= DATEADD(MINUTE,-@feedbackMinutes, @timeNow) and DATEADD(HOUR,-48,@timeNow)<im.interviewDate and im.confirmationStatus=1 and ic.InterviewerComment is null and ic.InterviewStatus is null and (ic.MeetingStatus=4 or ic.MeetingStatus is null)";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@feedbackMinutes", feedbackMinutes);
                dynamicParameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(str))
                {
                    interviewList = con.Query<InterviewTimeClass>(query, dynamicParameters).ToList();
                }

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
                    //            url: new Uri("https://beige-vulture-6005.twil.io/assets/FeedbackReminder.xml"));
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
}
