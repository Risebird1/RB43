using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using SendGrid.Helpers.Mail;
using Twilio.Clients;

namespace Risebird.MicroService
{
    public class UnassignedMeetingsReminder
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;


        private readonly string connectionString;
        //private readonly string SASConnectionString;

        public UnassignedMeetingsReminder(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        [FunctionName("UnassignedMeetingsReminder")]
        public async Task Run([TimerTrigger("0 0 */12 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"UnassignedMeetingsReminder : C# Timer trigger function started executing at: {DateTime.Now}");
            try
            {
                var twilioAuthToken = "3f15940e73243adb8e75f9689adaa0e2";
                var twilioAccountSID = "ACcad9f6682d4a76df9af7cde78dd0867e";
                //  var twilioSMSNumber = "+12055768041";
                var _twilioClient = new TwilioRestClient(twilioAccountSID, twilioAuthToken);

                var meetings = new List<UnassignedMeeting>();
                var query = "select im.id as imid, im.interviewDate, im.IRFId, im.candidateEmail, ul.MobileNumber as InterviewerPhone, ul.EmailId as InterviewerEmailId, irf.JDTiltle from InterviewMeetings im join irf on irf.IRFId=im.IRFId join TransInterviewer ti on ti.IRFId=irf.id join UserList ul on ti.UserId = ul.id where confirmationStatus = 1 and interviewer is null and interviewDate>@timeNow and ti.IsActive=1";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    meetings = con.Query<UnassignedMeeting>(query, dynamicParameters).ToList();
                }
                if (meetings != null)
                {
                    var meetingIds = meetings.Select(x => x.imid);
                    meetingIds = meetingIds.Distinct();
                    if (meetingIds != null)
                    {
                        foreach (var meetingId in meetingIds)
                        {

                            var interviewerMailList = meetings.Where(x => x.imid == meetingId).Select(x => x.InterviewerEmailId);
                            if (interviewerMailList == null || interviewerMailList.Count() == 0)
                            {
                                continue;
                            }
                            var meetingDetails = meetings.Where(x => x.imid == meetingId).FirstOrDefault();
                            var candidateEmail = meetingDetails.candidateEmail;
                            var irfName = meetingDetails.IRFId;
                            var primarySkill = meetingDetails.JDTiltle.Split('|')[0];
                            var interviewDate = meetingDetails.interviewDate.ToString("dd MMMM yyyy hh:mm:ss tt");

                            SendMailToInterviewer(interviewerMailList, irfName, candidateEmail, primarySkill, interviewDate, null, meetingId);

                            //var sb = new StringBuilder();
                            //sb.Append("You have received this communication as a reminder to assign a meeting scheduled for the date " + interviewDate + ".");
                            //sb.AppendLine();
                            //sb.Append("Skills Channel: " + irfName);
                            //sb.AppendLine();
                            //sb.Append("Candidate EmailId: " + candidateEmail);
                            //sb.AppendLine();
                            //sb.Append("Primary Skill: " + primarySkill);
                            //var msg = sb.ToString();

                            //var phoneNumbers = meetings.Where(x => x.imid == meetingId).Select(x => x.InterviewerPhone);
                            //if (phoneNumbers != null && phoneNumbers.Count() > 0)
                            //{
                            //    //send an sms to all the interviewers with the message

                            //    Parallel.ForEach(phoneNumbers, phoneNumber =>
                            //    {
                            //        if (!string.IsNullOrEmpty(phoneNumber))
                            //        {
                            //            if (!string.IsNullOrEmpty(phoneNumber))
                            //                MessageResource.CreateAsync(to: new PhoneNumber(phoneNumber), from: new PhoneNumber(twilioSMSNumber), body: msg, client: _twilioClient);
                            //            log.Info($"SMS Timer trigger function executed for: {phoneNumber}");
                            //        }

                            //    }
                            //    );
                            //}
                            //log.Info($"UnassignedMeetingsReminder Timer trigger function executed for meeting: {meetingId}");

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

            }

            log.LogInformation($"UnassignedMeetingsReminder : C# Timer trigger function finished executing at: {DateTime.Now}");
        }

        public void SendMailToInterviewer(IEnumerable<string> interviewerMailIds, string irfName, string candidateEmail, string primarySkill, string interviewDate, int? irfID = null, int? interviewId = null)
        {
            try
            {
                var mailFrom = "notification@risebird.io";
                var masg = new SendGridMessage();
                masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                foreach (var interviewerMailId in interviewerMailIds)
                {
                    if (!string.IsNullOrEmpty(interviewerMailId))
                    {
                        masg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(interviewerMailId, null));
                    }
                }

                object[] parameters = new object[] { interviewDate, irfName, candidateEmail, primarySkill };
                masg.Subject = "Reminder to assign interview scheduled for " + interviewDate;
                string rawEmail = GetBlobData("mailtemplate", "UnassignedMeetingReminderTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);

                masg.HtmlContent = msgBody;
                
                _ = _notificationHelper.SendMail(masg, irfID, interviewId).Result;
            }
            catch (Exception)
            {
                return;

            }
        }
        public string GetBlobData(string containerName, string fileName)
        {
            try
            {
                CloudBlobContainer blobContainer = GetCloudBlobContainer(containerName);
                CloudBlob blob = blobContainer.GetBlobReference(fileName);
                string contents; /* content to append */
                if (!blob.ExistsAsync().Result)
                {
                    contents = "";
                }
                else
                {
                    using (StreamReader reader = new StreamReader(blob.OpenReadAsync().Result))
                    {
                        contents = reader.ReadToEnd();
                    }
                }
                return contents;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public CloudBlobContainer GetCloudBlobContainer(string containerName)
        {
            CloudStorageAccount account = GetStorageAccount();
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
            if (!blobContainer.ExistsAsync().Result)
            {
                blobContainer.CreateIfNotExistsAsync();
                var permission = new BlobContainerPermissions();
                permission.PublicAccess = BlobContainerPublicAccessType.Container;
                blobContainer.SetPermissionsAsync(permission);
            }
            return blobContainer;
        }
        public static CloudStorageAccount GetStorageAccount()
        {
            StorageCredentials creds = new StorageCredentials("risebirdportal", "2AKmR4SJlNvtr0BY5h7gVJANY7vBcCqph7KWywH9cqLcel/cxT0fKLXyGUVrSj3Xj89HUf2Z0QuWmQ8Zd5SqeQ==");
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);
            return account;
        }
    }
    public class UnassignedMeeting
    {
        public int imid { get; set; }
        public string IRFId { get; set; }
        public string candidateEmail { get; set; }
        public string JDTiltle { get; set; }
        public string InterviewerEmailId { get; set; }
        public string InterviewerPhone { get; set; }
        public DateTime interviewDate { get; set; }
    }
}
