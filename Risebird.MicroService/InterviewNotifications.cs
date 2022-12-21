using Dapper;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class InterviewNotifications
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;


        private readonly string connectionString;
        //private readonly string SASConnectionString;

        public InterviewNotifications(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }

        [FunctionName("InterviewNotifications")]
        public async Task Run([TimerTrigger("0 0 1 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"InterviewNotifications : C# Timer trigger function started executing at: {DateTime.Now}");
            try
            {
                //Not scheduled section
                var noScheduleCandidates = new List<NoScheduleCandidate>();
                var unfilteredCandidate = new List<NoScheduleCandidate>();
                var queryCandidate = "select IRF.id,IRF.IRFId,IRF.EndDate, IRF.JDTiltle,Client.[Name] as Client,Candidate.[Name] as CandidateName, Candidate.EmailId, Candidate.Phone from Candidate join IRF on Candidate.IRFId= IRF.id join Client on Client.id=IRF.ClientId where Candidate.TotalInvites!=0";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var noScheduleCandidatesData = await con.QueryAsync<NoScheduleCandidate>(queryCandidate);
                    unfilteredCandidate = noScheduleCandidatesData.Where(x => x.EndDate > DateTime.Now).ToList();
                }

                foreach (var item in unfilteredCandidate)
                {
                    int checkScheduleCount = 0;
                    var checkQuery = "select id from InterviewMeetings where interviewDate>DATEADD(HOUR,-48, @timeNow) and IRFId=@IRFId and candidateEmail = @EmailId and confirmationStatus!=2";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@IRFId", item.IRFId);
                    dynamicParameters.Add("@EmailId", item.EmailId);
                    dynamicParameters.Add("@timeNow", DateTime.Now);
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var scheduleResult = await con.QueryAsync<int>(checkQuery, dynamicParameters);
                        checkScheduleCount = scheduleResult.Count();
                    }
                    if (checkScheduleCount == 0)
                    {
                        noScheduleCandidates.Add(item);
                    }
                }

                //Scheduled for today section
                var todayMeetings = new List<ScheduledCandidate>();
                var queryToday = "select InterviewMeetings.*, IRF.id as IRFintId, IRF.JDTiltle,Client.[Name] as Client from InterviewMeetings join IRF on InterviewMeetings.IRFId=IRF.IRFId join Client on Client.id=IRF.ClientId  where confirmationStatus=1";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var todayMeetingsdata = await con.QueryAsync<ScheduledCandidate>(queryToday);

                    if (todayMeetingsdata != null)
                    {
                        todayMeetings = todayMeetingsdata.Where(x => x.interviewDate.Date == DateTime.Today.Date).ToList();

                    }
                }

                //Completed yesterday
                var completedMeetings = new List<CompletedMeetings>();
                var queryCompleted = "select im.*, ic.MeetingStatus,irf.id as IRFintId, irf.JDTiltle,Client.[Name] as Client  from InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId join irf on irf.irfId = im.IRFId join Client on Client.id = irf.ClientId where MeetingStatus in (1,4)";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var completedMeetingsdata = await con.QueryAsync<CompletedMeetings>(queryCompleted);

                    if (completedMeetingsdata != null)
                    {
                        completedMeetings = completedMeetingsdata.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
                    }
                }

                //selection status
                var meetingsSelected = new List<MeetingSelectionStatus>();
                var querySelected = "select im.*, ic.MeetingStatus, ic.InterviewStatus,irf.id as IRFintId, irf.JDTiltle,Client.[Name] as Client  from InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId join irf on irf.irfId = im.IRFId join Client on Client.id = irf.ClientId where MeetingStatus in (4)";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    meetingsSelected = (List<MeetingSelectionStatus>)await con.QueryAsync<MeetingSelectionStatus>(querySelected);

                    if (meetingsSelected != null)
                    {
                        var meetings1 = meetingsSelected.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date && x.interviewDate.Hour <= 9).ToList();
                        var meetings2 = meetingsSelected.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-2).Date && x.interviewDate.Hour > 9).ToList();
                        meetings1.AddRange(meetings2);
                        meetingsSelected = meetings1;
                        //meetingsSelected = meetingsSelected.Where(x => x.interviewDate.Date == DateTime.Now.AddDays(-1).Date).ToList();
                    }
                }


                //irf list section
                var irfList = new List<IRF>();
                var queryIRF = "select IRF.id, IRFId, JDTiltle, Client.[Name] as Client  from IRF join Client on Client.id = IRF.ClientId";
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    irfList = con.Query<IRF>(queryIRF).ToList();
                }
                foreach (var irf in irfList)
                {
                    //find No schedule candidates
                    var noScheduleCandidateEmails = new List<string>();
                    if (noScheduleCandidates != null)
                    {
                        foreach (var candidate in noScheduleCandidates.Where(x => x.id == irf.id))
                        {
                            noScheduleCandidateEmails.Add(candidate.CandidateName + "|" + candidate.EmailId);
                        }
                    }


                    //scheduled today Reminder
                    var todayCandidateEmails = new List<string>();

                    if (todayMeetings != null)
                    {
                        foreach (var item in todayMeetings.Where(x => x.IRFintId == irf.id))
                        {
                            todayCandidateEmails.Add(item.candidateName + "|" + item.candidateEmail + "|" + item.interviewDate.ToString("dd MMMM yyyy hh:mm:ss tt"));
                        }
                    }


                    //today completed
                    var completedCandidateEmails = new List<string>();

                    if (completedMeetings != null)
                    {
                        foreach (var item in completedMeetings.Where(x => x.IRFintId == irf.id))
                        {
                            completedCandidateEmails.Add(item.candidateName + "|" + item.candidateEmail + "|" + item.interviewDate.ToString("dd MMMM yyyy hh:mm:ss tt") + "|" + (item.MeetingStatus == 4 ? "Completed" : "No Show"));
                        }
                    }

                    //selection status
                    var selectedCandidatesEmails = new List<string>();
                    if (meetingsSelected != null)
                    {
                        foreach (var item in meetingsSelected.Where(x => x.IRFintId == irf.id && x.InterviewStatus.HasValue))
                        {
                            var selectionStatus = string.Empty;
                            if (item.InterviewStatus == 1)
                            {
                                selectionStatus = "Proxy";
                            }
                            else if (item.InterviewStatus == 2)
                            {
                                selectionStatus = "Selected";
                            }
                            else if (item.InterviewStatus == 3)
                            {
                                selectionStatus = "Rejected";
                            }
                            else
                            {
                                selectionStatus = "Border Line";
                            }
                            selectedCandidatesEmails.Add(item.candidateName + "|" + item.candidateEmail + "|" + item.interviewDate.ToString("dd MMMM yyyy hh:mm:ss tt") + "|" + selectionStatus);
                        }

                    }
                    // Get HR mails
                    var checkQuery = "select UserList.EmailId from  TransClientUser join UserList on TransClientUser.ClientUserId = UserList.Id where TransClientUser.IRFId=@IRFId and ClientRoleId in (1,4)";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@IRFId", irf.id);
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        var hrMailListdata = await con.QueryAsync<string>(checkQuery, dynamicParameters);

                        if (hrMailListdata == null || hrMailListdata.Count() == 0)
                        {
                            continue;
                        }

                        //pass the data to send Email
                        await SendMailToHR(hrMailListdata.ToList(), noScheduleCandidateEmails, todayCandidateEmails, completedCandidateEmails, selectedCandidatesEmails, irf.IRFId, irf.Client, irf.JDTiltle.Split('|')[0], irf.id, null);

                    }
                    log.LogInformation($"Interview notifications Timer trigger function executed for IRFId: {irf.IRFId}");

                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);

            }
            log.LogInformation($"InterviewNotifications : C# Timer trigger function finished executing at: {DateTime.Now}");

        }
        private async Task SendMailToHR(List<string> hrEmails, List<string> noScheduleCandidateEmails, List<string> todayCandidateEmails, List<string> completedCandidateEmails, List<string> selectedCandidatesEmails, string irfName, string client, string primarySkill, int? irfId = null, int? interviewId = null)
        {
            try
            {
                if (noScheduleCandidateEmails.Count() == 0 && todayCandidateEmails.Count() == 0 && completedCandidateEmails.Count() == 0 && selectedCandidatesEmails.Count() == 0)
                {
                    return;
                }
                //var mailList = new List<string>();
                ////if(noScheduleCandidateEmails.Count()=0 )
                //mailList.Add("harshitjajoo95@gmail.com");
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

                //Not scheduled candidates
                StringBuilder sbNotScheduled = new StringBuilder();

                if (noScheduleCandidateEmails != null && noScheduleCandidateEmails.Count() > 0)
                {
                    DataTable dtNotScheduled = new DataTable();
                    dtNotScheduled.Columns.AddRange(new DataColumn[2] { new DataColumn("Name", typeof(string)),
                    new DataColumn("Email", typeof(string)) });
                    foreach (var candidate in noScheduleCandidateEmails)
                    {
                        dtNotScheduled.Rows.Add(candidate.Split('|')[0], candidate.Split('|')[1]);
                    }

                    //Table start.
                    sbNotScheduled.Append("<table cellpadding='5' cellspacing='0' style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial'>");

                    //Adding HeaderRow.
                    sbNotScheduled.Append("<tr>");
                    foreach (DataColumn column in dtNotScheduled.Columns)
                    {
                        sbNotScheduled.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc; width:200px'>" + column.ColumnName + "</th>");
                    }
                    sbNotScheduled.Append("</tr>");


                    //Adding DataRow.
                    foreach (DataRow row in dtNotScheduled.Rows)
                    {
                        sbNotScheduled.Append("<tr>");
                        foreach (DataColumn column in dtNotScheduled.Columns)
                        {
                            sbNotScheduled.Append("<td style='width:100px;border: 1px solid #ccc'>" + row[column.ColumnName].ToString() + "</td>");
                        }
                        sbNotScheduled.Append("</tr>");
                    }

                    //Table end.
                    sbNotScheduled.Append("</table>");
                }
                else
                {
                    sbNotScheduled.Append("No candidates for this IRF who have not scheduled.");
                }

                //scheduled today
                StringBuilder sbToday = new StringBuilder();
                if (todayCandidateEmails != null && todayCandidateEmails.Count() > 0)
                {
                    DataTable dtToday = new DataTable();
                    dtToday.Columns.AddRange(new DataColumn[3] { new DataColumn("Name", typeof(string)),
                    new DataColumn("Email", typeof(string)),
                    new DataColumn("Time",typeof(string))   });
                    foreach (var candidate in todayCandidateEmails)
                    {
                        dtToday.Rows.Add(candidate.Split('|')[0], candidate.Split('|')[1], candidate.Split('|')[2]);
                    }

                    //Table start.
                    sbToday.Append("<table cellpadding='5' cellspacing='0' style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial'>");

                    //Adding HeaderRow.
                    sbToday.Append("<tr>");
                    foreach (DataColumn column in dtToday.Columns)
                    {
                        sbToday.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc; width:200px'>" + column.ColumnName + "</th>");
                    }
                    sbToday.Append("</tr>");


                    //Adding DataRow.
                    foreach (DataRow row in dtToday.Rows)
                    {
                        sbToday.Append("<tr>");
                        foreach (DataColumn column in dtToday.Columns)
                        {
                            sbToday.Append("<td style='width:100px;border: 1px solid #ccc'>" + row[column.ColumnName].ToString() + "</td>");
                        }
                        sbToday.Append("</tr>");
                    }

                    //Table end.
                    sbToday.Append("</table>");
                }
                else
                {
                    sbToday.Append("No interviews scheduled for today.");

                }


                //meetings completed
                StringBuilder sbCompleted = new StringBuilder();
                if (completedCandidateEmails != null && completedCandidateEmails.Count() > 0)
                {
                    DataTable dtCompleted = new DataTable();
                    dtCompleted.Columns.AddRange(new DataColumn[4] { new DataColumn("Name", typeof(string)),
                    new DataColumn("Email", typeof(string)),
                    new DataColumn("Time", typeof(string)),
                    new DataColumn("Status",typeof(string))   });
                    foreach (var candidate in completedCandidateEmails)
                    {
                        dtCompleted.Rows.Add(candidate.Split('|')[0], candidate.Split('|')[1], candidate.Split('|')[2], candidate.Split('|')[3]);
                    }

                    //Table start.
                    sbCompleted.Append("<table cellpadding='5' cellspacing='0' style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial'>");

                    //Adding HeaderRow.
                    sbCompleted.Append("<tr>");
                    foreach (DataColumn column in dtCompleted.Columns)
                    {
                        sbCompleted.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc; width:200px'>" + column.ColumnName + "</th>");
                    }
                    sbCompleted.Append("</tr>");


                    //Adding DataRow.
                    foreach (DataRow row in dtCompleted.Rows)
                    {
                        sbCompleted.Append("<tr>");
                        foreach (DataColumn column in dtCompleted.Columns)
                        {
                            sbCompleted.Append("<td style='width:100px;border: 1px solid #ccc'>" + row[column.ColumnName].ToString() + "</td>");
                        }
                        sbCompleted.Append("</tr>");
                    }

                    //Table end.
                    sbCompleted.Append("</table>");
                }
                else
                {
                    sbCompleted.Append("No interviews were completed yesterday.");

                }

                //selected candidates table
                StringBuilder sbSelected = new StringBuilder();
                if (selectedCandidatesEmails != null && selectedCandidatesEmails.Count() > 0)
                {
                    DataTable dtSelected = new DataTable();
                    dtSelected.Columns.AddRange(new DataColumn[4] { new DataColumn("Name", typeof(string)),
                    new DataColumn("Email", typeof(string)),
                    new DataColumn("Time", typeof(string)),
                    new DataColumn("Status",typeof(string))   });
                    foreach (var candidate in selectedCandidatesEmails)
                    {
                        dtSelected.Rows.Add(candidate.Split('|')[0], candidate.Split('|')[1], candidate.Split('|')[2], candidate.Split('|')[3]);
                    }

                    //Table start.
                    sbSelected.Append("<table cellpadding='5' cellspacing='0' style='border: 1px solid #ccc;font-size: 9pt;font-family:Arial'>");

                    //Adding HeaderRow.
                    sbSelected.Append("<tr>");
                    foreach (DataColumn column in dtSelected.Columns)
                    {
                        sbSelected.Append("<th style='background-color: #B8DBFD;border: 1px solid #ccc; width:200px'>" + column.ColumnName + "</th>");
                    }
                    sbSelected.Append("</tr>");


                    //Adding DataRow.
                    foreach (DataRow row in dtSelected.Rows)
                    {
                        sbSelected.Append("<tr>");
                        foreach (DataColumn column in dtSelected.Columns)
                        {
                            sbSelected.Append("<td style='width:100px;border: 1px solid #ccc'>" + row[column.ColumnName].ToString() + "</td>");
                        }
                        sbSelected.Append("</tr>");
                    }

                    //Table end.
                    sbSelected.Append("</table>");
                }
                else
                {
                    sbSelected.Append("No interviews.");
                }

                object[] parameters = new object[] { irfName, client, primarySkill, sbNotScheduled, sbToday, sbCompleted, sbSelected };
                masg.Subject = "Interview Meeting Status Update.";//+ DateTime.Now.AddDays(-1).ToString("dd MMMM yyyy");
                string rawEmail = await GetBlobData("mailtemplate", "ConsolidatedInterviewNotifications.html");
                string msgBody = string.Format(rawEmail, parameters);

                masg.HtmlContent = msgBody;
                //CommonSendMail _common = new CommonSendMail();
                //_ = _common.SendMail(masg, irfId, interviewId).Result;
                _ = await _notificationHelper.SendMail(masg, irfId, interviewId);

                return;
            }
            catch (Exception e)
            {
                return;

            }
        }
        private async Task<string> GetBlobData(string containerName, string fileName)
        {
            try
            {
                CloudBlobContainer blobContainer = await GetCloudBlobContainer(containerName);
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
        private async Task<CloudBlobContainer> GetCloudBlobContainer(string containerName)
        {
            CloudStorageAccount account = GetStorageAccount();
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
            if (!(await blobContainer.ExistsAsync()))
            {
                await blobContainer.CreateIfNotExistsAsync();
                var permission = new BlobContainerPermissions();
                permission.PublicAccess = BlobContainerPublicAccessType.Container;
                await blobContainer.SetPermissionsAsync(permission);
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

    public class IRF
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string Client { get; set; }
        public string JDTiltle { get; set; }

    }
    public class NoScheduleCandidate
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
    public class ScheduledCandidate
    {
        public int id { get; set; }
        public int IRFintId { get; set; }
        public string IRFId { get; set; }
        public string candidateEmail { get; set; }
        public string candidateName { get; set; }
        public string JDTiltle { get; set; }
        public string Client { get; set; }
        public DateTime interviewDate { get; set; }
    }
    public class CompletedMeetings
    {
        public int id { get; set; }
        public int IRFintId { get; set; }
        public string IRFId { get; set; }
        public string candidateEmail { get; set; }
        public string candidateName { get; set; }
        public string JDTiltle { get; set; }
        public string Client { get; set; }
        public DateTime interviewDate { get; set; }
        public int? MeetingStatus { get; set; }
    }
    public class MeetingSelectionStatus
    {
        public int id { get; set; }
        public int IRFintId { get; set; }
        public string IRFId { get; set; }
        public string candidateEmail { get; set; }
        public string candidateName { get; set; }
        public string JDTiltle { get; set; }
        public string Client { get; set; }
        public DateTime interviewDate { get; set; }
        public int? MeetingStatus { get; set; }
        public int? InterviewStatus { get; set; }
    }
}
