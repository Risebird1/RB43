//using System;
//using System.IdentityModel.Tokens.Jwt;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Host;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Microsoft.IdentityModel.Tokens;
//using Newtonsoft.Json;
//using RestSharp;
//using Risebird.CommonHelper.Interface;
//using Risebird.Entities.Data;
//using Risebird.Repository.Interface;

//namespace Risebird.MicroService
//{
//    public class ZoomStartMeeting
//    {
//        private readonly IConfiguration _configuration;
//        private readonly IApplicationErrorLogger _applicationErrorLogger;
//        private readonly IMeetingRespository _meetingRespository;
//        private readonly IAzureStorageHelper _azureStorageHelper;

//        private readonly INotificationHelper _notificationHelper;

//        private readonly string connectionString;
//        private readonly string ContainerName;
//        private readonly string SASConnectionString;

//        public ZoomStartMeeting(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IAzureStorageHelper azureStorageHelper, IMeetingRespository meetingRespository)
//        {
//            _configuration = configuration;
//            _applicationErrorLogger = applicationErrorLogger;
//            _notificationHelper = notificationHelper;
//            _azureStorageHelper = azureStorageHelper;
//            _meetingRespository = meetingRespository;
//            connectionString = _configuration["ConnectionString:DBConnectionString"];
//            ContainerName = _configuration["SASURLSettings:ContainerName"];
//            SASConnectionString = _configuration["SASURLSettings:ConnectionString"];


//        }
//        [FunctionName("ZoomStartMeeting")]
//        public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer, ILogger log)
//        {
//            //var date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");

//            //get the meeting Id
//            var zoomMeetingId = "82495565803";
//            string token = ZoomToken();


//            var requestBody = new ZoomUpdateMeetingStatus
//            {
//                action="end"
//            };

//            //create HTTP Rest Client
//            var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"/meetings/{zoomMeetingId}/status"));

//            //create HTTP Request 
//            var request = new RestRequest(Method.PUT);
//            request.AddHeader("content-type", "application/json");
//            request.AddHeader("authorization", "Bearer " + token);
//            request.AddParameter("application/json", JsonConvert.SerializeObject(requestBody),
//                    ParameterType.RequestBody);
//            IRestResponse response = client.Execute(request);





//            //var requestBody = new ZoomUpdateMeetingData
//            //{
//            //    timezone = "Asia/Calcutta",
//            //    //start_time = DateTime.UtcNow.AddMinutes(332).ToString("yyyy-MM-dd’T’HH:mm:ss"),
//            //    //start_time = DateTime.UtcNow.AddMinutes(2).ToString("yyyy-MM-dd’T’HH:mm:ss’Z’"),
//            //    start_time = "2021-04-07T15:00:00Z",
//            //    topic = "My Fourth Test Meeting Updated",
//            //    type = 2,
//            //    settings = new ZoomUpdateSettings { join_before_host = true }

//            //};

//            ////create HTTP Rest Client
//            //var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"/meetings/{zoomMeetingId}"));

//            ////create HTTP Request 
//            //var request = new RestRequest(Method.PATCH);
//            //request.AddHeader("content-type", "application/json");
//            //request.AddHeader("authorization", "Bearer " + token);
//            //request.AddParameter("application/json", JsonConvert.SerializeObject(requestBody),
//            //        ParameterType.RequestBody);
//            //IRestResponse response = client.Execute(request);
//            //var content = response.Content;

//            //call the update command

//            log.LogInformation($"C# Zoom Start Video Timer trigger function executed at: {DateTime.Now}");
//        }


//        private string ZoomToken()
//        {
//            // Token will be good for 20 minutes
//            DateTime Expiry = DateTime.UtcNow.AddDays(1);

//            //string ApiKey = "YOUR_API_KEY_HERE";
//            //string ApiSecret = "YOUR_API_SECRET_HERE";

//            string ApiKey = _configuration["ZoomAppDetails:apiKey"];
//            string ApiSecret = _configuration["ZoomAppDetails:secretKey"];

//            int ts = (int)(Expiry - new DateTime(1970, 1, 1)).TotalSeconds;
//            //int ts = 12000;

//            // Create Security key  using private key above:
//            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiSecret));

//            // length should be >256b
//            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

//            //Finally create a Token
//            var header = new JwtHeader(credentials);

//            //Zoom Required Payload
//            var payload = new JwtPayload
//            {
//                { "iss", ApiKey},
//                { "exp", ts },
//            };

//            var secToken = new JwtSecurityToken(header, payload);
//            var handler = new JwtSecurityTokenHandler();

//            // Token to string so you can use it in your client
//            var tokenString = handler.WriteToken(secToken);

//            return tokenString;
//        }

//    }
//}
