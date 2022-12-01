using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace Risebird.Functions
{
    public static class TodaysCompletedNotifier
    {
        [FunctionName("TodaysCompletedNotifier")]
        public static void Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var meetings = new List<CompletedMeetings>();
                var query = "select im.*, ic.MeetingStatus,irf.id as IRFintId, irf.JDTiltle,Client.[Name] as Client  from InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId join irf on irf.irfId = im.IRFId join Client on Client.id = irf.ClientId where MeetingStatus in (1,4)";
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                using (SqlConnection con = new SqlConnection(str))
                {
                    meetings = con.Query<CompletedMeetings>(query).ToList();
                }
                if (meetings != null)
                {
                    meetings = meetings.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
                    var irfList = new List<int>();
                    //foreach (var meeting in meetings)
                    //{
                    //    if (irfList != null && irfList.Where(x => x == meeting.IRFintId).Count() == 0)
                    //    {
                    //        irfList.Add(meeting.IRFintId);
                    //    }
                    //}
                    irfList = meetings.Select(x => x.IRFintId).ToList();
                    irfList = irfList.Distinct().ToList();
                    if (irfList != null)
                    {
                        foreach (var irf in irfList)
                        {
                            var hrMailList = new List<string>();
                            var candidates = new List<string>();
                            var checkQuery = "select UserList.EmailId from  TransClientUser join UserList on TransClientUser.ClientUserId = UserList.Id where TransClientUser.IRFId=@IRFId and ClientRoleId in (1,4)";
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
                            foreach (var item in meetings.Where(x => x.IRFintId == irf))
                            {
                                candidates.Add(item.candidateName + "|" + item.candidateEmail + "|" + item.interviewDate.ToString("dd MMMM yyyy hh:mm:ss tt") + "|" + (item.MeetingStatus == 4 ? "Completed" : "No Show"));
                            }
                            if (candidates == null || candidates.Count() == 0)
                            {
                                continue;
                            }
                            //var now = (DateTime.Now);
                            //string asString = now.ToString("dd MMMM yyyy hh:mm:ss tt");
                            var client = meetings.Where(x => x.IRFintId == irf).FirstOrDefault().Client;
                            var primarySkill = meetings.Where(x => x.IRFintId == irf).FirstOrDefault().JDTiltle.Split('|')[0];

                            //SendMailToHR(hrMailList, candidates, meetings.Where(x => x.IRFintId == irf).FirstOrDefault().IRFId, client, primarySkill);
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

        public static void SendMailToHR(List<string> hrEmails, List<string> candidates, string irfName, string client, string primarySkill)
        {
            try
            {
                var mailFrom = "notification@risebird.io";
                var masg = new SendGridMessage();
                masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("notification@risebird.io", "Risebird"));
                foreach (var hrEmail in hrEmails)
                {
                    if (!string.IsNullOrEmpty(hrEmail))
                    {
                        masg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(hrEmail, null));
                    }
                }

                DataTable dt = new DataTable();
                dt.Columns.AddRange(new DataColumn[4] { new DataColumn("Name", typeof(string)),
                    new DataColumn("Email", typeof(string)),
                    new DataColumn("Time", typeof(string)),
                    new DataColumn("Status",typeof(string))   });
                foreach (var candidate in candidates)
                {
                    dt.Rows.Add(candidate.Split('|')[0], candidate.Split('|')[1], candidate.Split('|')[2], candidate.Split('|')[3]);
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

                object[] parameters = new object[] { irfName, client, primarySkill, sb };
                masg.Subject = "Completed Intervew Status for Date " + DateTime.Now.AddDays(-1).ToString("dd MMMM yyyy");
                string rawEmail = GetBlobData("mailtemplate", "TodayCompletedNotification.html");
                string msgBody = string.Format(rawEmail, parameters);

                masg.HtmlContent = msgBody;
                CommonSendMail _common = new CommonSendMail();
                _ = _common.SendMail(masg).Result;
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
    //public class CompletedMeetings
    //{
    //    public int id { get; set; }
    //    public int IRFintId { get; set; }
    //    public string IRFId { get; set; }
    //    public string candidateEmail { get; set; }
    //    public string candidateName { get; set; }
    //    public string JDTiltle { get; set; }
    //    public string Client { get; set; }
    //    public DateTime interviewDate { get; set; }
    //    public int? MeetingStatus { get; set; }
    //}
}
