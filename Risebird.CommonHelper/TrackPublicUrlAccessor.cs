
using Azure.Core;
using Azure;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Google.Apis.Auth.OAuth2;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Twilio.Base;
using Microsoft.Azure.ApplicationInsights.Query;
using Risebird.Entities.Data;

namespace Risebird.CommonHelper
{
    public class TrackPublicUrlAccessor : ITrackPublicUrlAccessor
    {
        private TelemetryConfiguration telemetryConfiguration = null;
        private TelemetryClient telemetryClient = null;
        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        public TrackPublicUrlAccessor(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void SendTelemetry(string emailId, string requestedUrl)
        {
            if (!string.IsNullOrEmpty(emailId))
            {
                emailId = emailId.Replace("\"", "");
            }
            if (telemetryConfiguration == null)
            {
                telemetryConfiguration = TelemetryConfiguration.CreateDefault();
                telemetryConfiguration.InstrumentationKey = _configuration["ApplicationInsights:InstrumentationKey"];//"";
            }
            if (telemetryClient == null)
            {
                telemetryClient = new TelemetryClient(telemetryConfiguration);
            }
            Dictionary<string, string> customProperties = new Dictionary<string, string>
            {
                { "emailId", emailId},
                { "requestedUrl", requestedUrl}
            };

            telemetryClient.TrackTrace("User Details", SeverityLevel.Information, customProperties);
        }

        public async Task<IEnumerable<AppInsightsPageViews>> FetchTelemetryData()
        {


            try
            {

                var pageViewsDetails = new List<AppInsightsPageViews>();
                string applicationId = "2ab2c3f0-0a6c-4f1b-929e-76961528de43";
                string key = "y1ua0zvcit5xnmfm43euqhshb4eg4swnd8y6qkun";

                // Create client
                var credentials = new ApiKeyClientCredentials(key);
                var applicationInsightsClient = new ApplicationInsightsDataClient(credentials);

                // Query Application Insights
                var query = $"requests" +
                            $" |join kind= inner pageViews on url" +
                            $" |project  timestamp, name, url, success, user_AuthenticatedId, client_IP, client_City, duration" +
                            $" |where timestamp > ago(24h) and user_AuthenticatedId !has('risebird.io')";

                var response = await applicationInsightsClient.Query.ExecuteWithHttpMessagesAsync(applicationId, query);

                // Parse result 
                if (response.Response.IsSuccessStatusCode)
                {
                    int i = 0;
                    foreach (var row in response.Body.Results)
                    {
                        if(i<1000)
                        {
                            var obj = new AppInsightsPageViews();
                            if (row["timestamp"] != null)
                            {
                                obj.timestamp = Convert.ToString(row["timestamp"]);
                            }
                            if (row["name"] != null)
                            {
                                obj.name = Convert.ToString(row["name"]);

                            }
                            if (row["user_AuthenticatedId"] != null)
                            {
                                obj.user_AuthenticatedId = Convert.ToString(row["user_AuthenticatedId"]);

                            }
                            if (row["url"] != null)
                            {
                                obj.url = Convert.ToString(row["url"]);
                            }
                            if (row["duration"] != null)
                            {
                                obj.duration = Convert.ToString(row["duration"]);

                            }
                            if (row["client_IP"] != null)
                            {
                                obj.client_IP = Convert.ToString(row["client_IP"]);

                            }
                            if (row["client_City"] != null)
                            {
                                obj.client_City = Convert.ToString(row["client_City"]);

                            }
                            if (row["success"] != null)
                            {
                                obj.success = Convert.ToBoolean(row["success"]);
                            }
                            if(obj!=null)
                            {
                                pageViewsDetails.Add(obj);

                            }
                            //Console.WriteLine($"[{row["timestamp"]:s}][{row["resultCode"]}] Failed request to: {row["url"]}");
                        }

                        i++;
                    }
                        
                }

                return pageViewsDetails;



                //string resourceId = "/subscriptions/05d4ebe3-b5c7-4725-b618-8bb535cf2c8c/resourceGroups/RisebirdPortal/providers/microsoft.insights/components/RisebirdportalAppInsight";


                ////var token = new ClientCredential();
                //var creds = new DefaultAzureCredential();
                ////var scopes = new string[1];
                ////scopes[0] = ".default";
                //var reqcontext = new TokenRequestContext(scopes : new string[] { resourceId + "/.default" }, tenantId: "05d4ebe3-b5c7-4725-b618-8bb535cf2c8c");

                //creds.GetToken(reqcontext);


                ////var creds = new ClientCredential
                ////var options = new DefaultAzureCredentialOptions { TenantId = "" };
                ////var client = new LogsQueryClient(new DefaultAzureCredential());
                //var client = new LogsQueryClient(creds);

                ////string tableName = "<table_name>";
                ////string tableName = "requests";
                //Response<LogsQueryResult> results = await client.QueryResourceAsync(
                //    new ResourceIdentifier(resourceId),
                //    $"requests | where name has \"index\"",
                //    new QueryTimeRange(TimeSpan.FromHours(1)));

                //LogsTable resultTable = results.Value.Table;
                //foreach (LogsTableRow row in resultTable.Rows)
                //{
                //    Console.WriteLine($"{row["OperationName"]} {row["ResourceGroup"]}");
                //}

                //foreach (LogsTableColumn columns in resultTable.Columns)
                //{
                //    Console.WriteLine("Name: " + columns.Name + " Type: " + columns.Type);
                //}
            }
            catch (Exception ex)
            {

                throw;
            }

        }
    }
}
