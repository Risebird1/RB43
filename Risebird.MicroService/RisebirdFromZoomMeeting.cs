using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class RisebirdFromZoomMeeting
    {
        public RisebirdFromZoomMeeting()
        {

        }

        [FunctionName("RisebirdFromZoomMeeting")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation(requestBody);
            string eventType = string.Empty;
            string UserName = string.Empty;
            string Emailid = string.Empty;
            string Meetingid = string.Empty;
            DateTime ActionDate = new DateTime();
            string ActionDatejoin = string.Empty;
            string ActionDateleft = string.Empty;
            dynamic stuff = Newtonsoft.Json.JsonConvert.DeserializeObject(requestBody);
            var children = stuff.Children().GetEnumerator();
            while (children.MoveNext())
            {
                var mainDetails = children.Current;
                if (((JProperty)mainDetails).Name == "event")
                {
                    eventType = ((JProperty)mainDetails).Value.ToString().Split('.').Last().ToUpper();
                }
                else if (((JProperty)mainDetails).Name == "payload")
                {
                    foreach (var data in mainDetails.Children())
                    {
                        var internalNodes = data.Children().GetEnumerator();
                        while (internalNodes.MoveNext())
                        {
                            var mainFamilies1 = internalNodes.Current;
                            if (((Newtonsoft.Json.Linq.JProperty)((JToken)mainFamilies1)).Name == "object")
                            {
                                Meetingid = ((JProperty)mainFamilies1).Value["id"].ToString();
                                var currentmainDetails = internalNodes.Current;
                                foreach (var internernodeParticipent in currentmainDetails.Children())
                                {
                                    var internalNodes1 = internernodeParticipent.Children().GetEnumerator();
                                    while (internalNodes1.MoveNext())
                                    {
                                        var mainFamilies2 = internalNodes1.Current;
                                        if (((Newtonsoft.Json.Linq.JProperty)mainFamilies2).Name == "participant")
                                        {
                                            UserName = ((JProperty)mainFamilies2).Value["user_name"].ToString();
                                            Emailid = ((JProperty)mainFamilies2).Value["email"].ToString();
                                            var datemainDetails = internalNodes1.Current;
                                            foreach (var finddate in datemainDetails.Children())
                                            {
                                                var internalNodes2 = finddate.Children().GetEnumerator();
                                                while (internalNodes2.MoveNext())
                                                {
                                                    var mainFamilies3 = internalNodes2.Current;
                                                    if (((Newtonsoft.Json.Linq.JProperty)mainFamilies3).Name == "leave_time")
                                                    {
                                                        ActionDateleft = ((JProperty)mainFamilies2).Value["leave_time"].ToString();
                                                    }
                                                    else if (((Newtonsoft.Json.Linq.JProperty)mainFamilies3).Name == "join_time")
                                                    {
                                                        ActionDatejoin = ((JProperty)mainFamilies2).Value["join_time"].ToString();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (eventType == "PARTICIPANT_LEFT" || eventType == "PARTICIPANT_JOINED")
            {
                try
                {
                    if (eventType == "PARTICIPANT_JOINED")
                    {
                        ActionDate = Convert.ToDateTime(ActionDatejoin);
                    }
                    else
                    {
                        ActionDate = Convert.ToDateTime(ActionDateleft);
                    }
                    ActionDate = TimeZoneInfo.ConvertTimeFromUtc(ActionDate, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                    string connectionString = "Server=tcp:risebirdprod.database.windows.net,1433;Initial Catalog=productProdDB;Persist Security Info=False;User ID=risebirdadmin;Password=Ri$£bird@567;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        string sqlProcReport = "INSERT INTO ZoomMeetingTransmission Values (@eventType, @UserName, @Meetingid,@Emailid,@ActionDate);";
                        var result3 = conn.Execute(sqlProcReport, new
                        {
                            eventType,
                            UserName,
                            Meetingid,
                            Emailid,
                            ActionDate
                        });
                    }
                }
                catch (Exception)
                {

                }
            }
            return new OkObjectResult("Success");
        }
    }
}
