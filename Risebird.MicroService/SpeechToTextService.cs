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

namespace Risebird.MicroService
{
    public class SpeechToTextService
    {

        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly string connectionString;

        public SpeechToTextService(IAzureHelper azureHelper, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];

        }
        [FunctionName("SpeechToTextService")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "SpeechToTextService")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"SpeechToTextService HTTP trigger function executed at: {DateTime.Now}");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            log.LogInformation($"SpeechToTextService HTTP trigger function completed at: {DateTime.Now}");

            return new OkObjectResult(responseMessage);
        }
    }
}
