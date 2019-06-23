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
           // string jsonString =  File.ReadAllText(@"c:\prem\json.txt", Encoding.UTF8);
            dynamic stuff = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonContent);

            string eventType = string.Empty;
            string clientName = string.Empty;
            string JobRole = string.Empty;
            string interviewDate = string.Empty;
            string interviewDuration = string.Empty;
            string slot = string.Empty;
            string primaryskill = string.Empty;
            string candidateEmail = string.Empty;
            string hrRiseBirdAssinged = string.Empty;
            string resumeLink = string.Empty;
            string comfirmationStatus = string.Empty;
            string zoomLink = string.Empty;
            string zoomDetails = string.Empty;
            string interviewer = string.Empty;
            string eventGeneratedOn = string.Empty;
            string eventGeneratedBy = string.Empty;
            string ClientHrContact = string.Empty;
            var children = stuff.Children().GetEnumerator();
            while (children.MoveNext())
            {
                var mainDetails = children.Current;
                if (((JProperty) mainDetails).Name == "event")
                {
                    eventType = ((JProperty) mainDetails).Value.ToString().Split('.').Last()
                        .ToUpper();
                    if (eventType == "CANCELED")
                    {
                        comfirmationStatus = "Cancelled";
                    }
                    else
                    {
                        comfirmationStatus = "Confirmed";
                    }
                }
                else if (((JProperty)mainDetails).Name == "time")
                {
                    eventGeneratedOn = ((JProperty) mainDetails).Value.ToString();
                    eventGeneratedBy = "Canlendly";
                }
                else if (((JProperty)mainDetails).Name == "payload")
                {
                    foreach (var data in mainDetails.Children())
                    {
                        var internalNodes = data.Children().GetEnumerator(); 
                        while (internalNodes.MoveNext())
                        {
                            var mainFamilies1 = internalNodes.Current;
                            if (((JToken) mainFamilies1).Path =="payload.event_type")
                            {
                                primaryskill = ((JProperty) mainFamilies1).Value["name"].ToString();
                                clientName = primaryskill.Split('|').First();
                                interviewDuration = ((JProperty)mainFamilies1).Value["duration"].ToString()+"Mins";
                            }
                           else if (((JToken)mainFamilies1).Path == "payload.event")
                            {
                                hrRiseBirdAssinged = ((JProperty)mainFamilies1).Value["assigned_to"].ToString();
                                hrRiseBirdAssinged = hrRiseBirdAssinged.Replace("\n", "").Replace("\"", "").Replace("[", "").Replace("]", "").Replace("\r", "");
                               
                                interviewDate = ((JProperty)mainFamilies1).Value["start_time_pretty"].ToString();
                                if (interviewDate.Contains("AM"))
                                {
                                    slot = "Morning";
                                }
                                else
                                {
                                    slot = "Evening";
                                }
                                zoomLink = ((JProperty)mainFamilies1).Value["location"].ToString();
                            }
                            else if (((JToken)mainFamilies1).Path == "payload.invitee")
                            {
                                candidateEmail = ((JProperty)mainFamilies1).Value["email"].ToString();
                            }
                            else if (((JToken)mainFamilies1).Path == "payload.questions_and_answers")
                            {
                                foreach (var questions in ((JProperty)mainFamilies1).First.Children())
                                {
                                    if (((JContainer) questions)["question"].ToString()
                                        .Contains("Your Linkedin or Resume Link"))
                                    {
                                        resumeLink = ((JContainer) questions)["answer"].ToString();
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

                //cmd.Parameters.Add(new SqlParameter("@eventType", eventType));
                cmd.Parameters.Add(new SqlParameter("@clientName", clientName));
                cmd.Parameters.Add(new SqlParameter("@JobRole", JobRole));
                cmd.Parameters.Add(new SqlParameter("@interviewDate", interviewDate));
                cmd.Parameters.Add(new SqlParameter("@interviewDuration", interviewDuration));
                cmd.Parameters.Add(new SqlParameter("@slot", slot));
                cmd.Parameters.Add(new SqlParameter("@primaryskill", primaryskill));
                cmd.Parameters.Add(new SqlParameter("@candidateEmail", candidateEmail));
                cmd.Parameters.Add(new SqlParameter("@hrRiseBirdAssinged", hrRiseBirdAssinged));
                cmd.Parameters.Add(new SqlParameter("@resumeLink", resumeLink));
                cmd.Parameters.Add(new SqlParameter("@zoomLink", zoomLink));
                cmd.Parameters.Add(new SqlParameter("@eventGeneratedOn", eventGeneratedOn));
                cmd.Parameters.Add(new SqlParameter("@eventGeneratedBy", eventGeneratedBy));
                cmd.Parameters.Add(new SqlParameter("@ClientHrContact", ClientHrContact));
                cmd.Parameters.Add(new SqlParameter("@comfirmationStatus", comfirmationStatus));
                
                cmd.ExecuteScalar();
              

                log.Info("Build update in database ended.");
            }

            return req.CreateResponse(HttpStatusCode.OK, "Hello " + "Ok"); 
            
        }
    }
}
