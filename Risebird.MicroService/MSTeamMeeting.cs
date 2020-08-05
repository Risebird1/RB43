using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.Entities.ViewModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class MSTeamMeeting
    {
        private readonly AuthenticationService _authenticationService;
        public MSTeamMeeting(AuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        [FunctionName("MSTeamMeeting")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "MSTeamMeeting")] HttpRequest req,
            ILogger log)
        {
            var accountEmail = req.Headers["accountEmail"].ToString();
            if (!string.IsNullOrEmpty(accountEmail))
            {
                //string requestBody = File.ReadAllText(@"c:\prem\meeting.txt", Encoding.UTF8);
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation(requestBody);
                OnlineMeeting data = JsonConvert.DeserializeObject<OnlineMeeting>(requestBody);
                log.LogInformation("C# HTTP trigger function processed a request.");

                var httpClient = await _authenticationService.CreateAuthorizedHttpClient();
                var requestUrl = $"https://graph.microsoft.com/v1.0/users/{accountEmail}/calendar/events";
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                httpRequest.Content = content;
                var response = await httpClient.SendAsync(httpRequest);
                var result = await response.Content.ReadAsStringAsync();
                return new OkObjectResult(result);
            }
            return new BadRequestResult();
        }
    }
}
