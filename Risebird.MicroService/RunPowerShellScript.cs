using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class RunPowerShellScript
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        //private readonly IProctorInterviewsRepository _proctorInterviewsRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public RunPowerShellScript(IConfiguration configuration, IAzureHelper azureHelper, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;
            //_proctorInterviewsRepository = proctorInterviewsRepository;
            _applicationErrorLogger = applicationErrorLogger;

        }
        [FunctionName("RunPowerShellScript")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "RunPowerShellScript")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("RunPowerShellScript HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<RunPowershellDTO>(requestBody);
            var result = RunScript(_azureHelper.GetAzureCredential(), data.Url, data.Script);
            return new OkObjectResult(result);
        }

        public string RunScript(AzureCredentials credentials, string url, string[] script)
        {
            var message = "success";
            var apiVersion = _configuration["AzureSettings:ApiVersionRunCommand"];
            string requestUrl = $"{url}/runCommand?api-version={apiVersion}";
            object requestBody = new RunCommandRequestObject
            {
                commandId = "RunPowerShellScript",
                script = script

            };
            var response = PostToAzure(credentials, requestUrl, Method.Post, requestBody);
            var trackUrl = response.Headers?.ToList()[3].Value; //response.Headers[3].Value;
            if ((response.StatusCode == HttpStatusCode.OK ||
                 response.StatusCode == HttpStatusCode.Accepted))
            {
                message = message + ">" + trackUrl;
                return message;
            }
            else if ((response.StatusCode != HttpStatusCode.OK ||
                      response.StatusCode == HttpStatusCode.BadRequest))
            {
                message = $"[{DateTime.Now}]: Could not complete the task.{response.Content}";
                return message;
            }

            message = $"[{DateTime.Now}]: Could not complete the task.";
            return message;

        }
        public RestResponse PostToAzure(AzureCredentials credentials, string requestUrl, Method method,
           object postData)
        {
            var tcs = new TaskCompletionSource<RestResponse>();
            var client =
                CreateClient(string.Format(_configuration["AzureSettings:endPoint"], credentials.SubscriptionId),
                    credentials);
            var request = CreateRequest(requestUrl, method, postData);
            var response = client.ExecuteAsync(request).Result;
            if (response.ErrorException != null)
                tcs.TrySetException(response.ErrorException);
            else
                tcs.TrySetResult(response);
            

            return tcs.Task.Result;
        }

        /// <summary>
        /// Create Client
        /// </summary>
        /// <param name="EndPoint"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        private RestClient CreateClient(string EndPoint, AzureCredentials credentials)
        {
            // Default request
            var client = new RestClient(EndPoint);
            client.AddDefaultHeader("Authorization", "Bearer " + credentials.Token);
            return client;
        }

        /// <summary>
        /// Generate request
        /// </summary>
        /// <param name="requestUrl"></param>
        /// <param name="requestMethod"></param>
        /// <param name="postData"></param>
        /// <returns></returns>
        private RestRequest CreateRequest(string requestUrl, Method requestMethod, object postData = null)
        {
            var request = new RestRequest(requestUrl, requestMethod) { RequestFormat = DataFormat.Json };
            if (postData != null)
                request.AddParameter("application/json", JsonConvert.SerializeObject(postData),
                    ParameterType.RequestBody);
            return request;
        }
    }
}
