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
using Risebird.Repository.Interface;
using System.IO;
using System.Net;
using System.Threading.Tasks;

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
            VMActionDTO data = JsonConvert.DeserializeObject<VMActionDTO>(requestBody);
            var result = ExecuteToDoProcess(data.ResourceGroupName, data.Name, data.ActionCommand);

            string responseMessage = string.Empty;
            if (result)
            {
                //add entry in VmTriggered table
                var expectedStatus = string.Empty;
                if (data.ActionCommand.ToLower() == "start")
                {
                    expectedStatus = "running";
                }
                else
                {
                    expectedStatus = "deallocated/stopped";
                }
                await _proctorInterviewsRepository.AddVMTrigger(data.VMPropertyId, data.ActionCommand, expectedStatus);
                responseMessage = "Successful";
            }
            else
            {
                responseMessage = "Failed";
            }

            return new OkObjectResult(responseMessage);
        }

        public bool ExecuteToDoProcess(string resourceGroupName, string resourceName, string excuteCommand)
        {
            var credentials = _azureHelper.GetAzureCredential();
            var client =
                CreateClient(string.Format(_configuration["AzureSettings:endPoint"], credentials.SubscriptionId),
                    credentials);
            var requestUrl =
                $"resourceGroups/{resourceGroupName}/providers/Microsoft.Compute/virtualMachines/{resourceName}/{excuteCommand}?api-version={_configuration["AzureSettings:ApiVersionToDoVM"]}"; // CREATE THE REQUEST URL
            var request = CreateRequest(requestUrl, Method.Post);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                return true;

            }

            return false;
        }
        private RestClient CreateClient(string EndPoint, AzureCredentials credentials)
        {
            // Default request
            var client = new RestClient(EndPoint);
            client.AddDefaultHeader("Authorization", "Bearer " + credentials.Token);
            return client;
        }
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
