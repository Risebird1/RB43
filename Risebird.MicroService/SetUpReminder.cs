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

namespace Risebird.MicroService
{
    public class SetUpReminder
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;


        private readonly string connectionString;
        //private readonly string SASConnectionString;

        public SetUpReminder(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        [FunctionName("SetUpReminder")]
        public async Task Run([TimerTrigger("0 0 */12 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"SetUpReminder : C# Timer trigger function started executing at: {DateTime.Now}");

            try
            {
                //var twilioAuthToken = "3f15940e73243adb8e75f9689adaa0e2";
                //var twilioAccountSID = "ACcad9f6682d4a76df9af7cde78dd0867e";
                // var twilioSMSNumber = "+12055768041";
                var receiverDetails = new List<ReceiverDetails>();
                var query = "select im.id, im.candidateEmail, im.interviewer as InterviewerEmailId, ca.Phone as CandidatePhone, ul.MobileNumber as InterviewerPhone, im.interviewDate, irf.JDTiltle from InterviewMeetings im join UserList ulc on im.candidateEmail = ulc.EmailId join irf on irf.IRFId= im.IRFId join CandidatesMaster cm on ulc.id = cm.UserId join Candidate ca on ca.CandidateMasterId = cm.id and ca.IRFId = irf.id join UserList ul on ul.EmailId = im.interviewer where im.confirmationStatus=1 and im.interviewDate>@timeNow";
                var parameters = new DynamicParameters();
                parameters.Add("@timeNow", DateTime.Now);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    receiverDetails = con.Query<ReceiverDetails>(query, parameters).ToList();
                }
                if (receiverDetails != null)
                {
                    foreach (var item in receiverDetails)
                    {

                        SendMail(item.candidateEmail, item.InteviewerEmailId, item.interviewDate.ToString("dd MMMM yyyy hh:mm:ss tt"), null, item.id);

                        //var phoneNumbers = new List<string>
                        //{
                        //    item.CandidatePhone,
                        //    item.InteviewerPhone
                        //};

                        //var msg = "You have received this communication as a reminder to set up your system prior to your meeting scheduled at " + item.interviewDate.ToString("dd MMMM yyyy hh:mm:ss tt") + " to avoid any last minute hassles.";
                        //if (phoneNumbers != null && phoneNumbers.Count() > 0)
                        //{
                        //    //send an sms to all the interviewers with the message
                        //    var _twilioClient = new TwilioRestClient(twilioAccountSID, twilioAuthToken);
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

                        log.LogInformation($"SetUpReminder Timer trigger function executed for meeting with date: {item.interviewDate}");

                    }

                }

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

            }


            log.LogInformation($"SetUpReminder : C# Timer trigger function finished executing at: {DateTime.Now}");
        }

        private void SendMail(string candidateEmail, string interviewerEmail, string interviewDate, int? irfId = null, int? interviewId = null)
        {
            try
            {
                var mailFrom = "notification@risebird.io";
                var masg = new SendGridMessage();
                masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                masg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(candidateEmail, null));
                masg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(interviewerEmail, null));
                object[] parameters = new object[] { interviewDate };
                masg.Subject = "System set up reminder for your meeting on " + interviewDate;
                string rawEmail = GetBlobData("mailtemplate", "SetUpReminderTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);

                masg.HtmlContent = msgBody;
                
                _ = _notificationHelper.SendMail(masg, irfId, interviewId).Result;
            }
            catch (Exception)
            {
                return;

            }
        }
        private string GetBlobData(string containerName, string fileName)
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
        private CloudBlobContainer GetCloudBlobContainer(string containerName)
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
        private CloudStorageAccount GetStorageAccount()
        {
            StorageCredentials creds = new StorageCredentials("risebirdportal", "2AKmR4SJlNvtr0BY5h7gVJANY7vBcCqph7KWywH9cqLcel/cxT0fKLXyGUVrSj3Xj89HUf2Z0QuWmQ8Zd5SqeQ==");
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);
            return account;
        }
    }
    public class ReceiverDetails
    {
        public string candidateEmail { get; set; }
        public string CandidatePhone { get; set; }
        public string InteviewerEmailId { get; set; }
        public string InteviewerPhone { get; set; }
        public DateTime interviewDate { get; set; }
        public int id { get; set; }
    }
}
