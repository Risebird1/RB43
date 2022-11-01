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

namespace Risebird.Functions
{
    public static class EmailtoInterviewer
    {
        [FunctionName("EmailtoInterviewer")]
        public static void Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            try
            {
                var msg = "Dear Interviewer! Greetings from RiseBird. This is a gentle reminder for your upcoming video based interview meeting scheduled today. Kindly be available on time.";
                var mails = new List<Meeting>();
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                var query = "select InterviewMeetings.id, InterviewMeetings.interviewer from InterviewMeetings where interviewDate<=@timeH and interviewDate>=@timeL and confirmationStatus=1";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@timeL", DateTime.Now);
                dynamicParameters.Add("@timeH", DateTime.Now.AddMinutes(35));
                using (SqlConnection con = new SqlConnection(str))
                {
                    mails = con.Query<Meeting>(query, dynamicParameters).ToList();
                }

                if (mails != null && mails.Count() > 0)
                {
                    try
                    {
                        var masg = new SendGridMessage();
                        masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("notification@risebird.io", "Risebird"));
                        foreach (var mail in mails)
                        {
                            if (!string.IsNullOrEmpty(mail.interviewer))
                            {
                                masg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(mail.interviewer, null));
                            }
                        }
                        masg.Subject = "Reminder! You have a video based interview meeting coming up in the next 30 minutes.";


                        StringBuilder msgBody = new StringBuilder("Dear Interviewer,");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine(msg);
                        msgBody.AppendLine(
                            "Kindly login over risebird portal and head over to \'MyInterview\' menu to join.");
                        msgBody.Append("\n");
                        msgBody.AppendLine("Note: Please login over zoom 2 minutes prior to the scheduled time of the interview and try finishing 2 minutes before the scheduled close time.");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine("Thank You, Team Risebird");

                        masg.PlainTextContent = msgBody.ToString();
                        CommonSendMail _common = new CommonSendMail();
                        _ = _common.SendMail(masg, null, mails.FirstOrDefault().id, log).Result;
                        log.Info("Interview schedule mail sent to interviewer.");

                    }
                    catch (Exception)
                    {
                        log.Error($"Failed to send mail. Please check all filled and try again.");
                    }
                }

            }
            catch (Exception)
            {
                log.Error($"Failed to send sms. Please check all filled and try again.");
            }



        }
    }
    public class Meeting
    {
        public int id { get; set; }
        public string interviewer { get; set; }
        
    }
}
