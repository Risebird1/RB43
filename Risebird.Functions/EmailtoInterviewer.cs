using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
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
                var mails = new List<string>();
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                var query = "select InterviewMeetings.interviewer from InterviewMeetings where interviewDate<=@timeH and interviewDate>=@timeL and confirmationStatus=1";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@timeL", DateTime.Now);
                dynamicParameters.Add("@timeH", DateTime.Now.AddMinutes(35));
                using (SqlConnection con = new SqlConnection(str))
                {
                    mails = con.Query<string>(query, dynamicParameters).ToList();
                }

                if (mails != null && mails.Count() > 0)
                {
                    try
                    {
                        var masg = new SendGridMessage();
                        masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("notification@risebird.io", "Risebird"));
                        foreach (var mail in mails)
                        {
                            if (!string.IsNullOrEmpty(mail))
                            {
                                masg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(mail, null));
                            }
                        }
                        masg.Subject = "Urgent Alert!! Your video based interview meeting in the next 30 minutes";


                        StringBuilder msgBody = new StringBuilder("Dear Interviewer,");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine(msg);
                        msgBody.AppendLine(
                            "Kindly login over risebird portal and click on MyInterview menu and take interview.");
                        msgBody.Append("\n");
                        msgBody.AppendLine("Note: Login over zoom account just 2 minutes before schedule interview and try to finish before 2 minutes.");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine("Thank You, Team Risebird");

                        masg.PlainTextContent = msgBody.ToString();
                        CommonSendMail _common = new CommonSendMail();
                        _common.SendMail(masg);
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
}
