using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Risebird.Repository.Interface;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;

namespace Risebird.Repository
{
    public class RisebirdMeetingsRepository : IRisebirdMeetingsRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        public RisebirdMeetingsRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        public bool AddOrUpdateMeeting(DateTime interviewDate, string interviewDuration, string candidateName, string candidateEmail, string resumeLink,
            string zoomLink, string IRFID, long zoommeetingId, string interviewOnDevice, string textreminder)
        {
            var str = connectionString;
            //string eventType = string.Empty;
            //string clientName = string.Empty;
            //string JobRole = string.Empty;

            //DateTime interviewDate = DateTime.UtcNow;
            //string interviewDuration = string.Empty;

            //string interviewDetails = string.Empty;
            string slot = string.Empty;
            //string primaryskill = string.Empty;

            //string candidateName = string.Empty;
            //string candidateEmail = string.Empty;
            string hrRiseBirdAssinged = string.Empty;

            //string resumeLink = string.Empty;
            string comfirmationStatus = "1";

            //string zoomLink = string.Empty;

            //string interviewer = string.Empty;
            //string eventGeneratedOn = string.Empty;
            //string eventGeneratedBy = string.Empty;
            string ClientHrContact = string.Empty;
            //string isReschedule = string.Empty;

            //string IRFID = string.Empty;
            string prefferedLocation = string.Empty;
            string expectedPerHourRate = string.Empty;
            string staffingAgenecy = string.Empty;
            string joiningTime = string.Empty;
            //string interviewOnDevice = string.Empty;
            string currentEmployerName = string.Empty;
            //string textreminder = string.Empty;
            //long zoommeetingId = 0;
            int interviewId = 0;
            var interviewDatestr = interviewDate.Hour;
            if (interviewDatestr < 12)
            {
                slot = "1";
            }
            else
            {
                slot = "2";
            }
            using (SqlConnection conn = new SqlConnection(str))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "InsertMeetings", conn);

                // 2. set the command object so it knows
                // to execute a stored procedure
                cmd.CommandType = CommandType.StoredProcedure;

