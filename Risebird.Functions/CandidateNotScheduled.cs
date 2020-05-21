using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Risebird.Functions
{
    public static class CandidateNotScheduled
    {
        [FunctionName("CandidateNotScheduled")]
        public static void Run([TimerTrigger("0 0 19 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var candidates = new List<NotScheduledCandidate>(); // will contain the candidates whose interviews arent scheduled.
                var unfilteredCandidates = new List<NotScheduledCandidate>();
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                var query = "select IRF.id,IRF.IRFId,IRF.EndDate, IRF.JDTiltle,Client.[Name] as Client, Candidate.EmailId, Candidate.Phone from Candidate join IRF on Candidate.IRFId= IRF.id join Client on Client.id=IRF.ClientId where Candidate.TotalInvites!=0";

                using (SqlConnection con = new SqlConnection(str))
                {
                    unfilteredCandidates = con.Query<NotScheduledCandidate>(query).ToList();
                }
                unfilteredCandidates = unfilteredCandidates.Where(x => x.EndDate > DateTime.Now).ToList();
                foreach (var item in unfilteredCandidates)
                {
                    int checkScheduleCount = 0;
                    var checkQuery = "select id from InterviewMeetings where IRFId=@IRFId and candidateEmail = @EmailId and confirmationStatus!=2";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@IRFId", item.IRFId);
                    dynamicParameters.Add("@EmailId", item.EmailId);
                    using (SqlConnection con = new SqlConnection(str))
                    {
                        var scheduleResult = con.Query<int>(checkQuery, dynamicParameters).ToList();
                        checkScheduleCount = scheduleResult.Count();
                    }
                    if (checkScheduleCount == 0)
                    {
                        candidates.Add(item);
                    }
                }
                if (candidates != null && candidates.Count() != 0)
                {
                    var candidateMailList = new List<string>();
                    candidateMailList = candidates.Select(x => x.EmailId).Distinct().ToList();
                    foreach (var candidateMail in candidateMailList)
                    {

                        //send mail to each candidate (pass all the irfs for which he has not scheduled any interview despite sending invitation)
                        var finalList = candidates.Where(x => x.EmailId == candidateMail).ToList();
                        SendMailToCandidate(candidateMail, finalList);

                    }

                    //call each candidate

                    var phoneNumbers = candidates.Select(x => x.Phone).Distinct();
                    
                    if (phoneNumbers != null && phoneNumbers.Count() > 0)
                    {
                        var twilioAuthToken = "3f15940e73243adb8e75f9689adaa0e2";
                        var twilioAccountSID = "ACcad9f6682d4a76df9af7cde78dd0867e";
                        var twilioVoiceCallNumber = "+17274782239";

                        Parallel.ForEach(phoneNumbers, phoneNumber =>
                        {
                            if (!string.IsNullOrEmpty(phoneNumber))
                            {
                                try
                                {
                                    TwilioClient.Init(twilioAccountSID, twilioAuthToken);
                                    var to = new PhoneNumber(phoneNumber);
                                    var from = new PhoneNumber(twilioVoiceCallNumber);
                                    var call = CallResource.CreateAsync(
                                        to: to,
                                        from: from,
                                        url: new Uri("https://beige-vulture-6005.twil.io/assets/CandidateScheduleReminder.xml"));
                                    log.Info($"Voice call Candidate Timer trigger function executed for: {phoneNumber}");
                                }
                                catch (Exception)
                                {
                                    log.Error($"Failed to make call. Please check all filled and try again.");
                                }
                            }

                        });
                    }


                }

            }
            catch (Exception ex)
            {
                log.Error($"Failed.");
            }
        }

        public static void SendMailToCandidate(string candidateEmail, List<NotScheduledCandidate> irfDetails)
        {
            try
            {
                var mailFrom = "notification@risebird.io";
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailFrom, "Risebird Notification");
                mailMessage.To.Add(new MailAddress(mailFrom, "Risebird"));
                mailMessage.Bcc.Add(new MailAddress(candidateEmail, null));


                DataTable dt = new DataTable();
                dt.Columns.AddRange(new DataColumn[3] { new DataColumn("Skill Channel", typeof(string)),
                    new DataColumn("Primary Skill", typeof(string)), new DataColumn("Client", typeof(string)) });
                foreach (var irf in irfDetails)
                {
                    dt.Rows.Add(irf.IRFId, irf.JDTiltle.Split('|')[0], irf.Client);
                }

                StringBuilder sb = new StringBuilder();
                //Table start.
                sb.Append("<table cellpadding='5' cellspacing='0' style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial'>");

                //Adding HeaderRow.
                sb.Append("<tr>");
                foreach (DataColumn column in dt.Columns)
                {
                    sb.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc; width:200px'>" + column.ColumnName + "</th>");
                }
                sb.Append("</tr>");


                //Adding DataRow.
                foreach (DataRow row in dt.Rows)
                {
                    sb.Append("<tr>");
                    foreach (DataColumn column in dt.Columns)
                    {
                        sb.Append("<td style='width:100px;border: 1px solid #ccc'>" + row[column.ColumnName].ToString() + "</td>");
                    }
                    sb.Append("</tr>");
                }

                //Table end.
                sb.Append("</table>");


                object[] parameters = new object[] { sb };
                mailMessage.Subject = "Schedule Pending Alert!";
                string rawEmail = GetBlobData("mailtemplate", "CandidateNotScheduledMailTemplate.html");
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

    public class NotScheduledCandidate
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string EmailId { get; set; }
        public string CandidateName { get; set; }
        public string Phone { get; set; }
        public string JDTiltle { get; set; }
        public string Client { get; set; }
        public DateTime EndDate { get; set; }
    }

}
