using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using System.Net;

namespace Risebird.MicroService
{
    public class GetVMList
    {

        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        //private readonly IProctorInterviewsRepository _proctorInterviewsRepository;

        public GetVMList(IConfiguration configuration, IAzureHelper azureHelper)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;

        }
        [FunctionName("GetVMList")]
        public string Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetVMList")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var response = GetVmList();

            return response;
        }

        public string GetVmList()
        {

            string requestUrl = $"providers/Microsoft.Compute/virtualMachines?api-version={_configuration["AzureSettings:ApiVersionVMList"]}";
            var response = GetFromAzureRM(_azureHelper.GetAzureCredential(), requestUrl);
            return response;
        }

        private string GetFromAzureRM(AzureCredentials credentials, string requestUrl)
        {
            string endpoint = _configuration["AzureSettings:endPoint"];
            if (credentials != null && !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(credentials.SubscriptionId))
            {
                var client = CreateClient(string.Format(endpoint, credentials.SubscriptionId), credentials);


                var createRequest = CreateRequest(requestUrl, Method.Get);
                var response = client.Execute(createRequest);
                if (response == null || (response.StatusCode != HttpStatusCode.OK))
                {
                    return null;
                }

                return response.Content;
            }
            else
            {
                return null;
            }
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
