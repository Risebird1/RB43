using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using SendGrid.Helpers.Mail;

namespace Risebird.MicroService
{
    public class EmailtoInterviewer
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;


        private readonly string connectionString;
        //private readonly string SASConnectionString;

        public EmailtoInterviewer(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        [FunctionName("EmailtoInterviewer")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"EmailtoInterviewer : C# Timer trigger function started executing  at: {DateTime.Now}");


            try
            {
                var msg = "Dear Interviewer! Greetings from RiseBird. This is a gentle reminder for your upcoming video based interview meeting scheduled today. Kindly be available on time.";
                var mails = new List<Meeting>();
                var query = "select InterviewMeetings.id, InterviewMeetings.interviewer from InterviewMeetings where interviewDate<=@timeH and interviewDate>=@timeL and confirmationStatus=1";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@timeL", DateTime.Now);
                dynamicParameters.Add("@timeH", DateTime.Now.AddMinutes(35));
                using (SqlConnection con = new SqlConnection(connectionString))
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
                        _ = await _notificationHelper.SendMail(masg, null, mails.FirstOrDefault().id);
                        log.LogInformation("EmailtoInterviewer : Interview schedule mail sent to interviewer.");


                    }
                    catch (Exception)
                    {
                        log.LogInformation($"EmailtoInterviewer : Failed to send mail. Please check all filled and try again.");
                    }
                }

            }
            catch (Exception)
            {
                log.LogInformation($"Failed to send mail. Please check all filled and try again.");

            }

            log.LogInformation($"EmailtoInterviewer : C# Timer trigger function executed at: {DateTime.Now}");

        }
    }

    public class Meeting
    {
        public int id { get; set; }
        public string interviewer { get; set; }

    }
}
