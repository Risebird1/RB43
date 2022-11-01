using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Risebird.Functions
{
    public static class InterviewAutoCancellation
    {
        [FunctionName("InterviewAutoCancellation")]
        public static void Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var interviewList = new List<AutoCancellationClass>();
                int autoCancellationsetting;
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                var query = "select id, interviewDate, candidateEmail, ClientHrContact from InterviewMeetings where confirmationStatus=1 and interviewer is null and interviewDate>@timeNow";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(str))
                {
                    interviewList = con.Query<AutoCancellationClass>(query, dynamicParameters).ToList();
                }
                var querySettings = "select AutoCancellationMailMinutes from Settings";
                using (SqlConnection con = new SqlConnection(str))
                {
                    autoCancellationsetting = con.Query<int>(querySettings).FirstOrDefault();
                }
                interviewList = interviewList.Where(x => (DateTime.Now.AddMinutes(autoCancellationsetting - 1) < x.interviewDate) && (DateTime.Now.AddMinutes(autoCancellationsetting) > x.interviewDate)).ToList();
                var idsJoined = string.Join(",", from item in interviewList select item.id);
                if (!string.IsNullOrEmpty(idsJoined))
                {
                    var queryUpdate = "update InterviewMeetings set confirmationStatus = 2 where id in (" + idsJoined + ")";
                    using (SqlConnection con = new SqlConnection(str))
                    {
                        con.Query(queryUpdate);
                    }
                }

                //send auto cancellation mails

                if (interviewList != null && interviewList.Count() > 0)
                {
                    Parallel.ForEach(interviewList, interview =>
                    {
                        if (interview.candidateEmail != null)
                        {
                            SendInterviewCancelledMailtoCandidate(interview.candidateEmail, interview.ClientHrContact, interview.interviewDate.ToString("MMMM dd, yyyy"), null, interview.id);
                            //SendInterviewCancelledMailtoCandidate("harshitjajoo95@gmail.com", "hjajoo995@gmail.com", interview.interviewDate.ToString("MMMM dd, yyyy"));
                        }
                    }
                    );

                }

            }
            catch (SqlException ex)
            {
                log.Error(ex.Message);
            }
            catch (Exception)
            {
                log.Error($"Failed to make call. Please check all filled and try again.");
            }
        }

        public static void SendInterviewCancelledMailtoCandidate(string candidateMail, string clientHRMail, string interviewDate, int? irfId = null, int? interviewId = null, TraceWriter log = null)
        {
            try
            {
                var mailFrom = "notification@risebird.io";
                var masg = new SendGridMessage();

                masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(candidateMail, "Risebird"));

                masg.Subject = "Interview has been Cancelled for the date: " + interviewDate;

                StringBuilder msgBody = new StringBuilder("Dear Candidate,");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("You have received this communication as an interview has been cancelled for the date: " + interviewDate + " .");
                msgBody.AppendLine(
                    "Thanks for your support, we will keep updating you about future interview based on your matching skill.");
                msgBody.Append("\n");
                msgBody.Append("\n");
                msgBody.AppendLine("Thank You, Team Risebird");

                masg.PlainTextContent = msgBody.ToString();
                CommonSendMail _common = new CommonSendMail();
                _ = _common.SendMail(masg, irfId, interviewId, log).Result;
                //return "Mail sent successfully.";
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return; //"Failed to send mail. Please check all filled and try again.";
            }
        }

    }
    public class AutoCancellationClass
    {
        public int id { get; set; }
        public DateTime interviewDate { get; set; }
        public string candidateEmail { get; set; }
        public string ClientHrContact { get; set; }
    }
}
