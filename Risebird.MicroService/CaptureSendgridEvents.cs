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
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Repository.Interface;
using Risebird.Entities.Enum;

namespace Risebird.MicroService
{
    public class CaptureSendgridEvents
    {
        private readonly IConfiguration _configuration;

        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ISendGridRepository _sendGridRepository;
        public CaptureSendgridEvents(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, ISendGridRepository sendGridRepository)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _sendGridRepository = sendGridRepository;

        }

        [FunctionName("CaptureSendgridEvents")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"CaptureSendgridEvents : C# HTTP trigger function invoked at {DateTime.Now}");

                string name = req.Query["name"];

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation($"CaptureSendgridEvents : Event data recieved with request body :  {requestBody}");

                SendgridEventData data = JsonConvert.DeserializeObject<SendgridEventData>(requestBody);

                //name = name ?? data?.name;
                var email = data?.email;
                var timestamp = data?.timestamp;
                var @event = data?.@event;

                var result = await _sendGridRepository.SaveSendGridEventDetails(data);
                //string responseMessage = string.IsNullOrEmpty(name)
                //    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                //    : $"Hello, {name}. This HTTP triggered function executed successfully.";

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
