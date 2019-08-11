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
            //log.Info(jsonContent);
            //string jsonString = File.ReadAllText(@"c:\prem\json.txt", Encoding.UTF8);
            dynamic stuff = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonContent);
           // dynamic stuff = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
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

                                var interviewDatestr = ((JProperty)mainFamilies1).Value["invitee_start_time"].ToString();
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
                                isReschedule = ((JProperty)mainFamilies1).Value["is_reschedule"].ToString();
                            }
                            else if (((JToken)mainFamilies1).Path == "payload.questions_and_answers")
                            {
                                foreach (var questions in ((JProperty)mainFamilies1).First.Children())
                                {
                                    if (((JContainer)questions)["question"].ToString()
                                        .Contains("Your Resume Link"))
                                    {
                                        resumeLink = ((JContainer)questions)["answer"].ToString();
                                    }
                                    //else if (((JContainer)questions)["question"].ToString()
                                    //    .Contains("Title of JD you are applying for"))
                                    //{
                                    //    JobRole = ((JContainer)questions)["answer"].ToString();
                                    //}
                                    else if (((JContainer)questions)["question"].ToString()
                                        .Contains("Level you are applying for"))
                                    {
                                        JobRole = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString()
                                        .Contains("HR coordinator"))
                                    {
                                        ClientHrContact = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString()
                                        .Contains("Interview Channel ID"))
                                    {
                                        IRFID = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString()
                                        .Contains("Your preferred location"))
                                    {
                                        prefferedLocation = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString()
                                        .Contains("Per Hour rate"))
                                    {
                                        expectedPerHourRate = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString()
                                        .Contains("staffing agency"))
                                    {
                                        staffingAgenecy = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString()
                                        .Contains("you can join"))
                                    {
                                        joiningTime = ((JContainer)questions)["answer"].ToString();
                                    }
                                    else if (((JContainer)questions)["question"].ToString()
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
                cmd.ExecuteScalar();

                log.Info("Build update in database ended.");
            }

            try
            {
                HttpClient client = new HttpClient();
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                client.BaseAddress = new Uri(ConfigurationManager.AppSettings["PortalBaseUri"].ToString());
                client.DefaultRequestHeaders.ExpectContinue = false;
                //throw new Exception();
                log.Info("Send mail based on IRF matched interviwer.");
                HttpResponseMessage response = await client.GetAsync("/api/Scheduler/IRFBasedInterviewerMail?IRFID=" + IRFID + "&interviewDate=" + interviewDate.ToString()).ConfigureAwait(false);
                var httpresponse = response.Content.ReadAsStringAsync().Result;
                log.Info($"httpsresponce for Auto sIRF Mail send:{httpresponse} ");
            }
            catch (Exception ex)
            {
                log.Info($"Failed to send mail to IRF: " + IRFID + " interviews exception: "+ex.Message);
            }
            //  return req.CreateResponse(HttpStatusCode.OK, "Hello " + "Ok");
            return req.CreateResponse(HttpStatusCode.OK, "Hello " + "Ok");
        }
    }
}