                // 3. add parameter to command, which
                // will be passed to the stored procedure
                cmd.Parameters.Add(new SqlParameter("@IRFId", IRFID));//
                cmd.Parameters.Add(new SqlParameter("@interviewDate", interviewDate));//
                cmd.Parameters.Add(new SqlParameter("@slot", slot));//
                cmd.Parameters.Add(new SqlParameter("@interviewDuration", Convert.ToInt32(interviewDuration)));//
                cmd.Parameters.Add(new SqlParameter("@candidateName", candidateName));//11/19
                cmd.Parameters.Add(new SqlParameter("@candidateEmail", candidateEmail));//
                cmd.Parameters.Add(new SqlParameter("@hrRiseBirdAssinged", hrRiseBirdAssinged));
                cmd.Parameters.Add(new SqlParameter("@resumeLink", resumeLink));//
                cmd.Parameters.Add(new SqlParameter("@zoomLink", zoomLink));//
                cmd.Parameters.Add(new SqlParameter("@ClientHrContact", ClientHrContact));
                cmd.Parameters.Add(new SqlParameter("@comfirmationStatus", comfirmationStatus));//
                cmd.Parameters.Add(new SqlParameter("@prefferedLocation", prefferedLocation));
                cmd.Parameters.Add(new SqlParameter("@expectedPerHourRate", expectedPerHourRate));
                cmd.Parameters.Add(new SqlParameter("@staffingAgenecy", staffingAgenecy));
                cmd.Parameters.Add(new SqlParameter("@joiningTime", joiningTime));
                cmd.Parameters.Add(new SqlParameter("@interviewOnDevice", interviewOnDevice));//
                cmd.Parameters.Add(new SqlParameter("@currentEmployerName", currentEmployerName));
                cmd.Parameters.Add(new SqlParameter("@textreminder", textreminder));
                cmd.Parameters.Add(new SqlParameter("@zoomMeetingId", zoommeetingId));//
                SqlParameter outPutVal = new SqlParameter("@interviewIdOutput", SqlDbType.Int);
                outPutVal.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(outPutVal);
                try
                {
                    cmd.ExecuteScalar();
                    interviewId = (int)cmd.Parameters["@interviewIdOutput"].Value;
                }
                catch (Exception ex)
                {
                    return false;
                }
                //log.Info("Interview update in database ended.");
            }
            if (!string.IsNullOrEmpty(IRFID) && comfirmationStatus == "1")
            {
                try
                {
                    var masg = new SendGridMessage();
                    using (SqlConnection conn = new SqlConnection(str))
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand(
                            "usp_ListOfIRFInterviwer", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@IRFID", IRFID));
                        var reader = cmd.ExecuteReader();
                        masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("notification@risebird.io", "Risebird"));
                        while (reader.Read())
                        {
                            masg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(reader.GetString(0), null));
                        }
                        masg.Subject = "New Interview has been scheduled on date: " + interviewDate;

                        StringBuilder msgBody = new StringBuilder("Dear Interviewer,");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine("You have received this communication as an interview has been scheduled based on your matching skills on date: " + interviewDate + " .");
                        msgBody.AppendLine(
                            "Kindly login over risebird portal and click on Open Interview menu and assign yourself.");
                        msgBody.Append("\n");
                        msgBody.AppendLine("Note: It is first come first assign.");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine("Thank You, Team Risebird");

                        masg.PlainTextContent = msgBody.ToString();
                        CommonSendMail _common = new CommonSendMail();
                        _common.SendMail(masg);
                        //log.Info("Interview scheduled mail sent to interviewer.");
                    }
                }
                catch (Exception)
                {
                    //log.Error($"Failed to send mail. Please check all filled and try again.");
                }
                try
                {
                    var masg = new SendGridMessage();
                    using (SqlConnection conn = new SqlConnection(str))
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand(
                            "usp_RisebirdHRs", conn);
                        cmd.CommandType = CommandType.StoredProcedure;

                        var reader = cmd.ExecuteReader();
                        masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("notification@risebird.io", "Risebird"));
                        while (reader.Read())
                        {
                            masg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(reader.GetString(0), null));
                        }
                        masg.Subject = "New Interview has been scheduled on date: " + interviewDate;

                        StringBuilder msgBody = new StringBuilder("Dear Hr,");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine("You have received this communication as an interview has been scheduled on date: " + interviewDate + " for your co-ordinatation.");
                        msgBody.AppendLine(
                            "Kindly login over risebird portal and click on Open Interview menu and assign yourself.");
                        msgBody.Append("\n");
                        msgBody.AppendLine("Note: It is first come first assign.");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine("Thank You, Team Risebird");

                        masg.PlainTextContent = msgBody.ToString();
                        CommonSendMail _common = new CommonSendMail();
                        _common.SendMail(masg);
                        //log.Info("Interview scheduled mail sent to HR.");
                    }
                }
                catch (Exception)
                {
                    //log.Error($"Failed to send mail. Please check all filled and try again.");
                }
                try
                {
                    var masg = new SendGridMessage();
                    using (SqlConnection conn = new SqlConnection(str))
                    {
                        conn.Open();
                        //SqlCommand cmd = new SqlCommand("usp_ListOfIRFClients", conn);
                        SqlCommand cmd = new SqlCommand("usp_GetCandidateStaffingAgencyAndClientHRByInterviewId", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        //cmd.Parameters.Add(new SqlParameter("@IRFID", IRFID));
                        cmd.Parameters.Add(new SqlParameter("@interviewId", interviewId));
                        var reader = cmd.ExecuteReader();
                        //masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("notification@risebird.io", "Risebird"));
                        while (reader.Read())
                        {
                            masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(reader.GetString(0), null));
                        }

                        string jdTitle = string.Empty;
                        SqlCommand cmdJD = new SqlCommand("usp_GetIRFJobDescription", conn);
                        cmdJD.CommandType = CommandType.StoredProcedure;
                        cmdJD.Parameters.Add(new SqlParameter("@irfId", IRFID));
                        var readerJD = cmdJD.ExecuteReader();
                        while (readerJD.Read())
                        {
                            jdTitle = readerJD.GetString(0);
                        }
                        jdTitle = jdTitle?.Split('|')[0];
                        masg.Subject = "New Interview Scheduled for the date : " + interviewDate;

                        object[] parameters = new object[] { interviewDate, candidateName, candidateEmail, jdTitle };
                        string rawEmail = GetBlobData("mailtemplate", "InterviewScheduledNotificationToClientsTemplate.html");
                        string msgBody = string.Format(rawEmail, parameters);

                        masg.HtmlContent = msgBody;

                        CommonSendMail _common = new CommonSendMail();
                        _common.SendMail(masg);
                        //log.Info("Interview scheduled mail sent to clients (ClientHR and staffing agency).");
                    }
                }
                catch (Exception)
                {
                    //log.Error($"Failed to send mail. Please check all filled and try again.");
                }
            }
            else if (!string.IsNullOrEmpty(IRFID) && comfirmationStatus == "2")
            {
                try
                {
                    var masg = new SendGridMessage();
                    using (SqlConnection conn = new SqlConnection(str))
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand(
                            "usp_ListOfCancelledInterviewInterviwer", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@candidateEmail", candidateEmail));
                        cmd.Parameters.Add(new SqlParameter("@interviewDate", interviewDate));
                        cmd.Parameters.Add(new SqlParameter("@IRFID", IRFID));
                        var reader = cmd.ExecuteReader();
                        masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("notification@risebird.io", "Risebird"));
                        while (reader.Read())
                        {
                            masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(reader.GetString(0), null));
                        }
                        masg.Subject = "Interview has been Cancelled on date: " + interviewDate;

                        StringBuilder msgBody = new StringBuilder("Dear Interviewer,");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine("You have received this communication as an interview has been cancelled on interviewdate: " + interviewDate + " .");
                        msgBody.AppendLine(
                            "Thanks for your support, we will keep updating you about future interview based on your matching skill.");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine("Thank You, Team Risebird");

                        masg.PlainTextContent = msgBody.ToString();
                        CommonSendMail _common = new CommonSendMail();
                        _common.SendMail(masg);
                        //log.Info("Interview cancelled mail sent to interviewer.");
                    }
                }
                catch (Exception)
                {
                    //log.Error($"Failed to send mail. Please check all filled and try again.");
                }
            }
            return true;
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
    public class CommonSendMail
    {
        private readonly string _sendGridClientKey = "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";
        public string SendMail(SendGridMessage msg)
        {
            try
            {
                var mailFrom = "notification@risebird.io";
                msg.From = new SendGrid.Helpers.Mail.EmailAddress(mailFrom, "Risebird Notification");
                var client = new SendGridClient(_sendGridClientKey);

                var response = client.SendEmailAsync(msg).Result;
                if (response.StatusCode != HttpStatusCode.Accepted)
                {
                    return "Failed to send mail. Please check all fields and try again.";

                }
                return "Mail sent successfully.";
            }
            catch (Exception ex)
            {
                return "Failed to send mail. Please check all fields and try again.";
            }
        }
    }
}
