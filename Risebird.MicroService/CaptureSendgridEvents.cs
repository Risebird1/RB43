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
using System.Collections.Generic;
using System.Net;
using System.Web.Http;
using RestSharp;
using Risebird.Repository;

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
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "CaptureSendgridEvents")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"CaptureSendgridEvents : C# HTTP trigger function invoked at {DateTime.Now}");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation($"CaptureSendgridEvents : Event data recieved with request body :  {requestBody}");

                var dataList = JsonConvert.DeserializeObject<IEnumerable<SendgridEventData>>(requestBody);

                if (dataList != null)
                {
                    //foreach (var data in dataList)
                    //{

                    //    await _sendGridRepository.SaveSendGridEventDetails(data);
                    //}
                    Parallel.ForEach(dataList, data =>
                    {
                        try
                        {
                            _ = _sendGridRepository.SaveSendGridEventDetails(data).Result;
                        }
                        catch (Exception ex)
                        {
                            _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                            log.LogError($"CaptureSendgridEvents : Error at: {DateTime.Now}, Message : {ex.Message}");
                            return;
                        }
                    });
                }

                return new OkObjectResult(true);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
