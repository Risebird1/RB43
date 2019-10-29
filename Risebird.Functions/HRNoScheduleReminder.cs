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
    public static class HRNoScheduleReminder
    {
        [FunctionName("HRNoScheduleReminder")]
        public static void Run([TimerTrigger("0 0 0 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                //var msg = "Dear Interviewer! Greetings from RiseBird. This is a gentle reminder for your upcoming video based interview meeting scheduled today. Kindly be available on time.";
                //var phoneNumbers = new List<string>();
                var candidates = new List<NoScheduleCandidate>();
                var unfilteredCandidate = new List<NoScheduleCandidate>();
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                var query = "select IRF.id,IRF.IRFId,IRF.EndDate, Candidate.EmailId, Candidate.Phone from Candidate join IRF on Candidate.IRFId= IRF.id where Candidate.TotalInvites!=0";
                
                using (SqlConnection con = new SqlConnection(str))
                {
                    unfilteredCandidate = con.Query<NoScheduleCandidate>(query).ToList();
                }
                unfilteredCandidate = unfilteredCandidate.Where(x => x.EndDate > DateTime.Now).ToList();
                foreach (var item in unfilteredCandidate)
                {
                    int checkScheduleCount =0;
                    var checkQuery = "select id from InterviewMeetings where IRFId=@IRFId and candidateEmail = @EmailId and confirmationStatus!=2";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@IRFId", item.IRFId);
                    dynamicParameters.Add("@EmailId", item.EmailId);
                    using (SqlConnection con = new SqlConnection(str))
                    {
                        var scheduleResult = con.Query<int>(checkQuery, dynamicParameters).ToList();
                        checkScheduleCount = scheduleResult.Count();
                    }
                    if(checkScheduleCount==0)
                    {
                        candidates.Add(item);
                    }
                }
               
                if (candidates != null && candidates.Count() != 0)
                {
                    var irfList = new List<int>();
                    irfList = candidates.Select(x => x.id).ToList();
                    irfList.Distinct();
                    foreach (var irf in irfList)
                    {
                        var hrMailList = new List<string>();
                        var candidateEmails= new List<string>();
                        var checkQuery = "select UserList.EmailId from  TransClientUser join UserList on TransClientUser.ClientUserId = UserList.Id where TransClientUser.IRFId=@IRFId and ClientRoleId in (2,4)";
                        var dynamicParameters = new DynamicParameters();
                        dynamicParameters.Add("@IRFId", irf);
                        using (SqlConnection con = new SqlConnection(str))
                        {
                            hrMailList = con.Query<string>(checkQuery, dynamicParameters).ToList();
                        }
                        if (hrMailList == null || hrMailList.Count() == 0)
                        {
                            continue;
                        }
                        if (hrMailList != null)
                        {
                            candidateEmails = candidates.Where(x => x.id == irf).Select(x => x.EmailId).ToList();
                        }
                        if (candidateEmails == null || candidateEmails.Count() == 0)
                        {
                            continue;
                        }
                        SendMailToHR(hrMailList, candidateEmails, candidates.Where(x=> x.id ==irf).FirstOrDefault().IRFId);
                        log.Info($"HrNoSchedule Timer trigger function executed for IRFId: {irf}");

                    }

                }

            }
            catch (Exception)
            {
                log.Error($"Failed to send email. Please check all filled and try again.");
            }

        }

        public static void SendMailToHR(List<string> hrEmails,List<string> candidates, string irfName)
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

                object[] parameters = new object[] { irfName, candidateBody};
                mailMessage.Subject = "Schedule pending!";
                string rawEmail = GetBlobData("mailtemplate", "HRNoScheduleMailTemplate.html");
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

    public class NoScheduleCandidate
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string EmailId { get; set; }
        public string Phone { get; set; }
        public DateTime EndDate { get; set; }
    }
}
