using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Routing;
using System.Text;
using System.Net.Http;

namespace Risebird.MicroService
{
    public class OnlineMeeting
    {
        private readonly AuthenticationService _authenticationService;
        public OnlineMeeting(AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [FunctionName("OnlineMeeting")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            //var onlineMeeting = new OnlineMeeting
            //{
            //    StartDateTime = DateTimeOffset.Parse("2020-07-30T21:30:34.2444915+00:00"),
            //    EndDateTime = DateTimeOffset.Parse("2020-07-30T22:00:34.2464912+00:00"),
            //    Subject = "User Team Meeting"
            //};
            string requestBody = File.ReadAllText(@"c:\prem\meeting.txt", Encoding.UTF8);
           // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation("C# HTTP trigger function processed a request.");

            var httpClient = await _authenticationService.CreateAuthorizedHttpClient();
           
            var requestUrl = "https://graph.microsoft.com/v1.0/me/onlineMeetings";
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);
          
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            httpRequest.Content = content;
            var response = await httpClient.SendAsync(httpRequest);
            var result = await response.Content.ReadAsStringAsync();
            return new OkObjectResult(result);
        }
    }
}
