using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using static System.Data.CommandType;

namespace Risebird.Repository
{
    public class RisebirdMeetingsRepository : IRisebirdMeetingsRepository
    {
        private readonly IConfiguration _configuration;
        private readonly string connectionString;
        private readonly INotificationHelper _notificationHelper;
        private readonly IIRFRepository _irfRepository;
        private readonly IMeetingRespository _meetingRespository;
        private readonly ICandidateRepository _candidateRepository;

        public RisebirdMeetingsRepository(IConfiguration configuration, INotificationHelper notificationHelper, IIRFRepository irfRepository, IMeetingRespository meetingRespository, ICandidateRepository candidateRepository)
        {
            _configuration = configuration;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
            _notificationHelper = notificationHelper;
            _irfRepository = irfRepository;
            _meetingRespository = meetingRespository;
            _candidateRepository = candidateRepository;
        }
        public bool AddOrUpdateMeeting(DateTime interviewDate, string interviewDuration, string candidateName, string candidateEmail, string resumeLink,
            string zoomLink, string IRFID, string zoommeetingId, string interviewOnDevice, string textreminder, string staffingAgenecy, string ClientHrContact, string zoomAccountEmail = null)
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
            string hrRiseBirdAssinged = string.IsNullOrWhiteSpace(zoomAccountEmail) ? string.Empty : zoomAccountEmail;

            //string resumeLink = string.Empty;
            string comfirmationStatus = "1";

            //string zoomLink = string.Empty;

            //string interviewer = string.Empty;
            //string eventGeneratedOn = string.Empty;
            //string eventGeneratedBy = string.Empty;
            //string ClientHrContact = string.Empty;
            //string isReschedule = string.Empty;

            //string IRFID = string.Empty;
            string prefferedLocation = string.Empty;
            string expectedPerHourRate = string.Empty;
            //string staffingAgenecy = string.Empty;
            if (String.IsNullOrEmpty(staffingAgenecy))
            {
                staffingAgenecy = "";
            }
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
                cmd.Parameters.Add(new SqlParameter("@staffingAgenecy", staffingAgenecy));//
                cmd.Parameters.Add(new SqlParameter("@joiningTime", joiningTime));
                cmd.Parameters.Add(new SqlParameter("@interviewOnDevice", interviewOnDevice));//
                cmd.Parameters.Add(new SqlParameter("@currentEmployerName", currentEmployerName));
                cmd.Parameters.Add(new SqlParameter("@textreminder", textreminder));//
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
                using (SqlConnection con = new SqlConnection(str))
                {
                    var query = "select* from[dbo].[InterviewMeetings] as intw join[dbo].[InterviewComments] as intc on intw.id = intc.interviewid where candidateEmail = @candidateEmail and intc.MeetingStatus = 4 and IRFId= @IRFId";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@candidateEmail", candidateEmail);
                    dynamicParameters.Add("@IRFId", IRFID);
                    var results = con.Query(query, dynamicParameters);
                    var dynamicParameters1 = new DynamicParameters();
                    dynamicParameters1.Add("@meetingid", interviewId);
                    if (results.Count() > 0)
                    {
                        //Cancel Interview
                        var query1 = "Update InterviewMeetings set isAutoCancelled=1,confirmationStatus=2,Cancellationsource='Already Completed Interview' where id=@meetingid";
                        results = con.Query(query1, dynamicParameters1);

                        var masg = new SendGridMessage();
                        masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(candidateEmail, null));
                        masg.AddCc(new SendGrid.Helpers.Mail.EmailAddress(ClientHrContact, null));
                        masg.Subject = "Interview has been Cancelled on date: " + interviewDate;

                        StringBuilder msgBody = new StringBuilder("Dear Candidate,");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine("Your interview has been cancelled on interview date " + interviewDate + ". As you have already completed interview for this particular requirement.");
                        msgBody.AppendLine("Please contact to your HR or Recruiter about your interview.");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine("Thank You, Team Risebird");

                        masg.PlainTextContent = msgBody.ToString();
                        CommonSendMail _common = new CommonSendMail();
                        _common.SendMail(masg);
                    }
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
            RecruiterStatusCheck(interviewId, candidateEmail, IRFID, interviewDate);
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
        public void RecruiterStatusCheck(int interviewid, string CandidateEmailid, string IRFName, DateTime interviewDate)
        {
            bool flag;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@interviewid", interviewid);
                var result = SqlMapper.Query<bool>(
                    con, "Select IsNull(tc.IsActive,1) as IsActive from interviewmeetings im join IRF on IRF.IRFId=im.IRFId Left join UserList ul on ul.EmailId=im.ClientHrContact left join TransClientUser tc on tc.ClientUserId=ul.id and tc.IRFId=IRF.id where im.id=@interviewid", dp, commandType: Text);
                flag = result.FirstOrDefault();
            }
            if (!flag)
            {
                if (_meetingRespository.CancelInterview(interviewid))
                {
                    var setvalue = _meetingRespository.UpdateCancelRecruiterInterviewDetails(interviewid);
                    var candidateId = _irfRepository.GetCandidateIdByEmail(CandidateEmailid).Result;
                    var irfid = _irfRepository.GetIRFIdByIRFName(IRFName);
                    var model = new CandidateStatus();
                    model.CandidateId = candidateId;
                    model.IRFId = irfid;
                    model.AudioFileLink = null;
                    model.Comments = "Recruiter Disabled";
                    model.Status = (int)CandidateStatusOptions.Cancelled;
                    model.PerformedBy = "System";
                    var result1 = _irfRepository.AddCandidateStatus(model);
                    var result = _candidateRepository.FetchChannelOwnerandInterviewer(interviewid).Result;

                    var masg = new SendGridMessage();
                    masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(result.candidateEmail, null));
                    masg.Subject = "Interview has been Cancelled on date: " + interviewDate;

                    StringBuilder msgBody = new StringBuilder("Dear " + result.CandidateName + ",");
                    msgBody.Append("\n");
                    msgBody.Append("\n");
                    msgBody.AppendLine("You have received this communication as an interview has been cancelled on interviewdate: " + interviewDate + " .");
                    msgBody.AppendLine("Please contact to your HR contact or Recruiter to schedule the interview.");
                    msgBody.Append("\n");
                    msgBody.Append("\n");
                    msgBody.AppendLine("Thank You, Team Risebird");

                    masg.PlainTextContent = msgBody.ToString();
                    CommonSendMail _common = new CommonSendMail();
                    _common.SendMail(masg);
                }
            }
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
