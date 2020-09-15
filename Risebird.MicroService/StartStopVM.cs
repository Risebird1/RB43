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

namespace Risebird.MicroService
{
    public class StartStopVM
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;

        public StartStopVM(IConfiguration configuration, IAzureHelper azureHelper)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;

        }
        [FunctionName("StartStopVM")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "StartStopVM")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            VMActionDTO data = JsonConvert.DeserializeObject<VMActionDTO>(requestBody);

            //dynamic data = JsonConvert.DeserializeObject(requestBody);

            string responseMessage = string.Empty;
            //string responseMessage = string.IsNullOrEmpty(name)
            //    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //    : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
