using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Risebird.Functions
{
    public static class HRScheduledTodayReminder
    {
        [FunctionName("HRScheduledTodayReminder")]
        public static void Run([TimerTrigger("0 0 0 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var unfilteredMeetings = new List<ScheduledCandidate>();
                var query = "select InterviewMeetings.*, IRF.id as IRFintId from InterviewMeetings join IRF on InterviewMeetings.IRFId=IRF.IRFId  where confirmationStatus=1";
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                using (SqlConnection con = new SqlConnection(str))
                {
                    unfilteredMeetings = con.Query<ScheduledCandidate>(query).ToList();
                }
                if(unfilteredMeetings!=null)
                {
                    unfilteredMeetings = unfilteredMeetings.Where(x => x.interviewDate.Date == DateTime.Today.Date).ToList();
                    var irfList = new List<int>();
                    irfList = unfilteredMeetings.Select(x => x.IRFintId).ToList();
                    irfList.Distinct();
                    if (irfList != null)
                    {
                        foreach (var irf in irfList)
                        {
                            var hrMailList = new List<string>();
                            var candidateEmails = new List<string>();
                            var checkQuery = "select UserList.EmailId from  TransClientUser join UserList on TransClientUser.ClientUserId = UserList.Id where TransClientUser.IRFId=@IRFId and ClientRoleId in (1,4)";
                            var dynamicParameters = new DynamicParameters();
                            dynamicParameters.Add("@IRFId", irf);
                            using (SqlConnection con = new SqlConnection(str))
                            {
                                hrMailList = con.Query<string>(checkQuery, dynamicParameters).ToList();
                            }
                            if(hrMailList==null || hrMailList.Count()==0)
                            {
                                continue;
                            }
                            foreach (var item in unfilteredMeetings.Where(x => x.IRFintId == irf))
                            {
                                candidateEmails.Add(item.candidateEmail + " - " + item.interviewDate.ToString("dd MMMM yyyy hh:mm:ss tt"));
                            }
                            if(candidateEmails==null || candidateEmails.Count()==0)
                            {
                                continue;
                            }
                            //var now = (DateTime.Now);
                            //string asString = now.ToString("dd MMMM yyyy hh:mm:ss tt");
                            SendMailToHR(hrMailList, candidateEmails, unfilteredMeetings.Where(x => x.IRFintId == irf).FirstOrDefault().IRFId);
                            log.Info($"HrTodayScheduled Timer trigger function executed for IRFId: {irf}");

                        }
                    }
                }
                
            }
            catch (Exception)
            {
                log.Error($"Failed to send email. Please check all filled and try again.");
            }
        }

        public static void SendMailToHR(List<string> hrEmails, List<string> candidates, string irfName)
        {
            try
            {
                var mailFrom = "hire@risebird.io";
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                mailMessage.To.Add(new MailAddress(mailFrom, "Risebird"));
                foreach (var hrEmail in hrEmails)
                {
                    mailMessage.Bcc.Add(new MailAddress(hrEmail, null));
                }
                StringBuilder candidateBody = new StringBuilder();
                foreach (var candidate in candidates)
                {
                    candidateBody.AppendLine(candidate);
                    candidateBody.Append("\n");
                }

                object[] parameters = new object[] { irfName, candidateBody };
                mailMessage.Subject = "Interview scheduled for taday " + DateTime.Now.ToString("dd MMMM yyyy");
                string rawEmail = GetBlobData("mailtemplate", "HRScheduledTodayReminder.html");
                string msgBody = string.Format(rawEmail, parameters);

                mailMessage.IsBodyHtml = true;
                mailMessage.Body = msgBody;
                SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential("azure_9119f7b456399652d5a99faa61a6fbe7@azure.com", "rise_123");
                smtpClient.Credentials = credentials;

                smtpClient.Send(mailMessage);
            }
            catch (Exception e)
            {
                return;

            }
        }
        public static string GetBlobData(string containerName, string fileName)
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
        public static CloudBlobContainer GetCloudBlobContainer(string containerName)
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

    public class ScheduledCandidate
    {
        public int id { get; set; }
        public int IRFintId { get; set; }
        public string IRFId { get; set; }
        public string candidateEmail{ get; set; }
        public DateTime interviewDate { get; set; }
    }
}
