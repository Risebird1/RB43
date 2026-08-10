using Dapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using System.Data.SqlClient;

namespace Risebird.AzureTimerFunctions
{
    public class EmailtoInterviewer
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly ITwilioHelper _twilioHelper;
        private readonly ILogger log;

        private readonly string connectionString;
        //private readonly string SASConnectionString;

        public EmailtoInterviewer(ILoggerFactory loggerFactory, ITwilioHelper twilioHelper, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _twilioHelper = twilioHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            log = loggerFactory.CreateLogger<EmailtoInterviewer>();
        }

        [Function("EmailtoInterviewer")]
        public async Task Run([TimerTrigger("0 0 8,16 * * *")] TimerInfo myTimer)
        {
            log.LogInformation($"EmailtoInterviewer : C# Timer trigger function started executing  at: {DateTime.Now}");


            try
            {
                var mails = new List<Meeting>();
                var query = "select distinct im.id, im.interviewer,ul.uniqueid, ul.MobileNumber from InterviewMeetings im join UserList ul on ul.Emailid=im.interviewer where interviewDate<=@timeH and interviewDate>=@timeL and confirmationStatus=1 and ul.IsActive=1";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@timeL", DateTime.Now);
                dynamicParameters.Add("@timeH", DateTime.Now.Date.AddDays(1));
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    mails = con.Query<Meeting>(query, dynamicParameters).ToList();
                }

                if (mails != null && mails.Count() > 0)
                {
                    foreach (var mail in mails)
                    {
                        if (!string.IsNullOrEmpty(mail.interviewer))
                        {
                            try
                            {
                                Mail _mail = new Mail
                                {
                                    TO = mail.interviewer,
                                    Subject = "Upcoming Risebird Interviews Confirmation Check In - Mandatory"
                                };
                                var InterviewerUrl = "https://portal.risebird.io/Interview/PublicMyInterviewInvite?id=" + mail.uniqueid;
                                var emailsend = await _notificationHelper.SendInerviewerMeetingReminder(_mail, InterviewerUrl, mail.uniqueid);
                                await _twilioHelper.MakeIVRCallForInterviewerMeetingReminder(mail.MobileNumber,mail.id);
                            }
                            catch (Exception ex)
                            {
                                log.LogInformation($"EmailtoInterviewer : Failed to send mail. Please check all filled and try again: " + mail.interviewer + ".");
                            }
                        }
                    }
                    //try
                    //{

                    //    StringBuilder msgBody = new StringBuilder("Dear Interviewer,");
                    //    msgBody.Append("\n");
                    //    msgBody.Append("\n");
                    //    msgBody.AppendLine(msg);
                    //    msgBody.AppendLine(
                    //        "Kindly login over risebird portal and head over to \'MyInterview\' menu to join.");
                    //    msgBody.Append("\n");
                    //    msgBody.AppendLine("Note: Please login over zoom 2 minutes prior to the scheduled time of the interview.");
                    //    msgBody.Append("\n");
                    //    msgBody.Append("\n");
                    //    msgBody.AppendLine("Thank You, Team Risebird");


                    //    //string finalMessageBody = msgBody.ToString();
                    //    var masg = new SendGridMessage();
                    //    masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("talent@risebird.io", null));
                    //    masg.Subject = "Reminder! You have a video based interview meeting coming up in the next 30 minutes.";
                    //    masg.From = new SendGrid.Helpers.Mail.EmailAddress("notification@risebird.io", "Risebird Notification");

                    //    masg.PlainTextContent = msgBody.ToString();

                    //    foreach (var mail in mails)
                    //    {
                    //        if (!string.IsNullOrEmpty(mail.interviewer))
                    //        {
                    //            masg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(mail.interviewer, null));
                    //        }
                    //    }

                    //    _ = await _notificationHelper.SendMail(masg, null, null, null, mails.Select(x => x.id).ToList());

                    //    log.LogInformation("EmailtoInterviewer : Interview schedule mail sent to interviewer.");


                    //}
                    //catch (Exception)
                    //{
                    //    log.LogInformation($"EmailtoInterviewer : Failed to send mail. Please check all filled and try again.");
                    //}
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
        public string uniqueid { get; set; }
        public string MobileNumber { get; set; }
    }
}
