using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.Entities.Data;
using System.Collections.Generic;
using Risebird.CommonHelper.Interface;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Risebird.Entities.ViewModel;
using Newtonsoft.Json.Linq;

namespace Risebird.MicroService
{
    public class SignzyEmploymentVerification
    {
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly IConfiguration _configuration;
        private readonly string employementapikey;
        private readonly string employementendpoint;
        public SignzyEmploymentVerification(IHttpRequestHelper httpRequestHelper, IConfiguration configuration)
        {
            _httpRequestHelper = httpRequestHelper;
            _configuration = configuration;
            employementapikey = _configuration["EmploymentVerfication:apikey"];
            employementendpoint = _configuration["EmploymentVerfication:endPoint"];
        }

        [FunctionName("SignzyEmploymentVerification")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string name = req.Query["name"];
            //Get request body data
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"SignzyEmploymentVerification : Event data recieved with request body :  {requestBody}");
            var data = JsonConvert.DeserializeObject<SignzyEmployeeRequest>(requestBody);
            Dictionary<string, string> headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Authorization", employementapikey }
                };
            var response = await _httpRequestHelper.MakeHttpRequest(employementendpoint, Method.Post, data, headers);
            var convertedResponse = JsonConvert.DeserializeObject<SignzyEmployeeResponse>(response.Content);
            dynamic datavalue = JObject.Parse(response.Content);
            log.LogInformation($"SignzyEmploymentVerification : Event data recieved with response :  {response}");
            log.LogInformation($"SignzyEmploymentVerification : Event data recieved with response datavalue :  {datavalue}");
            log.LogInformation($"SignzyEmploymentVerification : Event data recieved with response convertedResponse :  {convertedResponse}");
            //Save response data
            return new OkObjectResult(true);
        }
    }
}
