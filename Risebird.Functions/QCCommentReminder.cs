using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Risebird.Functions
{
    public static class QCCommentReminder
    {
        [FunctionName("QCCommentReminder")]
        public static void Run([TimerTrigger("0 0 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var interviewList = new List<InterviewQCDetails>();
                int qcFeedBackSetting;
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;

                //get feedback time from settings
                var querySettings = "select QCCommentReminderMinutes from Settings";
                using (SqlConnection con = new SqlConnection(str))
                {
                    qcFeedBackSetting = con.Query<int>(querySettings).FirstOrDefault();
                }

                //select im.interviewDate, im.interviewDuration, ul.MobileNumber as InterviewerMobile from InterviewMeetings im join UserList ul on im.interviewer=ul.EmailId  where @timeNow>interviewDate and @timeNow <DATEADD(MINUTE, interviewDuration,interviewDate)
                var query = "select im.interviewDate, im.interviewDuration, ul.MobileNumber as QCMobile from InterviewMeetings im left join IRF on IRF.IRFId = im.IRFId left join TransQualityController tqc on IRF.id = tqc.IRFId left join UserList ul on tqc.UserId=ul.id left join InterviewComments ic on im.id = ic.InterviewId  where DATEADD(MINUTE,interviewDuration,interviewDate) <= DATEADD(MINUTE, 330-@qcFeedBackSetting, GETDATE()) and ic.InterviewerComment is not null and (ic.MeetingStatus=4 or ic.MeetingStatus is null)";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@qcFeedBackSetting", qcFeedBackSetting);
                using (SqlConnection con = new SqlConnection(str))
                {
                    interviewList = con.Query<InterviewQCDetails>(query, dynamicParameters).ToList();
                }

                //interviewList = interviewList.Where(x => (x.interviewDate < DateTime.Now) && (x.interviewDate.AddMinutes(x.interviewDuration) > DateTime.Now) && (DateTime.Now.AddMinutes(windUpsetting - 1) < x.interviewDate.AddMinutes(x.interviewDuration)) && (DateTime.Now.AddMinutes(windUpsetting) > x.interviewDate.AddMinutes(x.interviewDuration))/*(x.interviewDate.AddMinutes(x.interviewDuration) < DateTime.Now.AddMinutes(30))*/).ToList();
                var phoneNumbers = interviewList.Select(x => x.QCMobile);
                phoneNumbers = phoneNumbers.Distinct();
                //var phoneNumbers = new List<string>();
                //phoneNumbers.Add("+919284487005");
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
                                url: new Uri("https://beige-vulture-6005.twil.io/assets/QCReminder.xml"));
                            log.Info($"Voice call Timer trigger function executed for: {phoneNumber}");
                        }

                    }
                    );

                }

            }
            catch (Exception)
            {
                log.Error($"Failed to make call. Please check all filled and try again.");
            }
        }
    }

    public class InterviewQCDetails
    {
        public DateTime interviewDate { get; set; }
        public int interviewDuration { get; set; }
        public string QCMobile { get; set; }
    }

}
