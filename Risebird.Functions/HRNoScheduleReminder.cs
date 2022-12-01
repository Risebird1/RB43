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
    public static class HRNoScheduleReminder
    {
        [FunctionName("HRNoScheduleReminder")]
        public static void Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                //var msg = "Dear Interviewer! Greetings from RiseBird. This is a gentle reminder for your upcoming video based interview meeting scheduled today. Kindly be available on time.";
                //var phoneNumbers = new List<string>();
                var candidates = new List<NoScheduleCandidate>();
                var unfilteredCandidate = new List<NoScheduleCandidate>();
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                var query = "select IRF.id,IRF.IRFId,IRF.EndDate, IRF.JDTiltle,Client.[Name] as Client,Candidate.[Name] as CandidateName, Candidate.EmailId, Candidate.Phone from Candidate join IRF on Candidate.IRFId= IRF.id join Client on Client.id=IRF.ClientId where Candidate.TotalInvites!=0";

                using (SqlConnection con = new SqlConnection(str))
                {
                    unfilteredCandidate = con.Query<NoScheduleCandidate>(query).ToList();
                }
                unfilteredCandidate = unfilteredCandidate.Where(x => x.EndDate > DateTime.Now).ToList();
                foreach (var item in unfilteredCandidate)
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
                    var irfList = new List<int>();

                    irfList = candidates.Select(x => x.id).ToList();
                    irfList = irfList.Distinct().ToList();
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
                        if (hrMailList == null || hrMailList.Count() == 0)
                        {
                            continue;
                        }
                        if (hrMailList != null)
                        {
                            foreach (var candidate in candidates.Where(x => x.id == irf))
                            {
                                candidateEmails.Add(candidate.CandidateName + "|" + candidate.EmailId);
                            }
                            //candidateEmails = candidates.Where(x => x.id == irf).Select(x => x.EmailId).ToList();
                        }
                        if (candidateEmails == null || candidateEmails.Count() == 0)
                        {
                            continue;
                        }
                        var client = candidates.Where(x => x.id == irf).FirstOrDefault().Client;
                        var primarySkill = (candidates.Where(x => x.id == irf).FirstOrDefault().JDTiltle).Split('|')[0];
                        //SendMailToHR(hrMailList, candidateEmails, candidates.Where(x=> x.id ==irf).FirstOrDefault().IRFId, client, primarySkill);
                        log.Info($"HrNoSchedule Timer trigger function executed for IRFId: {irf}");

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

                masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird"));
                foreach (var hrEmail in hrEmails)
                {
                    if (!string.IsNullOrEmpty(hrEmail))
                    {
                        masg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(hrEmail, null));
                    }
                }
                DataTable dt = new DataTable();
                dt.Columns.AddRange(new DataColumn[2] { new DataColumn("Name", typeof(string)),
                    new DataColumn("Email", typeof(string)) });
                foreach (var candidate in candidates)
                {
                    dt.Rows.Add(candidate.Split('|')[0], candidate.Split('|')[1]);
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




                //StringBuilder candidateBody = new StringBuilder();
                //foreach (var candidate in candidates)
                //{
                //    candidateBody.AppendLine(candidate);
                //    candidateBody.Append("\n");
                //}

                object[] parameters = new object[] { irfName, client, primarySkill, sb };
                masg.Subject = "Schedule Pending Alert!";
                string rawEmail = GetBlobData("mailtemplate", "HRNoScheduleMailTemplate.html");
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

    //public class NoScheduleCandidate
    //{
    //    public int id { get; set; }
    //    public string IRFId { get; set; }
    //    public string EmailId { get; set; }
    //    public string CandidateName { get; set; }
    //    public string Phone { get; set; }
    //    public string JDTiltle { get; set; }
    //    public string Client { get; set; }
    //    public DateTime EndDate { get; set; }
    //}
}
