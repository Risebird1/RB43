using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SendGrid.Helpers.Mail;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Data.CommandType;

namespace Risebird.Functions
{
    public static class RisebirdMeetings
    {
        [FunctionName("RisebirdMeeting")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            string jsonContent = await req.Content.ReadAsStringAsync();
            log.Info(jsonContent);
            //string jsonString = File.ReadAllText(@"c:\prem\json.txt", Encoding.UTF8);
            dynamic stuff = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonContent);
            //dynamic stuff = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
            string eventType = string.Empty;
            string clientName = string.Empty;
            string JobRole = string.Empty;
            DateTime interviewDate = DateTime.UtcNow;
            string interviewDuration = string.Empty;
            string interviewDetails = string.Empty;
            string slot = string.Empty;
            string primaryskill = string.Empty;
            string candidateName = string.Empty;
            string candidateEmail = string.Empty;
            string hrRiseBirdAssinged = string.Empty;
            string resumeLink = string.Empty;
            string comfirmationStatus = "1";
            string zoomLink = string.Empty;
            string interviewer = string.Empty;
            string eventGeneratedOn = string.Empty;
            string eventGeneratedBy = string.Empty;
            string ClientHrContact = string.Empty;
            string isReschedule = string.Empty;
            string IRFID = string.Empty;
            string prefferedLocation = string.Empty;
            string expectedPerHourRate = string.Empty;
            string staffingAgenecy = string.Empty;
            string joiningTime = string.Empty;
            string interviewOnDevice = string.Empty;
            string currentEmployerName = string.Empty;
            string textreminder = string.Empty;
            //long zoommeetingId = 0;
            string zoommeetingId = null;
            int interviewId = 0;
            var children = stuff.Children().GetEnumerator();
            while (children.MoveNext())
            {
                var mainDetails = children.Current;
                if (((JProperty)mainDetails).Name == "event")
                {
                    eventType = ((JProperty)mainDetails).Value.ToString().Split('.').Last()
                        .ToUpper();
                    if (eventType == "CANCELED")
                    {
                        comfirmationStatus = "2";
                    }
                    else
                    {
                        comfirmationStatus = "1";
                    }
                }
                //else if (((JProperty)mainDetails).Name == "time")
                //{
                //    eventGeneratedOn = ((JProperty) mainDetails).Value.ToString();
                //    eventGeneratedBy = "Canlendly";
                //}
                else if (((JProperty)mainDetails).Name == "payload")
                {
                    foreach (var data in mainDetails.Children())
                    {
                        var internalNodes = data.Children().GetEnumerator();
                        while (internalNodes.MoveNext())
                        {
                            var mainFamilies1 = internalNodes.Current;
                            if (((JToken)mainFamilies1).Path == "payload.event_type")
                            {
                                interviewDuration = ((JProperty)mainFamilies1).Value["duration"].ToString();
                                interviewDetails = ((JProperty)mainFamilies1).Value["name"].ToString();

                            }
                            else if (((JToken)mainFamilies1).Path == "payload.event")
                            {
                                var rbhrassigned = ((JProperty)mainFamilies1).Value["extended_assigned_to"].ToString();
                                hrRiseBirdAssinged = JsonConvert.DeserializeObject<JArray>(rbhrassigned)[0]["email"]
                                    .Value<string>();

                                var interviewDatestr = ((JProperty)mainFamilies1).Value["start_time"].ToString();
                                if (interviewDatestr.Contains("AM"))
                                {
                                    slot = "1";
                                }
                                else
                                {
                                    slot = "2";
                                }

                                interviewDate = Convert.ToDateTime(interviewDatestr);
                                if (interviewDate < DateTime.Now)
                                {
                                    return req.CreateResponse(HttpStatusCode.OK, "Past date Interview Update " + "Ok");
                                }
                                zoomLink = ((JProperty)mainFamilies1).Value["location"].ToString();
                                if (!string.IsNullOrEmpty(zoomLink))
                                {
                                    try
                                    {
                                        //zoommeetingId = Convert.ToInt64(zoomLink.Split('/').Last());
                                        zoommeetingId = zoomLink.Split('/').Last();
                                    }
                                    catch (Exception)
                                    {
                                        try
                                        {
                                            var link = zoomLink.Split('?').First();
                                            //zoommeetingId = Convert.ToInt64(link.Split('/').Last());
                                            zoommeetingId = link.Split('/').Last();
                                        }
                                        catch (Exception)
                                        {
                                            //zoommeetingId = 0;
                                            zoommeetingId = null;
                                        }

                                    }

                                }
                            }
                            else if (((JToken)mainFamilies1).Path == "payload.invitee")
                            {
                                try
                                {
                                    candidateName = ((JProperty)mainFamilies1).Value["name"].ToString();
                                }
                                catch (Exception)
                                {
                                }

                                candidateEmail = ((JProperty)mainFamilies1).Value["email"].ToString();
                                textreminder = ((JProperty)mainFamilies1).Value["text_reminder_number"].ToString();
                                isReschedule = ((JProperty)mainFamilies1).Value["is_reschedule"].ToString();
                            }
                            else if (((JToken)mainFamilies1).Path == "payload.questions_and_answers")
                            {
                                foreach (var questions in ((JProperty)mainFamilies1).First.Children())
                                {
                                    if (((JContainer)questions)["question"].ToString().ToLower()
                                        .Contains("resume link"))
                                    {
                                        resumeLink = ((JContainer)questions)["answer"].ToString();
                                    }
                                    //else if (((JContainer)questions)["question"].ToString()
                                    //    .Contains("Title of JD you are applying for"))
                                    //{
                                    //    JobRole = ((JContainer)questions)["answer"].ToString();
                                    //}
                                    else if (((JContainer)questions)["question"].ToString().ToLower()
                                        .Contains("level you are applying for"))
                                    {
                                        JobRole = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString().ToLower()
                                        .Contains("hr coordinator"))
                                    {
                                        ClientHrContact = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString().ToLower()
                                        .Contains("interview channel id"))
                                    {
                                        IRFID = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString().ToLower()
                                        .Contains("preferred location"))
                                    {
                                        prefferedLocation = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString().ToLower()
                                        .Contains("current employer name"))
                                    {
                                        currentEmployerName = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString().ToLower()
                                        .Contains("expected salary"))
                                    {
                                        expectedPerHourRate = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString().ToLower()
                                        .Contains("staffing agency"))
                                    {
                                        staffingAgenecy = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString().ToLower()
                                        .Contains("notice period"))
                                    {
                                        joiningTime = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString().ToLower()
                                        .Contains("infra for the video interview"))
                                    {
                                        interviewOnDevice = ((JContainer)questions)["answer"].ToString();
                                    }
                                }

                            }
                        }
                    }
                }
            }
            var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;

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
                cmd.Parameters.Add(new SqlParameter("@IRFId", IRFID));
                cmd.Parameters.Add(new SqlParameter("@interviewDate", interviewDate));
                cmd.Parameters.Add(new SqlParameter("@slot", slot));
                cmd.Parameters.Add(new SqlParameter("@interviewDuration", Convert.ToInt32(interviewDuration)));
                cmd.Parameters.Add(new SqlParameter("@candidateName", candidateName));
                cmd.Parameters.Add(new SqlParameter("@candidateEmail", candidateEmail));
                cmd.Parameters.Add(new SqlParameter("@hrRiseBirdAssinged", hrRiseBirdAssinged));
                cmd.Parameters.Add(new SqlParameter("@resumeLink", resumeLink));
                cmd.Parameters.Add(new SqlParameter("@zoomLink", zoomLink));
                cmd.Parameters.Add(new SqlParameter("@ClientHrContact", ClientHrContact));
                cmd.Parameters.Add(new SqlParameter("@comfirmationStatus", comfirmationStatus));
                cmd.Parameters.Add(new SqlParameter("@prefferedLocation", prefferedLocation));
                cmd.Parameters.Add(new SqlParameter("@expectedPerHourRate", expectedPerHourRate));
                cmd.Parameters.Add(new SqlParameter("@staffingAgenecy", staffingAgenecy));
                cmd.Parameters.Add(new SqlParameter("@joiningTime", joiningTime));
                cmd.Parameters.Add(new SqlParameter("@interviewOnDevice", interviewOnDevice));
                cmd.Parameters.Add(new SqlParameter("@currentEmployerName", currentEmployerName));
                cmd.Parameters.Add(new SqlParameter("@textreminder", textreminder));
                cmd.Parameters.Add(new SqlParameter("@zoomMeetingId", zoommeetingId));
                SqlParameter outPutVal = new SqlParameter("@interviewIdOutput", SqlDbType.Int);
                outPutVal.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(outPutVal);


