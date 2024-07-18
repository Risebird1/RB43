using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Data.RbAPIDataModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class CandidateLatestStatus
    {
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public CandidateLatestStatus(IHttpRequestHelper httpRequestHelper, IConfiguration configuration, IInterviewerService interviewerService,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _httpRequestHelper = httpRequestHelper;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            //employementapikey = _configuration["EmploymentVerfication:apikey"];
        }

        [FunctionName("CandidateLatestStatus")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function invoked at {DateTime.Now}");
                var tenantId = req.Headers["tenant-id"].ToString();
                //check if the tenant Id belongs to the client
                bool isAuthorizedTenant = false;
                if (isAuthorizedTenant)
                {
                    //string requestBody = File.ReadAllText(@"c:\prem\meeting.txt", Encoding.UTF8);
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    log.LogInformation(requestBody);
                    CandidateLatestStatusRequest reqData = JsonConvert.DeserializeObject<CandidateLatestStatusRequest>(requestBody);
                    log.LogInformation("C# HTTP trigger function processed a request.");

                    //var httpClient = await _authenticationService.CreateAuthorizedHttpClient();
                    //var requestUrl = $"https://graph.microsoft.com/v1.0/users/{accountEmail}/calendar/events";
                    //var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                    //var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    //httpRequest.Content = content;
                    //var response = await httpClient.SendAsync(httpRequest);
                    //var result = await response.Content.ReadAsStringAsync();
                    //log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");

                    //return new OkObjectResult(result);
                    return null;
                }

                log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function executed at {DateTime.Now}");
                return new UnauthorizedResult();
                //return new BadRequestResult();
            }
            catch (Exception ex)
            {

                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                log.LogInformation($"CandidateLatestStatus : C# HTTP trigger function threw exception with message {ex.Message} at {DateTime.Now}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

        }
    }
}
