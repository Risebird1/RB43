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
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Risebird.Functions
{
    public static class CandidateNotScheduled
    {
        [FunctionName("CandidateNotScheduled")]
        public static void Run([TimerTrigger("0 0 21 * * *")] TimerInfo myTimer, TraceWriter log)//  0 0 21 * * *
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var candidates = new List<NotScheduledCandidate>(); // will contain the candidates whose interviews arent scheduled.
                var unfilteredCandidates = new List<NotScheduledCandidate>();
                var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
                var query = "select IRF.id,IRF.IRFId,IRF.EndDate, IRF.JDTiltle,Client.[Name] as Client, ul.EmailId, Candidate.Phone, Candidate.InviteDate, irf.PrimarySkillInvite, ul.FirstName as [Name], irf.JDLink from Candidate join CandidatesMaster cm on Candidate.CandidateMasterId=cm.id join UserList ul on cm.UserId = ul.id join IRF on Candidate.IRFId= IRF.id join Client on Client.id=IRF.ClientId where Candidate.TotalInvites!=0 and IRF.IsPaused!= 1";

                using (SqlConnection con = new SqlConnection(str))
                {
                    unfilteredCandidates = con.Query<NotScheduledCandidate>(query).ToList();
                }
                unfilteredCandidates = unfilteredCandidates.Where(x => x.InviteDate != null && x.InviteDate >= DateTime.Now.AddDays(-5)).ToList();
                foreach (var item in unfilteredCandidates)
                {
                    int checkScheduleCount = 0;
                    var checkQuery = "select id from InterviewMeetings where IRFId=@IRFId and candidateEmail = @EmailId and interviewDate > dateadd(dd,-3,getdate())";
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

                    foreach (var candidate in candidates)
                    {

                        SendMailToCandidate(candidate);

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
        public static void SendMailToCandidate(NotScheduledCandidate obj)
        {
            try
            {

                string primarySkill = string.Empty;
                primarySkill = obj.PrimarySkillInvite != null ? obj.PrimarySkillInvite : obj.JDTiltle.Split('|')[0];

                var msg = new SendGridMessage();
                msg.Subject = "Schedule Pending Alert!";

                msg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(obj.EmailId, "Candidate"));

                object[] parameters = new object[] { obj.Name, obj.Client, primarySkill, obj.JDLink };
                string rawEmail = GetBlobData("mailtemplate", "InviteCandidateTemplate.html");
                string msgBody = string.Format(rawEmail, parameters);

                msg.HtmlContent = msgBody;
                CommonSendMail _common = new CommonSendMail();
                _=  _common.SendMail(msg).Result;
                return;
            }
            catch (Exception)
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
        public string Name { get; set; }
        public string Phone { get; set; }
        public string JDTiltle { get; set; }
        public string PrimarySkillInvite { get; set; }
        public string JDLink { get; set; }
        public string Client { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime InviteDate { get; set; }
    }

}