                cmd.ExecuteScalar();
                interviewId = (int)cmd.Parameters["@interviewIdOutput"].Value;

                log.Info("Interview update in database ended.");
            }
            if (!string.IsNullOrEmpty(IRFID) && comfirmationStatus == "1")
            {
                //try
                //{
                //    var masg = new SendGridMessage();
                //    using (SqlConnection conn = new SqlConnection(str))
                //    {
                //        conn.Open();
                //        SqlCommand cmd = new SqlCommand(
                //            "usp_ListOfIRFInterviwer", conn);
                //        cmd.CommandType = CommandType.StoredProcedure;
                //        cmd.Parameters.Add(new SqlParameter("@IRFID", IRFID));
                //        var reader = cmd.ExecuteReader();
                //        masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("notification@risebird.io", "Risebird"));
                //        while (reader.Read())
                //        {
                //            masg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(reader.GetString(0), null));
                //        }
                //        masg.Subject = "New Interview has been scheduled on date: " + interviewDate;

                //        StringBuilder msgBody = new StringBuilder("Dear Interviewer,");
                //        msgBody.Append("\n");
                //        msgBody.Append("\n");
                //        msgBody.AppendLine("You have received this communication as an interview has been scheduled based on your matching skills on date: " + interviewDate + " .");
                //        msgBody.AppendLine(
                //            "Kindly login over risebird portal and click on Open Interview menu and assign yourself.");
                //        msgBody.Append("\n");
                //        msgBody.AppendLine("Note: It is first come first assign.");
                //        msgBody.Append("\n");
                //        msgBody.Append("\n");
                //        msgBody.AppendLine("Thank You, Team Risebird");

                //        masg.PlainTextContent = msgBody.ToString();
                //        CommonSendMail _common = new CommonSendMail();
                //        _common.SendMail(masg);
                //        log.Info("Interview scheduled mail sent to interviewer.");
                //    }
                //}
                //catch (Exception)
                //{
                //    log.Error($"Failed to send mail. Please check all filled and try again.");
                //}
                //try
                //{
                //    var masg = new SendGridMessage();
                //    using (SqlConnection conn = new SqlConnection(str))
                //    {
                //        conn.Open();
                //        SqlCommand cmd = new SqlCommand(
                //            "usp_RisebirdHRs", conn);
                //        cmd.CommandType = CommandType.StoredProcedure;

                //        var reader = cmd.ExecuteReader();
                //        masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("notification@risebird.io", "Risebird"));
                //        while (reader.Read())
                //        {
                //            masg.AddBcc(new SendGrid.Helpers.Mail.EmailAddress(reader.GetString(0), null));
                //        }
                //        masg.Subject = "New Interview has been scheduled on date: " + interviewDate;

                //        StringBuilder msgBody = new StringBuilder("Dear Hr,");
                //        msgBody.Append("\n");
                //        msgBody.Append("\n");
                //        msgBody.AppendLine("You have received this communication as an interview has been scheduled on date: " + interviewDate + " for your co-ordinatation.");
                //        msgBody.AppendLine(
                //            "Kindly login over risebird portal and click on Open Interview menu and assign yourself.");
                //        msgBody.Append("\n");
                //        msgBody.AppendLine("Note: It is first come first assign.");
                //        msgBody.Append("\n");
                //        msgBody.Append("\n");
                //        msgBody.AppendLine("Thank You, Team Risebird");

                //        masg.PlainTextContent = msgBody.ToString();
                //        CommonSendMail _common = new CommonSendMail();
                //        _common.SendMail(masg);
                //        log.Info("Interview scheduled mail sent to HR.");
                //    }
                //}
                //catch (Exception)
                //{
                //    log.Error($"Failed to send mail. Please check all filled and try again.");
                //}
                //try
                //{
                //    var masg = new SendGridMessage();
                //    using (SqlConnection conn = new SqlConnection(str))
                //    {
                //        conn.Open();
                //        //SqlCommand cmd = new SqlCommand("usp_ListOfIRFClients", conn);
                //        SqlCommand cmd = new SqlCommand("usp_GetCandidateStaffingAgencyAndClientHRByInterviewId", conn);
                //        cmd.CommandType = CommandType.StoredProcedure;
                //        //cmd.Parameters.Add(new SqlParameter("@IRFID", IRFID));
                //        cmd.Parameters.Add(new SqlParameter("@interviewId", interviewId));
                //        var reader = cmd.ExecuteReader();
                //        //masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress("notification@risebird.io", "Risebird"));
                //        while (reader.Read())
                //        {
                //            masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(reader.GetString(0), null));
                //        }

                //        string jdTitle = string.Empty;
                //        SqlCommand cmdJD = new SqlCommand("usp_GetIRFJobDescription", conn);
                //        cmdJD.CommandType = CommandType.StoredProcedure;
                //        cmdJD.Parameters.Add(new SqlParameter("@irfId", IRFID));
                //        var readerJD = cmdJD.ExecuteReader();
                //        while (readerJD.Read())
                //        {
                //            jdTitle = readerJD.GetString(0);
                //        }
                //        jdTitle = jdTitle?.Split('|')[0];
                //        masg.Subject = "New Interview Scheduled for the date : " + interviewDate;

                //        object[] parameters = new object[] { interviewDate, candidateName, candidateEmail, jdTitle };
                //        string rawEmail = GetBlobData("mailtemplate", "InterviewScheduledNotificationToClientsTemplate.html");
                //        string msgBody = string.Format(rawEmail, parameters);

                //        masg.HtmlContent = msgBody;

                //        CommonSendMail _common = new CommonSendMail();
                //        _common.SendMail(masg);
                //        log.Info("Interview scheduled mail sent to clients (ClientHR and staffing agency).");
                //    }
                //}
                //catch (Exception)
                //{
                //    log.Error($"Failed to send mail. Please check all filled and try again.");
                //}
            }
            else if (!string.IsNullOrEmpty(IRFID) && comfirmationStatus == "2")
            {
                try
                {
                    var query = "Update InterviewMeetings set Cancellationsource='Calendly' where candidateEmail = @candidateEmail and interviewDate=@interviewDate and IRFId= @IRFId";
                    var dynamicParameters = new DynamicParameters();
                    dynamicParameters.Add("@candidateEmail", candidateEmail);
                    dynamicParameters.Add("@IRFId", IRFID);
                    dynamicParameters.Add("@interviewDate", interviewDate);
                    using (SqlConnection con = new SqlConnection(str))
                    {
                        var results = con.Query(query, dynamicParameters);
                    }
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
                        log.Info("Interview cancelled mail sent to interviewer.");
                    }
                }
                catch (Exception)
                {
                    log.Error($"Failed to send mail. Please check all filled and try again.");
                }
                //try
                //{
                //    var mailMessage = new MailMessage();
                //    mailMessage.From = new MailAddress("notification@risebird.io", "Interview Cancellation Alert");
                //    using (SqlConnection conn = new SqlConnection(str))
                //    {
                //        conn.Open();
                //        SqlCommand cmd = new SqlCommand(
                //            "usp_ListOfIRFClientHRs", conn);
                //        cmd.CommandType = CommandType.StoredProcedure;
                //        cmd.Parameters.Add(new SqlParameter("@IRFID", IRFID));
                //        var reader = cmd.ExecuteReader();
                //        mailMessage.To.Add(new MailAddress("notification@risebird.io", "Risebird"));
                //        while (reader.Read())
                //        {
                //            mailMessage.CC.Add(new MailAddress(reader.GetString(0), null));
                //        }
                //        mailMessage.Subject = candidateName + " has cancelled their interview on date: " + interviewDate;

