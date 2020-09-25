using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using RestSharp;
using System.Net;
using Risebird.Repository.Interface;

namespace Risebird.MicroService
{
    public class StartStopVM
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        private readonly IProctorInterviewsRepository _proctorInterviewsRepository;

        public StartStopVM(IConfiguration configuration, IAzureHelper azureHelper, IProctorInterviewsRepository proctorInterviewsRepository)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;
            _proctorInterviewsRepository = proctorInterviewsRepository;

        }
        [FunctionName("StartStopVM")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "StartStopVM")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation(requestBody);

            VMActionDTO data = JsonConvert.DeserializeObject<VMActionDTO>(requestBody);
            log.LogInformation("data converted to class" + data.ActionCommand + data.VMPropertyId);

            var result = ExecuteToDoProcess(data.ResourceGroupName, data.Name,data.ActionCommand, log);
            log.LogInformation("excecuted" + result.ToString());

            string responseMessage = string.Empty;
            if(result)
            {
                //add entry in VmTriggered table
                var expectedStatus = string.Empty;
                if(data.ActionCommand.ToLower()=="start")
                {
                    expectedStatus = "running";
                }
                else
                {
                    expectedStatus = "deallocated/stopped";
                }
                await _proctorInterviewsRepository.AddVMTrigger(data.VMPropertyId, data.ActionCommand,expectedStatus);
                log.LogInformation("Added to trigger table process done");

                responseMessage = "Successful";
            }
            else
            {
                responseMessage = "Failed";
            }
            log.LogInformation("completed");

            return new OkObjectResult(responseMessage);
        }

        public bool ExecuteToDoProcess(string resourceGroupName, string resourceName, string excuteCommand, ILogger log)
        {
            var credentials = _azureHelper.GetAzureCredential();
            log.LogInformation("credentials created");
            log.LogInformation("endpoint: " +_configuration["AzureSettings:endPoint"]);

            var client =
                CreateClient(string.Format(_configuration["AzureSettings:endPoint"], credentials.SubscriptionId),
                    credentials, log);
            log.LogInformation("client created");
            log.LogInformation(resourceGroupName + resourceName + excuteCommand + _configuration["AzureSettings:ApiVersionToDoVM"]);

            var requestUrl =
                $"resourceGroups/{resourceGroupName}/providers/Microsoft.Compute/virtualMachines/{resourceName}/{excuteCommand}?api-version={_configuration["AzureSettings:ApiVersionToDoVM"]}"; // CREATE THE REQUEST URL
            log.LogInformation("request url created");

            var request = CreateRequest(requestUrl, Method.POST, log);
            log.LogInformation("request created");

            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                return true;

            }

            return false;
        }
        private RestClient CreateClient(string EndPoint, AzureCredentials credentials, ILogger log)
        {
            // Default request
            var client = new RestClient(EndPoint);
            client.AddDefaultHeader("Authorization", "Bearer " + credentials.Token);
            return client;
        }
        private RestRequest CreateRequest(string requestUrl, Method requestMethod, ILogger log,object postData = null)
        {
            var request = new RestRequest(requestUrl, requestMethod) { RequestFormat = DataFormat.Json };
            if (postData != null)
                request.AddParameter("application/json", JsonConvert.SerializeObject(postData),
                    ParameterType.RequestBody);
            return request;
        }
    }
}
