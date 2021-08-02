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
    public static class SMStoCandidate
    {
        [FunctionName("SMStoCandidate")]
        public static void Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var candidates = new List<SMSCandidate>();
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                var query = "select IRF.id,IRF.IRFId,IRF.EndDate, IRF.JDTiltle,Client.[Name] as Client, ul.EmailId, Candidate.Phone, Candidate.InviteDate from Candidate join CandidatesMaster cm on Candidate.CandidateMasterId=cm.id join UserList ul on cm.UserId = ul.id join IRF on Candidate.IRFId= IRF.id join Client on Client.id=IRF.ClientId where Candidate.TotalInvites!=0 and IRF.IsPaused!= 1";
                //var dynamicParameters = new DynamicParameters();
                //dynamicParameters.Add("@timeL", DateTime.Now);
                //dynamicParameters.Add("@timeH", DateTime.Now.AddMinutes(35));
                using (SqlConnection con = new SqlConnection(str))
                {
                    candidates = con.Query<SMSCandidate>(query).ToList();
                }

                //if (candidates != null)
                //{
                //    var twilioAuthToken = "3f15940e73243adb8e75f9689adaa0e2";
                //    var twilioAccountSID = "ACcad9f6682d4a76df9af7cde78dd0867e";
                //    var twilioSMSNumber = "+12055768041";
                //    var _twilioClient = new TwilioRestClient(twilioAccountSID, twilioAuthToken);
                //    foreach (var candidate in candidates)
                //    {
                //        if (candidate.EndDate < DateTime.Now)
                //        {
                //            int checkScheduleCount = 0;
                //            var checkQuery = "select id from InterviewMeetings where IRFId=@IRFId and candidateEmail = @EmailId and confirmationStatus!=2 and interviewDate > dateadd(dd,-3,getdate())";
                //            var dynamicParameters = new DynamicParameters();
                //            dynamicParameters.Add("@IRFId", candidate.IRFId);
                //            dynamicParameters.Add("@EmailId", candidate.EmailId);
                //            using (SqlConnection con = new SqlConnection(str))
                //            {
                //                var scheduleResult = con.Query<int>(checkQuery, dynamicParameters).ToList();
                //                checkScheduleCount = scheduleResult.Count();
                //            }
                //            if (checkScheduleCount == 0)
                //            {
                //                var msg = "Dear Candidate! Greetings from RiseBird. This is a gentle reminder for you to kindly schedule a meeting against the invite for the Client Name " + candidate.ClientName + " and Skill " + candidate.JDTiltle + " sent to you.";
                //                if (!string.IsNullOrEmpty(candidate.Phone))
                //                    MessageResource.CreateAsync(to: new PhoneNumber(candidate.Phone), from: new PhoneNumber(twilioSMSNumber), body: msg, client: _twilioClient);
                //                log.Info($"SMS Timer trigger function executed for: {candidate.Phone}");
                //            }
                //        }

                //    }

                //}

            }
            catch (Exception)
            {
                log.Error($"Failed to send sms. Please check all filled and try again.");
            }
        }
    }

    public class SMSCandidate
    {
        public string IRFId { get; set; }
        public string EmailId { get; set; }
        public string Phone { get; set; }
        public string ClientName { get; set; }
        public string JDTiltle { get; set; }
        public DateTime EndDate { get; set; }
    }
}
