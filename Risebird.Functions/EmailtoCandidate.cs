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
using System.Net.Mail;
using System.Text;

namespace Risebird.Functions
{
    public static class EmailtoCandidate
    {
        [FunctionName("EmailtoCandidate")]
        public static void Run([TimerTrigger("0 */30 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            try
            {
                var msg = "Dear Candidate! Greetings from RiseBird. This is a gentle reminder for your upcoming video based interview meeting scheduled today. Kindly be available on time.";
                var mails = new List<string>();
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                var query = "select candidateEmail from InterviewMeetings where interviewDate<=@timeH and interviewDate>=@timeL and confirmationStatus=1";
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
                        var mailMessage = new MailMessage();
                        mailMessage.From = new MailAddress("hire@risebird.io", "Risebird Notification");

                        mailMessage.To.Add(new MailAddress("hire@risebird.io", "Risebird"));
                        foreach (var mail in mails)
                        {
                            if (!string.IsNullOrEmpty(mail))
                            {
                                mailMessage.Bcc.Add(new MailAddress(mail, null));
                            }
                        }
                        mailMessage.Subject = "Urgent Alert!! Your video based interview meeting in the next 30 minutes";

                        StringBuilder msgBody = new StringBuilder("Dear Candidate,");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine(msg);
                        msgBody.AppendLine(
                            "Kindly available 2 minutes before and use your zoom link to attend your interview meeting.");
                        msgBody.Append("\n");                       
                        msgBody.Append("\n");
                        msgBody.AppendLine("Thank You, Team Risebird");

                        mailMessage.Body = msgBody.ToString();
                        SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                        System.Net.NetworkCredential credentials = new System.Net.NetworkCredential("azure_9119f7b456399652d5a99faa61a6fbe7@azure.com", "rise_123");
                        smtpClient.Credentials = credentials;

                        smtpClient.Send(mailMessage);
                        log.Info("Candidate schedule mail sent to interviewer.");

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