                //        StringBuilder msgBody = new StringBuilder("Dear Clients,");
                //        msgBody.Append("\n");
                //        msgBody.Append("\n");
                //        msgBody.AppendLine(candidateName + " has cancelled their interview on date: " + interviewDate + " . Details are ");
                //        msgBody.Append("\n");
                //        msgBody.AppendLine("Candidate Email: " + candidateEmail);
                //        msgBody.Append("\n");
                //        msgBody.AppendLine("Interview Details: " + interviewDetails);
                //        msgBody.Append("\n");
                //        msgBody.Append("\n");
                //        msgBody.AppendLine("Thank You, Team Risebird");

                //        mailMessage.Body = msgBody.ToString();
                //        SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                //        System.Net.NetworkCredential credentials = new System.Net.NetworkCredential("azure_9119f7b456399652d5a99faa61a6fbe7@azure.com", "rise_123");
                //        smtpClient.Credentials = credentials;

                //        smtpClient.Send(mailMessage);
                //        log.Info("Interview schedule mail sent to interviewer.");
                //    }
                //}
                //catch (Exception)
                //{
                //    log.Error($"Failed to send mail. Please check all filled and try again.");
                //}
            }
            //  return req.CreateResponse(HttpStatusCode.OK, "Hello " + "Ok");
            RecruiterStatusCheck(interviewId, candidateEmail, IRFID, interviewDate);
            return req.CreateResponse(HttpStatusCode.OK, "Hello " + "Ok");
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
        public static void RecruiterStatusCheck(int interviewid, string CandidateEmailid, string IRFName, DateTime interviewDate)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;
            bool flag;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                DynamicParameters dp = new DynamicParameters();
                dp.Add("@interviewid", interviewid);
                var result = SqlMapper.Query<bool>(
                    con, "Select tc.IsActive from interviewmeetings im join IRF on IRF.IRFId=im.IRFId Left join UserList ul on ul.EmailId=im.ClientHrContact left join TransClientUser tc on tc.ClientUserId=ul.id and tc.IRFId=IRF.id where im.id=@interviewid", dp, commandType: Text);
                flag = result.FirstOrDefault();
            }
            if (!flag)
            {
                var query = "update [dbo].[InterviewMeetings] set confirmationStatus=@value where id = @meetingid";
                var dynamicParameters = new DynamicParameters();
                dynamicParameters.Add("@meetingid", interviewid);
                dynamicParameters.Add("@value", 2);
                var query1 = "Update InterviewMeetings set isAutoCancelled=1,Cancellationsource='Recruiter Disabled' where id=@meetingid and confirmationStatus=2";
                var dynamicParameters1 = new DynamicParameters();
                dynamicParameters1.Add("@meetingid", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var resultset = con.Query(query, dynamicParameters);
                    var results = con.Query(query1, dynamicParameters1);
                }
                var candidateId = 0;
                var dynamicParameterscm = new DynamicParameters();
                dynamicParameterscm.Add("@emailId", CandidateEmailid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var resultid = SqlMapper.Query<int>(con, "usp_GetCandidateIdByEmail", dynamicParameterscm,
                       commandType: CommandType.StoredProcedure);
                    candidateId = resultid.FirstOrDefault();
                }
                var irfid = 0;
                var queryirf = "Select id from IRF where IRFId=@IRFId";
                var dynamicParametersirf = new DynamicParameters();
                dynamicParameters.Add("@IRFId", IRFName);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var results = con.Query<int>(queryirf, dynamicParametersirf);
                    irfid = results.FirstOrDefault();
                }
                //Start:add Candidte Status
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@IRFId", irfid); // Normal parametersmeters           
                parameters.Add("@CandidateId", candidateId);
                parameters.Add("@Comments", "Recruiter Disabled");
                parameters.Add("@Date", DateTime.Now);
                parameters.Add("@AudioFileLink", "");
                parameters.Add("@Status", 18);
                parameters.Add("@PerformedBy", "System");
                parameters.Add("@InterviewId", interviewid);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    SqlMapper.Query(con, "usp_AddCandidateStatus", parameters,
                        commandType: CommandType.StoredProcedure);
                }
                //End:add Candidte Status
                var masg = new SendGridMessage();
                masg.AddTo(new SendGrid.Helpers.Mail.EmailAddress(CandidateEmailid, null));
                masg.Subject = "Interview has been Cancelled on date: " + interviewDate;

                StringBuilder msgBody = new StringBuilder("Dear Candidate,");
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
