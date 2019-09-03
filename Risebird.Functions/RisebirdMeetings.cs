using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Risebird.Functions
{
    public static class RisebirdMeetings
    {
        [FunctionName("RisebirdMeeting")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
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
            string slot = string.Empty;
            string primaryskill = string.Empty;
            string candidateName = string.Empty;
            string candidateEmail = string.Empty;
            string hrRiseBirdAssinged = string.Empty;
            string resumeLink = string.Empty;
            string comfirmationStatus = "1";
            string zoomLink = string.Empty;
            string zoomDetails = string.Empty;
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
                                
                                zoomLink = ((JProperty)mainFamilies1).Value["location"].ToString();
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
                                textreminder= ((JProperty)mainFamilies1).Value["text_reminder_number"].ToString();
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
                                        .Contains("per hour rate"))
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
                cmd.ExecuteScalar();

                log.Info("Interview update in database ended.");
            }
            if (!string.IsNullOrEmpty(IRFID) && comfirmationStatus == "1")
            {
                try
                {
                    var mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress("hire@risebird.io", "Risebird Notification");
                    using (SqlConnection conn = new SqlConnection(str))
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand(
                            "usp_ListOfIRFInterviwer", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@IRFID", IRFID));
                        var reader = cmd.ExecuteReader();
                        mailMessage.To.Add(new MailAddress("hire@risebird.io", "Risebird"));
                        while (reader.Read())
                        {
                            mailMessage.Bcc.Add(new MailAddress(reader.GetString(0), null));
                        }
                        mailMessage.Subject = "New Interview has been scheduled on date: " + interviewDate;

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

                        mailMessage.Body = msgBody.ToString();
                        SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                        System.Net.NetworkCredential credentials = new System.Net.NetworkCredential("azure_9119f7b456399652d5a99faa61a6fbe7@azure.com", "rise_123");
                        smtpClient.Credentials = credentials;

                        smtpClient.Send(mailMessage);
                        log.Info("Interview schedule mail sent to interviewer.");
                    }
                }
                catch (Exception)
                {
                    log.Error($"Failed to send mail. Please check all filled and try again.");
                }
            }
            else if (!string.IsNullOrEmpty(IRFID) && comfirmationStatus == "2")
            {
                try
                {
                    var mailMessage = new MailMessage();
                    mailMessage.From = new MailAddress("hire@risebird.io", "Risebird Notification");
                    using (SqlConnection conn = new SqlConnection(str))
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand(
                            "usp_ListOfCancelledInterviewInterviwer", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add(new SqlParameter("@candidateEmail", candidateEmail));
                        cmd.Parameters.Add(new SqlParameter("@interviewDate", interviewDate));
                        var reader = cmd.ExecuteReader();
                        mailMessage.To.Add(new MailAddress("hire@risebird.io", "Risebird"));
                        while (reader.Read())
                        {
                            mailMessage.Bcc.Add(new MailAddress(reader.GetString(0), null));
                        }
                        mailMessage.Subject = "Interview has been Cancelled on date: " + interviewDate;

                        StringBuilder msgBody = new StringBuilder("Dear Interviewer,");
                        msgBody.Append("\n");
                        msgBody.Append("\n");
                        msgBody.AppendLine("You have received this communication as an interview has been cancelled on interviewdate: " + interviewDate + " .");
                        msgBody.AppendLine(
                            "Thanks for your support, we will keep updating you about future interview based on your matching skill.");
                        msgBody.Append("\n");                        
                        msgBody.Append("\n");
                        msgBody.AppendLine("Thank You, Team Risebird");

                        mailMessage.Body = msgBody.ToString();
                        SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                        System.Net.NetworkCredential credentials = new System.Net.NetworkCredential("azure_9119f7b456399652d5a99faa61a6fbe7@azure.com", "rise_123");
                        smtpClient.Credentials = credentials;

                        smtpClient.Send(mailMessage);
                        log.Info("Interview cancelled mail sent to interviewer.");
                    }
                }
                catch (Exception)
                {
                    log.Error($"Failed to send mail. Please check all filled and try again.");
                }
            }
            //  return req.CreateResponse(HttpStatusCode.OK, "Hello " + "Ok");
            return req.CreateResponse(HttpStatusCode.OK, "Hello " + "Ok");
        }
    }
}
